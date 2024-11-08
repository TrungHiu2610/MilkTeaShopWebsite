using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn_LapTrinhWeb.Models
{
    public class GioHangModel
    {
        public GioHang cart;
        public GioHangModel(GioHang cart) 
        {
            this.cart = cart;
        }
        public int getSoLuongMatHang()
        {
            return cart.ChiTietGioHangs.Count;
        }
        public int getTongSoLuong()
        {
            return cart.ChiTietGioHangs.Sum(t=> t.SoLuong);
        }
        public decimal? getTongTien()
        {
            return cart.ChiTietGioHangs.Sum(t => t.ThanhTien);
        }
    }
}