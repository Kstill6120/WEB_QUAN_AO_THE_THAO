using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Data.Entity.Validation;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Tokenizer.Symbols;
using System.Web.Services.Description;
using System.Web.UI;
using WebBanDoTheThao.Models;

namespace WebBanDoTheThao.Controllers
{
    public class AdminController : Controller
    {
        private QLQAEntities db = new QLQAEntities();

        #region ========== TRANG CHỦ ==========
        public ActionResult TrangChu()
        {
            var allSP = db.SANPHAMs.ToList();
            var allTH = db.THUONGHIEUx.ToList();
            var allUsers = db.NGUOIDUNGs.ToList();

            ViewBag.TongSP = allSP.Count;
            ViewBag.TongUsers = allUsers.Count(u => u.VAITRO != "ADMIN");

            var brandStats = allSP
                .GroupBy(sp => sp.MATH)
                .Select(g => new
                {
                    BrandId = g.Key,
                    Count = g.Count()
                })
                .Join(allTH,
                      stat => stat.BrandId,
                      th => th.ID,
                      (stat, th) => new
                      {
                          BrandName = th.TENTH,
                          Count = stat.Count
                      })
                .ToList();
            var avgPriceStats = allSP
            .GroupBy(sp => sp.MATH)
            .Select(g => new
            {
                BrandId = g.Key,
                AvgPrice = g.Average(sp => sp.GIA)
            })
            .Join(allTH,
                  stat => stat.BrandId,
                  th => th.ID,
                  (stat, th) => new
                  {
                      BrandName = th.TENTH,
                      AvgPrice = stat.AvgPrice
                  })
            .ToList();
            ViewBag.BrandLabels = JsonConvert.SerializeObject(brandStats.Select(s => s.BrandName));
            ViewBag.BrandData = JsonConvert.SerializeObject(brandStats.Select(s => s.Count));
            ViewBag.Revenue = db.DONHANGs.Where(s => s.TRANGTHAI == "Đã giao").Sum(d => d.TONGTIEN);
            var today = DateTime.Now.Date;

            ViewBag.CountSPonSell = (from dh in db.DONHANGs
                                     join ct in db.CHITIETDONHANGs on dh.ID equals ct.MADH
                                     where dh.NGAYLAP == today && dh.TRANGTHAI != "Đã hủy"
                                     select (int?)ct.SOLUONG).Sum() ?? 0;
            ViewBag.PriceLabels = JsonConvert.SerializeObject(avgPriceStats.Select(s => s.BrandName));
            ViewBag.PriceData = JsonConvert.SerializeObject(avgPriceStats.Select(s => Math.Round(s.AvgPrice, 2)));
            ViewBag.TongSP = db.SANPHAMs.Count();
            ViewBag.SoLuongND = db.NGUOIDUNGs.Count(nd => nd.VAITRO == "User");
            ViewBag.ThuongHieu = allTH;
            return View();
        }
        #endregion

        #region ========== DANH SÁCH SẢN PHẨM ==========
        public ActionResult SanPham(string keyword, string thuongHieu, string tenMon, int page = 1, bool showAll = false)
        {
            var products = db.SANPHAMs.Include("THUONGHIEU").Include("MONTT").AsQueryable();

            // --- LOGIC LỌC MỚI ---
            // Nếu showAll = false (Gạt trái) -> Chỉ lấy sản phẩm KHÁC "Ngừng bán"
            // Nếu showAll = true (Gạt phải) -> Lấy tất cả
            if (!showAll)
            {
                products = products.Where(s => s.TRANGTHAI != "Ngừng bán");
            }
            // ---------------------

            if (!string.IsNullOrEmpty(keyword))
                products = products.Where(s => s.TENSP.Contains(keyword));

            if (!string.IsNullOrEmpty(thuongHieu))
                products = products.Where(s => s.THUONGHIEU.TENTH == thuongHieu);

            if (!string.IsNullOrEmpty(tenMon))
                products = products.Where(s => s.MONTT.TENMON == tenMon);

            int pageSize = 10;
            int totalPages = (int)Math.Ceiling((double)products.Count() / pageSize);
            var pagedProducts = products
                .OrderByDescending(s => s.ID) // Nên sắp xếp ID giảm dần để thấy cái mới thêm
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.ListThuongHieu = db.THUONGHIEUx.Select(s => s.TENTH).Distinct().ToList();
            ViewBag.ListMaMon = db.MONTTs.Select(s => s.TENMON).ToList();

            // Truyền lại giá trị filter cho View
            ViewBag.SelectedBrand = thuongHieu;
            ViewBag.SelectedMon = tenMon;
            ViewBag.Keyword = keyword;
            ViewBag.ShowAll = showAll; // Truyền biến này ra View để set trạng thái nút gạt

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedProducts);
        }
        #endregion

