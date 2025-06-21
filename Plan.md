**Các bước lớn cần thực hiện:**

1.  **Tạo Cấu Trúc Thư Mục Mới:**
    *   Tạo thư mục `ViewModels` bên trong thư mục `MangaReader_WebUI/Models/`.
    *   Trong `Models/ViewModels/`, tạo các thư mục con nếu cần để tổ chức các ViewModel tốt hơn (ví dụ: `Manga`, `Chapter`, `Auth`, `Shared`, `History`).

2.  **Định Nghĩa Các ViewModel Mới:**
    *   Dựa trên các View hiện tại, xác định các dữ liệu cần thiết để hiển thị.
    *   Tạo các lớp C# mới (ViewModel) trong thư mục `MangaReader_WebUI/Models/ViewModels/` (và các thư mục con tương ứng). Mỗi ViewModel sẽ chỉ chứa các thuộc tính mà View của nó cần.

3.  **Di Chuyển Logic và Thuộc Tính:**
    *   Chuyển các thuộc tính và logic hiển thị từ các Model cũ (ví dụ: các class trong `MangaReader_WebUI/Models/MangaDexModels.cs` đang được dùng làm ViewModel) sang các ViewModel mới tương ứng.

4.  **Cập Nhật Controllers:**
    *   Sửa đổi các action method trong Controllers để khởi tạo và truyền các ViewModel mới (từ `MangaReader.WebUI.Models.ViewModels`) cho Views, thay vì các model cũ.
    *   Controller sẽ lấy dữ liệu (DTOs) từ Services và thực hiện mapping sang ViewModel.

5.  **Cập Nhật Services (Mappers):**
    *   Điều chỉnh các service (đặc biệt là các lớp Mapper trong `Services/MangaServices/DataProcessing/Services/`) để chúng map dữ liệu từ DTOs của API (MangaDex hoặc MangaReaderLib) sang các ViewModel mới đã tạo.

6.  **Cập Nhật Views (.cshtml):**
    *   Thay đổi khai báo `@model` ở đầu mỗi file View để trỏ đến ViewModel tương ứng trong namespace `MangaReader.WebUI.Models.ViewModels`.
    *   Cập nhật mã Razor trong Views để truy cập các thuộc tính từ ViewModel mới.

7.  **Tách Model `SortManga`:**
    *   Model `SortManga` hiện đang nằm trong `MangaDexModels.cs`. Tách nó ra thành một file riêng `MangaReader_WebUI/Models/SortManga.cs` vì nó không phải là ViewModel mà là model dùng để xây dựng query parameters cho API.

8.  **Dọn Dẹp Các Model Cũ:**
    *   Sau khi tất cả các Views và Controllers đã chuyển sang sử dụng ViewModel mới, file `MangaReader_WebUI/Models/MangaDexModels.cs` có thể được xóa bỏ. Các model DTOs thuần túy cho API MangaDex (nếu có và vẫn cần thiết) nên được đặt trong thư mục riêng như `MangaReader_WebUI/Models/Mangadex/`. Tương tự cho các model DTO của Auth API.

9.  **Cập Nhật File `.csproj` và `_ViewImports.cshtml`:**
    *   Cập nhật file `MangaReader_WebUI.csproj` để thêm các thư mục `ViewModels` mới nếu cần (thường không cần thiết nếu chúng nằm trong `Models`).
    *   Thêm các `using` directive cho namespace `MangaReader.WebUI.Models.ViewModels` và các namespace con vào file `_ViewImports.cshtml`.

---

