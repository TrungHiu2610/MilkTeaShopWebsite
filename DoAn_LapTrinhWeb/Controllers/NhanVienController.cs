using DoAn_LapTrinhWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
namespace DoAn_LapTrinhWeb.Controllers
{
    [RoleAuthorize("NV")]
    public class NhanVienController : Controller
    {
        DB_QL_CuaHangTraSuaDataContext data = new DB_QL_CuaHangTraSuaDataContext();
        //
        // GET: /NhanVien/

        public ActionResult Index()
        {
            return View();
        }

        // ================================= QUẢN LÝ SẢN PHẨM ==========================================
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
            ViewBag.maSP = maSP;
            ViewBag.tenSP = tenSP;

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
                string newMaNV = "SP001"; // Mặc định nếu chưa có sản phẩm nào

                if (lastProduct != null)
                {
                    // Tách phần số từ mã sản phẩm cuối cùng (VD: SP015 -> 15)
                    string lastNumberStr = lastProduct.MaSanPham.Substring(2);
                    int lastNumber = int.Parse(lastNumberStr);

                    // Tăng lên 1 và định dạng lại
                    newMaNV = "SP" + (lastNumber + 1).ToString("D3");
                }

                sp.MaSanPham = newMaNV; // Gán mã sản phẩm mới

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

        // ================================= QUẢN LÝ DANH MỤC SẢN PHẨM ==========================================
        public ActionResult Categories(int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            List<LoaiSanPham> lstLSP = data.LoaiSanPhams.ToList();

            var lstPage = lstLSP.ToPagedList(pageNumber, pageSize);

            return View(lstPage);
        }

        [HttpPost]
        public ActionResult Categories( string maLoai,string tenLoai, int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            List<LoaiSanPham> lstLSP = data.LoaiSanPhams.ToList();


            if (!string.IsNullOrEmpty(maLoai))
            {
                lstLSP = lstLSP.Where(x => x.MaLoaiSP.Contains(maLoai)).ToList();
            }

            if (!string.IsNullOrEmpty(tenLoai))
            {
                lstLSP = lstLSP.Where(x => x.TenLoaiSP.Contains(tenLoai)).ToList();
            }

            ViewBag.maLoai = maLoai;    
            ViewBag.tenLoai = tenLoai;  

            var lstPage = lstLSP.ToPagedList(pageNumber, pageSize);

            return View(lstPage);
        }

        [HttpPost]
        public ActionResult HandleAction_Category(string action, string[] selectedCategories)
        {
            if (!action.Contains("Add") && (selectedCategories == null || selectedCategories.Length == 0))
            {
                TempData["Message"] = "Không có danh mục sản phẩm nào được chọn.";
                return RedirectToAction("Categories");
            }
            switch (action)
            {
                case "AddByView":
                    return RedirectToAction("AddCategory");
                case "AddByExcelFile":
                    return RedirectToAction("Categories");

                case "Edit":
                    // chuyển hướng sang trang sửa sản phẩm với danh sách sản phẩm được chọn
                    return RedirectToAction("EditCategory", new { selectedCategories = string.Join(",", selectedCategories) });

                case "Delete":
                    return RedirectToAction("ConfirmDelete_Category", new { selectedCategories = string.Join(",", selectedCategories) });

                default:
                    TempData["Message"] = "Hành động không hợp lệ.";
                    break;
            }


            return RedirectToAction("Categories");
        }

        [HttpGet]
        public ActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddCategory(LoaiSanPham lsp, string action)
        {

            // nếu ko nhập gì thì cho phép quay lại trang loại sản phẩm
            bool isAnyFieldEntered = !string.IsNullOrEmpty(lsp.TenLoaiSP);
            if (action == "QuayLaiDanhSach" && !isAnyFieldEntered)
            {
                return RedirectToAction("Categories", "NhanVien");
            }
            // kiểm tra các ô nhập liệu
            if (string.IsNullOrEmpty(lsp.TenLoaiSP))
            {
                ModelState.AddModelError("TenLoaiSP", "Vui lòng nhập tên loại sản phẩm.");
            }
            

            if (ModelState.IsValid)
            {
                // Tạo mã sản phẩm mới
                var lastCategory = data.LoaiSanPhams.OrderByDescending(p => p.MaLoaiSP).FirstOrDefault();
                string newMaNV = "LSP001"; // Mặc định nếu chưa có sản phẩm nào

                if (lastCategory != null)
                {
                    // Tách phần số từ mã sản phẩm cuối cùng (VD: LSP015 -> 15)
                    string lastNumberStr = lastCategory.MaLoaiSP.Substring(3);
                    int lastNumber = int.Parse(lastNumberStr);

                    // Tăng lên 1 và định dạng lại
                    newMaNV = "LSP" + (lastNumber + 1).ToString("D3");
                }

                lsp.MaLoaiSP = newMaNV; // Gán mã sản phẩm mới
                data.LoaiSanPhams.InsertOnSubmit(lsp);
                data.SubmitChanges();

                if (action == "ThemTiep")
                {
                    ModelState.Clear();
                    ViewBag.Message = "Thêm thành công";
                    return View();
                }
                else if (action == "QuayLaiDanhSach")
                {
                    return RedirectToAction("Categories", "NhanVien");
                }
            }

            return View(lsp);
        }
        // Trang xác nhận xóa
        [HttpGet]
        public ActionResult ConfirmDelete_Category(string selectedCategories)
        {
            if (string.IsNullOrEmpty(selectedCategories))
            {
                TempData["Message"] = "Không có loại sản phẩm nào được chọn.";
                return RedirectToAction("Categories");
            }

            // Lấy danh sách sản phẩm cần xóa
            var ids = selectedCategories.Split(',');
            var categoriesToDelete = data.LoaiSanPhams.Where(p => ids.Contains(p.MaLoaiSP)).ToList();

            return View(categoriesToDelete);
        }

