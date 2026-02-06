using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebBanDoTheThao.Models;
using System.Data.Entity;
using System.Web.UI;

namespace WebBanDoTheThao.Controllers
{
    public class HomeController : Controller
    {
        private readonly Data dt = new Data();
        private QLQAEntities db = new QLQAEntities();

        private int? GetCurrentUserID()
        {
            return Session["UserID"] as int?;
        }

        public ActionResult Index()
        {
            int? userID = GetCurrentUserID();

            // ---------------------------------------------------------
            // 1. Xử lý Yêu thích (Favorites)
            // ---------------------------------------------------------
            Dictionary<int, bool> isFavoriteDict = new Dictionary<int, bool>();
            if (userID.HasValue)
            {
                int favCount = dt.GetFavoriteCount(userID.Value);
                Session["FavCount"] = favCount;

                // Lấy danh sách ID sản phẩm yêu thích của user
                var favoriteIDs = db.YEUTHICHes
                                    .Where(yt => yt.MAND == userID.Value)
                                    .Select(yt => yt.MASP)
                                    .ToHashSet();

                // Lấy danh sách tất cả ID sản phẩm hiện có để map trạng thái
                // (Cách này tối ưu hơn là query DB trong vòng lặp)
                var allIds = db.SANPHAMs.Select(s => s.ID).ToList();
                foreach (var id in allIds)
                {
                    isFavoriteDict[id] = favoriteIDs.Contains(id);
                }
            }
            else
            {
                Session["FavCount"] = 0;
            }
            ViewBag.IsFavoriteDict = isFavoriteDict;

            // ---------------------------------------------------------
            // 2. Lấy danh sách SẢN PHẨM GIÁ RẺ
            // (Sắp xếp giá tăng dần, lấy 4 sản phẩm đầu tiên)
            // ---------------------------------------------------------
            var cheapProducts = db.SANPHAMs
                .Include(s => s.THUONGHIEU)
                .OrderBy(s => s.GIA)
                .Take(4)
                .ToList();

            // ---------------------------------------------------------
            // 3. Lấy danh sách SẢN PHẨM BÁN CHẠY
            // (Logic: Đếm tổng SOLUONG trong các đơn hàng có TRANGTHAI là "Đã giao")
            // ---------------------------------------------------------

            // Bước A: Tính toán số lượng bán ra bằng LINQ Join
            // Sử dụng join trực tiếp giữa DONHANG -> CHITIETDONHANG -> BIENTHESP để tránh lỗi navigation property
            var topSoldStats = (from dh in db.DONHANGs
                                join ct in db.CHITIETDONHANGs on dh.ID equals ct.MADH
                                join bt in db.BIENTHESPs on ct.ID_BIENTHE equals bt.ID
                                where dh.TRANGTHAI == "Đã giao"
                                group ct by bt.MASP into g
                                select new
                                {
                                    ProductID = g.Key,              // Kiểu int (vì BIENTHESP.MASP là int)
                                    SoldCount = g.Sum(x => x.SOLUONG) // Kiểu int (vì CHITIETDONHANG.SOLUONG là int)
                                })
                                .OrderByDescending(x => x.SoldCount)
                                .Take(4)
                                .ToList();

            // Bước B: Lấy danh sách ID sản phẩm từ kết quả thống kê
            var topProductIDs = topSoldStats.Select(x => x.ProductID).ToList();

            // Bước C: Truy vấn thông tin chi tiết của các sản phẩm này từ bảng SANPHAM
            var bestSellersList = db.SANPHAMs
                .Include(s => s.THUONGHIEU)
                .Where(s => topProductIDs.Contains(s.ID))
                .ToList();

            // Bước D: Sắp xếp lại list kết quả theo đúng thứ tự bán chạy (vì lệnh Where bên trên không bảo toàn thứ tự)
            bestSellersList = bestSellersList
                .OrderBy(s => topProductIDs.IndexOf(s.ID))
                .ToList();

            // Bước E: Tạo Dictionary để truyền số lượng đã bán qua View
            Dictionary<int, int> soldCounts = new Dictionary<int, int>();
            foreach (var stat in topSoldStats)
            {
                // Vì ProductID và SoldCount đều là int (không phải int?), ta gán trực tiếp
                soldCounts[stat.ProductID] = stat.SoldCount;
            }

            // ---------------------------------------------------------
            // 4. Truyền dữ liệu qua View
            // ---------------------------------------------------------
            ViewBag.CheapProducts = cheapProducts;
            ViewBag.BestSellers = bestSellersList;
            ViewBag.SoldCounts = soldCounts;

            return View();
        }

