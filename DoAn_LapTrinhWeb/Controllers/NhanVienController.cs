using DoAn_LapTrinhWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Web.UI;
namespace DoAn_LapTrinhWeb.Controllers
{
    public class NhanVienController : Controller
    {
        DB_QL_CuaHangTraSuaDataContext data = new DB_QL_CuaHangTraSuaDataContext();
        //
        // GET: /NhanVien/

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Products(int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            List<SanPham> lstSP = data.SanPhams
                    .GroupBy(t => t.MaSanPham)
                    .Select(g => g.FirstOrDefault())
                    .ToList();

            var lstPage = lstSP.ToPagedList(pageNumber, pageSize);

            List<LoaiSanPham> lstLoai = data.LoaiSanPhams.ToList();

            ViewBag.DanhMucLoai = new SelectList(lstLoai, "MaLoaiSP", "TenLoaiSP");

            return View(lstPage);
        }

        [HttpPost]
        public ActionResult Products(string maSP, string tenSP, string maLoai, int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            List<SanPham> lstSP = data.SanPhams
                    .GroupBy(t => t.MaSanPham)
                    .Select(g => g.FirstOrDefault())
                    .ToList();


            if (!string.IsNullOrEmpty(maSP))
            {
                lstSP = lstSP.Where(x => x.MaSanPham.Contains(maSP)).ToList();
            }

            if (!string.IsNullOrEmpty(tenSP))
            {
                lstSP = lstSP.Where(x => x.TenSanPham.Contains(tenSP)).ToList();
            }

            if (!string.IsNullOrEmpty(maLoai))
            {
                lstSP = lstSP.Where(x => x.MaLoaiSP.Contains(maLoai)).ToList();
            }

            List<LoaiSanPham> lstLoai = data.LoaiSanPhams.ToList();

            ViewBag.DanhMucLoai = new SelectList(lstLoai, "MaLoaiSP", "TenLoaiSP");

            var lstPage = lstSP.ToPagedList(pageNumber, pageSize);

            return View(lstPage);
        }

        [HttpPost]
        public ActionResult HandleAction(string action, string[] selectedProducts)
        {
            if (!action.Contains("Add") && (selectedProducts == null || selectedProducts.Length == 0))
            {
                TempData["Message"] = "Không có sản phẩm nào được chọn.";
                return RedirectToAction("Products");
            }
            switch (action)
            {
                case "AddByView":
                    return RedirectToAction("AddProduct");
                case "AddByExcelFile":
                    return RedirectToAction("Products");

                case "Edit":
                    // chuyển hướng sang trang sửa sản phẩm với danh sách sản phẩm được chọn
                    return RedirectToAction("EditProduct", new { selectedProducts = string.Join(",", selectedProducts) });

                case "Delete":
                    return RedirectToAction("ConfirmDelete", new { selectedProducts = string.Join(",", selectedProducts) });

                case "InventoryCheck":
                    TempData["Message"] = "Kiểm kho các sản phẩm thành công.";
                    break;

                default:
                    TempData["Message"] = "Hành động không hợp lệ.";
                    break;
            }


            return RedirectToAction("Products");
        }
        // Trang xác nhận xóa
        [HttpGet]
        public ActionResult ConfirmDelete(string selectedProducts)
        {
            if (string.IsNullOrEmpty(selectedProducts))
            {
                TempData["Message"] = "Không có sản phẩm nào được chọn.";
                return RedirectToAction("Products");
            }

            // Lấy danh sách sản phẩm cần xóa
            var ids = selectedProducts.Split(',');
            var productsToDelete = data.SanPhams.Where(p => ids.Contains(p.MaSanPham)).GroupBy(x => x.MaSanPham).Select(x => x.FirstOrDefault()).ToList();

            return View(productsToDelete);
        }

        // xóa khi người dùng xác nhận
        [HttpPost]
        public ActionResult ConfirmDelete(string[] selectedProducts)
        {
            if (selectedProducts == null || selectedProducts.Length == 0)
            {
                TempData["Message"] = "Không có sản phẩm nào được chọn để xóa.";
                return RedirectToAction("Products");
            }

            foreach (var id in selectedProducts)
            {
                var lstProducts = data.SanPhams.Where(p => p.MaSanPham == id).ToList();
                if (lstProducts.Count > 0)
                {
                    foreach (SanPham sp in lstProducts)
                        data.SanPhams.DeleteOnSubmit(sp);
                }
            }
            data.SubmitChanges();

            TempData["Message"] = "Xóa sản phẩm thành công.";
            return RedirectToAction("Products");
        }