        // xóa khi người dùng xác nhận
        [HttpPost]
        public ActionResult ConfirmDelete_Category(string[] selectedCategories)
        {
            if (selectedCategories == null || selectedCategories.Length == 0)
            {
                TempData["Message"] = "Không có loại sản phẩm nào được chọn để xóa.";
                return RedirectToAction("Categories");
            }

            foreach (var id in selectedCategories)
            {
                var lstCategories = data.LoaiSanPhams.Where(p => p.MaLoaiSP == id).ToList();
                if (lstCategories.Count > 0)
                {
                    foreach (LoaiSanPham lsp in lstCategories)
                        data.LoaiSanPhams.DeleteOnSubmit(lsp);
                }
            }
            data.SubmitChanges();

            TempData["Message"] = "Xóa sản phẩm thành công.";
            return RedirectToAction("Categories");
        }

        [HttpGet]
        public ActionResult EditCategory(string selectedCategories)
        {
            if (string.IsNullOrEmpty(selectedCategories))
            {
                TempData["Message"] = "Không có sản phẩm nào được chọn.";
                return RedirectToAction("Categories");
            }

            var ids = selectedCategories.Split(',').Where(id => !string.IsNullOrWhiteSpace(id))
                               .ToList();

            var categoriesToEdit = data.LoaiSanPhams.Where(p => ids.Contains(p.MaLoaiSP)).ToList();

            if (!categoriesToEdit.Any())
            {
                TempData["Message"] = "Không tìm thấy sản phẩm nào để chỉnh sửa.";
                return RedirectToAction("Categories");
            }
            return View(categoriesToEdit);
        }

        [HttpPost]
        public ActionResult EditCategory(Dictionary<string, LoaiSanPham> updatedCategories)
        {

            if (updatedCategories == null || updatedCategories.Count == 0)
            {
                TempData["Message"] = "Không có loại sản phẩm nào được cập nhật.";
                return RedirectToAction("Categories");
            }
            foreach (var item in updatedCategories)
            {
                var lsp = data.LoaiSanPhams.FirstOrDefault(p => p.MaLoaiSP == item.Key);
                if (lsp != null)
                {
                    // Cập nhật thông tin chung của sản phẩm
                    lsp.TenLoaiSP = item.Value.TenLoaiSP;
                }
            }
            data.SubmitChanges();
            TempData["Message"] = "Cập nhật loại sản phẩm thành công.";
            return RedirectToAction("Categories", "NhanVien");
        }


        // ================================= QUẢN LÝ ĐƠN HÀNG ==========================================
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
            ViewBag.tongDonHang = data.ChiTietHoaDonBanHangs.Where(x => x.MaHoaDon == maHD).Sum(x => x.Gia * x.SoLuong);

