using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_LapTrinhWeb.Models;

namespace DoAn_LapTrinhWeb.Controllers
{
    
    public class HomeController : Controller
    {

        //
        // GET: /Home/
        DB_QL_CuaHangTraSuaDataContext data = new DB_QL_CuaHangTraSuaDataContext();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(FormCollection form)
        {
            string customerName = form["CustomerName"];
            string phoneNumber = form["PhoneNumber"];
            string password = form["Password"];
            string confirmPassword = form["ConfirmPassword"];

            var existingUser = data.NguoiDungs.FirstOrDefault(u => u.SoDienThoai == phoneNumber);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Số điện thoại đã tồn tại. Vui lòng thử lại với số khác.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp. Vui lòng thử lại.";
                return View();
            }

            // Tạo người dùng mới mà không cần gán mã
            NguoiDung newUser = new NguoiDung
            {
                HoTen = customerName,
                SoDienThoai = phoneNumber,
                MatKhau = password,
                MaQuyen = "KH" // Ví dụ mã quyền "Khách hàng"
            };

            data.NguoiDungs.InsertOnSubmit(newUser);
            data.SubmitChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("LogIn", "Home");
        }


        public ActionResult LogIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogIn(FormCollection form)
        {
            NguoiDung user = data.NguoiDungs.FirstOrDefault
                (t => t.SoDienThoai == form["PhoneNumber"] && t.MatKhau == form["Password"]);
            if(user!= null)
            {
                Session["acc"] = user;
                string vaitro = user.PhanQuyen.VaiTro;
                if(vaitro == "Khách hàng")
                {
                    
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("Index", "NhanVien");
                }
            }
            return View();
        }
        public ActionResult LogOut()
        {
            Session["acc"] = null;
            return RedirectToAction("Index","Home");
        }
        public ActionResult Menu_Product()
        {
            List<LoaiSanPham> lst_type = data.LoaiSanPhams.ToList();
            return PartialView(lst_type);
        }
        public ActionResult Products(string id)
        {
            List<SanPham> danhSachSP = new List<SanPham>();
            if (id == null)
                danhSachSP = data.SanPhams.ToList();
            else
                danhSachSP = data.SanPhams.Where(t => t.MaLoaiSP == id).ToList();
            return View(danhSachSP);
        }
    }
}
