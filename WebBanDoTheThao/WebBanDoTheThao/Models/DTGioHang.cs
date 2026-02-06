using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanDoTheThao.Models
{
    public class DTGioHang
    {
        QLQAEntities db = new QLQAEntities();

        public void ThemCTGH(int maSP,string size, int soLuong,int makh)
        { 
            SIZE a=db.SIZEs.FirstOrDefault(t => t.TENSIZE == size);
            if(a == null)
                throw new Exception($"Size '{size}' không tồn tại.");
            BIENTHESP btsp=db.BIENTHESPs.FirstOrDefault(t => t.MASP == maSP && t.ID_SIZE == a.ID);
            SANPHAM sp=db.SANPHAMs.FirstOrDefault(t => t.ID == maSP);
            GIOHANG gh=db.GIOHANGs.FirstOrDefault(t => t.MAKH == makh);
            

            if (btsp == null || sp == null) return;
            CHITIETGIOHANG ctgh = new CHITIETGIOHANG() {
                MAGH = gh.ID,
                ID_BIENTHE = btsp.ID,
                SOLUONG = soLuong,
                DONGIA = sp.GIA,
                THANHTIEN = sp.GIA * soLuong
            };
            db.CHITIETGIOHANGs.Add(ctgh);
            db.SaveChanges();
        }
        public void XoaCTGH(int idBienThe, int makh)
        {
            GIOHANG gh = db.GIOHANGs.FirstOrDefault(t => t.MAKH == makh);
            if (gh == null) return;

            CHITIETGIOHANG ctgh = db.CHITIETGIOHANGs.FirstOrDefault(t => t.ID_BIENTHE == idBienThe && t.MAGH == gh.ID);
            if (ctgh == null) return;

            if (ctgh.SOLUONG > 1)
            {
                ctgh.SOLUONG -= 1; 
                ctgh.THANHTIEN = ctgh.SOLUONG * ctgh.DONGIA; 
                db.SaveChanges();   
            }
            else
            {
                db.CHITIETGIOHANGs.Remove(ctgh);
                db.SaveChanges();
            }
        }


        public int DemCTGH(int makh)
        {
            GIOHANG gh=db.GIOHANGs.FirstOrDefault(t => t.MAKH == makh);
            if (gh == null) return 0;
            List<CHITIETGIOHANG> listCTGH=db.CHITIETGIOHANGs.Where(t => t.MAGH == gh.ID).ToList();
            return listCTGH.Sum(t=>t.SOLUONG);
        }

        
    }
}