            if (data.ToppingDonHangs.Where(x => x.MaHoaDon == maHD).ToList().Count > 0)
            {
                ViewBag.tongDonHang += data.ToppingDonHangs.Where(x => x.MaHoaDon == maHD).Sum(x => x.Topping.Gia);
            }

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
                    hd.TrangThai = "Đã từ chối";
                    TempData["Message"] = "Đã từ chối đơn hàng";
                }
                data.SubmitChanges();
            }
            return RedirectToAction("OrderDetail", "NhanVien", new { maHD = maHD });
        }



        // ================================= QUẢN LÝ NHÂN VIÊN ==========================================
        public ActionResult Employees(int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            List<NguoiDung> lstUser = data.NguoiDungs.Where(x => x.MaQuyen == "NV").ToList();

            var lstPage = lstUser.ToPagedList(pageNumber, pageSize);

            return View(lstPage);
        }

        [HttpPost]
        public ActionResult Employees(int maNV, string tenNV, int? page)
        {
            int pageSize = 6; // số sp mỗi trang
            int pageNumber = page ?? 1; // trang hiện tại

            List<NguoiDung> lstUser = data.NguoiDungs.Where(x => x.MaQuyen == "NV").ToList();


            if (!string.IsNullOrEmpty(maNV.ToString()))
            {
                lstUser = lstUser.Where(x => x.MaNguoiDung == maNV).ToList();
            }

            if (!string.IsNullOrEmpty(tenNV))
            {
                lstUser = lstUser.Where(x => x.HoTen.Contains(tenNV)).ToList();
            }

            ViewBag.maNV = maNV;
            ViewBag.tenNV = tenNV;

            var lstPage = lstUser.ToPagedList(pageNumber, pageSize);

            return View(lstPage);
        }


        [HttpGet]
        public ActionResult AddEmployee()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddEmployee(NguoiDung user, string action)
        {
            // nếu ko nhập gì thì cho phép quay lại trang sản phẩm
            bool isAnyFieldEntered = !string.IsNullOrEmpty(user.HoTen) || !string.IsNullOrEmpty(user.SoDienThoai) || !string.IsNullOrEmpty(user.DiaChi) || !string.IsNullOrEmpty(user.MatKhau);
            if (action == "QuayLaiDanhSach" && !isAnyFieldEntered)
            {
                return RedirectToAction("Employees", "NhanVien");
            }
            // kiểm tra các ô nhập liệu
            if (string.IsNullOrEmpty(user.HoTen))
            {
                ModelState.AddModelError("HoTen", "Họ tên không được bỏ trống");
            }
            if (string.IsNullOrEmpty(user.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại không được bỏ trống.");
            }
            else
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(user.SoDienThoai, @"^\d{10,11}$"))
                {
                    ModelState.AddModelError("SoDienThoai", "Số điện thoại phải là dãy số có từ 10 đến 11 chữ số.");
                }
            }
            if (string.IsNullOrEmpty(user.MatKhau))
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu không được bỏ trống.");
            }    

            if (ModelState.IsValid)
            {
                data.NguoiDungs.InsertOnSubmit(user);
                data.SubmitChanges();
                if (action == "ThemTiep")
                {
                    ModelState.Clear();
                    ViewBag.Message = "Thêm thành công";
                    return View();
                }
                else if (action == "QuayLaiDanhSach")
                {
                    return RedirectToAction("Employees", "NhanVien");
                }
            }

            return View(user);
        }
        [HttpPost]
        public ActionResult HandleAction_Employee(string action, int[] selectedEmployees)
        {
            if (!action.Contains("Add") && (selectedEmployees == null || selectedEmployees.Length == 0))
            {
                TempData["Message"] = "Không có nhân viên nào được chọn.";
                return RedirectToAction("Employees");
            }

            switch (action)
            {
                case "AddByView":
                    return RedirectToAction("AddEmployee");

                case "Delete":
                    return RedirectToAction("ConfirmDelete_Empl", new { selectedEmployees = string.Join(",", selectedEmployees) });

                default:
                    TempData["Message"] = "Hành động không hợp lệ.";
                    break;
            }

            return RedirectToAction("Employees");
        }

        // Trang xác nhận xóa
        [HttpGet]
        public ActionResult ConfirmDelete_Empl(string selectedEmployees)
        {
            if (string.IsNullOrEmpty(selectedEmployees))
            {
                TempData["Message"] = "Không có nhân viên nào được chọn.";
                return RedirectToAction("Employees");
            }

            // Chuyển chuỗi sang mảng số nguyên
            var ids = selectedEmployees.Split(',').Select(int.Parse).ToArray();

            // Lấy danh sách nhân viên cần xóa
            var EmployeesToDelete = data.NguoiDungs.Where(p => ids.Contains(p.MaNguoiDung)).ToList();

            return View(EmployeesToDelete);
        }


        // xóa khi người dùng xác nhận
        [HttpPost]
        public ActionResult ConfirmDelete_Empl(int[] selectedEmployees)
        {
            if (selectedEmployees == null || selectedEmployees.Length == 0)
            {
                TempData["Message"] = "Không có nhân viên nào được chọn để xóa.";
                return RedirectToAction("Employees");
            }

            // Lấy danh sách các nhân viên cần xóa
            var EmployeesToDelete = data.NguoiDungs.Where(p => selectedEmployees.Contains(p.MaNguoiDung)).ToList();

            if (EmployeesToDelete.Count > 0)
            {
                data.NguoiDungs.DeleteAllOnSubmit(EmployeesToDelete);
                data.SubmitChanges();
                TempData["Message"] = "Xóa nhân viên thành công.";
            }
            else
            {
                TempData["Message"] = "Không tìm thấy nhân viên nào để xóa.";
            }

            return RedirectToAction("Employees");
        }

    }
}
