# Services

Thư mục `Services` chứa các lớp logic nghiệp vụ cốt lõi của ứng dụng frontend. Các service này chịu trách nhiệm xử lý dữ liệu, tương tác với các API bên ngoài (thông qua Backend API hoặc trực tiếp nếu cần), và cung cấp dữ liệu đã được xử lý cho các Controllers.

## Cấu trúc

Các service được tổ chức thành các thư mục con dựa trên chức năng chính:

- **`AuthServices/`**: Chứa các service liên quan đến xác thực và quản lý người dùng.
  - `IUserService.cs`, `UserService.cs`: Giao tiếp với Backend API để xử lý đăng nhập Google OAuth, quản lý JWT token và lấy thông tin người dùng.
- **`MangaServices/`**: Tập hợp các service xử lý logic liên quan đến manga.
  - **`ChapterServices/`**:
    - `ChapterService.cs`: Lấy và xử lý thông tin chi tiết về các chapter (từ MangaDex API thông qua Backend), bao gồm việc định dạng tiêu đề, sắp xếp và phân loại theo ngôn ngữ.
  - **`MangaInformation/`**:
    - `MangaDescription.cs`: Xử lý và lấy mô tả manga ưu tiên theo ngôn ngữ.
    - `MangaRelationshipService.cs`: Trích xuất thông tin tác giả, họa sĩ từ dữ liệu relationships.
    - `MangaTagService.cs`: Lấy, dịch (Anh-Việt) và xử lý danh sách tags của manga.
    - `MangaTitleService.cs`: Xử lý và lấy tiêu đề manga ưu tiên theo ngôn ngữ (bao gồm cả tiêu đề gốc và tiêu đề thay thế).
    - `MangaUtilityService.cs`: Cung cấp các hàm tiện ích liên quan đến manga (ví dụ: tính toán rating giả).
  - **`MangaPageService/`**:
    - `MangaDetailsService.cs`: Tổng hợp thông tin chi tiết của một manga (thông tin cơ bản, tags, tác giả, ảnh bìa, trạng thái theo dõi) và danh sách chapters để hiển thị trên trang chi tiết.
    - `MangaSearchService.cs`: Xử lý logic tìm kiếm manga, chuyển đổi tham số tìm kiếm thành các query parameters phù hợp cho API, gọi API tìm kiếm và xử lý kết quả trả về (bao gồm phân trang và tạo ViewModel).
  - `MangaDexService.cs`: Service chính để tương tác với **Backend API** (đóng vai trò proxy cho MangaDex API). Nó thực hiện các cuộc gọi HTTP để lấy danh sách manga, chi tiết manga, chapters, tags, ảnh bìa, v.v.
  - `MangaFollowService.cs`: Quản lý trạng thái theo dõi manga của người dùng, tương tác với Backend API khi người dùng đăng nhập để lưu trữ và truy xuất danh sách manga đã theo dõi.
- **`UtilityServices/`**: Chứa các service tiện ích dùng chung trong ứng dụng.
  - `JsonConversionService.cs`: Cung cấp các hàm để chuyển đổi giữa `JsonElement` và các cấu trúc dữ liệu .NET (Dictionary, List).
  - `LocalizationService.cs`: Cung cấp các hàm để lấy nội dung (tiêu đề, mô tả) được địa phương hóa từ dữ liệu JSON đa ngôn ngữ.
  - `ViewRenderService.cs`: Giúp quyết định render `View` hay `PartialView` dựa trên việc request có phải là từ HTMX hay không.

## Nguyên tắc thiết kế

- **Dependency Injection:** Tất cả các service đều được đăng ký trong `Program.cs` và được inject vào các lớp cần sử dụng (chủ yếu là Controllers).
- **Single Responsibility Principle:** Mỗi service tập trung vào một nhiệm vụ hoặc một nhóm nhiệm vụ liên quan chặt chẽ.
- **Abstraction:** Sử dụng interface (ví dụ: `IUserService`) để tăng tính linh hoạt và khả năng kiểm thử.
- **Error Handling:** Các service cố gắng xử lý lỗi một cách hợp lý (ví dụ: logging lỗi, trả về giá trị mặc định) để tránh làm crash ứng dụng.

## Hướng phát triển

- **Mở rộng các API:** Tích hợp thêm các nguồn dữ liệu manga khác ngoài MangaDex API.
- **Cải thiện hiệu suất:** Thêm caching cho các service gọi API thường xuyên để giảm tải cho backend.
- **Cải thiện khả năng mở rộng:** Tiếp tục chuẩn hóa các interface để dễ dàng thay đổi hoặc mở rộng các service trong tương lai.
- **Tăng cường bảo mật:** Phát triển thêm các cơ chế xác thực và kiểm soát truy cập cho người dùng.