        #region ========== NGỪNG BÁN SẢN PHẨM ==========
        [HttpPost]
        public ActionResult NgungBan(int id)
        {
            try
            {
                var sp = db.SANPHAMs.FirstOrDefault(x => x.ID == id);
                if (sp == null) return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                var tongTonKho = db.BIENTHESPs.Where(b => b.MASP == id).Sum(b => (int?)b.SOLUONG_TON) ?? 0;

                if (tongTonKho > 0)
                {
                    return Json(new { success = false, message = $"Không thể ngừng bán! Sản phẩm còn tồn kho {tongTonKho} cái trong các biến thể." });
                }

                // Cập nhật trạng thái
                sp.TRANGTHAI = "Ngừng bán";
                db.SaveChanges();

                return Json(new { success = true, message = "Đã chuyển trạng thái sang Ngừng bán." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        #endregion

        #region ========== CHI TIẾT SẢN PHẨM ==========
        [HttpGet]
        public ActionResult SanPhamChiTiet(string MaSP)
        {
            var sp = db.SANPHAMs.FirstOrDefault(s => s.MASP == MaSP);
            if (sp == null)
                return HttpNotFound();
            ViewBag.TenTH = db.THUONGHIEUx.FirstOrDefault(th => th.ID == sp.MATH).TENTH;
            ViewBag.TenMon = db.MONTTs.FirstOrDefault(m => m.ID == sp.MAMON).TENMON;
            ViewBag.TenLoai = db.LOAIs.FirstOrDefault(l => l.ID == sp.MALOAI).TENLOAI;
            return PartialView("SanPhamChiTiet", sp);
        }
        #endregion

        #region ============ HIỂN THỊ THÔNG TIN CHO THÊM, SỬA SẢN PHẨM =============
        private void PopulateModalData()
        {
            ViewBag.ThuongHieuList = new SelectList(db.THUONGHIEUx, "Id", "TENTH");
            ViewBag.LoaiList = new SelectList(db.LOAIs, "Id", "TENLOAI");
            ViewBag.MonList = new SelectList(db.MONTTs, "Id", "TENMON");
        }
        #endregion

        #region ========== THÊM SẢN PHẨM ==========
        public ActionResult ThemSanPham()
        {
            PopulateModalData();
            return View();
        }
        [HttpPost]
        public ActionResult ThemSanPham(SANPHAM sp, HttpPostedFileBase HinhAnh)
        {
            PopulateModalData();

            if (ModelState.IsValid)
            {
                var exists = db.SANPHAMs.FirstOrDefault(x => x.TENSP == sp.TENSP);
                if (exists != null)
                {
                    ViewBag.Kq = "Tên sản phẩm đã tồn tại.";
                    return View(sp);
                }

                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(HinhAnh.FileName);
                    string path = Path.Combine(Server.MapPath("~/Assets/Clothes_Images"), fileName);
                    HinhAnh.SaveAs(path);
                    sp.AVATAR = fileName;
                }
                else
                {
                    sp.AVATAR = "default.png";
                }

                db.SANPHAMs.Add(sp);
                db.SaveChanges();
                TempData["Kq"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("SanPham");
            }

            ViewBag.Kq = "Dữ liệu không hợp lệ.";
            return View(sp);
        }
        #endregion

        #region ========== SỬA SẢN PHẨM ==========
        public ActionResult SuaSanPham(string MaSP)
        {
            var sp = db.SANPHAMs.FirstOrDefault(x => x.MASP == MaSP);
            if (sp == null)
                return HttpNotFound();

            PopulateModalData();
            return View(sp);
        }

        [HttpPost]
        public ActionResult SuaSanPham(SANPHAM sp, HttpPostedFileBase HinhAnh)
        {
            PopulateModalData();

            if (ModelState.IsValid)
            {
                var existing = db.SANPHAMs.FirstOrDefault(x => x.MASP == sp.MASP);
                if (existing == null)
                    return HttpNotFound();

                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(HinhAnh.FileName);
                    string path = Path.Combine(Server.MapPath("~/Assets/Clothes_Images"), fileName);
                    HinhAnh.SaveAs(path);
                    existing.AVATAR = fileName;
                }

                if (db.SANPHAMs.Any(p => p.TENSP == sp.TENSP && p.MASP != sp.MASP))
                {
                    ViewBag.Kq = "Tên sản phẩm đã tồn tại.";
                    return View(sp);
                }

                existing.TENSP = sp.TENSP;
                existing.MATH = sp.MATH;
                existing.GIA = sp.GIA;
                existing.COLOR = sp.COLOR;
                existing.MALOAI = sp.MALOAI;
                existing.MAMON = sp.MAMON;
                existing.MOTA = sp.MOTA;

                db.SaveChanges();
                return RedirectToAction("SanPham");
            }

            return View(sp);
        }
        #endregion

        #region ====== SETTINGS =======
        public ActionResult Settings()
        {
            return View();
        }
        #endregion

        #region ========== THƯƠNG HIỆU ==========
        public ActionResult ThuongHieu(string search, string quocgia, int page = 1)
        {
            var th = db.THUONGHIEUx.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                th = th.Where(x => x.TENTH.Contains(search));

            if (!string.IsNullOrEmpty(quocgia) && quocgia != "-- Tất cả --")
                th = th.Where(x => x.QUOCGIA == quocgia);

            int pageSize = 5;
            var model = th.OrderBy(x => x.TENTH)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToList();

            ViewBag.TotalPages = Math.Ceiling((double)th.Count() / pageSize);
            ViewBag.Page = page;
            ViewBag.QuocGiaList = db.THUONGHIEUx.Select(x => x.QUOCGIA)
                                                .Distinct()
                                                .ToList();

            return View(model);
        }
        #endregion

        #region ==== Lấy thương hiệu qua ID ====
        [HttpGet]
        public JsonResult GetById(int id)
        {
            var th = db.THUONGHIEUx.Find(id);
            if (th == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                ID = th.ID,
                MATH = th.MATH,
                TENTH = th.TENTH,
                QUOCGIA = th.QUOCGIA,
                LOGO = th.LOGO,
                MOTA = th.MOTA
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ==== THÊM THƯƠNG HIỆU ====
        [HttpPost]
        public ActionResult ThemThuongHieu(THUONGHIEU th, HttpPostedFileBase Logo)
        {
            try
            {
                var last = db.THUONGHIEUx.OrderByDescending(x => x.ID).FirstOrDefault();
                int nextId = (last == null) ? 1 : last.ID + 1;
                th.MATH = "TH" + nextId.ToString("D6");

                if (Logo != null && Logo.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(Logo.FileName);
                    string folder = Server.MapPath("~/Assets/Logos");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string path = Path.Combine(folder, fileName);
                    Logo.SaveAs(path);
                    th.LOGO = fileName;
                }

                db.THUONGHIEUx.Add(th);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region ============ SỬA THƯƠNG HIỆU ==========
        [HttpPost]
        public ActionResult SuaThuongHieu(THUONGHIEU th, HttpPostedFileBase Logo)
        {
            var existing = db.THUONGHIEUx.Find(th.ID);
            if (existing == null)
                return HttpNotFound();

            existing.TENTH = th.TENTH;
            existing.QUOCGIA = th.QUOCGIA;
            existing.MOTA = th.MOTA;

            if (Logo != null && Logo.ContentLength > 0)
            {
                string fileName = Path.GetFileName(Logo.FileName);
                string path = Path.Combine(Server.MapPath("~/Assets/Logos"), fileName);
                Logo.SaveAs(path);
                existing.LOGO = fileName;
            }

            db.SaveChanges();
            return RedirectToAction("ThuongHieu");
        }
        #endregion

        #region ============== XÓA THƯƠNG HIỆU ================
        [HttpPost]
        public ActionResult XoaThuongHieu(int id)
        {
            var th = db.THUONGHIEUx.Find(id);
            if (th != null)
            {
                db.THUONGHIEUx.Remove(th);
                db.SaveChanges();
            }
            return RedirectToAction("ThuongHieu");
        }
        #endregion

        #region ==================== NGƯỜI DÙNG =================
        public ActionResult NguoiDung(string search, string vaitro, int page = 1)
        {
            var q = db.NGUOIDUNGs.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                q = q.Where(x => (x.HO + " " + x.TEN).Contains(search) || x.EMAIL.Contains(search) || x.SDT.Contains(search));

            if (!string.IsNullOrEmpty(vaitro))
                q = q.Where(x => x.VAITRO == vaitro);

            int pageSize = 10;
            int total = q.Count();
            var model = q.OrderBy(x => x.ID)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            return View(model);
        }
        #endregion

        #region ============ CHI TIẾT NGƯỜI DÙNG ==============

        [HttpGet]
        public ActionResult NguoiDungChiTiet(int id)
        {
            var nd = db.NGUOIDUNGs.Find(id);
            if (nd == null) return HttpNotFound();

            // Lấy danh sách khách hàng liên kết với tài khoản này
            var khs = db.KHACHHANGs.Where(k => k.MAND == id).ToList();

            // Lấy ra danh sách các ID Khách hàng
            var khIDs = khs.Select(k => k.ID).ToList();

            // Lấy đơn hàng (Code cũ của bạn đã đúng phần này)
            var donHangs = db.DONHANGs.Where(d => d.MAKH.HasValue && khIDs.Contains(d.MAKH.Value)).ToList();

            // --- SỬA LẠI DÒNG NÀY ---
            // Tìm đánh giá dựa trên ID KHACHHANG (nằm trong list khIDs) chứ không phải id NGUOIDUNG
            var danhGias = db.DANHGIAs.Where(g => g.MAKH.HasValue && khIDs.Contains(g.MAKH.Value)).ToList();
            // -----------------------

            ViewBag.KhachHangs = khs;
            ViewBag.DonHangs = donHangs;
            ViewBag.DanhGias = danhGias;

            // Thống kê
            ViewBag.TongDon = donHangs.Count;
            ViewBag.TongChi = donHangs.Sum(x => (decimal?)x.TONGTIEN) ?? 0;

            return View(nd);
        }
        #endregion

        #region ============ BAN NGƯỜI DÙNG ===========
        [HttpPost]
        public ActionResult BanNguoiDung(int id)
        {
            try
            {
                var nd = db.NGUOIDUNGs.Find(id);
                if (nd == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

                nd.VAITRO = "BAN";

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region ============= Xóa đánh giá ===========
        [HttpPost]
        public ActionResult XoaDanhGia(int id)
        {
            var dg = db.DANHGIAs.Find(id);
            if (dg == null) return HttpNotFound();

            db.DANHGIAs.Remove(dg);
            db.SaveChanges();
            return Json(new { success = true });
        }
        #endregion

        #region ======= LẤY NGƯỜI DÙNG THEO ID ===========
        [HttpGet]
        public JsonResult GetNguoiDungById(int id)
        {
            var nd = db.NGUOIDUNGs.Find(id);
            if (nd == null) return Json(null, JsonRequestBehavior.AllowGet);

            var kh = db.KHACHHANGs.FirstOrDefault(x => x.MAND == nd.ID);

            return Json(new
            {
                ID = nd.ID,
                MAND = nd.MAND,
                HO = nd.HO,
                TEN = nd.TEN,
                HOTEN = (nd.HO ?? "") + " " + (nd.TEN ?? ""),
                SDT = nd.SDT,
                EMAIL = nd.EMAIL,
                VAITRO = nd.VAITRO,
                MATKHAU = nd.MATKHAU,
                KHACHHANG = kh == null ? null : new
                {
                    ID = kh.ID,
                    MAKH = kh.MAKH,
                    HOTEN = kh.HOTEN,
                    DIACHI = kh.DIACHI,
                    SDT = kh.SDT,
                    GIOITINH = kh.GIOITINH,
                    NGAYSINH = kh.NGAYSINH.HasValue ? kh.NGAYSINH.Value.ToString("yyyy-MM-dd") : ""
                }
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ========= THEM NGUOIDUNG ===========
        [HttpPost]
        public ActionResult ThemNguoiDung(FormCollection form)
        {
            try
            {
                string ho = form["HO"];
                string ten = form["TEN"];
                string email = form["EMAIL"];
                string sdt = form["SDT"];
                string matkhau = form["MATKHAU"];
                string vaitro = form["VAITRO"] ?? "USER";

                // simple email/username duplicate check by email
                if (!string.IsNullOrEmpty(email) && db.NGUOIDUNGs.Any(x => x.EMAIL == email))
                    return new HttpStatusCodeResult(400, "Email đã tồn tại");

                var u = new NGUOIDUNG
                {
                    HO = ho,
                    TEN = ten,
                    EMAIL = email,
                    SDT = sdt,
                    MATKHAU = matkhau,
                    VAITRO = vaitro
                };

                db.NGUOIDUNGs.Add(u);
                db.SaveChanges();

                // Optionally: create KHACHHANG record when VAITRO == 'KHACHHANG'
                if (vaitro == "USER")
                {
                    var kh = new KHACHHANG
                    {
                        MAND = u.ID,
                        HOTEN = (u.HO ?? "") + " " + (u.TEN ?? ""),
                        DIACHI = form["DIACHI"],
                        SDT = u.SDT,
                        GIOITINH = form["GIOITINH"],
                        NGAYSINH = string.IsNullOrEmpty(form["NGAYSINH"]) ? (DateTime?)null : DateTime.Parse(form["NGAYSINH"])
                    };
                    db.KHACHHANGs.Add(kh);
                    db.SaveChanges();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region ============ SỬA NGƯỜI DÙNG ============
        [HttpPost]
        public ActionResult SuaNguoiDung(FormCollection form)
        {
            try
            {
                int id = int.Parse(form["ID"]);
                var nd = db.NGUOIDUNGs.Find(id);
                if (nd == null) return HttpNotFound();

                nd.HO = form["HO"];
                nd.TEN = form["TEN"];
                nd.EMAIL = form["EMAIL"];
                nd.SDT = form["SDT"];
                nd.VAITRO = form["VAITRO"];

                // Nếu muốn đổi mật khẩu (optional)
                if (!string.IsNullOrEmpty(form["MATKHAU"]))
                    nd.MATKHAU = form["MATKHAU"];

                db.SaveChanges();

                var kh = db.KHACHHANGs.FirstOrDefault(k => k.MAND == nd.ID);
                if (kh != null)
                {
                    kh.HOTEN = (nd.HO ?? "") + " " + (nd.TEN ?? "");
                    kh.DIACHI = form["DIACHI"];
                    kh.SDT = form["SDT"];
                    if (!string.IsNullOrEmpty(form["NGAYSINH"])) kh.NGAYSINH = DateTime.Parse(form["NGAYSINH"]);
                    kh.GIOITINH = form["GIOITINH"];
                    db.SaveChanges();
                }

                return Json(new { success = true });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException e)
            {
                // Đoạn này giúp in ra lỗi cụ thể thay vì thông báo chung chung
                string errorMsg = "";
                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        errorMsg += $"- {ve.PropertyName}: {ve.ErrorMessage}\n";
                    }
                }
                return Json(new { success = false, message = "Lỗi dữ liệu:\n" + errorMsg });
            }
        }
        #endregion

        #region ============ UNLOCK NGƯỜI DÙNG ===========
        [HttpPost]
        public ActionResult UnbanNguoiDung(int id)
        {
            try
            {
                var nd = db.NGUOIDUNGs.Find(id);
                if (nd == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

                // Trả lại quyền KHACHHANG (Hoặc bạn có thể lưu quyền cũ vào 1 field khác để restore)
                nd.VAITRO = "KHACHHANG";

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region ============== Toggle VAITRO between ADMIN and KHACHHANG ===============
        [HttpPost]
        public ActionResult ToggleVaiTro(int id)
        {
            var nd = db.NGUOIDUNGs.Find(id);
            if (nd == null) return HttpNotFound();

            nd.VAITRO = nd.VAITRO == "ADMIN" ? "KHACHHANG" : "ADMIN";
            db.SaveChanges();
            return new HttpStatusCodeResult(200);
        }
        #endregion

        #region ============== QUẢN LÝ KHO HÀNG ================
        public ActionResult Kho(string keyword, string thuongHieu, int page = 1)
        {
            // Lấy Sản phẩm và include Biến thể (BIENTHESP) để tính tổng tồn kho
            var products = db.SANPHAMs.Include("THUONGHIEU").Include("BIENTHESPs").AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                products = products.Where(s => s.TENSP.Contains(keyword));

            if (!string.IsNullOrEmpty(thuongHieu))
                products = products.Where(s => s.THUONGHIEU.TENTH == thuongHieu);

            // Phân trang...
            int pageSize = 10;
            int totalPages = (int)Math.Ceiling((double)products.Count() / pageSize);
            var pagedProducts = products
                .OrderBy(s => s.MASP)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.ListThuongHieu = db.THUONGHIEUx.Select(s => s.TENTH).Distinct().ToList();
            ViewBag.SelectedBrand = thuongHieu;
            ViewBag.Keyword = keyword;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedProducts); // Truyền list SANPHAM (có BIENTHESPs)
        }
        #endregion

        #region =============== Hàm AJAX để lấy thông tin biến thể cho Modal Nhập Kho ==============
        [HttpGet]
        public JsonResult GetBienTheForNhapKho(int MASP)
        {
            var variants = db.BIENTHESPs
                .Where(b => b.MASP == MASP)
                .Select(b => new
                {
                    ID_BienThe = b.ID,
                    ID_Size = b.ID_SIZE,
                    TenSize = b.SIZE.TENSIZE,
                    SoLuongTon = b.SOLUONG_TON,
                    GiaHienTai = b.GIA_BIENTHE ?? b.SANPHAM.GIA // Lấy giá biến thể nếu có, nếu không lấy giá gốc
                })
                .ToList();

            return Json(variants, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region =============== Xử lý logic Nhập Kho ============
        [HttpPost]
        public ActionResult XuLyNhapKho(FormCollection form)
        {
            try
            {
                int masp = int.Parse(form["MASP"]);
                decimal? giaNhap = string.IsNullOrEmpty(form["GiaNhap"]) ? (decimal?)null : decimal.Parse(form["GiaNhap"]);

                // Lặp qua các size (ID_SIZE từ 1 đến 5)
                for (int sizeId = 1; sizeId <= 5; sizeId++)
                {
                    // Tên trường trong form là SoLuongNhap_1, SoLuongNhap_2, ...
                    string soLuongKey = $"SoLuongNhap_{sizeId}";
                    string bienTheIdKey = $"ID_BienThe_{sizeId}";

                    if (!string.IsNullOrEmpty(form[soLuongKey]) && int.Parse(form[soLuongKey]) > 0)
                    {
                        int soLuongNhapThem = int.Parse(form[soLuongKey]);

                        // Tìm biến thể hiện tại
                        var bienThe = db.BIENTHESPs.FirstOrDefault(b => b.MASP == masp && b.ID_SIZE == sizeId);

                        if (bienThe == null)
                        {

                            bienThe = new BIENTHESP
                            {
                                MASP = masp,
                                ID_SIZE = sizeId,
                                SOLUONG_TON = soLuongNhapThem
                            };
                            db.BIENTHESPs.Add(bienThe);
                        }
                        else
                        {
                            // Cập nhật tồn kho
                            bienThe.SOLUONG_TON += soLuongNhapThem;
                        }

                        // Cần thêm logic lưu lại Lịch sử nhập kho nếu có bảng LICHSUNHAPKHO

                        db.SaveChanges();
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region ========== ĐƠN HÀNG ============
        public ActionResult DonHang(int page = 1)
        {
            // KHÔNG THỰC THI TRUY VẤN Ở ĐÂY
            var LstDonHang = db.DONHANGs.Include("KHACHHANG");

            int pageSize = 10;

            // Đảm bảo Count() được gọi sau khi lọc/include nếu cần, nhưng trước khi phân trang
            int totalPages = (int)Math.Ceiling((double)LstDonHang.Count() / pageSize);

            // Thực hiện phân trang
            var pagedDH = LstDonHang
                .OrderByDescending(s => s.NGAYLAP) // Nên sắp xếp theo ngày lập mới nhất
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList(); // <--- THỰC THI TRUY VẤN Ở ĐÂY!

            ViewBag.ListThuongHieu = null;
            ViewBag.SelectedBrand = null;
            ViewBag.Keyword = null;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            // SỬA LỖI: Truyền đối tượng đã được thực thi và phân trang (pagedProducts)
            return View(pagedDH);
        }

        #endregion

        #region ========== LẤY ĐƠN HÀNG ===============
        public ActionResult GetDonHangInfo(int madh)
        {
            var donHang = db.DONHANGs
                            .Include("KHACHHANG")
                            .Include("GIAMGIA") // Thêm Include để lấy thông tin Mã Giảm Giá
                            .FirstOrDefault(d => d.ID == madh);

            if (donHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." }, JsonRequestBehavior.AllowGet);
            }

            // Khai báo mức giảm giá thực tế
            decimal giamGiaThucTe = 0;
            string tenGiamGia = null;

            if (donHang.GIAMGIA != null)
            {
                giamGiaThucTe = donHang.GIAMGIA.SOTIENGIAM ?? 0m;
                tenGiamGia = donHang.GIAMGIA.TENGG;
            }

            return Json(new
            {
                success = true,
                MaDH = donHang.MADH,
                TenKhachHang = donHang.KHACHHANG != null ? donHang.KHACHHANG.HOTEN : "Khách vãng lai",
                DiaChi = donHang.DIACHI,
                PhuongThuc = donHang.PHUONGTHUC,
                NgayLap = donHang.NGAYLAP?.ToString("dd/MM/yyyy"),
                // Truyền tên và mức giảm thực tế
                TenGG = tenGiamGia,
                GiamGiaThucTe = giamGiaThucTe,
                TongTien = donHang.TONGTIEN
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ========== LẤY CHITIETDONHANG =========
        public ActionResult GetChiTietDonHang(int madh)
        {
            var chiTiet = db.CHITIETDONHANGs
                            .Include("BIENTHESP.SANPHAM")
                            .Where(c => c.MADH == madh)
                            .Select(c => new
                            {
                                TenSP = c.BIENTHESP.SANPHAM.TENSP,
                                TenBienThe = c.BIENTHESP.SIZE.TENSIZE,
                                SoLuong = c.SOLUONG,
                                SoLuongTon = c.BIENTHESP.SOLUONG_TON,
                                DonGia = c.DONGIA,
                                ThanhTien = c.THANHTIEN
                            })
                            .ToList();

            return Json(chiTiet, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ======== CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG =========
        public JsonResult UpdateTrangThaiDH(string status, int madh)
        {
            try
            {
                var dh = db.DONHANGs.FirstOrDefault(d => d.ID == madh);
                var tt = db.LICHSUTHANHTOANs.FirstOrDefault(t => t.MADH == madh);

                if (dh == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." }, JsonRequestBehavior.AllowGet);
                }

                // --- LOGIC KIỂM TRA TỒN KHO ---

                if ((status == "Đang giao" || status == "Đã giao") && dh.TRANGTHAI != "Đang giao" && dh.TRANGTHAI != "Đã giao")
                {
                    var chiTietDH = db.CHITIETDONHANGs.Include("BIENTHESP.SANPHAM").Include("BIENTHESP.SIZE").Where(c => c.MADH == madh).ToList();
                    List<string> sanPhamThieuHang = new List<string>();

                    foreach (var item in chiTietDH)
                    {
                        // Kiểm tra: Số lượng đặt > Số lượng tồn
                        if (item.SOLUONG > item.BIENTHESP.SOLUONG_TON)
                        {
                            string tenSP = item.BIENTHESP.SANPHAM.TENSP;
                            string size = item.BIENTHESP.SIZE.TENSIZE;
                            int tonKho = item.BIENTHESP.SOLUONG_TON;

                            sanPhamThieuHang.Add($"- {tenSP} ({size}): Đặt {item.SOLUONG} / Tồn {tonKho}");
                        }
                    }

                    // Nếu có sản phẩm thiếu hàng -> Trả về lỗi, KHÔNG lưu
                    if (sanPhamThieuHang.Count > 0)
                    {
                        string msg = "Không thể duyệt đơn! Kho không đủ hàng:\n" + string.Join("\n", sanPhamThieuHang);
                        return Json(new { success = false, message = msg }, JsonRequestBehavior.AllowGet);
                    }
                }
                // ------------------------------

                dh.TRANGTHAI = status;
                if (tt != null)
                {
                    if (status == "Đã hủy")
                    {
                        tt.TRANGTHAI = "Hủy thanh toán";
                    }
                    else if (status == "Đã giao") // Chỉ xác nhận thanh toán khi giao thành công
                    {
                        tt.TRANGTHAI = "Đã thanh toán";
                    }
                }
                db.SaveChanges();
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lưu CSDL: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region ========= CẬP NHẬT LẠI ĐƠN HÀNG ==============
        public ActionResult GetPartialOrderTable(int page = 1, string keyword = null, string status = null)
        {
            var LstDonHang = db.DONHANGs.Include("KHACHHANG").AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                LstDonHang = LstDonHang.Where(dh => dh.TRANGTHAI == status);
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                LstDonHang = LstDonHang.Where(dh =>
                    dh.MADH.ToString().Contains(keyword) ||
                    dh.KHACHHANG.HOTEN.Contains(keyword)
                );
            }

            int pageSize = 10;
            int totalItems = LstDonHang.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pagedDH = LstDonHang
                .OrderByDescending(s => s.NGAYLAP)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentKeyword = keyword;
            ViewBag.CurrentStatus = status;
            ViewBag.Page = page;
            return PartialView("DonHangPartial", pagedDH);
        }
        #endregion

        #region ========= QUẢN LÝ DANH MỤC ===========
        public ActionResult QuanLyLoaiSP(string search, int page = 1)
        {
            IQueryable<LOAI> loaiList = db.LOAIs;

            if (!string.IsNullOrEmpty(search))
            {
                loaiList = loaiList.Where(l => l.TENLOAI.Contains(search));
            }
            int pageSize = 5;
            var model = loaiList.OrderBy(x => x.TENLOAI)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToList();

            ViewBag.TotalPages = Math.Ceiling((double)loaiList.Count() / pageSize);
            ViewBag.Page = page;

            return View(model);
        }

        public JsonResult GetLoaiSPById(int id)
        {
            var loai = db.LOAIs.FirstOrDefault(l => l.ID == id);
            if (loai == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            var result = new
            {
                loai.ID,
                loai.MALOAI,
                loai.TENLOAI,
                SANPHAM = loai.SANPHAMs.Select(sp => new { sp.MASP })
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemLoaiSP(LOAI loai)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.LOAIs.Add(loai);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Thêm danh mục thành công." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi lưu dữ liệu: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaLoaiSP(LOAI loai)
        {
            try
            {
                // 1. Kiểm tra ID hợp lệ
                if (loai.ID <= 0)
                {
                    return Json(new { success = false, message = "ID danh mục không hợp lệ." });
                }

                // 2. Tìm đối tượng trong Database
                var existingLoai = db.LOAIs.Find(loai.ID);
                if (existingLoai == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục trong CSDL." });
                }

                // 3. Cập nhật dữ liệu thủ công (An toàn nhất)
                // Chỉ cập nhật những trường cho phép sửa, không đụng vào MALOAI
                if (string.IsNullOrWhiteSpace(loai.TENLOAI))
                {
                    return Json(new { success = false, message = "Tên loại không được để trống." });
                }

                existingLoai.TENLOAI = loai.TENLOAI.Trim();

                // 4. Lưu thay đổi
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (DbEntityValidationException e)
            {
                // Bắt lỗi Validation cụ thể của Entity Framework (VD: Quá ký tự, Null...)
                string errorMsg = "";
                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        errorMsg += $"- {ve.PropertyName}: {ve.ErrorMessage}\n";
                    }
                }
                return Json(new { success = false, message = "Lỗi dữ liệu:\n" + errorMsg });
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác (VD: Mất kết nối SQL)
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaLoaiSP(int id)
        {
            var loai = db.LOAIs.Find(id);
            if (loai == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            if (loai.SANPHAMs.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục này vì còn sản phẩm liên quan.";
                return RedirectToAction("QuanLyLoaiSP");
            }

            db.LOAIs.Remove(loai);
            db.SaveChanges();
            return RedirectToAction("QuanLyLoaiSP");
        }


        // GET: Admin/QuanLyMonTT (Hiển thị danh sách)
        public ActionResult QuanLyMonTT(string search, int page = 1)
        {
            IQueryable<MONTT> monTTList = db.MONTTs;

            if (!string.IsNullOrEmpty(search))
            {
                monTTList = monTTList.Where(m => m.TENMON.Contains(search));
            }

            int pageSize = 5;
            var model = monTTList.OrderBy(x => x.TENMON)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .ToList();

            ViewBag.TotalPages = Math.Ceiling((double)monTTList.Count() / pageSize);
            ViewBag.Page = page;

            return View(model);
        }

        public JsonResult GetMonTTById(int id)
        {
            var montt = db.MONTTs.FirstOrDefault(m => m.ID == id);
            if (montt == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            var result = new
            {
                montt.ID,
                montt.MAMON,
                montt.TENMON,
                SANPHAM = montt.SANPHAMs.Select(sp => new { sp.MASP })
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMonTT(MONTT montt)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.MONTTs.Add(montt);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Thêm môn thể thao thành công." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi lưu dữ liệu: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        // POST: Admin/SuaMonTT (Sửa môn thể thao)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaMonTT(MONTT montt)
        {
            if (ModelState.IsValid)
            {
                var existingMonTT = db.MONTTs.Find(montt.ID);
                if (existingMonTT == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy môn thể thao." });
                }

                existingMonTT.TENMON = montt.TENMON;

                db.Entry(existingMonTT).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        // POST: Admin/XoaMonTT (Xóa môn thể thao)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaMonTT(int id)
        {
            var montt = db.MONTTs.Find(id);
            if (montt == null)
            {
                return Json(new { success = false, message = "Không tìm thấy môn." });
            }

            if (montt.SANPHAMs.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Không thể xóa môn thể thao này vì còn sản phẩm liên quan."
                });
            }

            db.MONTTs.Remove(montt);
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Xóa môn thể thao thành công!"
            });
        }

        #endregion

        #region ============= THỐNG KÊ =============
        public ActionResult ThongKe()
        {
            // 1. Các số liệu tổng quan (KPIs)
            // Chỉ tính doanh thu các đơn hàng "Đã giao" hoặc "Hoàn thành" để chính xác
            var donHangHoanThanh = db.DONHANGs.Where(dh => dh.TRANGTHAI == "Đã giao" || dh.TRANGTHAI == "Hoàn thành");

            decimal tongDoanhThu = donHangHoanThanh.Sum(dh => (decimal?)dh.TONGTIEN) ?? 0;
            int tongDonHang = db.DONHANGs.Count();
            int tongSanPham = db.SANPHAMs.Count();
            int tongKhachHang = db.KHACHHANGs.Count();

            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.TongDonHang = tongDonHang;
            ViewBag.TongSanPham = tongSanPham;
            ViewBag.TongKhachHang = tongKhachHang;

            // 2. BIỂU ĐỒ ĐƯỜNG (Line Chart): Doanh thu theo tháng trong năm nay
            var currentYear = DateTime.Now.Year;
            var doanhThuTheoThang = donHangHoanThanh
                .Where(dh => dh.NGAYLAP.HasValue && dh.NGAYLAP.Value.Year == currentYear)
                .GroupBy(dh => dh.NGAYLAP.Value.Month)
                .Select(g => new { Thang = g.Key, DoanhThu = g.Sum(x => x.TONGTIEN) })
                .ToList();

            // Tạo mảng dữ liệu chuẩn cho 12 tháng (gán 0 nếu tháng đó không có đơn)
            decimal[] dataDoanhThu = new decimal[12];
            foreach (var item in doanhThuTheoThang)
            {
                dataDoanhThu[item.Thang - 1] = item.DoanhThu;
            }
            ViewBag.ChartDoanhThuData = JsonConvert.SerializeObject(dataDoanhThu);

            // 3. BIỂU ĐỒ TRÒN (Pie Chart): Tỷ lệ trạng thái đơn hàng
            var trangThaiDonHang = db.DONHANGs
                .GroupBy(dh => dh.TRANGTHAI)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.ChartTrangThaiLabels = JsonConvert.SerializeObject(trangThaiDonHang.Select(x => string.IsNullOrEmpty(x.Label) ? "Chưa xử lý" : x.Label));
            ViewBag.ChartTrangThaiData = JsonConvert.SerializeObject(trangThaiDonHang.Select(x => x.Count));

            // 4. BIỂU ĐỒ CỘT (Bar Chart): Top 5 Sản phẩm bán chạy nhất (Theo số lượng)
            // Join CHITIETDONHANG -> BIENTHESP -> SANPHAM
            var topSanPham = db.CHITIETDONHANGs
                .GroupBy(ct => ct.BIENTHESP.SANPHAM.TENSP)
                .Select(g => new { TenSP = g.Key, SoLuong = g.Sum(x => x.SOLUONG) })
                .OrderByDescending(x => x.SoLuong)
                .Take(5)
                .ToList();

            ViewBag.ChartTopSPLabels = JsonConvert.SerializeObject(topSanPham.Select(x => x.TenSP.Length > 20 ? x.TenSP.Substring(0, 17) + "..." : x.TenSP)); // Cắt tên nếu quá dài
            ViewBag.ChartTopSPData = JsonConvert.SerializeObject(topSanPham.Select(x => x.SoLuong));

            // 5. BIỂU ĐỒ VÀNH KHUYÊN (Doughnut Chart): Doanh thu theo Thương hiệu
            // Logic: Join ChiTiet -> BienThe -> SanPham -> ThuongHieu
            // Lưu ý: Tính ước lượng dựa trên chi tiết đơn hàng (đã bán)
            var topThuongHieu = db.CHITIETDONHANGs
                .Where(ct => ct.BIENTHESP.SANPHAM.THUONGHIEU != null) // Đảm bảo có thương hiệu
                .GroupBy(ct => ct.BIENTHESP.SANPHAM.THUONGHIEU.TENTH)
                .Select(g => new { TenTH = g.Key, DoanhThu = g.Sum(x => x.THANHTIEN) })
                .OrderByDescending(x => x.DoanhThu)
                .Take(5) // Lấy top 5 thương hiệu, còn lại có thể gộp vào "Khác" nếu muốn
                .ToList();

            ViewBag.ChartThuongHieuLabels = JsonConvert.SerializeObject(topThuongHieu.Select(x => x.TenTH));
            ViewBag.ChartThuongHieuData = JsonConvert.SerializeObject(topThuongHieu.Select(x => x.DoanhThu));

            return View();
        }
        #endregion

        #region ========== PROFILE ADMIN ==========

        // 1. Hiển thị trang Profile
        [HttpGet]
        public ActionResult Profile()
        {
            // Lấy ID từ Session (Giả sử bạn lưu Session["UserID"] khi đăng nhập)
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Login");
            }

            int userId = (int)Session["UserID"];
            var admin = db.NGUOIDUNGs.Find(userId);

            if (admin == null || admin.VAITRO != "ADMIN")
            {
                return RedirectToAction("Login", "Login");
            }

            return View(admin);
        }

        // 2. Cập nhật thông tin cá nhân
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(NGUOIDUNG model)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Login");

            try
            {
                var admin = db.NGUOIDUNGs.Find(model.ID);
                if (admin != null)
                {
                    admin.HO = model.HO;
                    admin.TEN = model.TEN;
                    admin.SDT = model.SDT;
                    // Không cho phép đổi Email và Vai trò ở đây để đảm bảo an toàn

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi cập nhật: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }

        // 3. Đổi mật khẩu Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Login");

            int userId = (int)Session["UserID"];
            var admin = db.NGUOIDUNGs.Find(userId);

            if (admin != null)
            {
                // Kiểm tra mật khẩu cũ (Lưu ý: Nếu dùng mã hóa MD5/BCrypt thì phải mã hóa CurrentPassword trước khi so sánh)
                if (admin.MATKHAU != CurrentPassword)
                {
                    TempData["PassError"] = "Mật khẩu hiện tại không đúng.";
                    return RedirectToAction("Profile");
                }

                if (NewPassword != ConfirmPassword)
                {
                    TempData["PassError"] = "Mật khẩu xác nhận không khớp.";
                    return RedirectToAction("Profile");
                }

                if (NewPassword.Length < 6)
                {
                    TempData["PassError"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                    return RedirectToAction("Profile");
                }

                admin.MATKHAU = NewPassword;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            }

            return RedirectToAction("Profile");
        }
        #endregion
    }
}