        [HttpGet]
        public ActionResult AddProduct()
        {
            List<LoaiSanPham> lstLoai = data.LoaiSanPhams.ToList();
            ViewBag.DanhMucLoai = new SelectList(lstLoai, "MaLoaiSP", "TenLoaiSP");
            List<Size> lstSize = data.Sizes.ToList();
            ViewBag.lstSize = lstSize;
            return View();
        }

        [HttpPost]
        public ActionResult AddProduct(SanPham sp, string action, HttpPostedFileBase fileProductImg, string[] SelectedSizes, FormCollection fc)
        {
            List<LoaiSanPham> lstLoai = data.LoaiSanPhams.ToList();
            ViewBag.DanhMucLoai = new SelectList(lstLoai, "MaLoaiSP", "TenLoaiSP");
            List<Size> lstSize = data.Sizes.ToList();
            ViewBag.lstSize = lstSize;

            // nếu ko nhập gì thì cho phép quay lại trang sản phẩm
            bool isAnyFieldEntered = !string.IsNullOrEmpty(sp.TenSanPham) || !string.IsNullOrEmpty(sp.MaLoaiSP) || sp.Gia > 0 || !string.IsNullOrEmpty(sp.MoTa) || sp.TrangThai != null || sp.SoLuong > 0;
            if (action == "QuayLaiDanhSach" && !isAnyFieldEntered)
            {
                return RedirectToAction("Products", "NhanVien");
            }
            // kiểm tra các ô nhập liệu
            if (string.IsNullOrEmpty(sp.MaLoaiSP))
            {
                ModelState.AddModelError("MaLoaiSP", "Vui lòng chọn loại sản phẩm.");
            }
            if (string.IsNullOrEmpty(sp.TenSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Tên sản phẩm không được bỏ trống.");
            }
            if (sp.Gia <= 0)
            {
                ModelState.AddModelError("Gia", "Giá sản phẩm phải lớn hơn 0.");
            }
            if (string.IsNullOrEmpty(sp.MoTa))
            {
                ModelState.AddModelError("MoTa", "Mô tả sản phẩm không được bỏ trống.");
            }
            if (string.IsNullOrEmpty(sp.TrangThai.ToString()))
            {
                ModelState.AddModelError("TrangThai", "Vui lòng chọn trạng thái sản phẩm.");
            }
            if (sp.SoLuong <= 0)
            {
                ModelState.AddModelError("SoLuong", "Số lượng phải lớn hơn 0.");
            }

            if (ModelState.IsValid)
            {
                // Tạo mã sản phẩm mới
                var lastProduct = data.SanPhams.OrderByDescending(p => p.MaSanPham).FirstOrDefault();
                string newMaSanPham = "SP001"; // Mặc định nếu chưa có sản phẩm nào

                if (lastProduct != null)
                {
                    // Tách phần số từ mã sản phẩm cuối cùng (VD: SP015 -> 15)
                    string lastNumberStr = lastProduct.MaSanPham.Substring(2);
                    int lastNumber = int.Parse(lastNumberStr);

                    // Tăng lên 1 và định dạng lại
                    newMaSanPham = "SP" + (lastNumber + 1).ToString("D3");
                }

                sp.MaSanPham = newMaSanPham; // Gán mã sản phẩm mới

                if (fileProductImg != null)
                {
                    string path = Server.MapPath("~/Content/HinhAnh/" + fileProductImg.FileName);
                    fileProductImg.SaveAs(path);
                    sp.HinhAnh = fileProductImg.FileName;
                }

                decimal giaGoc = sp.Gia;
                List<SanPham> lstSPToAdd = new List<SanPham>();
                if (SelectedSizes != null && SelectedSizes.Length > 0)
                {
                    foreach (var maSize in SelectedSizes)
                    {
                        SanPham spToAdd = new SanPham
                        {
                            MaSanPham = sp.MaSanPham,
                            TenSanPham = sp.TenSanPham,
                            MaLoaiSP = sp.MaLoaiSP,
                            Gia = giaGoc,  
                            MoTa = sp.MoTa,
                            TrangThai = sp.TrangThai,
                            SoLuong = sp.SoLuong,
                            HinhAnh = sp.HinhAnh
                        };
                        string keyGiaThem = "GiaThem_" + maSize;
                        decimal giaThem = 0;

                        // Lấy giá thêm từ FormCollection
                        if (!string.IsNullOrEmpty(fc[keyGiaThem]))
                        {
                            decimal.TryParse(fc[keyGiaThem], out giaThem);
                        }

                        spToAdd.MaSize = maSize;
                        spToAdd.Gia = giaGoc + giaThem;

                        lstSPToAdd.Add(spToAdd);
                    }
                }

                data.SanPhams.InsertAllOnSubmit(lstSPToAdd);
                data.SubmitChanges();

                if (action == "ThemTiep")
                {
                    ModelState.Clear();
                    ViewBag.Message = "Thêm thành công";
                    return View();
                }
                else if (action == "QuayLaiDanhSach")
                {
                    return RedirectToAction("Products", "NhanVien");
                }
            }

            return View(sp);
        }
        [HttpGet]
        public ActionResult EditProduct(string selectedProducts)
        {
            if (string.IsNullOrEmpty(selectedProducts))
            {
                TempData["Message"] = "Không có sản phẩm nào được chọn.";
                return RedirectToAction("Products");
            }

            var ids = selectedProducts.Split(',').Where(id => !string.IsNullOrWhiteSpace(id))
                               .ToList();

            var productsToEdit = data.SanPhams.Where(p => ids.Contains(p.MaSanPham)).ToList();

            if (!productsToEdit.Any())
            {
                TempData["Message"] = "Không tìm thấy sản phẩm nào để chỉnh sửa.";
                return RedirectToAction("Products");
            }
            List<LoaiSanPham> lstLoai = data.LoaiSanPhams.ToList();

            ViewBag.DanhMucLoai = lstLoai;
            List<Size> lstSize = data.Sizes.ToList();
            ViewBag.lstSize = lstSize;
            return View(productsToEdit);
        }

        [HttpPost]
        public ActionResult EditProduct(Dictionary<string, SanPham> updatedProducts, Dictionary<string, Dictionary<string, double>> updatedSizes)
        {
            List<LoaiSanPham> lstLoai = data.LoaiSanPhams.ToList();
            ViewBag.DanhMucLoai = lstLoai;

            List<Size> lstSize = data.Sizes.ToList();
            ViewBag.lstSize = lstSize;

            if (updatedProducts == null || updatedProducts.Count == 0)
            {
                TempData["Message"] = "Không có sản phẩm nào được cập nhật.";
                return RedirectToAction("Products");
            }

            foreach (var item in updatedProducts)
            {
                var sp = data.SanPhams.FirstOrDefault(p => p.MaSanPham == item.Key);
                if (sp != null)
                {
                    // Cập nhật thông tin chung của sản phẩm
                    sp.TenSanPham = item.Value.TenSanPham;
                    sp.Gia = item.Value.Gia;
                    sp.SoLuong = item.Value.SoLuong;

                    // Cập nhật ảnh nếu có thay đổi
                    if (!string.IsNullOrEmpty(item.Value.HinhAnh))
                    {
                        sp.HinhAnh = item.Value.HinhAnh;
                    }

                    sp.TrangThai = item.Value.TrangThai;

                    // Cập nhật loại sản phẩm
                    var loaiSanPham = lstLoai.FirstOrDefault(l => l.MaLoaiSP == item.Value.LoaiSanPham.MaLoaiSP);
                    if (loaiSanPham != null)
                    {
                        sp.LoaiSanPham = loaiSanPham;
                    }

                    if (updatedSizes.ContainsKey(item.Key))
                    {
                        var sizes = updatedSizes[item.Key]; // Lấy danh sách kích cỡ của sản phẩm

                        foreach (var size in sizes)
                        {
                            var maSize = size.Key;
                            var gia = size.Value;

                            // Cập nhật giá cho sản phẩm và kích cỡ tương ứng
                            var spSize = data.SanPhams.FirstOrDefault(s => s.MaSanPham == item.Key && s.MaSize == maSize);
                            if (spSize != null)
                            {
                                spSize.Gia = (decimal)gia;
                                data.SubmitChanges();
                            }
                        }
                    }
                }
            }

            TempData["Message"] = "Cập nhật sản phẩm thành công.";
            return RedirectToAction("Products", "NhanVien");
        }

        [HttpGet]
        public ActionResult Orders(int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            List<HoaDonBanHang> lstSP = data.HoaDonBanHangs.ToList();
            var lstPage = lstSP.ToPagedList(pageNumber, pageSize);

            ViewBag.lstCTHD = data.ChiTietHoaDonBanHangs.ToList();

            return View(lstPage);
        }
        [HttpPost]
        public ActionResult Orders(int? page, string maKH, string maNV, string ngayBD, string ngayKT)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            DateTime? dtNgayBD = null, dtNgayKT = null;
            if (!string.IsNullOrEmpty(ngayBD))
            {
                if (!DateTime.TryParse(ngayBD, out DateTime tempNgayBD))
                {
                    ModelState.AddModelError("ngayBD", "Ngày bắt đầu không hợp lệ.");
                }
                else
                {
                    dtNgayBD = tempNgayBD;
                }
            }
            if (!string.IsNullOrEmpty(ngayKT))
            {
                if (!DateTime.TryParse(ngayKT, out DateTime tempNgayKT))
                {
                    ModelState.AddModelError("ngayKT", "Ngày kết thúc không hợp lệ.");
                }
                else
                {
                    dtNgayKT = tempNgayKT;
                }
            }

            List<HoaDonBanHang> lstSP = data.HoaDonBanHangs.ToList();

            if (!string.IsNullOrEmpty(maKH))
            {
                lstSP = lstSP.Where(x => x.MaKH.ToString() == maKH).ToList();
            }

            if (!string.IsNullOrEmpty(maNV))
            {
                lstSP = lstSP.Where(x => x.MaNV.ToString() == maNV).ToList();
            }

            if (dtNgayBD.HasValue)
            {
                lstSP = lstSP.Where(x => x.NgayLap >= dtNgayBD.Value).ToList();
            }
            if (dtNgayKT.HasValue)
            {
                lstSP = lstSP.Where(x => x.NgayLap <= dtNgayKT.Value).ToList();
            }
            var lstPage = lstSP.ToPagedList(pageNumber, pageSize);

            ViewBag.maKH = maKH;
            ViewBag.maNV = maNV;
            ViewBag.ngayBD = ngayBD;
            ViewBag.ngayKT = ngayKT;

            ViewBag.lstCTHD = data.ChiTietHoaDonBanHangs.ToList();

            return View(lstPage);
        }