        public ActionResult Search(string query, int page = 1)
        {
            // 1. Kiểm tra từ khóa
            if (string.IsNullOrEmpty(query))
            {
                return RedirectToAction("Index");
            }

            // 2. Chuẩn hóa về chữ thường và xóa khoảng trắng thừa
            string keyword = query.ToLower().Trim();

            // 3. Truy vấn Database (Tìm theo Tên SP hoặc Tên Thương hiệu)
            // Lưu ý: .ToLower() trong LINQ to Entities giúp so sánh không phân biệt hoa thường
            var products = db.SANPHAMs
                             .Include(s => s.THUONGHIEU)
                             .Where(s => s.TENSP.ToLower().Contains(keyword) ||
                                         s.THUONGHIEU.TENTH.ToLower().Contains(keyword))
                             .OrderByDescending(s => s.ID) // Sắp xếp sản phẩm mới nhất lên đầu
                             .AsQueryable();

            // 4. Xử lý phân trang (Tái sử dụng logic cũ)
            int pageSize = 12;
            int totalProducts = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var pagedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 5. Xử lý Yêu thích (Để hiển thị trái tim đỏ/trắng đúng trạng thái)
            int? userID = GetCurrentUserID();
            Dictionary<int, bool> isFavoriteDict = new Dictionary<int, bool>();
            if (userID.HasValue)
            {
                var favoriteIDs = db.YEUTHICHes.Where(yt => yt.MAND == userID.Value).Select(yt => yt.MASP).ToHashSet();
                foreach (var sp in pagedProducts) isFavoriteDict[sp.ID] = favoriteIDs.Contains(sp.ID);
            }

            // 6. Truyền dữ liệu sang View
            ViewBag.IsFavoriteDict = isFavoriteDict;
            ViewBag.Keyword = query; // Truyền lại từ khóa gốc để hiển thị ở tiêu đề
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalResults = totalProducts;

            return View(pagedProducts);
        }

        [HttpPost]
        public ActionResult AddFavorite(string masp)
        {
            int? userID = GetCurrentUserID();
            if (!userID.HasValue)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để dùng tính năng này." });
            }

            try
            {
                int productID = Convert.ToInt32(masp);

                bool isAdded = dt.ToggleFavorite(userID.Value, productID);
                int count = dt.GetFavoriteCount(userID.Value);
                Session["FavCount"] = count;

                return Json(new
                {
                    success = true,
                    isFav = isAdded,
                    count = count
                });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi hệ thống khi xử lý yêu thích." });
            }
        }

        public ActionResult Favorites()
        {
            int? userID = GetCurrentUserID();
            if (!userID.HasValue)
            {
                return RedirectToAction("Login", "Login");
            }

            List<SANPHAM> favoriteProducts = dt.GetFavoriteProducts(userID.Value);

            ViewBag.FavCount = favoriteProducts.Count;
            return View(favoriteProducts);
        }

        public ActionResult ShowQABD(List<string> thuonghieu = null, List<string> mausac = null, List<string> mucgia = null, string sort = null, int page = 1) // <--- THÊM PAGE
        {
            int? userID = GetCurrentUserID();

            var allFilteredSanphams = dt.GetSanPhamsFiltered(thuonghieu, mausac, mucgia, sort) ?? new List<SANPHAM>();

            int pageSize = 12;
            int totalProducts = allFilteredSanphams.Count;
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var pagedSanphams = allFilteredSanphams
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Dictionary<int, bool> isFavoriteDict = new Dictionary<int, bool>();
            if (userID.HasValue)
            {
                int favCount = dt.GetFavoriteCount(userID.Value);
                Session["FavCount"] = favCount;

                var favoriteIDs = db.YEUTHICHes
                                    .Where(yt => yt.MAND == userID.Value)
                                    .Select(yt => yt.MASP)
                                    .ToHashSet();

                foreach (var sp in pagedSanphams)
                {
                    isFavoriteDict[sp.ID] = favoriteIDs.Contains(sp.ID);
                }
            }
            else
            {
                Session["FavCount"] = 0;
            }

            ViewBag.IsFavoriteDict = isFavoriteDict;
            ViewBag.Sort = sort;
            ViewBag.ThuongHieu = thuonghieu ?? new List<string>();
            ViewBag.MauSac = mausac ?? new List<string>();
            ViewBag.MucGia = mucgia ?? new List<string>();

            ViewBag.AllBrands = dt.GetAllBrands();
            ViewBag.DSMauSac = dt.GetSanPhams().Select(sp => sp.COLOR).Distinct().ToList() ?? new List<string>();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            return View(pagedSanphams);
        }
        public ActionResult QuickView(int id)
        {
            var sanPham = db.SANPHAMs
                           .Include(s => s.THUONGHIEU)
                           .Include(s => s.BIENTHESPs.Select(b => b.SIZE)) // <--- ĐÃ THÊM INCLUDE
                           .FirstOrDefault(s => s.ID == id);

            if (sanPham == null)
            {
                Response.StatusCode = 404;
                return Content("Sản phẩm không tồn tại.");
            }

            ViewBag.BrandName = sanPham.THUONGHIEU?.TENTH ?? "N/A";

            // TRUYỀN DANH SÁCH SIZE CÓ TỒN KHO > 0 (Tùy chọn lọc tồn kho)
            ViewBag.AvailableSizes = sanPham.BIENTHESPs
                                            .Where(b => b.SOLUONG_TON > 0)
                                            .Select(b => b.SIZE.TENSIZE)
                                            .Distinct()
                                            .ToList();

            return PartialView("QuickView", sanPham);
        }

