# BigSport - Website Bán Quần Áo Thể Thao Chuyên Nghiệp



## 📖 Giới thiệu (Introduction)

**BigSport** là nền tảng thương mại điện tử chuyên cung cấp quần áo và dụng cụ thể thao chính hãng. Dự án được xây dựng nhằm mang lại trải nghiệm mua sắm trực tuyến mượt mà, hỗ trợ người dùng tìm kiếm, lọc sản phẩm và quản lý đơn hàng hiệu quả.

Dự án được phát triển theo mô hình **MVC**, tối ưu hóa SEO và trải nghiệm người dùng (UX/UI) với các tính năng tương tác không tải lại trang (AJAX).

## 🚀 Công nghệ sử dụng (Tech Stack)

### Backend
* **Framework:** ASP.NET MVC 5
* **ORM:** Entity Framework (Code First / DB First)
* **Ngôn ngữ:** C#
* **Cơ sở dữ liệu:** SQL Server

### Frontend
* **Giao diện:** HTML5, CSS3, Bootstrap
* **Scripting:** JavaScript, jQuery, AJAX (Xử lý thêm vào yêu thích, giỏ hàng, lọc sản phẩm)

## 🌟 Tính năng chính (Key Features)

### 1. Phía Người dùng (Client)
* **Trang chủ:** Hiển thị sản phẩm nổi bật, banner khuyến mãi.
* **Danh mục sản phẩm (Product Listing):**
    * Hệ thống bộ lọc nâng cao (Filter): Lọc theo Thương hiệu, Màu sắc, và Khoảng giá.
    * Xem nhanh (Quick View Modal) chi tiết sản phẩm.
* **Tương tác người dùng:**
    * **Yêu thích (Wishlist):** Thêm/Xóa sản phẩm yêu thích bằng AJAX (không load lại trang), hiển thị trạng thái tim đỏ trực quan.
    * **Giỏ hàng:** Thêm sản phẩm, cập nhật số lượng, tính tổng tiền tự động.
    * **Thanh toán:** Quy trình đặt hàng và lưu đơn hàng vào hệ thống.
* **Tài khoản:** Đăng ký, Đăng nhập, Quản lý thông tin cá nhân.

### 2. Phía Quản trị (Admin - Nếu có)
* Quản lý danh mục (Brands, Categories).
* Quản lý sản phẩm (Thêm, Xóa, Sửa, Upload hình ảnh).
* Quản lý đơn hàng và khách hàng.

## 🗄️ Cấu trúc Cơ sở dữ liệu (Database Schema)

Dự án sử dụng SQL Server với các bảng thực thể chính:
* `NGUOIDUNG`: Quản lý thông tin tài khoản khách hàng/admin.
* `SANPHAM`: Lưu trữ thông tin hàng hóa, giá cả, hình ảnh.
* `THUONGHIEU` / `LOAI`: Phân loại sản phẩm.
* `DONHANG` & `CHITIETDONHANG`: Lưu trữ lịch sử giao dịch.
* `GIOHANG`: Quản lý giỏ hàng tạm thời.
* `YEUTHICH`: Lưu trữ danh sách sản phẩm người dùng quan tâm.

## 🔧 Hướng dẫn Cài đặt (Installation)

Để chạy dự án này trên máy cục bộ, vui lòng làm theo các bước sau:

**Yêu cầu:**
* Visual Studio 2019/2022
* SQL Server
* .NET Framework 4.5 trở lên

**Các bước thực hiện:**

1.  **Clone repository:**
    ```bash
    git clone [https://github.com/Kstill6120/WEB_QUAN_AO_THE_THAO.git](https://github.com/Kstill6120/WEB_QUAN_AO_THE_THAO.git)
    ```
2.  **Mở dự án:**
    Khởi động Visual Studio và mở file `BigSport.sln`.
3.  **Cấu hình Database:**
    * Mở file `Web.config`.
    * Tìm thẻ `<connectionStrings>` và cập nhật `Data Source` phù hợp với SQL Server của bạn.
4.  **Khôi phục Database:**
    * **Cách 1 (Nếu có file script):** Chạy file `script.sql` trong thư mục `Database` trên SQL Server Management Studio.
    * **Cách 2 (Entity Framework):** Mở *Package Manager Console* và chạy lệnh:
        ```powershell
        Update-Database
        ```
5.  **Chạy dự án:**
    Nhấn `F5` hoặc `Ctrl + F5` để khởi chạy ứng dụng trên trình duyệt.
## 📄 Bản quyền (License)

Dự án này được thực hiện cho mục đích học tập/đồ án môn học.
