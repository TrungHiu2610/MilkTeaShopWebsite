using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls.WebParts;
using DoAn_LapTrinhWeb.Models;
using PagedList;

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
                GioHang cart = data.GioHangs.FirstOrDefault(x => x.MaNguoiDung == user.MaNguoiDung);
                if (cart != null)
                {
                    Session["gh"] = cart;
                }
                else
                {
                    Session["gh"] = null;
                }
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
            Session["gh"] = null;
            return RedirectToAction("Index", "Home");
        }
        public ActionResult Menu_Product()
        {
            List<LoaiSanPham> lst_type = data.LoaiSanPhams.ToList();
            return PartialView(lst_type);
        }
        public ActionResult Products(string id)
        {
            List<SanPham> danhSachSP;

            if (id == null)
            {
                danhSachSP = data.SanPhams
                    .GroupBy(t => t.MaSanPham)
                    .Select(g => g.FirstOrDefault())
                    .ToList();
            }
            else
            {
                danhSachSP = data.SanPhams
                    .Where(t => t.MaLoaiSP == id)
                    .GroupBy(t => t.MaSanPham)
                    .Select(g => g.FirstOrDefault())
                    .ToList();
            }

            ViewBag.MaLoaiSP = id;
            return View(danhSachSP);
        }

        private void capNhatTongTienGioHang(string maGH)
        {
            decimal tongTien = (decimal)data.ChiTietGioHangs.Where(x => x.MaGioHang == maGH).Sum(x => x.ThanhTien);
            GioHang gh = data.GioHangs.FirstOrDefault(x => x.MaGioHang == maGH);
            if (gh != null)
            {
                gh.TongTien = tongTien;
                data.SubmitChanges();
            }
        }

        public ActionResult AddToCart(string MaSP, int SoLuong, string MaLoaiSP, string maSize = null, string[] lstMaTopping = null)
        {
            int currentUserId;
            GioHang cart = null;
            NguoiDung user = Session["acc"] as NguoiDung;

            if (Session["gh"] == null)
            {
                if (user != null)
                {
                    currentUserId = user.MaNguoiDung;
                    cart = data.GioHangs.FirstOrDefault(t => t.MaNguoiDung == currentUserId);
                }
                else
                {
                    currentUserId = 1; // ID mặc định cho khách chưa đăng nhập
                    cart = data.GioHangs.FirstOrDefault(t => t.MaNguoiDung == currentUserId);

                    // Nếu có giỏ hàng tạm trong SQL nhưng Session["gh"] == null => Clear giỏ hàng tạm
                    if (cart != null)
                    {
                        data.GioHangs.DeleteOnSubmit(cart);
                        data.SubmitChanges();
                    }
                    cart = null;
                }

                if (cart == null)
                {
                    string sMaGH = "GH0001";
                    if (data.GioHangs.Any())
                    {
                        GioHang gh = data.GioHangs.OrderByDescending(t => t.MaGioHang).FirstOrDefault();
                        int iMaGH = int.Parse(gh.MaGioHang.Substring(2)) + 1;
                        sMaGH = "GH" + iMaGH.ToString("D3");
                    }
                    cart = new GioHang { MaGioHang = sMaGH, MaNguoiDung = currentUserId, NgayTao = DateTime.Now, TongTien = 0, TrangThai = "Đang xử lý" };
                    data.GioHangs.InsertOnSubmit(cart);
                    data.SubmitChanges();
                }
            }
            else
            {
                cart = Session["gh"] as GioHang;
            }

            if (maSize == null)
            {
                maSize = "SZ01";
            }

            ChiTietGioHang cartItem = data.ChiTietGioHangs.FirstOrDefault(t => t.MaGioHang == cart.MaGioHang && t.MaSanPham == MaSP && t.MaSize == maSize);
            decimal donGia = data.SanPhams.FirstOrDefault(t => t.MaSanPham == MaSP && t.MaSize == maSize)?.Gia ?? 0;
            decimal giaTopping = 0;
            if (cartItem == null)
            {
                cartItem = new ChiTietGioHang
                {
                    MaGioHang = cart.MaGioHang,
                    MaSanPham = MaSP,
                    SoLuong = SoLuong,
                    MaSize = maSize,
                    ThanhTien = (donGia + giaTopping) * SoLuong
                };
                data.ChiTietGioHangs.InsertOnSubmit(cartItem);
            }
            else
            {
                cartItem.SoLuong += SoLuong;
                cartItem.ThanhTien += (donGia + giaTopping) * SoLuong;
            }

            if (lstMaTopping != null && lstMaTopping.Any())
            {

                List<ToppingDonHang> lstTP = lstMaTopping.Select(maTP => new ToppingDonHang
                {
                    MaHoaDon = null,
                    MaGioHang = cart.MaGioHang,
                    MaSanPham = MaSP,
                    MaSize = maSize,
                    MaTopping = int.Parse(maTP)
                }).ToList();

                data.ToppingDonHangs.InsertAllOnSubmit(lstTP);
                data.SubmitChanges();
                giaTopping = data.ToppingDonHangs.Where(x => x.MaGioHang == cart.MaGioHang && x.MaSanPham == MaSP && x.MaSize == maSize).Sum(x => x.Topping.Gia);
                // cập nhật lại thành tiền
                cartItem = data.ChiTietGioHangs.FirstOrDefault(t => t.MaGioHang == cart.MaGioHang && t.MaSanPham == MaSP && t.MaSize == maSize);
                cartItem.ThanhTien = (donGia + giaTopping) * cartItem.SoLuong;
                data.SubmitChanges();
            }

            cart = data.GioHangs.FirstOrDefault(x => x.MaGioHang == cart.MaGioHang);

            Session["gh"] = cart;
            data.SubmitChanges();

            // Cập nhật tổng tiền
            capNhatTongTienGioHang(cart.MaGioHang);

            // Chuyển hướng đến trang Products với hoặc không có tham số MaLoaiSP
            if (string.IsNullOrEmpty(MaLoaiSP))
                return RedirectToAction("Products"); // Không có id => trang mặc định
            else
                return RedirectToAction("Products", new { id = MaLoaiSP.Trim() }); // Chuyển hướng đến danh mục đã chọn
        }

        [HttpPost]
        public ActionResult UpdateCart(string maSP, string maSize, string maGH, int soLuong, decimal giaTopping)
        {
            GioHang gh = Session["gh"] as GioHang;
            if (gh == null)
            {
                gh = new GioHang();
            }

            ChiTietGioHang ctgh = data.ChiTietGioHangs.FirstOrDefault(x => x.MaSanPham == maSP && x.MaSize == maSize && x.MaGioHang == maGH);
            if (ctgh != null)
            {
                if (soLuong > 0)
                {
                    ctgh.SoLuong = soLuong;
                    ctgh.ThanhTien = (ctgh.SanPham.Gia+giaTopping)*soLuong;
                }
                else
                {
                    data.ChiTietGioHangs.DeleteOnSubmit(ctgh);
                    data.SubmitChanges();
                    GioHang ghToDel = data.GioHangs.FirstOrDefault(x=>x.MaGioHang==maGH);
                    if(ghToDel.ChiTietGioHangs.ToList().Count>0)
                    {
                        data.GioHangs.DeleteOnSubmit(ghToDel);
                    }    
                }
                data.SubmitChanges();
            }

            gh = data.GioHangs.FirstOrDefault(x => x.MaGioHang == gh.MaGioHang);

            Session["gh"] = gh;

            return RedirectToAction("ViewCart");
        }

        public ActionResult ViewCart()
        {
            GioHang cart = Session["gh"] as GioHang;
            if (cart == null)
            {
                return RedirectToAction("Products");
            }
            List<ChiTietGioHang> cartItems = data.ChiTietGioHangs.Where(t => t.MaGioHang == cart.MaGioHang).ToList();
            ViewBag.lstTopping = data.ToppingDonHangs.ToList();

            return View(cartItems);
        }

        private double layGiaThem(string maSP, string maSize)
        {
            decimal giaMacDinh = data.SanPhams.FirstOrDefault(x => x.MaSanPham == maSP && x.MaSize == "SZ01").Gia;
            decimal giaThem = data.SanPhams.FirstOrDefault(x => x.MaSanPham == maSP && x.MaSize == maSize).Gia - giaMacDinh;
            return double.Parse(giaThem.ToString());
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

            List<ThongTinSize> lst_size = data.SanPhams
                            .Where(t => t.MaSanPham == id)
                            .Select(x => new ThongTinSize
                            {
                                MaSize = x.MaSize,
                                TenSize = x.Size.TenSize,
                                GiaThem = layGiaThem(x.MaSanPham, x.MaSize)
                            })
                            .ToList();
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
            List<ThongTinSize> lst_size = data.SanPhams
                            .Where(t => t.MaSanPham == idSanPham)
                            .Select(x => new ThongTinSize
                            {
                                MaSize = x.MaSize,
                                TenSize = x.Size.TenSize,
                                GiaThem = layGiaThem(x.MaSanPham, x.MaSize)
                            })
                            .ToList();
            return PartialView(lst_size);
        }
        public ActionResult CheckOut()
        {
            return View();
        }

        public ActionResult HistoryOrders(int? id, int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            List<HoaDonBanHang> lstHD = data.HoaDonBanHangs.Where(x => x.MaKH == id).ToList();
            ViewBag.lstCTHD = data.ChiTietHoaDonBanHangs.ToList();

            var lstPage = lstHD.ToPagedList(pageNumber, pageSize);
            return View(lstPage);
        }

        public ActionResult OrderDetail(string id)
        {
            HoaDonBanHang hd = data.HoaDonBanHangs.FirstOrDefault(x => x.MaHoaDon == id);
            ViewBag.lstCTHD = data.ChiTietHoaDonBanHangs.Where(x => x.MaHoaDon == id).ToList();
            ViewBag.lstTopping = data.ToppingDonHangs.ToList();
            // tổng đơn hàng ko tính giảm giá, phí vận chuyển
            ViewBag.tongDonHang = data.ChiTietHoaDonBanHangs.Where(x => x.MaHoaDon == id).Sum(x => x.Gia * x.SoLuong);

            if (data.ToppingDonHangs.Where(x => x.MaHoaDon == id).ToList().Count > 0)
            {
                ViewBag.tongDonHang += data.ToppingDonHangs.Where(x => x.MaHoaDon == id).Sum(x => x.Topping.Gia);
            }

            return View(hd);
        }
    }
}
