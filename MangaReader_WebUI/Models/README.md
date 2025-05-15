# Models

Thư mục `Models` chứa các lớp định nghĩa cấu trúc dữ liệu được sử dụng trong ứng dụng frontend. Các model này bao gồm:

1.  **Data Transfer Objects (DTOs):** Các lớp đại diện cho dữ liệu nhận được từ API (Backend API hoặc trực tiếp từ MangaDex qua Backend).
2.  **View Models:** Các lớp được thiết kế đặc biệt để chứa dữ liệu cần thiết cho một `View` cụ thể, giúp tách biệt logic hiển thị khỏi logic nghiệp vụ.
3.  **Configuration Models:** Các lớp đại diện cho cấu hình ứng dụng (ít phổ biến ở tầng frontend này).

## Phân loại Models

- **`Auth/`**: Chứa các model liên quan đến xác thực và thông tin người dùng.
  - `UserModel.cs`: Đại diện cho thông tin chi tiết của người dùng (ID, tên, email, ảnh đại diện, danh sách manga đang theo dõi/đọc) nhận từ Backend API.
  - `AuthResponse.cs`: Đại diện cho các phản hồi từ API xác thực (ví dụ: chứa JWT token hoặc URL xác thực).
- **`MangaDexModels.cs`**: File này chứa phần lớn các model liên quan đến dữ liệu manga và chapter, bao gồm cả DTOs và ViewModels.
  - `Chapter.cs`: (Có vẻ ít được sử dụng trực tiếp, chủ yếu dùng `ChapterViewModel`).
  - `SortManga.cs`: Model quan trọng chứa tất cả các tùy chọn lọc và sắp xếp khi tìm kiếm manga. Được sử dụng bởi `MangaController` và `MangaSearchService` để xây dựng query parameters cho API.
  - `MangaViewModel.cs`: Đại diện cho dữ liệu của một manga cần thiết để hiển thị trên giao diện (trang danh sách, trang chi tiết). Chứa các thông tin đã được xử lý như tiêu đề ưu tiên, ảnh bìa, tags đã dịch, trạng thái theo dõi, v.v.
  - `ChapterViewModel.cs`: Đại diện cho dữ liệu của một chapter cần thiết để hiển thị (ID, tiêu đề đã định dạng, số chapter, ngôn ngữ, ngày đăng).
  - `ChapterRelationship.cs`: Đại diện cho mối quan hệ của một chapter (ví dụ: scanlation group).
  - `MangaDetailViewModel.cs`: ViewModel cho trang chi tiết manga, kết hợp `MangaViewModel` và danh sách `ChapterViewModel`.
  - `MangaListViewModel.cs`: ViewModel cho trang danh sách/tìm kiếm manga, chứa danh sách `MangaViewModel`, thông tin phân trang (`TotalCount`, `CurrentPage`, `PageSize`, `MaxPages`), và các tùy chọn tìm kiếm (`SortManga`).
  - `ChapterReadViewModel.cs`: ViewModel cho trang đọc chapter, chứa thông tin về manga, chapter hiện tại, danh sách ảnh các trang, và ID của chapter trước/sau để điều hướng.
- **`ErrorViewModel.cs`**: Model đơn giản được sử dụng bởi trang lỗi mặc định (`Views/Shared/Error.cshtml`) để hiển thị Request ID (nếu có).

## Mục đích

Việc sử dụng các lớp Model, đặc biệt là ViewModels, giúp:

- **Tổ chức dữ liệu:** Gom nhóm dữ liệu liên quan một cách logic.
- **Tách biệt logic:** Tách biệt dữ liệu cần cho giao diện khỏi cấu trúc dữ liệu thô từ API.
- **Tăng tính bảo trì:** Dễ dàng thay đổi cấu trúc dữ liệu hiển thị mà không ảnh hưởng nhiều đến logic nghiệp vụ hoặc dữ liệu gốc.
- **Validation:** (Mặc dù ít dùng trong project này) Có thể sử dụng Data Annotations để kiểm tra dữ liệu đầu vào.
