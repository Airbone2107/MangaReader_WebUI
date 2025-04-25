# TODO.md: Khắc phục lỗi sau khi Refactor API Services

## Mục tiêu

Sửa các lỗi build phát sinh sau khi refactor các service gọi API MangaDex, đảm bảo `HomeController` và `MangaSearchService` sử dụng đúng các model strongly-typed mới (`Models/Mangadex/*`) và các API service tương ứng (`IMangaApiService`, `IChapterApiService`,...).

---

## 1. Sửa lỗi trong `HomeController.cs`

### Lỗi 1.1: `CS0019: Operator '==' cannot be applied to operands of type 'method group' and 'int'` (Dòng 61)

*   **Nguyên nhân:** Bạn đang so sánh phương thức `Count` (method group) với số 0 thay vì truy cập thuộc tính `Count` của danh sách dữ liệu hoặc thuộc tính `Total` của đối tượng `MangaList`. Đối tượng `recentManga` bây giờ là kiểu `MangaList?`, và danh sách thực sự nằm trong thuộc tính `Data`.
*   **Vị trí:** `HomeController.cs`, dòng 61
*   **Code cũ (dự đoán):**
    ```csharp
    if (recentManga == null || recentManga.Count == 0) // Lỗi ở đây
    ```
*   **Code sửa:** Truy cập vào thuộc tính `Data` (là `List<Manga>`) và kiểm tra null hoặc `Count`, hoặc kiểm tra thuộc tính `Total` của `MangaList`. Nên kiểm tra cả `Data`.
    ```csharp
    if (recentManga?.Data == null || !recentManga.Data.Any()) // Kiểm tra Data null hoặc rỗng
    // Hoặc:
    // if (recentManga == null || recentManga.Total == 0) // Kiểm tra Total
    ```
    *(**Lưu ý:** Chọn cách kiểm tra `Data` để đảm bảo có dữ liệu thực sự để xử lý)*

### Lỗi 1.2: `CS0019: Operator '>' cannot be applied to operands of type 'method group' and 'int'` (Dòng 73) và `CS1061: 'MangaList' does not contain a definition for 'Skip'` (Dòng 73)

*   **Nguyên nhân:** Tương tự lỗi 1.1, bạn đang cố gắng dùng toán tử `>` và phương thức `Skip()` trực tiếp trên đối tượng `MangaList` thay vì trên danh sách `recentManga.Data`. `Skip()` là phương thức mở rộng cho `IEnumerable<T>`.
*   **Vị trí:** `HomeController.cs`, dòng 73
*   **Code cũ (dự đoán):**
    ```csharp
    var mangaListToProcess = recentManga.Count > 1 ? recentManga.Skip(1).ToList() : new List<dynamic>(); // Lỗi ở Count và Skip
    ```
*   **Code sửa:** Truy cập `Data`, kiểm tra `Count` của `Data`, và gọi `Skip()` trên `Data`.
    ```csharp
    // Cần thêm: using System.Linq;
    var mangaListToProcess = recentManga?.Data != null && recentManga.Data.Count > 0
        ? recentManga.Data.ToList() // Lấy toàn bộ danh sách Manga từ Data
        : new List<MangaReader.WebUI.Models.Mangadex.Manga>(); // Trả về danh sách Manga rỗng
    ```
    *(**Giải thích:** Logic cũ bỏ qua phần tử đầu tiên có vẻ không còn cần thiết vì API service mới trả về `MangaList` với `Data` là danh sách `Manga` thuần túy, không có metadata ở phần tử đầu. Nếu bạn vẫn muốn bỏ qua phần tử đầu tiên vì lý do nào đó, hãy dùng `recentManga.Data.Skip(1).ToList()` nhưng cần đảm bảo `recentManga.Data` không null và có phần tử.)*

### Lỗi 1.3: `CS8978: 'method group' cannot be made nullable.` (Dòng 159)