        public ActionResult ShowFullProduct(int id, int page = 1, string sort = "newest", int? starFilter = null)
        {
            // 1. Lấy thông tin sản phẩm
            var sanPham = db.SANPHAMs
                .Include(s => s.THUONGHIEU)
                .Include(s => s.BIENTHESPs.Select(b => b.SIZE))
                .FirstOrDefault(s => s.ID == id);

            if (sanPham == null) return HttpNotFound();

            // 2. Lấy danh sách Size có sẵn (Tồn kho > 0) để khách chọn mua
            ViewBag.AvailableSizes = sanPham.BIENTHESPs
                                            .Where(b => b.SOLUONG_TON > 0)
                                            .Select(b => b.SIZE.TENSIZE)
                                            .Distinct()
                                            .ToList();

            // 3. Truy vấn Đánh giá (Review)
            var danhGiaQuery = db.DANHGIAs
                .Where(dg => dg.MASP == sanPham.ID)
                .Include(dg => dg.KHACHHANG)
                .AsQueryable();

            // Lọc theo sao
            if (starFilter.HasValue && starFilter.Value >= 1 && starFilter.Value <= 5)
            {
                danhGiaQuery = danhGiaQuery.Where(dg => dg.SOSAO == starFilter.Value);
            }

            // Sắp xếp đánh giá
            switch (sort)
            {
                case "oldest": danhGiaQuery = danhGiaQuery.OrderBy(dg => dg.NGAYDG); break;
                case "highest": danhGiaQuery = danhGiaQuery.OrderByDescending(dg => dg.SOSAO); break;
                case "newest":
                default: danhGiaQuery = danhGiaQuery.OrderByDescending(dg => dg.NGAYDG); break;
            }

            // 4. Phân trang
            int pageSize = 5;
            int totalReviews = danhGiaQuery.Count();
            int totalPages = (int)Math.Ceiling((double)totalReviews / pageSize);

            var reviewsList = danhGiaQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Reviews = reviewsList;

            // ====================================================================
            // [LOGIC MỚI]: LẤY SIZE KHÁCH ĐÃ MUA (DÙNG JOIN VÌ KHÔNG CÓ NAVIGATION)
            // ====================================================================
            Dictionary<int, string> reviewSizes = new Dictionary<int, string>();

            foreach (var review in reviewsList)
            {
                // Nếu khách hàng ẩn danh hoặc không có MAKH, bỏ qua
                if (review.MAKH == null)
                {
                    reviewSizes[review.ID] = "N/A";
                    continue;
                }

                // Thực hiện JOIN các bảng để tìm Size
                // CHITIETDONHANG (ct) JOIN DONHANG (dh) ON MADH
                // JOIN BIENTHESP (bt) ON ID_BIENTHE
                // JOIN SIZE (sz) ON ID_SIZE
                var sizeName = (from ct in db.CHITIETDONHANGs
                                join dh in db.DONHANGs on ct.MADH equals dh.ID
                                join bt in db.BIENTHESPs on ct.ID_BIENTHE equals bt.ID
                                join sz in db.SIZEs on bt.ID_SIZE equals sz.ID
                                where dh.MAKH == review.MAKH          // Của khách hàng này
                                   && bt.MASP == id                   // Mua sản phẩm này
                                   && dh.TRANGTHAI == "Đã giao"       // Đơn đã giao thành công
                                orderby dh.NGAYLAP descending         // Lấy đơn mới nhất
                                select sz.TENSIZE).FirstOrDefault();

                reviewSizes[review.ID] = sizeName ?? "Tiêu chuẩn";
            }
            ViewBag.ReviewSizes = reviewSizes;
            // ====================================================================


            // 5. Các thông số khác cho View
            ViewBag.TotalReviews = totalReviews;
            ViewBag.AvgStars = db.DANHGIAs.Where(dg => dg.MASP == sanPham.ID).Any()
                               ? db.DANHGIAs.Where(dg => dg.MASP == sanPham.ID).Average(x => x.SOSAO)
                               : 0;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.ProductId = id;
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentStarFilter = starFilter;

            // Reset ViewBag thừa
            ViewBag.Keyword = null;
            ViewBag.SelectedBrand = null;
            ViewBag.SelectedMon = null;

            return View(sanPham);
        }
    }
}