# Models

Thư mục `Models` chứa các lớp định nghĩa cấu trúc dữ liệu được sử dụng trong ứng dụng frontend. Các model này bao gồm:

1.  **Data Transfer Objects (DTOs):** Các lớp đại diện cho dữ liệu nhận được từ API (MangaDex hoặc MangaReaderLib qua Backend API, hoặc trực tiếp từ Backend Auth API). Chúng thường được đặt trong các thư mục con như `Mangadex/` hoặc `Auth/`.
2.  **View Models:** Các lớp được thiết kế đặc biệt để chứa dữ liệu cần thiết cho một `View` cụ thể, giúp tách biệt logic hiển thị khỏi logic nghiệp vụ. Tất cả các ViewModel đều nằm trong thư mục `ViewModels/` và các thư mục con của nó.
3.  **Query Models:** Các lớp dùng để xây dựng tham số truy vấn cho API (ví dụ: `SortManga.cs`).

## Cấu trúc Thư Mục `Models`

- **`ViewModels/`**: Chứa tất cả các ViewModel được sử dụng bởi Views.
  - `Auth/ProfileViewModel.cs`: ViewModel cho trang thông tin cá nhân.
  - `Chapter/`: ViewModels liên quan đến Chapter.
    - `ChapterInfoViewModel.cs`: Thông tin cơ bản của chapter (ID, Title, PublishedAt).
    - `ChapterReadViewModel.cs`: ViewModel cho trang đọc chapter.
    - `ChapterRelationshipViewModel.cs`: ViewModel cho mối quan hệ của chapter.
    - `ChapterViewModel.cs`: ViewModel chi tiết của một chapter để hiển thị trong danh sách.
    - `SimpleChapterInfoViewModel.cs`: Thông tin rất cơ bản của chapter (ID, DisplayTitle, PublishedAt).
  - `History/LastReadMangaViewModel.cs`: ViewModel cho một mục trong lịch sử đọc.
  - `Manga/`: ViewModels liên quan đến Manga.
    - `FollowedMangaViewModel.cs`: ViewModel cho một mục manga đang theo dõi.
    - `MangaDetailViewModel.cs`: ViewModel cho trang chi tiết manga.
    - `MangaInfoViewModel.cs`: Thông tin cơ bản của manga (ID, Title, CoverUrl).
    - `MangaListViewModel.cs`: ViewModel cho trang danh sách/tìm kiếm manga.
    - `MangaViewModel.cs`: ViewModel chi tiết của một manga để hiển thị trong danh sách hoặc các thành phần nhỏ.
  - `Shared/ErrorViewModel.cs`: ViewModel cho trang lỗi.
- **`Mangadex/`**: Chứa các lớp DTO (Data Transfer Objects) đại diện cho cấu trúc dữ liệu trả về từ **MangaDex API** (thông qua Backend proxy). Các file như `Author.cs`, `Manga.cs`, `Chapter.cs`, `Cover.cs`, `Tag.cs`, `Relationship.cs`, `ScanlationGroup.cs`, `ErrorResponse.cs` nằm ở đây.
- **`Auth/`**: Chứa các lớp DTO liên quan đến xác thực và thông tin người dùng từ **Backend Auth API**. Ví dụ: `UserModel.cs`, `AuthResponse.cs`.
- **`SortManga.cs`**: Model chứa các tùy chọn lọc và sắp xếp khi tìm kiếm manga, dùng để xây dựng query parameters cho API.

## Mục đích của ViewModels

Việc sử dụng các lớp ViewModel trong thư mục `ViewModels/` giúp:

- **Tổ chức dữ liệu cho View:** Gom nhóm dữ liệu một cách logic, chỉ chứa những gì View cần.
- **Tách biệt logic:** Tách biệt dữ liệu cần cho giao diện khỏi cấu trúc dữ liệu thô từ API (DTOs). View không làm việc trực tiếp với DTOs.
- **Tăng tính bảo trì:** Dễ dàng thay đổi cấu trúc dữ liệu hiển thị mà không ảnh hưởng nhiều đến logic nghiệp vụ hoặc DTOs gốc từ API.
- **Đơn giản hóa View:** View chỉ cần làm việc với các thuộc tính đã được xử lý và định dạng sẵn trong ViewModel.

Các **Services** (đặc biệt là các Mappers trong `Services/MangaServices/DataProcessing/Services/`) chịu trách nhiệm chuyển đổi (map) từ DTOs (từ `Models/Mangadex/` hoặc `Models/Auth/`) sang các ViewModels (trong `Models/ViewModels/`) trước khi truyền cho Controller.