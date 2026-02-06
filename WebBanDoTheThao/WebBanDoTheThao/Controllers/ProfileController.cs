using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using WebBanDoTheThao.Models; // Sử dụng namespace Models của bạn

namespace WebBanDoTheThao.Controllers
{
    public class ProfileController : Controller
    {
        private QLQAEntities db = new QLQAEntities();

        // GET: /Profile
        [HttpGet]
        public ActionResult Profile()
        {
            // 1. Kiểm tra đăng nhập
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Login");
            }

            string userEmail = Session["UserEmail"].ToString();

            // 2. Lấy thông tin Khách hàng (Model)
            var nguoiDung = db.NGUOIDUNGs.FirstOrDefault(n => n.EMAIL == userEmail);
            if (nguoiDung == null)
            {
                Session.Clear();
                return RedirectToAction("Login", "Login");
            }

            // Giả định: NGUOIDUNG.ID (int) liên kết với KHACHHANG.MAND (int?)
            var khachHang = db.KHACHHANGs.FirstOrDefault(kh => kh.MAND == nguoiDung.ID);

            if (khachHang == null)
            {
                ViewBag.ErrorMessage = "Hồ sơ khách hàng của bạn chưa được thiết lập.";
                return View(new KHACHHANG());
            }

            // 3. Lấy và phân loại Đơn hàng
            var allDonHangs = db.DONHANGs
                               .Where(dh => dh.MAKH == khachHang.ID) // Giả định: KHACHHANG.ID (Primary Key)
                               .OrderByDescending(dh => dh.NGAYLAP)
                               .ToList();

            // 3A. Đơn hàng Đã giao (Lịch sử Hoàn tất)
            ViewBag.LichSuDonHangHoanTat = allDonHangs
                                           .Where(dh => dh.TRANGTHAI == "Đã giao")
                                           .ToList();

            // 3B. Đơn hàng Đang chờ/Đang xử lý (Trạng thái đơn hàng)
            ViewBag.TrangThaiDonHang = allDonHangs
                                       .Where(dh => dh.TRANGTHAI != "Đã giao")
                                       .ToList();

            // 4. Lấy Sản phẩm Đề xuất ngẫu nhiên
            ViewBag.DeXuatSanPham = db.SANPHAMs
                                    .Where(sp => sp.TRANGTHAI == "Hoạt động")
                                    .OrderBy(r => Guid.NewGuid())
                                    .Take(8)
                                    .ToList();

            // 5. Truyền Model Khách hàng
            return View(khachHang);
        }