*   **Nguyên nhân:** Trong hàm `GetLocalizedTitle`, có thể bạn đang cố gắng gán hoặc trả về một tham chiếu đến phương thức truy cập dictionary (ví dụ `TryGetValue`) thay vì giá trị thực sự lấy ra từ dictionary.
*   **Vị trí:** `HomeController.cs`, dòng 159 (bên trong hàm `GetLocalizedTitle`)
*   **Code cũ (dự đoán):** Có thể có lỗi logic phức tạp hơn, nhưng ví dụ đơn giản:
    ```csharp
    // Ví dụ lỗi tiềm ẩn
    Func<string, string?>? getter = titles.TryGetValue; // Lỗi: Không thể làm nullable method group
    return getter?.Invoke("vi");
    ```
    Hoặc lỗi khi xử lý kết quả `TryGetValue`:
    ```csharp
    titles.TryGetValue("vi", out var viTitle);
    return viTitle?.ToString; // Lỗi: Trả về method group ToString thay vì chuỗi
    ```
*   **Code sửa:** Đảm bảo bạn đang làm việc với *giá trị* trả về từ dictionary. Code hiện tại của hàm `GetLocalizedTitle` có vẻ đúng logic, hãy kiểm tra kỹ lại dòng 159 xem có thao tác nào khác liên quan đến method group không. Nếu code đúng như trong file bạn cung cấp:
    ```csharp
    private string GetLocalizedTitle(string titleJson)
    {
        try
        {
            // Deserialize thành Dictionary<string, string>
            var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);

            if (titles == null || !titles.Any()) return "Không có tiêu đề"; // Thêm kiểm tra null/empty

            // Ưu tiên trả về tiêu đề tiếng Việt nếu có
            if (titles.TryGetValue("vi", out var viTitle) && !string.IsNullOrEmpty(viTitle))
            {
                return viTitle; // Đảm bảo trả về chuỗi viTitle
            }

            // Sau đó là tiếng Anh
            if (titles.TryGetValue("en", out var enTitle) && !string.IsNullOrEmpty(enTitle))
            {
                return enTitle; // Đảm bảo trả về chuỗi enTitle
            }

            // Cuối cùng là ngôn ngữ gốc (key đầu tiên trong từ điển)
            // Dòng 159 có thể nằm ở đây nếu logic phức tạp hơn
            return titles.Values.FirstOrDefault(t => !string.IsNullOrEmpty(t)) ?? "Không có tiêu đề"; // Trả về giá trị hoặc mặc định
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi khi parse tiêu đề manga: {ex.Message}");
            return "Lỗi tiêu đề"; // Trả về lỗi rõ ràng hơn
        }
    }
    ```
    *Kiểm tra kỹ dòng 159 trong file của bạn để xác định chính xác biến hoặc biểu thức nào đang gây lỗi.*

### Lỗi 1.4: `CS0019: Operator '>' cannot be applied...` (Dòng 260) và `CS1061: 'MangaList' does not contain a definition for 'Skip'` (Dòng 260)

*   **Nguyên nhân:** Giống hệt lỗi 1.2, xảy ra trong phương thức `GetLatestMangaPartial`.
*   **Vị trí:** `HomeController.cs`, dòng 260
*   **Code cũ (dự đoán):**
    ```csharp
    var mangaListToProcess = recentManga.Count > 1 ? recentManga.Skip(1).ToList() : new List<dynamic>(); // Lỗi ở Count và Skip
    ```
*   **Code sửa:** Tương tự như sửa lỗi 1.2.
    ```csharp
    // Cần thêm: using System.Linq;
    // Cần thêm using cho Manga model nếu chưa có: using MangaReader.WebUI.Models.Mangadex;
    var mangaListToProcess = recentManga?.Data != null && recentManga.Data.Any()
        ? recentManga.Data.ToList() // Lấy toàn bộ danh sách Manga từ Data
        : new List<Manga>(); // Trả về danh sách Manga rỗng
    ```

