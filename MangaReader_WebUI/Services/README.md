# Services

Thư mục `Services` chứa các lớp logic nghiệp vụ cốt lõi của ứng dụng frontend. Các service này chịu trách nhiệm xử lý dữ liệu, tương tác với các API bên ngoài (`MangaReaderLib API` và `Backend Auth API`), và cung cấp dữ liệu đã được xử lý cho các Controllers.

## Cấu trúc

Các service được tổ chức thành các thư mục con dựa trên chức năng chính:

- **`AuthServices/`**: Chứa các service liên quan đến xác thực và quản lý người dùng.
  - `IUserService.cs`, `UserService.cs`: Giao tiếp với **Backend Auth API** để xử lý đăng nhập Google OAuth, quản lý JWT token và lấy thông tin người dùng.

- **`MangaServices/`**: Tập hợp các service xử lý logic liên quan đến manga, lấy dữ liệu từ **MangaReaderLib API**.
  - **`ChapterServices/`**:
    - `ChapterService.cs`: Lấy và xử lý thông tin chi tiết về các chapter, bao gồm việc định dạng tiêu đề, sắp xếp và phân loại theo ngôn ngữ.
    - `ChapterReadingServices.cs`: Tổng hợp tất cả dữ liệu cần thiết để hiển thị trang đọc truyện.
    - `MangaIdService.cs`, `ChapterLanguageServices.cs`: Các service helper để lấy thông tin liên quan đến chapter.
  - **`MangaPageService/`**:
    - `MangaDetailsService.cs`: Tổng hợp thông tin chi tiết của một manga và danh sách chapters để hiển thị trên trang chi tiết.
    - `MangaSearchService.cs`: Xử lý logic tìm kiếm manga, gọi API tìm kiếm và xử lý kết quả trả về.
  - `IMangaFollowService.cs`, `MangaFollowService.cs`: Quản lý trạng thái theo dõi manga của người dùng, tương tác với **Backend Auth API**.
  - `IFollowedMangaService.cs`, `FollowedMangaService.cs`: Lấy danh sách manga đang theo dõi và các chapter mới nhất của chúng.
  - `IReadingHistoryService.cs`, `ReadingHistoryService.cs`: Lấy lịch sử đọc của người dùng từ **Backend Auth API**.
  - `IMangaInfoService.cs`, `MangaInfoService.cs`: Cung cấp thông tin cơ bản của một manga.

- **`APIServices/MangaReaderLibApiClients/`**: Chứa các client service (và interface của chúng) được tạo ra từ project `MangaReaderLib`, chịu trách nhiệm thực hiện các lệnh gọi HTTP trực tiếp đến `MangaReaderLib API`.

- **`MangaServices/DataProcessing/`**: Chứa các Mappers chịu trách nhiệm chuyển đổi các đối tượng DTO từ `MangaReaderLib` thành các `ViewModel` mà `View` có thể sử dụng.

- **`UtilityServices/`**: Chứa các service tiện ích dùng chung trong ứng dụng.
  - `JsonConversionService.cs`: Cung cấp các hàm để chuyển đổi dữ liệu JSON.
  - `LocalizationService.cs`: Cung cấp các hàm dịch thuật (ví dụ: trạng thái manga).
  - `ViewRenderService.cs`: Giúp quyết định render `View` hay `PartialView` dựa trên request HTMX.

## Nguyên tắc thiết kế

- **Dependency Injection:** Tất cả các service đều được đăng ký trong `Program.cs` và được inject vào các lớp cần sử dụng.
- **Single Responsibility Principle:** Mỗi service tập trung vào một nhiệm vụ hoặc một nhóm nhiệm vụ liên quan chặt chẽ.
- **Abstraction:** Sử dụng interface (ví dụ: `IUserService`) để tăng tính linh hoạt và khả năng kiểm thử.
- **Error Handling:** Các service cố gắng xử lý lỗi một cách hợp lý (ví dụ: logging lỗi, trả về giá trị mặc định) để tránh làm crash ứng dụng.

## Hướng phát triển

- **Mở rộng các API:** Tích hợp thêm các nguồn dữ liệu manga khác ngoài MangaDex API.
- **Cải thiện hiệu suất:** Thêm caching cho các service gọi API thường xuyên để giảm tải cho backend.
- **Cải thiện khả năng mở rộng:** Tiếp tục chuẩn hóa các interface để dễ dàng thay đổi hoặc mở rộng các service trong tương lai.
- **Tăng cường bảo mật:** Phát triển thêm các cơ chế xác thực và kiểm soát truy cập cho người dùng.
