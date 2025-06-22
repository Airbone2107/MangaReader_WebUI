# Controllers

Thư mục `Controllers` chứa các lớp Controller theo kiến trúc Model-View-Controller (MVC) của ASP.NET Core. Vai trò chính của các Controller là:

1.  **Tiếp nhận yêu cầu HTTP:** Xử lý các request từ trình duyệt của người dùng (GET, POST, ...).
2.  **Điều phối logic:** Gọi các `Services` tương ứng để thực hiện logic nghiệp vụ, lấy hoặc xử lý dữ liệu.
3.  **Chuẩn bị dữ liệu cho View:** Tạo hoặc lấy các đối tượng `ViewModel` từ `Services` và truyền chúng đến `View`.
4.  **Trả về phản hồi:** Quyết định và trả về kết quả cho người dùng, thường là một `View` (trang HTML) hoặc `JsonResult` (cho các API endpoint hoặc HTMX request).

## Danh sách Controllers

- **`AuthController.cs`**:
  - Xử lý các action liên quan đến xác thực người dùng.
  - `Login`: Hiển thị trang đăng nhập.
  - `GoogleLogin`: Bắt đầu luồng đăng nhập bằng Google OAuth bằng cách gọi `UserService` để lấy URL xác thực và chuyển hướng người dùng.
  - `Callback`: Xử lý callback từ Backend Auth API sau khi xác thực Google thành công, nhận JWT token và lưu trữ nó.
  - `Logout`: Xóa thông tin đăng nhập (token).
  - `GetCurrentUser`: API endpoint (AJAX/Fetch) để kiểm tra trạng thái đăng nhập và lấy thông tin người dùng hiện tại.
  - `Profile`: Hiển thị trang thông tin cá nhân của người dùng đã đăng nhập.
- **`ChapterController.cs`**:
  - Xử lý các action liên quan đến việc đọc chapter.
  - `Read(string id)`: Hiển thị trang đọc của một chapter cụ thể. Gọi `ChapterReadingServices` để lấy toàn bộ thông tin cần thiết cho trang đọc.
  - `SaveReadingProgress`: API endpoint để lưu tiến độ đọc của người dùng vào Backend Auth API.
- **`HomeController.cs`**:
  - Xử lý các action cho các trang cơ bản của ứng dụng.
  - `Index`: Hiển thị trang chủ, lấy danh sách manga mới cập nhật từ `MangaReaderLib API`.
  - `Privacy`: Hiển thị trang chính sách bảo mật.
  - `Error`: Hiển thị trang lỗi chung của ứng dụng.
- **`MangaController.cs`**:
  - Xử lý các action liên quan đến thông tin và danh sách manga.
  - `Details(string id)`: Hiển thị trang chi tiết của một manga. Sử dụng `MangaDetailsService` để lấy toàn bộ thông tin cần thiết.
  - `Search(...)`: Hiển thị trang tìm kiếm manga và xử lý kết quả tìm kiếm. Sử dụng `MangaSearchService` để phân tích tham số, gọi `MangaReaderLib API` và xử lý phân trang.
  - `GetTags()`: API endpoint để lấy danh sách tags từ `MangaReaderLib API`.
  - `ToggleFollowProxy(...)`: Proxy action để xử lý việc theo dõi/hủy theo dõi manga thông qua Backend Auth API.
  - `Followed()`: Hiển thị danh sách truyện đang theo dõi của người dùng.
  - `History()`: Hiển thị lịch sử đọc của người dùng.

## Tích hợp HTMX

Một số action trong các controller (đặc biệt là `HomeController`, `MangaController`) sử dụng `ViewRenderService` để trả về `PartialView` thay vì `View` đầy đủ khi nhận được request từ HTMX (có header `HX-Request`). Điều này cho phép cập nhật chỉ một phần của trang web, mang lại trải nghiệm mượt mà hơn cho người dùng.