---

## 2. Sửa lỗi trong `Program.cs`

### Lỗi 2.1: `CS1729: 'MangaIdService' does not contain a constructor that takes 3 arguments` (Dòng 94)

*   **Nguyên nhân:** Constructor của `MangaIdService` đã thay đổi (trong file `TODO2.md`, nó chỉ nhận `IChapterApiService` và `ILogger`), nhưng khi đăng ký DI, bạn vẫn truyền 3 tham số cũ.
*   **Vị trí:** `Program.cs`, dòng 94
*   **Code cũ (dự đoán):**
    ```csharp
    builder.Services.AddScoped<MangaIdService>(provider =>
        new MangaIdService(
            provider.GetRequiredService<HttpClient>(), // Tham số cũ 1?
            provider.GetRequiredService<ILogger<MangaIdService>>(), // Tham số cũ 2?
            provider.GetRequiredService<SomeOtherService>() // Tham số cũ 3?
        )
    );
    // Hoặc:
    // builder.Services.AddScoped<MangaIdService>(); // Nếu constructor cũ có 3 tham số và DI tự động tìm
    ```
*   **Code sửa:** Cập nhật lời gọi constructor để khớp với định nghĩa mới (chỉ cần 2 tham số: `IChapterApiService` và `ILogger`).
    ```csharp
    builder.Services.AddScoped<MangaIdService>(provider =>
        new MangaIdService(
            provider.GetRequiredService<IChapterApiService>(), // Tham số mới 1
            provider.GetRequiredService<ILogger<MangaIdService>>() // Tham số mới 2
        )
    );
    // Hoặc nếu dùng AddScoped<T>() trực tiếp:
    // builder.Services.AddScoped<MangaIdService>(); // Đảm bảo DI có thể tự động giải quyết IChapterApiService và ILogger
    ```

### Lỗi 2.2: `CS1729: 'ChapterLanguageServices' does not contain a constructor that takes 3 arguments` (Dòng 104)

*   **Nguyên nhân:** Tương tự lỗi 2.1, constructor của `ChapterLanguageServices` đã thay đổi (chỉ nhận `IChapterApiService` và `ILogger`), nhưng đăng ký DI vẫn dùng 3 tham số cũ.
*   **Vị trí:** `Program.cs`, dòng 104
*   **Code cũ (dự đoán):**
    ```csharp
    builder.Services.AddScoped<ChapterLanguageServices>(provider =>
        new ChapterLanguageServices(
            provider.GetRequiredService<HttpClient>(), // Tham số cũ 1?
            provider.GetRequiredService<ILogger<ChapterLanguageServices>>(), // Tham số cũ 2?
            provider.GetRequiredService<SomeOtherService>() // Tham số cũ 3?
        )
    );
    // Hoặc:
    // builder.Services.AddScoped<ChapterLanguageServices>();
    ```
*   **Code sửa:** Cập nhật lời gọi constructor.
    ```csharp
    builder.Services.AddScoped<ChapterLanguageServices>(provider =>
        new ChapterLanguageServices(
            provider.GetRequiredService<IChapterApiService>(), // Tham số mới 1
            provider.GetRequiredService<ILogger<ChapterLanguageServices>>() // Tham số mới 2
        )
    );
    // Hoặc nếu dùng AddScoped<T>() trực tiếp:
    // builder.Services.AddScoped<ChapterLanguageServices>(); // Đảm bảo DI có thể tự động giải quyết IChapterApiService và ILogger
    ```

---

## 3. Sửa lỗi trong `MangaSearchService.cs`

### Lỗi 3.1: `CS0019: Operator '>' cannot be applied...` (Dòng 175)

*   **Nguyên nhân:** Giống lỗi 1.1 và 1.2, đang so sánh `Count` (method group) với số nguyên thay vì truy cập `result.Data.Count` hoặc `result.Total`.
*   **Vị trí:** `MangaSearchService.cs`, dòng 175
*   **Code cũ (dự đoán):**
    ```csharp
    if (result != null && result.Count > 0) // Lỗi ở Count
    ```
