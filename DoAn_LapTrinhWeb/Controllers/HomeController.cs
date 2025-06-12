using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls.WebParts;
using DoAn_LapTrinhWeb.Models;
using PagedList;
using static System.Collections.Specialized.BitVector32;

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

        #region Register - Login - Account Info
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = data.NguoiDungs.FirstOrDefault(u => u.SoDienThoai == model.PhoneNumber);
            if (existingUser != null)
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại đã tồn tại.");
                return View(model);
            }

            NguoiDung newUser = new NguoiDung
            {
                HoTen = model.CustomerName,
                SoDienThoai = model.PhoneNumber,
                MatKhau = model.Password,
                MaQuyen = "KH"
            };

            data.NguoiDungs.InsertOnSubmit(newUser);
            data.SubmitChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return Redirect(Url.Action("LogIn", "Home"));
        }

        public ActionResult LogIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogIn(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            NguoiDung user = data.NguoiDungs.FirstOrDefault
                (t => t.SoDienThoai == model.PhoneNumber && t.MatKhau == model.Password);

            if (user != null)
            {
                var sessionCart = Session["cartSession"] as GioHang;

                GioHang dbCart = data.GioHangs.FirstOrDefault(x => x.MaNguoiDung == user.MaNguoiDung);
                if (sessionCart != null)
                {
                    if (dbCart != null)
                    {
                        var dbItems = data.ChiTietGioHangs.Where(c => c.MaGioHang == dbCart.MaGioHang).ToList();
                        // nếu đã có giỏ hàng trong db thì gộp các sản phẩm của session với db luôn
                        if (dbItems.Count > 0)
                        {
                            // Xóa chi tiết giỏ hàng cũ
                            data.ChiTietGioHangs.DeleteAllOnSubmit(dbItems);
                            data.SubmitChanges();
                        }

                        // Gán lại các item từ session vào dbCart
                        foreach (var item in sessionCart.ChiTietGioHangs)
                        {
                            var newItem = new ChiTietGioHang
                            {
                                MaGioHang = dbCart.MaGioHang,
                                MaSanPham = item.MaSanPham,
                                MaSize = item.MaSize,
                                SoLuong = item.SoLuong,
                                ThanhTien = item.ThanhTien,
                            };
                            data.ChiTietGioHangs.InsertOnSubmit(newItem);
                            data.SubmitChanges();

                            // Xử lý topping nếu có
                            foreach (var topping in item.ToppingDonHangs)
                            {
                                var newTopping = new ToppingDonHang
                                {
                                    MaTopping = topping.MaTopping,
                                    MaGioHang = newItem.MaGioHang,
                                    MaHoaDon = null,
                                    MaSize = newItem.MaSize,
                                    MaSanPham = newItem.MaSanPham,
                                };

                                data.ToppingDonHangs.InsertOnSubmit(newTopping);
                            }

                            data.SubmitChanges();
                        }

                        GioHang dbCurrentCart = data.GioHangs.FirstOrDefault(x => x.MaNguoiDung == user.MaNguoiDung);
                        dbCurrentCart.TongTien = dbCurrentCart.ChiTietGioHangs.Sum(x => x.ThanhTien);

                        Session["gh"] = dbCurrentCart;
                    }
                    else
                    {
                        // Không có giỏ hàng trong DB, gán sessionCart cho user
                        sessionCart.MaNguoiDung = user.MaNguoiDung;
                        GioHang dbCartAdmin = data.GioHangs.FirstOrDefault(x => x.MaNguoiDung == 1);
                        dbCartAdmin.MaNguoiDung = user.MaNguoiDung;
                        data.SubmitChanges();
                        Session["gh"] = sessionCart;
                    }
                }
                else
                {
                    // Không có session, load từ DB
                    Session["gh"] = dbCart;
                }
                Session["acc"] = user;
                string vaitro = user.PhanQuyen.VaiTro;

                // gán role vào cookie
                var ticket = new FormsAuthenticationTicket(
                                1,
                                user.HoTen,
                                DateTime.Now,
                                DateTime.Now.AddMinutes(30),
                                false,
                                user.PhanQuyen.MaQuyen.Trim()
                                );
                string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                Response.Cookies.Add(authCookie);

                var success = TempData["SuccessMessage"] as string;

                if (!string.IsNullOrEmpty(success))
                {
                    TempData.Remove("SuccessMessage");
                }

                // Kiểm tra nếu có URL cần quay lại
                if (TempData["ReturnUrl"] != null)
                {
                    string returnUrl = TempData["ReturnUrl"].ToString();
                    return Redirect(returnUrl);
                }

                if (vaitro == "Khách hàng")
                {

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("Index", "NhanVien");
                }
            }
            TempData["ErrorMessage"] = "Sai tên đăng nhập hoặc mật khẩu";
            return View();
        }
        public ActionResult LogOut()
        {
            Session["acc"] = null;
            Session["gh"] = null;
            Session["cartSession"] = null;
            return RedirectToAction("Index", "Home");
        }

        public ActionResult AccountInfo()
        {
            TempData["SuccessMessage"] = null;
            NguoiDung user = Session["acc"] as NguoiDung;
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateAccountInfo(string CustomerName, string Address, string PhoneNumber)
        {
            var currentUser = Session["acc"] as NguoiDung;
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            NguoiDung user = data.NguoiDungs.FirstOrDefault(u => u.MaNguoiDung == currentUser.MaNguoiDung);
            if (user != null)
            {
                user.HoTen = CustomerName;
                user.DiaChi = Address;
                user.SoDienThoai = PhoneNumber;

                // Sử dụng phương thức SubmitChanges()

                data.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, user); // Đảm bảo trạng thái đúng
                data.SubmitChanges(); // Lưu thay đổi

                // Cập nhật thông tin trong session
                Session["acc"] = user;
                TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng!";
            }
            return RedirectToAction("AccountInfo", "Home");
        }

        // Đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string OldPassword, string NewPassword)
        {
            var currentUser = Session["acc"] as NguoiDung;
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Tìm người dùng hiện tại trong cơ sở dữ liệu
            var user = data.NguoiDungs.FirstOrDefault(u => u.MaNguoiDung == currentUser.MaNguoiDung);
            if (user != null)
            {
                // Loại bỏ khoảng trắng thừa từ mật khẩu trong cơ sở dữ liệu
                string dbPassword = user.MatKhau;
                if (dbPassword != null)
                {
                    dbPassword = dbPassword.Trim();  // Loại bỏ khoảng trắng trong mật khẩu lưu trữ
                }

                // Loại bỏ khoảng trắng thừa từ mật khẩu cũ người dùng nhập
                if (OldPassword != null)
                {
                    OldPassword = OldPassword.Trim();  // Loại bỏ khoảng trắng từ mật khẩu cũ nhập vào
                }

                // So sánh mật khẩu cũ sau khi đã loại bỏ khoảng trắng
                if (dbPassword == OldPassword)
                {
                    // Loại bỏ khoảng trắng thừa từ mật khẩu mới người dùng nhập
                    if (NewPassword != null)
                    {
                        NewPassword = NewPassword.Trim();  // Loại bỏ khoảng trắng từ mật khẩu mới nhập vào
                    }

                    // Cập nhật mật khẩu mới vào cơ sở dữ liệu
                    user.MatKhau = NewPassword;

                    // Đồng bộ hóa trạng thái và lưu thay đổi vào cơ sở dữ liệu
                    data.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, user);
                    data.SubmitChanges();

                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Mật khẩu cũ không đúng!";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng!";
            }

            return RedirectToAction("AccountInfo", "Home");
        }



        #endregion

        #region Products
        public ActionResult Menu_Product()
        {
            List<LoaiSanPham> lst_type = data.LoaiSanPhams.ToList();
            return PartialView(lst_type);
        }
        public ActionResult Products(string maLoai, string keyword, string sortType, int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            var danhSachSP = data.SanPhams
                    .GroupBy(t => t.MaSanPham)
                    .Select(g => g.FirstOrDefault());

            if (!string.IsNullOrEmpty(maLoai))
            {
                danhSachSP = danhSachSP.Where(t => t.MaLoaiSP == maLoai);
            }

            // Khi user click tìm kiếm mà bỏ trống thì mới hiện lỗi

            bool isSearchSubmit = Request.HttpMethod == "GET" && Request.QueryString["keyword"] != null;

            if (string.IsNullOrEmpty(keyword) && isSearchSubmit)
            {
                TempData["ErrorMessage"] = "Phải nhập từ khóa tìm kiếm";
            }

            else if(!string.IsNullOrEmpty(keyword))
            {
                danhSachSP = danhSachSP.Where(t => t.TenSanPham.Contains(keyword.ToLower()));
            }

            if (!string.IsNullOrEmpty(sortType))
            {
                switch (sortType)
                {
                    case "MostPopular":
                        {
                            danhSachSP = danhSachSP.OrderByDescending(sp => sp.ChiTietHoaDonBanHangs.Count(ct => ct.MaSanPham == sp.MaSanPham));
                        }
                        break;
                    case "AscPrice":
                        {
                            danhSachSP = danhSachSP.OrderBy(sp => sp.Gia);
                        }
                        break;
                    case "DescPrice":
                        {
                            danhSachSP = danhSachSP.OrderByDescending(sp => sp.Gia);
                        }
                        break;
                }
            }

            ViewBag.MaLoaiSP = maLoai;
            var dssp = danhSachSP.ToList().ToPagedList(pageNumber,pageSize);
            return View(dssp);
        }


        public ActionResult ProductDetails(string id)
        {
            SanPham sanPham = new SanPham();
            if (id == null)
                return RedirectToAction("NotFound", "Error");
            else
                sanPham = data.SanPhams.FirstOrDefault(t => t.MaSanPham == id);

            if(sanPham==null)
            {
                return RedirectToAction("NotFound", "Error");
            }

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
            List<SanPham> spcl = data.SanPhams.Where(t => t.MaLoaiSP == sanPham.MaLoaiSP && t.MaSanPham != sanPham.MaSanPham).GroupBy(x => x.MaSanPham).Select(x => x.FirstOrDefault()).Take(4).ToList();
            ViewBag.SanPhamCungLoai = spcl;
            return View(sanPham);
        }
        #endregion

        #region Checkout - Discount
        public ActionResult CheckOut()
        {
            GioHang cart = Session["gh"] as GioHang;
            NguoiDung user = Session["acc"] as NguoiDung;
            if (cart == null)
            {
                return RedirectToAction("Products");
            }
            if (user == null)
            {
                TempData["ReturnUrl"] = Url.Action("CheckOut", "Home");
                return RedirectToAction("LogIn");
            }
            List<ChiTietGioHang> cartItems = data.ChiTietGioHangs.Where(t => t.MaGioHang == cart.MaGioHang).ToList();
            return View(cartItems);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckOut(FormCollection form)
        {
            try
            {
                // Lấy thông tin giỏ hàng từ session
                GioHang cart = Session["gh"] as GioHang;
                NguoiDung user = Session["acc"] as NguoiDung;

                // Kiểm tra nếu giỏ hàng trống
                if (cart == null || cart.ChiTietGioHangs == null || cart.ChiTietGioHangs.Count == 0)
                {
                    TempData["Message"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("ViewCart");
                }

                // Xác định mã người dùng hiện tại
                int currentUserId = (user != null) ? user.MaNguoiDung : 1; // 1: mã người dùng mặc định cho khách chưa đăng nhập

                // Lấy thông tin từ form
                string hoTenKH = form["CustomerName"];
                string diaChiGiaoHang = form["Address"] + ", " + form["District"] + ", " + form["City"]; // Kết hợp Địa chỉ, Quận, Tỉnh
                string sdtGiaoHang = form["PhoneNumber"];

                // Tạo mã hóa đơn mới
                string sMaHD = "HD0001";
                if (data.HoaDonBanHangs.Any())
                {
                    HoaDonBanHang lastHD = data.HoaDonBanHangs.OrderByDescending(t => t.MaHoaDon).FirstOrDefault();
                    int iMaHD = int.Parse(lastHD.MaHoaDon.Substring(2)) + 1;
                    sMaHD = "HD" + iMaHD.ToString("D3");
                }

                // Tạo đối tượng hóa đơn bán hàng
                HoaDonBanHang hoaDon = new HoaDonBanHang
                {
                    MaHoaDon = sMaHD,
                    MaKH = currentUserId,
                    NgayLap = DateTime.Now,
                    TongTien = (decimal)cart.TongTien, // Tính tổng tiền từ chi tiết giỏ hàng
                    TrangThai = "Chờ xác nhận",
                    PhuongThucThanhToan = form["paymentMethod"],
                    DiaChiGiaoHang = diaChiGiaoHang, // Địa chỉ giao hàng đầy đủ
                    HoTenKH = hoTenKH, // Họ tên khách hàng
                    SdtGiaoHang = sdtGiaoHang, // Số điện thoại giao hàng
                    MaGG = cart.MaGG
                };

                // Thêm hóa đơn vào cơ sở dữ liệu
                data.HoaDonBanHangs.InsertOnSubmit(hoaDon);

                // Thêm chi tiết hóa đơn bán hàng
                foreach (var item in cart.ChiTietGioHangs)
                {
                    SanPham sp = data.SanPhams.FirstOrDefault(x => x.MaSanPham == item.MaSanPham && x.MaSize == item.MaSize);
                    ChiTietHoaDonBanHang chiTietHD = new ChiTietHoaDonBanHang
                    {
                        MaHoaDon = sMaHD,
                        MaSanPham = item.MaSanPham,
                        MaSize = item.MaSize,
                        SoLuong = item.SoLuong,
                        Gia = sp.Gia
                        //Thêm thông tin còn thiếu
                    };

                    // cập nhật ToppingDonHang => chuyển từ giỏ hàng sang hóa đơn
                    List<ToppingDonHang> lstTp = data.ToppingDonHangs.Where(x => x.MaSanPham == item.MaSanPham && x.MaSize == item.MaSize && x.MaGioHang == item.MaGioHang).ToList();
                    foreach (ToppingDonHang tpdh in lstTp)
                    {
                        if (tpdh != null)
                        {
                            tpdh.MaGioHang = null;
                            tpdh.MaHoaDon = chiTietHD.MaHoaDon;
                        }
                    }

                    // Thêm chi tiết hóa đơn vào cơ sở dữ liệu
                    data.ChiTietHoaDonBanHangs.InsertOnSubmit(chiTietHD);
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                data.SubmitChanges();

                // Xóa giỏ hàng của người dùng hiện tại sau khi thanh toán thành công

                var cartToDelete = data.GioHangs.Where(t => t.MaNguoiDung == currentUserId).FirstOrDefault();
                if (cartToDelete != null)
                {
                    // Xóa giỏ hàng có mã người dùng tương ứng và trạng thái là "Đang xử lý"
                    data.GioHangs.DeleteOnSubmit(cartToDelete);
                    data.SubmitChanges();
                }

                // Xóa giỏ hàng khỏi session
                Session["gh"] = null;

                // Thông báo thành công
                TempData["SuccessMessage"] = "Đặt hàng thành công! Chúng tôi sẽ liên hệ bạn sớm nhất.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình thanh toán. Vui lòng thử lại sau.";
                // Có thể thêm ghi log lỗi: Log.Error(ex);
                return RedirectToAction("ViewCart");
            }
        }

        [HttpPost]
        public ActionResult ApplyDiscount(string maGH, string maGG)
        {
            // kiểm tra mã giảm giá hợp lệ ko
            GiamGia gg = data.GiamGias.FirstOrDefault(x => x.MaGG == maGG);
            if (gg == null)
            {
                TempData["Message"] = "Mã giảm giá không hợp lệ";
                return RedirectToAction("ViewCart");
            }
            else
            {
                if (gg.NgayBD <= DateTime.Now && gg.NgayKT >= DateTime.Now) // hợp lệ
                {
                    // kiểm tra xem đã có giảm giá hay chưa
                    GioHang gh = data.GioHangs.FirstOrDefault(x => x.MaGioHang == maGH);
                    if (gh.MaGG != null)
                    {
                        TempData["Message"] = "Bạn đã áp mã giảm giá rồi";
                        return RedirectToAction("ViewCart");
                    }

                    // cập nhật giỏ hàng
                    gh.MaGG = maGG;
                    gh.TongTien = gh.TongTien - (gh.TongTien * (gg.PhanTramGiam / 100));
                    data.SubmitChanges();
                    TempData["Message"] = "Áp dụng mã giảm giá thành công";
                    Session["gh"] = gh;
                    return RedirectToAction("ViewCart");
                }
                else
                {
                    TempData["Message"] = "Mã giảm giá đã hết hạn";
                    return RedirectToAction("ViewCart");
                }
            }
        }
        #endregion

        #region Cart
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
                // update lại ToppingDonHang

                List<ToppingDonHang> lstTP = lstMaTopping.Select(maTP => new ToppingDonHang
                {
                    MaHoaDon = null,
                    MaGioHang = cart.MaGioHang,
                    MaSanPham = MaSP,
                    MaSize = maSize,
                    MaTopping = int.Parse(maTP)
                }).ToList();
                // thêm soLuong dòng mới ToppingDonHang
                for (int i = 0; i < SoLuong; i++)
                {
                    foreach (var tp in lstTP)
                    {
                        ToppingDonHang newTp = new ToppingDonHang
                        {
                            MaGioHang = tp.MaGioHang,
                            MaSanPham = tp.MaSanPham,
                            MaSize = tp.MaSize,
                            MaTopping = tp.MaTopping
                        };
                        data.ToppingDonHangs.InsertOnSubmit(newTp);
                    }
                }
                data.SubmitChanges();


                giaTopping = data.ToppingDonHangs.Where(x => x.MaGioHang == cart.MaGioHang && x.MaSanPham == MaSP && x.MaSize == maSize).Sum(x => x.Topping.Gia);
                // cập nhật lại thành tiền
                cartItem = data.ChiTietGioHangs.FirstOrDefault(t => t.MaGioHang == cart.MaGioHang && t.MaSanPham == MaSP && t.MaSize == maSize);
                cartItem.ThanhTien = (donGia * cartItem.SoLuong) + giaTopping;
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

        List<ToppingDonHang> layDanhSachToppingKoTrung(List<ToppingDonHang> lstTp)
        {
            if (lstTp == null || lstTp.Count == 0)
                return null;

            var lstKoTrung = lstTp
                .GroupBy(tp => new { tp.MaGioHang, tp.MaSanPham, tp.MaSize, tp.MaTopping })
                .Select(group => group.First())
                .ToList();

            return lstKoTrung;
        }

        decimal layGiaTopping(string maSP, string maSize, string maGH)
        {
            List<ToppingDonHang> lstTp = data.ToppingDonHangs.Where(x => x.MaSanPham == maSP && x.MaSize == maSize && x.MaGioHang == maGH).ToList();
            if (lstTp.Count > 0)
            {
                return lstTp.Sum(x => x.Topping.Gia);
            }
            return 0;
        }

        [HttpPost]
        public ActionResult UpdateCart(string maSP, string maSize, string maGH, int soLuong)
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
                    // update lại ToppingDonHang
                    // xóa các ToppingDonHang cũ
                    List<ToppingDonHang> lstTp = data.ToppingDonHangs.Where(x => x.MaSanPham == maSP && x.MaSize == maSize && x.MaGioHang == maGH).ToList();
                    if (lstTp.Count > 0)
                    {
                        List<ToppingDonHang> lstKoTrung = layDanhSachToppingKoTrung(lstTp);
                        List<ToppingDonHang> lstTpToAdd = lstKoTrung.Select(x => new ToppingDonHang
                        {
                            MaGioHang = x.MaGioHang,
                            MaSanPham = x.MaSanPham,
                            MaSize = x.MaSize,
                            MaTopping = x.MaTopping
                        }).ToList();

                        data.ToppingDonHangs.DeleteAllOnSubmit(lstTp);

                        // thêm soLuong dòng mới ToppingDonHang
                        for (int i = 0; i < soLuong; i++)
                        {
                            foreach (var tp in lstTpToAdd)
                            {
                                ToppingDonHang newTp = new ToppingDonHang
                                {
                                    MaGioHang = tp.MaGioHang,
                                    MaSanPham = tp.MaSanPham,
                                    MaSize = tp.MaSize,
                                    MaTopping = tp.MaTopping
                                };
                                data.ToppingDonHangs.InsertOnSubmit(newTp);
                            }
                        }
                        data.SubmitChanges();
                    }

                    decimal giaTopping = layGiaTopping(maSP, maSize, maGH);
                    ctgh.SoLuong = soLuong;
                    ctgh.ThanhTien = (ctgh.SanPham.Gia * soLuong) + giaTopping;

                    //data.SubmitChanges();
                }
                else
                {
                    data.ChiTietGioHangs.DeleteOnSubmit(ctgh);
                    data.SubmitChanges();
                    GioHang ghToDel = data.GioHangs.FirstOrDefault(x => x.MaGioHang == maGH);
                    if (ghToDel.ChiTietGioHangs.ToList().Count == 0)
                    {
                        data.GioHangs.DeleteOnSubmit(ghToDel);
                    }
                }
                data.SubmitChanges();
            }

            gh = data.GioHangs.FirstOrDefault(x => x.MaGioHang == gh.MaGioHang);

            Session["gh"] = gh;

            // Cập nhật tổng tiền
            if (gh != null)
                capNhatTongTienGioHang(gh.MaGioHang);

            return RedirectToAction("ViewCart");
        }

        public ActionResult ViewCart()
        {
            GioHang cart = Session["gh"] as GioHang;

            if (cart == null)
            {
                return RedirectToAction("Products");
            }

            if (cart.ChiTietGioHangs == null || cart.ChiTietGioHangs.Count == 0)
            {
                return RedirectToAction("Products");
            }
            List<ChiTietGioHang> cartItems = data.ChiTietGioHangs.Where(t => t.MaGioHang == cart.MaGioHang).ToList();
            ViewBag.lstTopping = data.ToppingDonHangs.ToList();
            // tổng đơn hàng ko tính giảm giá, phí vận chuyển
            ViewBag.tongDonHang = data.ChiTietGioHangs.Where(x => x.MaGioHang == cart.MaGioHang).Sum(x => x.ThanhTien);

            if (data.ToppingDonHangs.Where(x => x.MaGioHang == cart.MaGioHang).ToList().Count > 0)
            {
                ViewBag.tongDonHang += data.ToppingDonHangs.Where(x => x.MaGioHang == cart.MaGioHang).Sum(x => x.Topping.Gia);
            }
            return View(cartItems);
        }

        private double layGiaThem(string maSP, string maSize)
        {
            decimal giaMacDinh = data.SanPhams.FirstOrDefault(x => x.MaSanPham == maSP && x.MaSize == "SZ01").Gia;
            decimal giaThem = data.SanPhams.FirstOrDefault(x => x.MaSanPham == maSP && x.MaSize == maSize).Gia - giaMacDinh;
            return double.Parse(giaThem.ToString());
        }
        #endregion

        #region Order
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

        public ActionResult ConfirmOrder(string maHD)
        {
            NguoiDung user = Session["acc"] as NguoiDung;
            HoaDonBanHang hd = data.HoaDonBanHangs.FirstOrDefault(x => x.MaHoaDon == maHD);
            if (hd != null)
            {
                hd.TrangThai = "Đã hoàn thành";
                TempData["SuccessMessage"] = "Xin cảm ơn quí khách";
                data.SubmitChanges();
            }
            return RedirectToAction("OrderDetail", "Home", new { id = maHD });
        }
        #endregion
    }
}
