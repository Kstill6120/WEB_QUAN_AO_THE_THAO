using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDoTheThao.Models;

namespace WebBanDoTheThao.Controllers
{
    public class MuaSPController : Controller
    {
        // GET: MuaSP
        QLQAEntities db = new QLQAEntities();
        DTGioHang dt = new DTGioHang();
        private int? GetCurrentUserID()
        {
            return Session["UserID"] as int?;
        }
        [HttpPost]
        public ActionResult ThemGH(int masp,string size,int sl)
        {
            
            int? user = GetCurrentUserID();
            KHACHHANG kh = db.KHACHHANGs.FirstOrDefault(t => t.MAND == user);
            if (kh == null)
            {
                return Json(new { success = false, code = "LOGIN_REQUIRED", url = Url.Action("Login", "Login") });
            }


            GIOHANG gh = db.GIOHANGs.FirstOrDefault(t => t.MAKH == kh.ID);
            if (gh == null)
            {
                gh = new GIOHANG();
                gh.MAKH = kh.ID;
                gh.NGAYTAO = DateTime.Now;
                db.GIOHANGs.Add(gh);
                db.SaveChanges();
                dt.ThemCTGH(masp, size, sl, kh.ID);
                db.SaveChanges();

            }
            else
            {
                SIZE a = db.SIZEs.FirstOrDefault(t => t.TENSIZE == size);
                BIENTHESP btsp = db.BIENTHESPs.FirstOrDefault(t => t.MASP == masp && t.ID_SIZE == a.ID); 
                if(btsp == null)
                {
                    return Json(new { success = false, message = "Sản phẩm này hiện không có size " + size });
                }
                CHITIETGIOHANG gia = db.CHITIETGIOHANGs.FirstOrDefault(t => t.ID_BIENTHE == btsp.ID && t.MAGH == gh.ID);

                if (gia != null)
                {
                    gia.SOLUONG += sl;
                    gia.THANHTIEN = gia.DONGIA * gia.SOLUONG;
                    db.SaveChanges();

                    return Json(new { success = true, message = "Cập nhật số lượng thành công" });
                }
                dt.ThemCTGH(masp, size, sl,kh.ID);
                db.SaveChanges();
            }

            return Json(new { success = true, message = "Thêm thành công" });
        }
        public ActionResult SLGH()
        {
            int? userID = GetCurrentUserID();
            int sl = 0; // Khởi tạo sl = 0

            if (userID.HasValue)
            {
                KHACHHANG kh = db.KHACHHANGs.FirstOrDefault(t => t.MAND == userID);
                if (kh != null)
                {
                    GIOHANG gh = db.GIOHANGs.FirstOrDefault(t => t.MAKH == kh.ID);
                    if (gh != null)
                    {
                        sl = dt.DemCTGH(kh.ID);
                    }
                }
            }

            // Cập nhật Session mỗi khi AJAX gọi
            Session["CartCount"] = sl;
            // Trả về con số (dạng text) cho JavaScript
            return Content(sl.ToString());
        }

