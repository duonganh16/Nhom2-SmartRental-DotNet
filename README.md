# 🏠 SmartRental – HỆ THỐNG QUẢN LÝ NHÀ TRỌ

**Nhóm 2 | SmartRental – Quản lý nhà trọ**

## 1. Giới thiệu dự án

SmartRental là hệ thống quản lý nhà trọ được xây dựng nhằm hỗ trợ chủ nhà trọ quản lý hoạt động kinh doanh một cách tập trung, thuận tiện và hiệu quả.

Hệ thống cung cấp các chức năng quản lý tòa nhà, phòng trọ, người thuê, hợp đồng, hóa đơn, thanh toán và yêu cầu sửa chữa. Đồng thời, người thuê có thể đăng nhập để theo dõi thông tin thuê phòng, hợp đồng, hóa đơn và lịch sử thanh toán của mình.

Dự án hướng đến việc số hóa quy trình quản lý nhà trọ, giảm thao tác thủ công, hạn chế sai sót trong quá trình quản lý và nâng cao trải nghiệm của người sử dụng.

## 2. Mục tiêu dự án

- Xây dựng hệ thống quản lý nhà trọ tập trung.
- Hỗ trợ quản lý thông tin tòa nhà và phòng trọ.
- Theo dõi người thuê và hợp đồng thuê phòng.
- Quản lý hóa đơn, thanh toán và công nợ.
- Tiếp nhận và theo dõi yêu cầu sửa chữa.
- Cung cấp số liệu thống kê phục vụ công tác quản lý.
- Đảm bảo dữ liệu được phân quyền theo từng loại tài khoản.

## 3. Đối tượng sử dụng và phân quyền

| Vai trò | Chức năng |
|---|---|
| Admin | Quản lý toàn bộ hệ thống, tài khoản và dữ liệu |
| Landlord | Quản lý tòa nhà, phòng, người thuê, hợp đồng, hóa đơn và thanh toán thuộc quyền quản lý |
| Tenant | Theo dõi phòng đang thuê, hợp đồng, hóa đơn, thanh toán và yêu cầu sửa chữa của mình |

## 4. Các chức năng chính

### 4.1. Quản lý tài khoản
- Đăng nhập và đăng xuất.
- Phân quyền theo vai trò Admin, Landlord và Tenant.
- Quản lý thông tin tài khoản người dùng.

### 4.2. Quản lý tòa nhà và phòng trọ
- Thêm, sửa, xem và xóa thông tin tòa nhà.
- Quản lý danh sách phòng trọ.
- Theo dõi giá thuê, diện tích và trạng thái phòng.
- Phân biệt phòng trống và phòng đang được thuê.

### 4.3. Quản lý người thuê
- Thêm và cập nhật thông tin người thuê.
- Theo dõi trạng thái hồ sơ người thuê.
- Phê duyệt hoặc từ chối hồ sơ.
- Cấp tài khoản đăng nhập cho người thuê đã được duyệt.

### 4.4. Quản lý hợp đồng
- Tạo và cập nhật hợp đồng thuê phòng.
- Quản lý ngày bắt đầu, ngày kết thúc và tiền đặt cọc.
- Theo dõi trạng thái hợp đồng.
- Kiểm tra tình trạng phòng khi tạo hợp đồng.

### 4.5. Quản lý hóa đơn
- Quản lý hóa đơn theo hợp đồng.
- Theo dõi kỳ thanh toán.
- Quản lý trạng thái hóa đơn.
- Theo dõi hóa đơn chưa thanh toán và quá hạn.

### 4.6. Quản lý thanh toán
- Ghi nhận các khoản thanh toán.
- Theo dõi lịch sử thanh toán.
- Cập nhật trạng thái hóa đơn sau thanh toán.

### 4.7. Quản lý yêu cầu sửa chữa
- Người thuê gửi yêu cầu sửa chữa.
- Chủ nhà theo dõi và xử lý yêu cầu.
- Cập nhật trạng thái xử lý yêu cầu.

### 4.8. Thống kê và báo cáo
- Thống kê số lượng phòng.
- Theo dõi phòng đang thuê và phòng còn trống.
- Thống kê hợp đồng và người thuê.
- Theo dõi doanh thu và tình hình hóa đơn.

## 5. Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Ngôn ngữ lập trình | C# |
| Framework | ASP.NET Core MVC |
| Giao diện | HTML, CSS, JavaScript, Bootstrap |
| Cơ sở dữ liệu | Microsoft SQL Server |
| ORM | Entity Framework Core |
| Công cụ phát triển | Visual Studio |
| Quản lý mã nguồn | Git và GitHub |

## 6. Cấu trúc thư mục dự án

```text
SmartRental/
│
├── RoomRentalManagement/
│   ├── Controllers/
│   ├── Data/
│   ├── Migrations/
│   ├── Models/
│   ├── Views/
│   ├── wwwroot/
│   ├── Properties/
│   ├── appsettings.json
│   ├── Program.cs
│   └── RoomRentalManagement.csproj
│
├── .gitignore
└── README.md
```

## 7. Hướng dẫn cài đặt và chạy dự án

### Yêu cầu môi trường
- Visual Studio có hỗ trợ ASP.NET Core.
- .NET SDK tương thích với phiên bản TargetFramework trong file `.csproj`.
- Microsoft SQL Server hoặc SQL Server LocalDB.

### Các bước thực hiện

**Bước 1: Clone repository**

```bash
git clone https://github.com/duonganh16/Nhom5-HeThongQuanLyPhongTro.git
```

**Bước 2: Mở dự án**

Mở file `RoomRentalManagement.csproj` bằng Visual Studio.

**Bước 3: Cấu hình cơ sở dữ liệu**

Mở `appsettings.json` và kiểm tra chuỗi kết nối SQL Server phù hợp với máy tính.

**Bước 4: Khôi phục các package**

```bash
dotnet restore
```

**Bước 5: Cập nhật cơ sở dữ liệu**

Sử dụng Package Manager Console trong Visual Studio:

```powershell
Update-Database
```

**Bước 6: Chạy ứng dụng**

Nhấn `F5` hoặc `Ctrl + F5` trong Visual Studio để khởi chạy hệ thống.

## 8. Kiểm thử hệ thống

Dự án được kiểm tra theo các nhóm chức năng chính:

- Kiểm thử đăng nhập và phân quyền.
- Kiểm thử quản lý tòa nhà và phòng trọ.
- Kiểm thử quản lý người thuê.
- Kiểm thử tạo, sửa và theo dõi hợp đồng.
- Kiểm thử hóa đơn và thanh toán.
- Kiểm thử yêu cầu sửa chữa.
- Kiểm thử quyền truy cập dữ liệu giữa các vai trò.

## 9. Định hướng phát triển

- Hoàn thiện giao diện và trải nghiệm người dùng.
- Bổ sung thông báo tự động về hóa đơn và thời hạn hợp đồng.
- Phát triển chức năng gửi thông báo thanh toán.
- Tối ưu báo cáo và thống kê doanh thu.
- Nâng cao khả năng bảo mật và quản lý dữ liệu.
- Hỗ trợ truy cập trên nhiều thiết bị.

## 10. Thông tin dự án

**Tên dự án:** SmartRental – Quản lý nhà trọ  
**Nhóm thực hiện:** Nhóm 2  
**Lĩnh vực:** Quản lý nhà trọ  
**Nền tảng:** Web Application  
**Repository:** GitHub

---

© 2026 SmartRental – Nhóm 2. All rights reserved.