        public ActionResult OrderDetail(string maHD)
        {
            HoaDonBanHang hd = data.HoaDonBanHangs.FirstOrDefault(x => x.MaHoaDon == maHD);
            ViewBag.lstCTHD = data.ChiTietHoaDonBanHangs.Where(x => x.MaHoaDon == maHD).ToList();
            ViewBag.lstTopping = data.ToppingDonHangs.ToList();
            // tổng đơn hàng ko tính giảm giá, phí vận chuyển
            ViewBag.tongDonHang = data.ChiTietHoaDonBanHangs.Where(x => x.MaHoaDon == maHD).Sum(x => x.Gia * x.SoLuong)
                + data.ToppingDonHangs.Where(x => x.MaHoaDon == maHD).Sum(x => x.Topping.Gia);

            return View(hd);
        }

        [HttpPost]
        public ActionResult ConfirmOrder(string action, string maHD, string maNV)
        {
            HoaDonBanHang hd = data.HoaDonBanHangs.FirstOrDefault(x => x.MaHoaDon == maHD);
            if (hd != null)
            {
                hd.MaNV = int.Parse(maNV);
                if (action == "confirm")
                {
                    hd.TrangThai = "Đang xử lý";
                    TempData["Message"] = "Đã xác nhận đơn hàng";
                }
                else if (action == "reject")
                {
                    hd.TrangThai = "Đã hủy";
                    TempData["Message"] = "Đã từ chối đơn hàng";
                }
                data.SubmitChanges();
            }
            return RedirectToAction("OrderDetail", "NhanVien", new { maHD = maHD });
        }
    }
}
