# Repair Center

Ứng dụng quản lý hoạt động của trung tâm sửa chữa thiết bị, gồm giao diện React, API ASP.NET Core và cơ sở dữ liệu Microsoft SQL Server.

## Công nghệ sử dụng

- .NET 10 và ASP.NET Core Web API
- Entity Framework Core 10, SQL Server
- React 19, Vite 8
- JWT authentication và phân quyền Admin, Receptionist, Technician

## Chức năng

- Tiếp nhận, tra cứu, phân công và theo dõi phiếu sửa chữa.
- Quản lý khách hàng, thiết bị, loại thiết bị và kỹ thuật viên.
- Quản lý dịch vụ sửa chữa, phụ tùng và giao dịch kho.
- Tạo báo giá, lập hóa đơn, ghi nhận thanh toán, hoàn tiền và hủy giao dịch.
- Cấp và tra cứu bảo hành, xử lý yêu cầu bảo hành.
- Quản lý tài khoản, vai trò và mật khẩu tạm.

## Yêu cầu

- .NET SDK 10.0
- Node.js 20.19+ hoặc 22.12+
- Microsoft SQL Server (LocalDB, Express hoặc SQL Server)

## Cấu hình ứng dụng

Ứng dụng cần chuỗi kết nối SQL Server và cấu hình JWT. Nên dùng User Secrets cho thông tin nhạy cảm:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=RepairCenterDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet user-secrets set "Jwt:Key" "ThayBangKhoaBiMatItNhat32Byte"
dotnet user-secrets set "Jwt:Issuer" "RepairCenter"
dotnet user-secrets set "Jwt:Audience" "RepairCenter"
```

Để tạo tài khoản Admin lần đầu, cấu hình đủ các khóa sau. Mật khẩu cần ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt:

```powershell
dotnet user-secrets set "BootstrapAdmin:UserName" "admin"
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.com"
dotnet user-secrets set "BootstrapAdmin:Password" "ThayDoi!MatKhau123"
```

`appsettings.json` có chuỗi kết nối dành cho máy phát triển. Hãy chỉnh lại cho môi trường của bạn và không commit mật khẩu hoặc khóa JWT thật.

## Cài đặt và chạy

Chạy các lệnh sau từ thư mục gốc dự án:

```powershell
npm ci
npm run build
dotnet restore
dotnet ef database update
dotnet run --launch-profile http
```

Mở ứng dụng tại [http://localhost:5080](http://localhost:5080). Trong môi trường Development, tài liệu OpenAPI có tại `/openapi/v1.json`.

Nếu chưa cài công cụ EF Core CLI:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
```

Sau khi chỉnh sửa giao diện React, chạy lại `npm run build` để cập nhật bundle trong `wwwroot/build`.

## Cấu trúc dự án

- `Controllers/`: API endpoints.
- `DTOs/`: dữ liệu request và response.
- `Data/`, `Entities/`, `Migrations/`: DbContext, mô hình dữ liệu và database migrations.
- `Repositories/`, `Services/`: truy cập dữ liệu và xử lý nghiệp vụ.
- `src/`: các trang và thành phần giao diện React.
- `wwwroot/`: giao diện web, CSS, API bridge và bundle frontend.

## API và xác thực

Các API sử dụng tiền tố `/api`, ví dụ `/api/auth`, `/api/repair-tickets`, `/api/customers`, `/api/invoices` và `/api/warranties`. Đăng nhập qua `POST /api/auth/login` để nhận access token. Phần lớn endpoint yêu cầu xác thực và quyền phù hợp.
