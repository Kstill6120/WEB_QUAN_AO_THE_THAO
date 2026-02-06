using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDoTheThao.Models;

namespace WebBanDoTheThao.Controllers
{
    public class LoginController : Controller
    {
        private QLQAEntities db = new QLQAEntities();

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Email, string Password)
        {
            // Tìm user theo Email trước để tránh lỗi khoảng trắng
            var user = db.NGUOIDUNGs.FirstOrDefault(u => u.EMAIL.ToLower() == Email.Trim().ToLower());

            if (user != null)
            {
                // So sánh mật khẩu (Cắt khoảng trắng thừa nếu có trong DB)
                if (user.MATKHAU.Trim() == Password.Trim())
                {
                    string userRole = (user.VAITRO ?? "").Trim().ToUpper();

                    if (userRole == "BAN")
                    {
                        ViewBag.Kq = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.";
                        return View();
                    }
                    Session["User"] = user;
                    Session["UserID"] = user.ID;
                    Session["UserEmail"] = user.EMAIL;
                    Session["VaiTro"] = userRole;

                    if (userRole == "ADMIN")
                        return RedirectToAction("TrangChu", "Admin");
                    else
                        return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Kq = "Đăng nhập không thành công (Sai email hoặc mật khẩu)";
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(string Ho, string Ten, string SDT, string Email, string MatKhau)
        {
            if (db.NGUOIDUNGs.Any(u => u.EMAIL == Email))
            {
                ViewBag.Kq = "Email đã tồn tại";
                return View();
            }

            NGUOIDUNG user = new NGUOIDUNG
            {
                HO = Ho,
                TEN = Ten,
                SDT = SDT,
                EMAIL = Email,
                MATKHAU = MatKhau,
                VAITRO = "USER"
            };

            db.NGUOIDUNGs.Add(user);
            db.SaveChanges();

            return RedirectToAction("Login");
        }

        // --- XỬ LÝ QUÊN MẬT KHẨU (KHÔNG GỬI MAIL) ---
        [HttpPost]
        public ActionResult ForgotPassword(string Email, string SDT, string NewPass, string ConfirmPass)
        {
            // 1. Kiểm tra xác nhận mật khẩu
            if (NewPass != ConfirmPass)
            {
                ViewBag.Kq = "Mật khẩu xác nhận không khớp!";
                ViewBag.ShowForgot = true; // Giữ form quên mật khẩu hiện
                return View("Login");
            }

            // 2. Tìm user khớp cả Email và SĐT
            var user = db.NGUOIDUNGs.FirstOrDefault(u => u.EMAIL.ToLower() == Email.Trim().ToLower() && u.SDT == SDT.Trim());

            if (user != null)
            {
                // 3. Đổi mật khẩu trực tiếp
                user.MATKHAU = NewPass;
                db.SaveChanges();

                ViewBag.Kq = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
                ViewBag.ShowForgot = false; // Quay về form đăng nhập
            }
            else
            {
                ViewBag.Kq = "Thông tin Email hoặc SĐT không chính xác.";
                ViewBag.ShowForgot = true;
            }

            return View("Login");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session["FavCount"] = 0;
            return RedirectToAction("Index", "Home");
        }
    }
}