        public ActionResult ChiTietTrangThai(int id)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Login");
            }

            // 2. Lấy đơn hàng và kiểm tra quyền sở hữu
            var donHang = db.DONHANGs.Find(id);
            if (donHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng này.";
                return RedirectToAction("Profile");
            }

            string userEmail = Session["UserEmail"].ToString();
            var nguoiDung = db.NGUOIDUNGs.FirstOrDefault(n => n.EMAIL == userEmail);

            // Kiểm tra nguoiDung có null không trước khi truy cập .ID
            if (nguoiDung == null)
            {
                Session.Clear();
                return RedirectToAction("Login", "Login");
            }

            var khachHang = db.KHACHHANGs.FirstOrDefault(kh => kh.MAND == nguoiDung.ID);

            // Kiểm tra quyền sở hữu đơn hàng
            if (khachHang == null || donHang.MAKH != khachHang.ID)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập đơn hàng này.";
                return RedirectToAction("Profile");
            }

            // 3. Lấy lịch sử trạng thái (từ bảng LICHSUDONHANG)
            ViewBag.LichSuTrangThai = db.LICHSUDONHANGs // Giả định Model là LICHSUDONHANG
                                        .Where(ls => ls.MADH == id)
                                        .OrderBy(ls => ls.NGAYTHAYDOI)
                                        .ToList();

            // Lấy chi tiết sản phẩm của đơn hàng (ĐÃ SỬA LỖI: Sử dụng Eager Loading cho SANPHAM)
            ViewBag.ChiTietSanPham = db.CHITIETDONHANGs
                                       .Include(ct => ct.BIENTHESP.SANPHAM)
                                       .Where(ct => ct.MADH == id)
                                       .ToList();

            return View(donHang);
        }

        //
        // POST: /Profile/CapNhatThongTin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatThongTin(KHACHHANG model)
        {
            if (ModelState.IsValid)
            {
                // 1. KIỂM TRA NGÀY SINH
                if (model.NGAYSINH.HasValue && model.NGAYSINH.Value > DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Ngày sinh không được lớn hơn ngày hiện tại.";
                    return RedirectToAction("Profile");
                }

                // 2. KIỂM TRA ĐỊNH DẠNG SỐ ĐIỆN THOẠI (Regex)
                // Giải thích: ^0 bắt đầu bằng 0, \d{9,10} là 9 đến 10 số tiếp theo (tổng 10-11 số)
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.SDT, @"^0\d{9,10}$"))
                {
                    TempData["ErrorMessage"] = "Số điện thoại không hợp lệ (Phải bắt đầu bằng số 0 và dài 10-11 số).";
                    return RedirectToAction("Profile");
                }

                // 3. KIỂM TRA SỐ ĐIỆN THOẠI ĐÃ TỒN TẠI (TRÙNG LẶP)
                // Logic: Tìm xem có ai KHÁC (khác MAKH hiện tại) mà có cùng SDT không
                bool isDuplicatePhone = db.KHACHHANGs.Any(kh => kh.SDT == model.SDT && kh.MAKH != model.MAKH);
                if (isDuplicatePhone)
                {
                    TempData["ErrorMessage"] = "Số điện thoại này đã được đăng ký bởi tài khoản khác.";
                    return RedirectToAction("Profile");
                }

                var khachHangHienTai = db.KHACHHANGs.FirstOrDefault(kh => kh.MAKH == model.MAKH);

                if (khachHangHienTai != null)
                {
                    khachHangHienTai.HOTEN = model.HOTEN;
                    khachHangHienTai.SDT = model.SDT;
                    khachHangHienTai.NGAYSINH = model.NGAYSINH;
                    khachHangHienTai.GIOITINH = model.GIOITINH;
                    khachHangHienTai.DIACHI = model.DIACHI;

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }
            }
            TempData["ErrorMessage"] = "Cập nhật thất bại. Vui lòng kiểm tra lại thông tin.";
            return RedirectToAction("Profile");
        }

        //
        // POST: /Profile/DoiMatKhau
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiMatKhau(string MatKhauCu, string MatKhauMoi, string XacNhanMatKhau)
        {
            string userEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Login");
            }

            if (MatKhauMoi != XacNhanMatKhau)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
                return RedirectToAction("Profile");
            }

            var nguoiDung = db.NGUOIDUNGs.FirstOrDefault(n => n.EMAIL == userEmail);

            // CẢNH BÁO: PHẢI SỬ DỤNG HASHING TRONG THỰC TẾ
            if (nguoiDung != null && nguoiDung.MATKHAU == MatKhauCu)
            {
                nguoiDung.MATKHAU = MatKhauMoi;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Mật khẩu cũ không chính xác.";
            }

            return RedirectToAction("Profile");
        }

        //
        // POST: /Profile/DoiEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiEmail(string EmailMoi, string MatKhauXacNhan)
        {
            string userEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Login");
            }

            var nguoiDung = db.NGUOIDUNGs.FirstOrDefault(n => n.EMAIL == userEmail);

            if (nguoiDung != null)
            {
                // CẢNH BÁO: PHẢI SỬ DỤNG HASHING TRONG THỰC TẾ
                if (nguoiDung.MATKHAU != MatKhauXacNhan)
                {
                    TempData["ErrorMessage"] = "Mật khẩu xác nhận không chính xác.";
                    return RedirectToAction("Profile");
                }

                if (db.NGUOIDUNGs.Any(n => n.EMAIL == EmailMoi))
                {
                    TempData["ErrorMessage"] = "Email này đã được sử dụng bởi tài khoản khác.";
                    return RedirectToAction("Profile");
                }

                nguoiDung.EMAIL = EmailMoi;
                db.SaveChanges();

                Session["UserEmail"] = EmailMoi;
                TempData["SuccessMessage"] = "Đổi Email thành công! Bạn có thể cần đăng nhập lại.";

            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LuuDanhGia(int ProductID, int OrderID, int Rating, string Comment)
        {
            // 1. Kiểm tra đăng nhập
            string userEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Login");
            }

            // 2. Lấy thông tin khách hàng
            var nguoiDung = db.NGUOIDUNGs.FirstOrDefault(n => n.EMAIL == userEmail);
            var khachHang = db.KHACHHANGs.FirstOrDefault(kh => kh.MAND == nguoiDung.ID);

            if (khachHang == null)
            {
                TempData["ErrorMessage"] = "Lỗi xác thực người dùng.";
                return RedirectToAction("ChiTietTrangThai", new { id = OrderID });
            }

            try
            {
                // 3. Kiểm tra xem Khách hàng đã đánh giá sản phẩm này trong đơn hàng này chưa (Tránh spam)
                // Logic này tùy chọn: Cho phép 1 đơn hàng đánh giá 1 lần cho 1 sản phẩm
                var daDanhGia = db.DANHGIAs.Any(dg => dg.MAKH == khachHang.ID && dg.MASP == ProductID && dg.BINHLUAN.Contains("Đơn hàng #" + OrderID));

                if (daDanhGia)
                {
                    TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm này rồi.";
                    return RedirectToAction("ChiTietTrangThai", new { id = OrderID });
                }

                // 4. Tạo đối tượng đánh giá mới
                DANHGIA danhGiaMoi = new DANHGIA();
                danhGiaMoi.MAKH = khachHang.ID;
                danhGiaMoi.MASP = ProductID;
                danhGiaMoi.SOSAO = Rating;
                // Lưu kèm mã đơn hàng vào comment để dễ quản lý (hoặc tạo cột MADH trong bảng DANHGIA nếu có)
                danhGiaMoi.BINHLUAN = Comment;
                danhGiaMoi.NGAYDG = DateTime.Now;

                db.DANHGIAs.Add(danhGiaMoi);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu đánh giá: " + ex.Message;
            }

            return RedirectToAction("ChiTietTrangThai", new { id = OrderID });
        }
    }
}