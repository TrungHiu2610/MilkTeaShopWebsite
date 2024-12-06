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
            if (user != null)
            {
                Session["acc"] = user;
                string vaitro = user.PhanQuyen.VaiTro;
                if (vaitro == "Khách hàng")
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
            return RedirectToAction("Index", "Home");
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

            ViewBag.MaLoaiSP = id;
            return View(danhSachSP);
        }
        public ActionResult AddToCart(string MaSP, int SoLuong, string MaLoaiSP)
        {
            int currentUserId;
            GioHang cart = Session["gh"] as GioHang;
            NguoiDung user = Session["acc"] as NguoiDung;

            if (cart == null)
            {
                Session["gh"] = new GioHang();
            }
            if (user != null)
            {
                currentUserId = user.MaNguoiDung;
                cart = data.GioHangs.FirstOrDefault(t => t.MaNguoiDung == currentUserId);
            }
            else // nếu chưa đăng nhập thì gán id user bằng 1
            {
                currentUserId = 1;
                cart = data.GioHangs.FirstOrDefault(t => t.MaNguoiDung == currentUserId);
            }

            if (cart == null)
            {
                string sMaGH = "GH0001";
                if (data.GioHangs.ToList().Count > 0)
                {
                    GioHang gh = data.GioHangs.OrderByDescending(t => t.MaGioHang).FirstOrDefault();
                    int iMaGH = int.Parse(gh.MaGioHang.Substring(2)) + 1;
                    sMaGH = "GH" + iMaGH.ToString("D3");
                }
                cart = new GioHang { MaGioHang = sMaGH, MaNguoiDung = currentUserId, NgayTao = DateTime.Now, TongTien = 0, TrangThai = "Đang xử lý" };
                data.GioHangs.InsertOnSubmit(cart);
                data.SubmitChanges();
            }

            ChiTietGioHang cartItem = data.ChiTietGioHangs.FirstOrDefault(t => t.MaGioHang == cart.MaGioHang && t.MaSanPham == MaSP.ToString());
            if (cartItem == null) // nếu sp chưa có trong giỏ hàng thì thêm sp
            {   //Gán size và topping mặc định trước gòi mốt thiết kế vụ chọn size, chọn topping sau heh
                decimal donGia = data.SanPhams.FirstOrDefault(t => t.MaSanPham == MaSP).Gia;
                decimal? giaSize = data.QuanLySP_Sizes.FirstOrDefault(t => t.MaSanPham == MaSP && t.MaSize == "SZ01").GiaThem;
                decimal giaTopping = data.Toppings.FirstOrDefault(t => t.MaTopping == 1).Gia;
                cartItem = new ChiTietGioHang { MaGioHang = cart.MaGioHang, MaSanPham = MaSP.ToString(), SoLuong = SoLuong, MaSize = "SZ01", MaTopping = 0, ThanhTien = donGia + giaSize + giaTopping }; //Cho topping mặc định là 0, để trống ý
                data.ChiTietGioHangs.InsertOnSubmit(cartItem);
                data.SubmitChanges();
            }
            else // có ròi thì tăng hoặc giảm sl
            {
                decimal donGia = data.SanPhams.FirstOrDefault(t => t.MaSanPham == cartItem.MaSanPham).Gia;
                decimal? giaSize = data.QuanLySP_Sizes.FirstOrDefault(t => t.MaSanPham == cartItem.MaSanPham && t.MaSize == "SZ01").GiaThem;
                decimal giaTopping = data.Toppings.FirstOrDefault(t => t.MaTopping == 1).Gia;
                cartItem.SoLuong += SoLuong;
                cartItem.ThanhTien += (donGia + giaSize + giaTopping) * SoLuong;
            }

            Session["gh"] = cart;
            data.SubmitChanges();

            // Chuyển hướng đến trang Products với hoặc không có tham số MaLoaiSP
            if (string.IsNullOrEmpty(MaLoaiSP))
                return RedirectToAction("Products"); // Không có id => trang mặc định
            else
                return RedirectToAction("Products", new { id = MaLoaiSP }); // Chuyển hướng đến danh mục đã chọn
        }

        public ActionResult ViewCart()
        {
            GioHang cart = Session["gh"] as GioHang;
            if (cart == null)
            {
                return RedirectToAction("Products");
            }
            List<ChiTietGioHang> cartItems = data.ChiTietGioHangs.Where(t => t.MaGioHang == cart.MaGioHang).ToList();
            return View(cartItems);
        }
       
        public ActionResult ProductDetails(string id)
        {
            SanPham sanPham = new SanPham();
            if (id == null)
                return RedirectToAction("NotFound", "Error");
            else
                sanPham = data.SanPhams.FirstOrDefault(t => t.MaSanPham == id);

            List<Topping> lst_topping = data.Toppings.ToList();
            ViewBag.lst_topping = lst_topping;

            List<QuanLySP_Size> lst_size = data.QuanLySP_Sizes.Where(t => t.MaSanPham == id).ToList();
            ViewBag.lst_size = lst_size;
            return View(sanPham);
        }
        // action lấy loại topping
        public ActionResult GetTopping()
        {
            List<Topping> lst_topping = data.Toppings.ToList();
            return PartialView(lst_topping);
        }
        public ActionResult GetSize(string idSanPham)
        {
            List<QuanLySP_Size> lst_size = data.QuanLySP_Sizes.Where(t => t.MaSanPham == idSanPham).ToList();
            return PartialView(lst_size);
        }
        public ActionResult CheckOut()
        {
            return View();
        }
    }
}
