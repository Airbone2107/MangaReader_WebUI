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
  - `Callback`: Xử lý callback từ Backend API sau khi xác thực Google thành công, nhận JWT token và lưu trữ nó.
  - `Logout`: Xóa thông tin đăng nhập (token).
  - `GetCurrentUser`: API endpoint (AJAX/Fetch) để kiểm tra trạng thái đăng nhập và lấy thông tin người dùng hiện tại.
  - `Profile`: Hiển thị trang thông tin cá nhân của người dùng đã đăng nhập.
- **`ChapterController.cs`**:
  - Xử lý các action liên quan đến việc đọc chapter.
  - `Read(string id, string mangaId)`: Hiển thị trang đọc của một chapter cụ thể. Gọi `MangaDexService` để lấy ảnh các trang, thông tin manga, và xử lý việc lấy danh sách chapter (từ Session hoặc API) để điều hướng giữa các chapter.
- **`HomeController.cs`**:
  - Xử lý các action cho các trang cơ bản của ứng dụng.
  - `Index`: Hiển thị trang chủ, lấy danh sách manga mới cập nhật từ `MangaDexService`.
  - `Privacy`: Hiển thị trang chính sách bảo mật.
  - `Error`: Hiển thị trang lỗi chung của ứng dụng.
  - `ApiTest`: Trang dùng để kiểm tra kết nối và các endpoint cơ bản của API (dành cho mục đích debug).
- **`MangaController.cs`**:
  - Xử lý các action liên quan đến thông tin và danh sách manga.
  - `Details(string id)`: Hiển thị trang chi tiết của một manga. Sử dụng `MangaDetailsService` để lấy toàn bộ thông tin cần thiết (manga details, chapters, alternative titles).
  - `Search(...)`: Hiển thị trang tìm kiếm manga và xử lý kết quả tìm kiếm. Sử dụng `MangaSearchService` để phân tích tham số tìm kiếm, gọi API và xử lý phân trang.
  - `GetTags()`: API endpoint để lấy danh sách tags từ `MangaDexService` (thường được gọi bằng JavaScript/AJAX từ trang tìm kiếm).
  - `ToggleFollow(string mangaId)`: (Placeholder) API endpoint để xử lý việc theo dõi/hủy theo dõi manga (sẽ gọi `MangaFollowService`).

## Tích hợp HTMX

Một số action trong các controller (đặc biệt là `HomeController`, `MangaController`) sử dụng `ViewRenderService` để trả về `PartialView` thay vì `View` đầy đủ khi nhận được request từ HTMX (có header `HX-Request`). Điều này cho phép cập nhật chỉ một phần của trang web, mang lại trải nghiệm mượt mà hơn cho người dùng.
