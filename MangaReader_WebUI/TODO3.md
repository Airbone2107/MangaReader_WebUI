Cập nhật để thông tin chapter trong lịch sử đọc sẽ được lưu chung với Backend User.

### Các file bị ảnh hưởng

**Backend (`manga_reader_app_backend`):**

1.  `// manga_reader_app_backend/models/User.js`
    *   Cần cập nhật `userSchema` trong phần `readingManga` để lưu trữ thêm thông tin chi tiết của chapter.
2.  `// manga_reader_app_backend/routes/userRoutes.js`
    *   Route `POST /api/users/reading-progress` (cập nhật tiến độ đọc) cần được sửa để nhận và lưu trữ thông tin chi tiết của chapter.
    *   Route `GET /api/users/reading-history` (lấy lịch sử đọc) sẽ trả về thông tin chapter đã được lưu trữ sẵn.

**Frontend (`MangaReader_WebUI`):**

1.  `// MangaReader_WebUI/Services/MangaServices/ReadingHistoryService.cs`
    *   Logic lấy lịch sử đọc sẽ thay đổi. Thay vì gọi API để lấy chi tiết từng chapter, dịch vụ này sẽ nhận thông tin chapter đầy đủ từ backend.
    *   Vẫn cần gọi API để lấy Cover và tên Manga cho từng mục lịch sử.
2.  `// MangaReader_WebUI/Controllers/ChapterController.cs`
    *   Action `SaveReadingProgress` (hoặc tương đương) cần gửi thêm thông tin chi tiết của chapter (ChapterID, Tên Chapter đã format, Ngày đăng, Ngôn ngữ, etc.) lên backend khi người dùng đọc một chapter.
3.  `// MangaReader_WebUI/Models/Auth/UserModel.cs` (hoặc các ViewModel liên quan)
    *   Cấu trúc của `ReadingMangaInfo` (hoặc tương đương) cần được cập nhật để phản ánh việc lưu trữ thông tin chapter chi tiết hơn.
4.  `// MangaReader_WebUI/Services/MangaServices/Models/LastReadMangaViewModel.cs`
    *   Model này (hoặc các model tương tự dùng để hiển thị lịch sử) cần được cập nhật để bao gồm các trường thông tin chapter mới.
5.  `// MangaReader_WebUI/Services/MangaServices/DataProcessing/Services/MangaMapper/LastReadMangaViewModelMapperService.cs` (hoặc mapper tương ứng)
    *   Logic map dữ liệu từ `BackendHistoryItem` sang `LastReadMangaViewModel` sẽ thay đổi.
6.  `// MangaReader_WebUI/Views/Manga/MangaHistory/History.cshtml`
    *   Và các Partial View liên quan (ví dụ: `_ReadingHistoryItemPartial.cshtml`) sẽ cần cập nhật để hiển thị thông tin chapter từ dữ liệu mới.
7.  `// MangaReader_WebUI/Services/APIServices/Interfaces/IChapterApiService.cs`
    `// MangaReader_WebUI/Services/APIServices/Services/ChapterApiService.cs`
    *   Có thể không cần gọi `FetchChapterInfoAsync` thường xuyên từ `ReadingHistoryService` nữa, nhưng các service khác vẫn có thể sử dụng nó.

### Workflow thực hiện sửa đổi

**Giai đoạn 1: Cập nhật Backend (`manga_reader_app_backend`)**

1.  **Định nghĩa lại cấu trúc lưu trữ Chapter trong `User.js`:**
    *   Trong `userSchema`, thay đổi mảng `readingManga`. Mỗi phần tử sẽ không chỉ chứa `mangaId` và `lastChapter` (là chapterId) nữa, mà sẽ chứa một object `chapterDetails` bao gồm:
        *   `id` (ChapterID)
        *   `title` (Tên Chapter đã được format)
        *   `publishAt` (Ngày đăng)
        *   `translatedLanguage` (Ngôn ngữ của Chapter)
        *   Có thể thêm `volume`, `chapterNumber` (số chapter dạng string gốc) nếu cần.
    *   Trường `lastReadAt` vẫn giữ nguyên.
2.  **Cập nhật Route lưu tiến độ đọc (`/api/users/reading-progress` trong `userRoutes.js`):**
    *   Khi frontend gửi yêu cầu cập nhật tiến độ, nó sẽ gửi kèm `mangaId` và một object `chapterDetails` chứa đầy đủ thông tin như đã định nghĩa ở trên.
    *   Backend sẽ lưu object `chapterDetails` này cùng với `mangaId` và `lastReadAt` vào mảng `readingManga` của User.