*   **Code sửa:** Truy cập thuộc tính `Data` và kiểm tra `Count` hoặc kiểm tra `Total`.
    ```csharp
    if (result?.Data != null && result.Data.Any()) // Kiểm tra Data
    // Hoặc:
    // if (result != null && result.Total > 0) // Kiểm tra Total
    ```

### Lỗi 3.2: `CS0021: Cannot apply indexing with [] to an expression of type 'MangaList'` (Dòng 180)

*   **Nguyên nhân:** Đang cố gắng truy cập phần tử bằng index `[]` trực tiếp trên đối tượng `MangaList` thay vì trên danh sách `result.Data`.
*   **Vị trí:** `MangaSearchService.cs`, dòng 180
*   **Code cũ (dự đoán):**
    ```csharp
    var firstItem = result[0]; // Lỗi ở đây
    ```
*   **Code sửa:** Truy cập phần tử thông qua thuộc tính `Data`.
    ```csharp
    // Cần kiểm tra Data không null và có phần tử trước khi truy cập
    if (result?.Data != null && result.Data.Any())
    {
        var firstItem = result.Data[0]; // Truy cập qua Data
        // ... xử lý firstItem
    }
    ```
    *(**Lưu ý:** Đoạn code này dùng để lấy `totalCount`. Logic này có thể không cần thiết nữa vì `MangaList` đã có thuộc tính `Total`)*. Sửa lại logic lấy `totalCount`:
    ```csharp
    int totalCount = result?.Total ?? 0; // Lấy trực tiếp từ thuộc tính Total
    ```

### Lỗi 3.3: `CS0019: Operator '*' cannot be applied...` (Dòng 200) và `CS0019: Operator '+' cannot be applied...` (Dòng 200)

*   **Nguyên nhân:** Giống lỗi 3.1, đang sử dụng `Count` (method group) trong phép tính toán học thay vì `result.Data.Count` hoặc `result.Total`.
*   **Vị trí:** `MangaSearchService.cs`, dòng 200
*   **Code cũ (dự đoán):**
    ```csharp
    totalCount = Math.Max(result.Count * 10, (page - 1) * pageSize + result.Count + pageSize); // Lỗi ở các chỗ dùng result.Count
    ```
*   **Code sửa:** Sử dụng `result.Data.Count` hoặc `result.Total`. Vì đây là ước tính khi không lấy được `totalCount` từ API, nên dùng `result.Data.Count` hợp lý hơn.
    ```csharp
    totalCount = Math.Max(result.Data.Count * 10, (page - 1) * pageSize + result.Data.Count + pageSize); // Sử dụng result.Data.Count
    ```
    *(**Lưu ý:** Logic ước tính này có thể không cần thiết nếu bạn luôn lấy được `result.Total`)*

### Lỗi 3.4: `CS1503: Argument 1: cannot convert from 'MangaList' to 'List<object>'` (Dòng 206)

*   **Nguyên nhân:** Phương thức `ConvertToMangaViewModelsAsync` đang được gọi với đối tượng `MangaList` (kiểu mới), nhưng signature của nó vẫn mong đợi kiểu dữ liệu cũ (`List<object>` hoặc `List<dynamic>`).
*   **Vị trí:** `MangaSearchService.cs`, dòng 206 (lời gọi `ConvertToMangaViewModelsAsync`) và định nghĩa của phương thức `ConvertToMangaViewModelsAsync`.
*   **Code cũ (dự đoán):**
    ```csharp
    // Lời gọi ở dòng 206
    var mangaViewModels = await ConvertToMangaViewModelsAsync(result); // result là MangaList?

    // Định nghĩa phương thức cũ
    private async Task<List<MangaViewModel>> ConvertToMangaViewModelsAsync(List<object> result)
    {
        // ... xử lý List<object> ...
    }
    ```
