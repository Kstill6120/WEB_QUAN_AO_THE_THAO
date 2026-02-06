using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebBanDoTheThao.Models;
using System.Data.Entity;

namespace WebBanDoTheThao.Models
{
    public class Data
    {
        private QLQAEntities db = new QLQAEntities();

        public List<SANPHAM> GetSanPhams()
        {
            return db.SANPHAMs.Include(s => s.THUONGHIEU).ToList();
        }

        public List<THUONGHIEU> GetAllBrands()
        {
            return db.THUONGHIEUx.ToList();
        }

        public string GetBrandName(int id)
        {
            return db.THUONGHIEUx.FirstOrDefault(th => th.ID == id)?.TENTH ?? "Không rõ";
        }

        public bool ToggleFavorite(int userID, int MASP)
        {
            var favorite = db.YEUTHICHes
                             .SingleOrDefault(yt => yt.MAND == userID && yt.MASP == MASP);

            if (favorite != null)
            {
                db.YEUTHICHes.Remove(favorite);
                db.SaveChanges();
                return false;
            }
            else
            {
                var newFavorite = new YEUTHICH
                {
                    MAND = userID,
                    MASP = MASP,
                    NGAYTHEM = DateTime.Now
                };
                db.YEUTHICHes.Add(newFavorite);
                db.SaveChanges();
                return true;
            }
        }

        public int GetFavoriteCount(int userID)
        {
            return db.YEUTHICHes.Count(yt => yt.MAND == userID);
        }

        public List<SANPHAM> GetFavoriteProducts(int userID)
        {
            var favoriteProducts = (from sp in db.SANPHAMs.Include(s => s.THUONGHIEU)
                                    join yt in db.YEUTHICHes on sp.ID equals yt.MASP
                                    where yt.MAND == userID
                                    select sp)
                                   .ToList();
            return favoriteProducts;
        }

        public List<SANPHAM> GetSanPhamsFiltered(List<string> thuonghieu = null, List<string> mausac = null, List<string> mucgia = null, string sort = null)
        {
            IQueryable<SANPHAM> sanphamsQuery = db.SANPHAMs.AsQueryable();

            var loai = db.LOAIs.FirstOrDefault(l => l.TENLOAI.Trim() == "Quần áo bóng đá không logo");
            if (loai != null)
            {
                sanphamsQuery = sanphamsQuery.Where(sp => sp.MALOAI == loai.ID);
            }

            if (thuonghieu != null && thuonghieu.Any())
            {
                var thuonghieuInts = thuonghieu.Select(int.Parse).ToList();
                sanphamsQuery = sanphamsQuery.Where(sp => sp.MATH.HasValue && thuonghieuInts.Contains(sp.MATH.Value));
            }

            if (mausac != null && mausac.Any())
                sanphamsQuery = sanphamsQuery.Where(sp => mausac.Contains(sp.COLOR));

            if (mucgia != null && mucgia.Any())
            {
                IQueryable<SANPHAM> priceFilteredQuery = db.SANPHAMs.Where(sp => false);

                foreach (var m in mucgia)
                {
                    switch (m)
                    {
                        case "duoi100": priceFilteredQuery = priceFilteredQuery.Concat(sanphamsQuery.Where(sp => sp.GIA < 100000)); break;
                        case "100-200": priceFilteredQuery = priceFilteredQuery.Concat(sanphamsQuery.Where(sp => sp.GIA >= 100000 && sp.GIA <= 200000)); break;
                        case "200-500": priceFilteredQuery = priceFilteredQuery.Concat(sanphamsQuery.Where(sp => sp.GIA >= 200000 && sp.GIA <= 500000)); break;
                        case "500-700": priceFilteredQuery = priceFilteredQuery.Concat(sanphamsQuery.Where(sp => sp.GIA >= 500000 && sp.GIA <= 700000)); break;
                        case "700-1000": priceFilteredQuery = priceFilteredQuery.Concat(sanphamsQuery.Where(sp => sp.GIA >= 700000 && sp.GIA <= 1000000)); break;
                        case "1000-2000": priceFilteredQuery = priceFilteredQuery.Concat(sanphamsQuery.Where(sp => sp.GIA >= 1000000 && sp.GIA <= 2000000)); break;
                        case "tren3000": priceFilteredQuery = priceFilteredQuery.Concat(sanphamsQuery.Where(sp => sp.GIA > 3000000)); break;
                    }
                }
                sanphamsQuery = priceFilteredQuery.Distinct();
            }

            switch (sort)
            {
                case "az": sanphamsQuery = sanphamsQuery.OrderBy(sp => sp.TENSP); break;
                case "za": sanphamsQuery = sanphamsQuery.OrderByDescending(sp => sp.TENSP); break;
                case "gia-tang": sanphamsQuery = sanphamsQuery.OrderBy(sp => sp.GIA); break;
                case "gia-giam": sanphamsQuery = sanphamsQuery.OrderByDescending(sp => sp.GIA); break;
                case "hang-moi": sanphamsQuery = sanphamsQuery.OrderByDescending(sp => sp.ID); break;
            }

            return sanphamsQuery.Include(s => s.THUONGHIEU).ToList();
        }
    }
}