**Cấu trúc Project dự kiến (tập trung vào thư mục `Models`):**
```
MangaReader_WebUI/
├── Models/
│   ├── ViewModels/                        # THƯ MỤC MỚI
│   │   ├── Manga/                         # Tổ chức ViewModel cho Manga
│   │   │   ├── MangaViewModel.cs
│   │   │   ├── MangaDetailViewModel.cs
│   │   │   ├── MangaListViewModel.cs
│   │   │   ├── MangaInfoViewModel.cs
│   │   │   └── FollowedMangaViewModel.cs
│   │   ├── Chapter/                       # Tổ chức ViewModel cho Chapter
│   │   │   ├── ChapterViewModel.cs
│   │   │   ├── ChapterRelationshipViewModel.cs
│   │   │   ├── ChapterReadViewModel.cs
│   │   │   ├── ChapterInfoViewModel.cs
│   │   │   └── SimpleChapterInfoViewModel.cs
│   │   ├── Auth/                          # Tổ chức ViewModel cho Auth
│   │   │   └── ProfileViewModel.cs
│   │   ├── History/                       # Tổ chức ViewModel cho History
│   │   │   └── LastReadMangaViewModel.cs
│   │   └── Shared/                        # ViewModel dùng chung
│   │       └── ErrorViewModel.cs
│   │
│   ├── Mangadex/                          # Chứa DTOs từ API MangaDex (NẾU CÒN CẦN)
│   │   ├── Author.cs
│   │   ├── Chapter.cs
│   │   ├── Cover.cs
│   │   ├── ErrorResponse.cs
│   │   ├── Manga.cs
│   │   ├── Relationship.cs
│   │   ├── ScanlationGroup.cs
│   │   └── Tag.cs
│   │
│   ├── Auth/                              # Chứa DTOs từ Backend Auth API (NẾU CÒN CẦN)
│   │   ├── AuthResponse.cs
│   │   └── UserModel.cs
│   │
│   └── SortManga.cs                       # Model cho query parameters (TÁCH RA TỪ MangaDexModels.cs)
│
├── Controllers/
│   └── ...
├── Services/
│   └── ...
├── Views/
│   └── ...
├── wwwroot/
└── ...
```

---

**Danh sách các file cần tạo mới/cập nhật và công dụng:**