3.  **Cập nhật Route lấy lịch sử đọc (`/api/users/reading-history` trong `userRoutes.js`):**
    *   Route này sẽ truy vấn và trả về mảng `readingManga` của User.
    *   Mỗi phần tử trong mảng trả về sẽ chứa `mangaId`, `chapterDetails` (object) và `lastReadAt`.

**Giai đoạn 2: Cập nhật Frontend (`MangaReader_WebUI`)**

1.  **Thu thập thông tin Chapter đầy đủ khi người dùng đọc:**
    *   Trong `ChapterController.cs`, khi action `Read` được gọi (hoặc khi `SaveReadingProgress` được trigger), frontend cần lấy các thông tin chi tiết của chapter hiện tại (ID, tên đã format, ngày đăng, ngôn ngữ). Thông tin này có thể đã có sẵn trong `ChapterReadViewModel` hoặc cần được lấy từ `ChapterService` / `ChapterApiService` một lần khi tải trang đọc.
2.  **Gửi thông tin Chapter chi tiết lên Backend:**
    *   Khi gọi API `/api/users/reading-progress` từ `ChapterController.cs` (hoặc service xử lý việc này), gửi kèm object `chapterDetails`.
3.  **Cập nhật Model ở Frontend:**
    *   Trong `MangaReader_WebUI/Models/Auth/UserModel.cs` (hoặc `BackendHistoryItem.cs` trong `ReadingHistoryService`), cập nhật cấu trúc để khớp với dữ liệu mới từ API `/api/users/reading-history`. Sẽ có một object `ChapterDetails` bên trong.
4.  **Điều chỉnh `ReadingHistoryService.cs`:**
    *   Hàm `GetReadingHistoryAsync` sẽ gọi API `/api/users/reading-history`.
    *   Deserialize response JSON, mỗi item sẽ có `MangaId` và `ChapterDetails`.
    *   Khi map sang `LastReadMangaViewModel`:
        *   Thông tin chapter (ID, Title, PublishAt, Language) sẽ lấy trực tiếp từ `item.ChapterDetails`.
        *   **Vẫn cần gọi `_mangaInfoService.GetMangaInfoAsync(item.MangaId)` để lấy Cover và tên Manga.**
5.  **Cập nhật `LastReadMangaViewModel.cs` và Mapper tương ứng:**
    *   Thêm các thuộc tính cần thiết vào `LastReadMangaViewModel` để hiển thị đầy đủ thông tin chapter.
    *   Cập nhật logic trong `LastReadMangaViewModelMapperService` để sử dụng `ChapterDetails` từ `BackendHistoryItem`.
6.  **Cập nhật Views hiển thị lịch sử:**
    *   Sửa `History.cshtml` và các partial view liên quan để hiển thị các thông tin chapter mới (tên, ngày đăng, ngôn ngữ) trực tiếp từ `LastReadMangaViewModel`.

**Ví dụ về cấu trúc dữ liệu mới trong `User.js` (Backend):**
```javascript
// manga_reader_app_backend/models/User.js
// ...
  readingManga: [{
    mangaId: {
      type: String,
      required: true
    },
    chapterDetails: {
      id: { type: String, required: true }, // ChapterID
      title: { type: String, required: true }, // Formatted Chapter Name, e.g., "Chapter 10: The Beginning"
      publishAt: { type: Date, required: true },
      translatedLanguage: { type: String, required: true },
      volume: { type: String, default: null },
      chapterNumber: { type: String, default: null } // Original chapter number string like "10", "10.5"
    },
    lastReadAt: {
      type: Date,
      default: Date.now
    }
  }],
// ...
```

**Ví dụ về payload gửi từ Frontend (`ChapterController.cs` -> `SaveReadingProgress`):**
```json
{
  "mangaId": "manga-uuid-123",
  "chapterDetails": {
    "id": "chapter-uuid-abc",
    "title": "Chương 15: Khám Phá Mới",
    "publishAt": "2024-03-15T00:00:00Z",
    "translatedLanguage": "vi",
    "volume": "2",
    "chapterNumber": "15"
  }
}
```

**Ví dụ về response từ Backend (`/api/users/reading-history`):**
```json
[
  {
    "mangaId": "manga-uuid-123",
    "chapterDetails": {
      "id": "chapter-uuid-abc",
      "title": "Chương 15: Khám Phá Mới",
      "publishAt": "2024-03-15T00:00:00.000Z",
      "translatedLanguage": "vi",
      "volume": "2",
      "chapterNumber": "15"
    },
    "lastReadAt": "2024-07-20T10:30:00.000Z"
  },
  // ... more items
]
```

Như vậy, bạn sẽ giảm thiểu đáng kể số lần gọi API đến MangaDex để lấy thông tin chapter khi hiển thị lịch sử đọc.