        public ActionResult XemGH()
        {
            int? userID = GetCurrentUserID();
            if (userID == null) 
                return RedirectToAction("Login", "Login");

            KHACHHANG kh = db.KHACHHANGs.FirstOrDefault(t => t.MAND == userID);
            if (kh == null)
            {
                return RedirectToAction("Index", "Home");
            }
            GIOHANG gh = db.GIOHANGs.FirstOrDefault(t => t.MAKH == kh.ID);
            if (gh == null)
            {
                return RedirectToAction("Index", "Home");
            }
            List<CHITIETGIOHANG> listCTGH = db.CHITIETGIOHANGs.Where(t => t.MAGH == gh.ID).ToList();
            ViewBag.TongTien = listCTGH.Sum(t => t.THANHTIEN);
            return View(listCTGH);
        }
        [HttpPost]
        public ActionResult Xoa1GH(int idBienThe)
        {
            int? userID = GetCurrentUserID();
            if (userID == null)
                return RedirectToAction("Login", "Login");
            KHACHHANG kh = db.KHACHHANGs.FirstOrDefault(t => t.MAND == userID);
            GIOHANG gh = db.GIOHANGs.FirstOrDefault(t => t.MAKH == kh.ID);
            List<CHITIETGIOHANG> listCTGH = db.CHITIETGIOHANGs.Where(t => t.MAGH == gh.ID).ToList();
            
            dt.XoaCTGH(idBienThe, kh.ID);
            return RedirectToAction("XemGH", "MuaSP");
        }
        [HttpPost]
        public ActionResult XoaHet()
        {
            int? userID = GetCurrentUserID();
            if (userID == null)
                return RedirectToAction("Login", "Login");
            KHACHHANG kh = db.KHACHHANGs.FirstOrDefault(t => t.MAND == userID);
            GIOHANG gh = db.GIOHANGs.FirstOrDefault(t => t.MAKH == kh.ID);
            List<CHITIETGIOHANG> listCTGH = db.CHITIETGIOHANGs.Where(t => t.MAGH == gh.ID).ToList();
            db.CHITIETGIOHANGs.RemoveRange(listCTGH);
            db.GIOHANGs.Remove(gh);
            db.SaveChanges();
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public ActionResult NhapTTTT()
        {
            int? userID = GetCurrentUserID();
            if (userID == null)
                return RedirectToAction("Login", "Login");
            KHACHHANG kh = db.KHACHHANGs.FirstOrDefault(t => t.MAND == userID);
            var giohang = db.GIOHANGs.FirstOrDefault(g => g.MAKH == kh.ID);
            if (giohang == null)
                return RedirectToAction("XemGH");

            int magh = giohang.ID;
            var listCTGH = db.CHITIETGIOHANGs.Where(c => c.MAGH == magh).ToList();
            if (!listCTGH.Any())
                return RedirectToAction("XemGH");

            ViewBag.TongT = listCTGH.Sum(c => c.THANHTIEN);

            if (kh != null)
            {
                ViewBag.HoTen = kh.HOTEN;
                ViewBag.SDT = kh.SDT;
                ViewBag.DiaChi = kh.DIACHI;
            }

            return View(listCTGH);
        }

        [HttpPost]
        public ActionResult XacNhanThanhToan(string HoTen, string SDT, string DiaChi, string GhiChu, string PhuongThuc)
        {
            int? userID = GetCurrentUserID();
            if (userID == null)
                return RedirectToAction("Login", "Login");
            KHACHHANG kh = db.KHACHHANGs.FirstOrDefault(t => t.MAND == userID);
            var giohang = db.GIOHANGs.FirstOrDefault(g => g.MAKH == kh.ID);
            if (giohang == null)
                return RedirectToAction("XemGH");

            int magh = giohang.ID;
            var listCTGH = db.CHITIETGIOHANGs.Where(c => c.MAGH == magh).ToList();
            if (!listCTGH.Any())
                return RedirectToAction("XemGH");

            // 1️⃣ Tạo đơn hàng
            DONHANG dh = new DONHANG
            {
                MAKH = kh.ID,
                NGAYLAP = DateTime.Now,
                DIACHI = DiaChi,
                TRANGTHAI = "Chờ xử lý",
                PHUONGTHUC = PhuongThuc,
                TONGTIEN = listCTGH.Sum(c => c.THANHTIEN).GetValueOrDefault()

            };
            db.DONHANGs.Add(dh);
            db.SaveChanges(); // Lưu để có dh.ID

            int madh = dh.ID; // Lấy ID mới tạo

            // 2️⃣ Chuyển chi tiết giỏ hàng sang chi tiết đơn hàng
            foreach (var item in listCTGH)
            {
                CHITIETDONHANG ctdh = new CHITIETDONHANG
                {
                    MADH = madh,
                    ID_BIENTHE = item.ID_BIENTHE,
                    SOLUONG = item.SOLUONG,
                    DONGIA = item.DONGIA,
                    THANHTIEN = item.THANHTIEN
                };
                db.CHITIETDONHANGs.Add(ctdh);
            }

            // 3️⃣ Lưu lịch sử đơn hàng
            LICHSUDONHANG lsdh = new LICHSUDONHANG
            {
                MADH = madh,
                NGAYTHAYDOI = DateTime.Now,
                GHICHU = GhiChu ?? ""
            };
            db.LICHSUDONHANGs.Add(lsdh);

            // 4️⃣ Lưu lịch sử thanh toán
            if(PhuongThuc == "Tiền mặt")
            {
                LICHSUTHANHTOAN lstt = new LICHSUTHANHTOAN
                {
                    MADH = madh,
                    NGAYTT = DateTime.Now,
                    PHUONGTHUC = PhuongThuc,
                    SOTIEN = dh.TONGTIEN,
                    TRANGTHAI = "Đã thanh toán"
                };
                db.LICHSUTHANHTOANs.Add(lstt);
            }
            else
            {
                LICHSUTHANHTOAN lstt = new LICHSUTHANHTOAN
                {
                    MADH = madh,
                    NGAYTT = DateTime.Now,
                    PHUONGTHUC = PhuongThuc,
                    SOTIEN = dh.TONGTIEN,
                    TRANGTHAI = "Chưa thanh toán"
                };
                db.LICHSUTHANHTOANs.Add(lstt);
            }

           
            

            // 5️⃣ Xóa giỏ hàng
            db.CHITIETGIOHANGs.RemoveRange(listCTGH);
            db.GIOHANGs.Remove(giohang);

            db.SaveChanges();

            // 6️⃣ Lưu thông tin tạm thời để hiển thị kết quả
            TempData["MADH"] = madh;
            TempData["MAKH"] = kh.ID;
            TempData["PhuongThuc"] = PhuongThuc;
            TempData["TongTien"] = dh.TONGTIEN;

            return RedirectToAction("KQ");
        }


        public ActionResult KQ()
        {
            ViewBag.MADH = TempData["MADH"];
            ViewBag.MAKH = TempData["MAKH"];
            ViewBag.PhuongThuc = TempData["PhuongThuc"];
            ViewBag.TongTien = TempData["TongTien"];
            return View();
        }

        [ChildActionOnly] // Chỉ cho phép action này được gọi từ bên trong một View
        public ActionResult GetCartCount()
        {
            int? userID = GetCurrentUserID();
            int sl = 0;
            if (userID.HasValue)
            {
                KHACHHANG kh = db.KHACHHANGs.FirstOrDefault(t => t.MAND == userID);
                if (kh != null)
                {
                    GIOHANG gh = db.GIOHANGs.FirstOrDefault(t => t.MAKH == kh.ID);
                    if (gh != null)
                    {
                        sl = dt.DemCTGH(kh.ID);
                    }
                }
            }
            // Cập nhật Session mỗi khi layout được tải
            Session["CartCount"] = sl;
            // Trả về PartialView chỉ chứa con số
            return PartialView("_CartCount", sl);
        }

    }
}