*   **File cần cập nhật/sửa đổi:**
    *   `MangaReader_WebUI/MangaReader.WebUI.csproj`:
        *   **Công dụng:** Cập nhật để đảm bảo các file ViewModel mới được bao gồm đúng cách trong quá trình build (thường không cần thay đổi nếu cấu trúc `Folder` được thêm đúng).
    *   `MangaReader_WebUI/Views/_ViewImports.cshtml`:
        *   **Công dụng:** Thêm các `using` directive cho namespace `MangaReader.WebUI.Models.ViewModels` và các namespace con để các View có thể tham chiếu đến các ViewModel mới một cách ngắn gọn.
    *   `MangaReader_WebUI/Controllers/AuthController.cs`:
        *   **Công dụng:** Sửa đổi action `Profile` để sử dụng `ProfileViewModel` từ `Models/ViewModels/Auth/` thay vì `UserModel` trực tiếp hoặc `ProfileViewModel` cũ (nếu có).
    *   `MangaReader_WebUI/Controllers/ChapterController.cs`:
        *   **Công dụng:** Sửa đổi action `Read` và các action liên quan (như `GetChapterImagesPartial`) để sử dụng `ChapterReadViewModel` từ `Models/ViewModels/Chapter/`.
    *   `MangaReader_WebUI/Controllers/HomeController.cs`:
        *   **Công dụng:** Sửa đổi action `Index` để sử dụng `List<MangaViewModel>`, action `Error` để sử dụng `ErrorViewModel` từ `Models/ViewModels/`.
    *   `MangaReader_WebUI/Controllers/MangaController.cs`:
        *   **Công dụng:** Sửa đổi action `Details` để sử dụng `MangaDetailViewModel`, action `Search` để sử dụng `MangaListViewModel`, action `Followed` để sử dụng `List<FollowedMangaViewModel>`, action `History` để sử dụng `List<LastReadMangaViewModel>`. Tất cả các ViewModel này đều từ `Models/ViewModels/`.
    *   `MangaReader_WebUI/Services/MangaServices/DataProcessing/Services/MangaMapper/*MapperService.cs` (tất cả các file mapper cho MangaDex):
        *   **Công dụng:** Cập nhật logic mapping trong các service này để chúng trả về các instance của ViewModel mới (trong `Models/ViewModels/`) thay vì các model cũ.
    *   `MangaReader_WebUI/Services/MangaServices/DataProcessing/Services/MangaReaderLibMappers/*MapperService.cs` (tất cả các file mapper cho MangaReaderLib):
        *   **Công dụng:** Tương tự như trên, cập nhật để trả về các ViewModel mới sau khi map từ DTO của MangaReaderLib.
    *   `MangaReader_WebUI/Views/Auth/Profile.cshtml`:
        *   **Công dụng:** Cập nhật khai báo `@model` để sử dụng `MangaReader.WebUI.Models.ViewModels.Auth.ProfileViewModel`.
    *   `MangaReader_WebUI/Views/ChapterRead/Read.cshtml` (và các partial view liên quan):
        *   **Công dụng:** Cập nhật `@model` để sử dụng `MangaReader.WebUI.Models.ViewModels.Chapter.ChapterReadViewModel`.
    *   `MangaReader_WebUI/Views/Home/Index.cshtml`:
        *   **Công dụng:** Cập nhật `@model` để sử dụng `List<MangaReader.WebUI.Models.ViewModels.Manga.MangaViewModel>`.
    *   `MangaReader_WebUI/Views/Home/Error.cshtml`:
        *   **Công dụng:** Cập nhật `@model` để sử dụng `MangaReader.WebUI.Models.ViewModels.Shared.ErrorViewModel`.
    *   `MangaReader_WebUI/Views/Manga/MangaDetails/Details.cshtml`:
        *   **Công dụng:** Cập nhật `@model` để sử dụng `MangaReader.WebUI.Models.ViewModels.Manga.MangaDetailViewModel`.
    *   `MangaReader_WebUI/Views/MangaSearch/Search.cshtml` (và các partial view liên quan):
        *   **Công dụng:** Cập nhật `@model` để sử dụng `MangaReader.WebUI.Models.ViewModels.Manga.MangaListViewModel` hoặc `List<MangaReader.WebUI.Models.ViewModels.Manga.MangaViewModel>` tùy theo partial.
    *   `MangaReader_WebUI/Views/Manga/MangaFollowed/Followed.cshtml` (và `_FollowedMangaItemPartial.cshtml`):
        *   **Công dụng:** Cập nhật `@model` để sử dụng `List<MangaReader.WebUI.Models.ViewModels.Manga.FollowedMangaViewModel>`.
    *   `MangaReader_WebUI/Views/Manga/MangaHistory/History.cshtml` (và các partial view liên quan):
        *   **Công dụng:** Cập nhật `@model` để sử dụng `List<MangaReader.WebUI.Models.ViewModels.History.LastReadMangaViewModel>`.
    *   `MangaReader_WebUI/Models/README.md`:
        *   **Công dụng:** Cập nhật tài liệu để phản ánh cấu trúc thư mục `Models` mới và giới thiệu về thư mục `ViewModels`.

