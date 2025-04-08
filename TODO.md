# Kế hoạch triển khai đăng nhập/tạo tài khoản bằng OAuth Website Application

## Bước 1: Thiết lập các gói phụ thuộc cần thiết
- Cài đặt các gói NuGet cần thiết:
  ```
  Microsoft.AspNetCore.Authentication.JwtBearer
  System.IdentityModel.Tokens.Jwt
  ```

## Bước 2: Cấu hình thông tin OAuth trong appsettings.json
- Thêm cấu hình API Backend và JWT vào appsettings.json:
  ```json
  "Authentication": {
    "Jwt": {
      "Secret": "your-jwt-secret-key"
    }
  },
  "BackendApi": {
    "BaseUrl": "https://manga-reader-app-backend.onrender.com/api"
  }
  ```

## Bước 3: Cấu hình xác thực trong Program.cs
- Cấu hình JWT Bearer token để xác thực các yêu cầu API tiếp theo
- Thiết lập HttpClient để gọi API backend
- Cấu hình middleware xác thực và ủy quyền

## Bước 4: Tạo các Model cần thiết
- Tạo model UserDTO để lưu trữ thông tin người dùng từ API
- Tạo model AuthResponse để đại diện cho phản hồi xác thực

## Bước 5: Tạo UserService để giao tiếp với API backend
- Tạo interface IUserService
- Triển khai UserService để:
  - Lấy URL xác thực từ backend
  - Nhận và lưu trữ JWT token từ callback
  - Gửi token trong header của các yêu cầu API tiếp theo
  - Lấy thông tin người dùng từ API backend

## Bước 6: Tạo AuthController để xử lý luồng đăng nhập
- Tạo endpoint để bắt đầu quá trình đăng nhập Google
  - Gọi đến API backend để lấy URL xác thực Google
  - Chuyển hướng người dùng đến URL xác thực
- Tạo endpoint để xử lý callback từ backend với token
  - Lưu token nhận được vào cookie/session
  - Chuyển hướng người dùng đến trang chính hoặc trang yêu cầu trước đó

## Bước 7: Tạo các View liên quan đến xác thực
- Tạo trang đăng nhập (Login.cshtml)
- Cập nhật layout để hiển thị thông tin người dùng khi đã đăng nhập
- Tạo trang profile người dùng

## Bước 8: Triển khai cơ chế Authorization
- Tạo AuthorizeAttribute tùy chỉnh để kiểm tra token JWT
- Tạo middleware để thêm thông tin người dùng vào HttpContext

## Bước 9: Triển khai các tính năng người dùng
- Theo dõi/bỏ theo dõi manga
- Cập nhật tiến độ đọc
- Xem danh sách manga đang theo dõi

## Bước 10: Triển khai đăng xuất
- Tạo endpoint để đăng xuất
- Xóa token và thông tin người dùng khỏi cookie/session

## Bước 11: Tối ưu hóa trải nghiệm người dùng
- Lưu trữ URL trước khi đăng nhập để chuyển hướng người dùng sau khi xác thực
- Cải thiện thông báo lỗi và thành công
- Xử lý các tình huống lỗi

## Bước 12: Kiểm thử
- Kiểm thử luồng đăng nhập/đăng ký
- Kiểm thử các tính năng người dùng
- Kiểm thử xử lý lỗi và các tình huống đặc biệt

## Bước 13: Triển khai và giám sát
- Triển khai lên môi trường sản xuất
- Giám sát lỗi và hiệu suất
- Thu thập phản hồi từ người dùng để cải thiện

## Lưu ý quan trọng về luồng xác thực:
1. Người dùng nhấp vào nút "Đăng nhập bằng Google" trên frontend
2. Frontend gọi API `/auth/google/url` từ backend để lấy URL xác thực
3. Frontend chuyển hướng người dùng đến URL xác thực Google
4. Sau khi người dùng xác thực, Google chuyển hướng về endpoint callback của backend
5. Backend xử lý callback, tạo JWT token, và chuyển hướng về frontend với token
6. Frontend nhận và lưu trữ token này cho các yêu cầu API tiếp theo 