*   **Code sửa:**
    1.  **Cập nhật lời gọi:** Truyền vào danh sách `Manga` từ thuộc tính `Data`.
        ```csharp
        // Lời gọi ở dòng 206
        var mangaViewModels = await ConvertToMangaViewModelsAsync(result?.Data); // Truyền vào result.Data
        ```
    2.  **Cập nhật định nghĩa phương thức:** Thay đổi kiểu tham số thành `List<Manga>?`.
        ```csharp
        // Định nghĩa phương thức mới
        private async Task<List<MangaViewModel>> ConvertToMangaViewModelsAsync(List<MangaReader.WebUI.Models.Mangadex.Manga>? mangaList) // Thay đổi kiểu tham số
        {
            var mangaViewModels = new List<MangaViewModel>();
            if (mangaList == null || !mangaList.Any()) return mangaViewModels; // Kiểm tra null hoặc rỗng

            // Lấy danh sách ID để fetch cover một lần
            var mangaIds = mangaList.Select(m => m.Id.ToString()).ToList();
            // Gọi CoverApiService để lấy URL ảnh bìa cho tất cả manga trong danh sách
            var coverUrls = await _coverApiService.FetchRepresentativeCoverUrlsAsync(mangaIds) ?? new Dictionary<string, string>();

            foreach (var manga in mangaList) // Lặp qua danh sách Manga
            {
                if (manga?.Attributes == null) continue; // Bỏ qua nếu thiếu dữ liệu

                string id = manga.Id.ToString();
                var attributes = manga.Attributes;

                try
                {
                    // Lấy title (MangaTitleService đã cập nhật)
                    string title = _mangaTitleService.GetMangaTitle(attributes.Title, attributes.AltTitles);

                    // Lấy author/artist (MangaRelationshipService đã cập nhật)
                    var (author, artist) = _mangaRelationshipService.GetAuthorArtist(manga.Relationships);

                    // Lấy description (MangaDescription đã cập nhật)
                    string description = _mangaDescriptionService.GetDescription(attributes);

                    // Lấy status (LocalizationService đã cập nhật)
                    string status = _localizationService.GetStatus(attributes); // Cần đảm bảo LocalizationService nhận MangaAttributes

                    // Lấy tags (MangaTagService đã cập nhật)
                    var tags = _mangaTagService.GetMangaTags(attributes);

                    // Lấy cover URL từ dictionary đã fetch
                    string coverUrl = coverUrls.TryGetValue(id, out var url) ? url : "/images/cover-placeholder.jpg"; // Ảnh mặc định nếu không có

                    DateTime? lastUpdated = attributes.UpdatedAt.DateTime;

                    var viewModel = new MangaViewModel
                    {
                        Id = id,
                        Title = title,
                        Author = author,
                        Artist = artist,
                        Description = description,
                        CoverUrl = coverUrl,
                        Status = status,
                        LastUpdated = lastUpdated,
                        Tags = tags
                        // Các trường khác như Rating, Views giữ nguyên logic cũ hoặc bỏ đi
                    };
                    mangaViewModels.Add(viewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi chuyển đổi manga ID: {id} sang ViewModel.");
                    // Có thể thêm một ViewModel lỗi vào danh sách nếu muốn
                    mangaViewModels.Add(new MangaViewModel { Id = id, Title = "Lỗi xử lý dữ liệu", CoverUrl = "/images/cover-placeholder.jpg" });
                }
            }

            return mangaViewModels;
        }
        ```
    *(**Quan trọng:** Cần đảm bảo các helper service như `_mangaTitleService`, `_mangaRelationshipService`, `_mangaDescriptionService`, `_localizationService`, `_mangaTagService` đã được cập nhật để nhận tham số là các model mới như `MangaAttributes`, `List<Relationship>`, `List<Tag>` thay vì `Dictionary<string, object>`.)*

---