*   **File cần tạo mới:**
    *   `MangaReader_WebUI/Models/ViewModels/Manga/MangaViewModel.cs`:
        *   **Công dụng:** Định nghĩa các thuộc tính cần thiết để hiển thị một mục manga trong danh sách hoặc các thành phần nhỏ (ví dụ: tên, ảnh bìa, trạng thái theo dõi, chương mới nhất).
    *   `MangaReader_WebUI/Models/ViewModels/Manga/MangaDetailViewModel.cs`:
        *   **Công dụng:** Định nghĩa model cho trang chi tiết manga, bao gồm `MangaViewModel` cho thông tin manga, danh sách `ChapterViewModel` và các tiêu đề thay thế.
    *   `MangaReader_WebUI/Models/ViewModels/Manga/MangaListViewModel.cs`:
        *   **Công dụng:** Định nghĩa model cho trang danh sách/tìm kiếm manga, bao gồm danh sách `MangaViewModel`, thông tin phân trang, và các tùy chọn lọc/sắp xếp (`SortManga`).
    *   `MangaReader_WebUI/Models/ViewModels/Manga/MangaInfoViewModel.cs`:
        *   **Công dụng:** Định nghĩa model rút gọn chỉ chứa thông tin cơ bản nhất của manga (ID, Title, CoverUrl), dùng khi không cần toàn bộ chi tiết, ví dụ trong danh sách truyện theo dõi hoặc lịch sử.
    *   `MangaReader_WebUI/Models/ViewModels/Manga/FollowedMangaViewModel.cs`:
        *   **Công dụng:** Định nghĩa model cho một mục trong danh sách truyện đang theo dõi, bao gồm `MangaInfoViewModel` và danh sách các chapter mới nhất (dưới dạng `SimpleChapterInfoViewModel`).
    *   `MangaReader_WebUI/Models/ViewModels/Chapter/ChapterViewModel.cs`:
        *   **Công dụng:** Định nghĩa các thuộc tính cần thiết để hiển thị một chapter trong danh sách (ví dụ: ID, tiêu đề đã định dạng, số chapter, ngôn ngữ, ngày đăng, các relationship).
    *   `MangaReader_WebUI/Models/ViewModels/Chapter/ChapterRelationshipViewModel.cs`:
        *   **Công dụng:** Định nghĩa model cho một mối quan hệ của chapter (ví dụ: scanlation group, user upload).
    *   `MangaReader_WebUI/Models/ViewModels/Chapter/ChapterReadViewModel.cs`:
        *   **Công dụng:** Định nghĩa model cho trang đọc chapter, bao gồm thông tin manga, chapter hiện tại, danh sách URL trang ảnh, và ID của chapter trước/sau để điều hướng.
    *   `MangaReader_WebUI/Models/ViewModels/Chapter/ChapterInfoViewModel.cs`:
        *   **Công dụng:** Định nghĩa model chứa thông tin cơ bản của một chapter (ID, Title, PublishedAt) dùng trong các ngữ cảnh cần thông tin tối thiểu, ví dụ như trong `LastReadMangaViewModel`.
    *   `MangaReader_WebUI/Models/ViewModels/Chapter/SimpleChapterInfoViewModel.cs`:
        *   **Công dụng:** Định nghĩa model chứa thông tin rất cơ bản của một chapter (ID, DisplayTitle, PublishedAt), thường dùng để hiển thị trong danh sách các chapter mới nhất của một manga.
    *   `MangaReader_WebUI/Models/ViewModels/Auth/ProfileViewModel.cs`:
        *   **Công dụng:** Định nghĩa model cho trang thông tin cá nhân người dùng, thường chứa đối tượng `UserModel` (DTO từ Auth API).
    *   `MangaReader_WebUI/Models/ViewModels/History/LastReadMangaViewModel.cs`:
        *   **Công dụng:** Định nghĩa model cho một mục trong lịch sử đọc truyện, bao gồm `MangaInfoViewModel`, `ChapterInfoViewModel` và thời điểm đọc cuối.
    *   `MangaReader_WebUI/Models/ViewModels/Shared/ErrorViewModel.cs`:
        *   **Công dụng:** Định nghĩa model cho trang lỗi chung của ứng dụng, chứa `RequestId`.
    *   `MangaReader_WebUI/Models/SortManga.cs`:
        *   **Công dụng:** Tách lớp `SortManga` (dùng để xây dựng query parameters cho API tìm kiếm manga, không phải là ViewModel hiển thị) ra thành một file riêng biệt.

*   **File cần xóa/thay thế:**
    *   `MangaReader_WebUI/Models/MangaDexModels.cs`:
        *   **Công dụng:** File này hiện chứa nhiều model đang được dùng như ViewModel. Sau khi các ViewModel mới được tạo và `SortManga` đã được tách ra, file này sẽ không còn cần thiết cho mục đích ViewModel nữa và có thể được xóa. Các định nghĩa DTO thuần túy cho API MangaDex (nếu vẫn dùng) nên được chuyển vào thư mục `Models/Mangadex/`.
    *   `MangaReader_WebUI/Models/ProfileViewModel.cs` (nếu tồn tại ở thư mục `Models` gốc):
        *   **Công dụng:** Sẽ được thay thế hoàn toàn bằng `MangaReader_WebUI/Models/ViewModels/Auth/ProfileViewModel.cs`.
    *   `MangaReader_WebUI/Models/ErrorViewModel.cs` (nếu tồn tại ở thư mục `Models` gốc):
        *   **Công dụng:** Sẽ được thay thế hoàn toàn bằng `MangaReader_WebUI/Models/ViewModels/Shared/ErrorViewModel.cs`.