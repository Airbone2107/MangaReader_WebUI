# TODO: Sửa lỗi RuntimeBinderException trong ChapterInfoService (Lần 2)

Lỗi `RuntimeBinderException` vẫn xảy ra. Chúng ta sẽ thay đổi cách truy cập dữ liệu trong `GetChapterInfoAsync` để giống với phương thức `GetChapterLanguageAsync` đã hoạt động thành công, bằng cách sử dụng `TryGetProperty` trực tiếp trên `JsonElement` thay vì chuyển đổi sang `Dictionary`.

## Các bước thực hiện

1.  **Mở file:** `manga_reader_web\Services\MangaServices\ChapterServices\ChapterInfoService.cs`

2.  **Tìm phương thức:** `GetChapterInfoAsync(string chapterId)`

3.  **Xóa bỏ việc sử dụng `JsonConversionService` trong phương thức này:** Xóa dòng sau (nếu có, hoặc đảm bảo không sử dụng `_jsonConversionService` để chuyển đổi `attributes`):
    ```csharp
    // var chapterDict = _jsonConversionService.ConvertJsonElementToDict(chapterElement); // Xóa hoặc comment dòng này
    // var attributesDict = (Dictionary<string, object>)chapterDict["attributes"]; // Xóa hoặc comment dòng này
    ```

4.  **Thay thế toàn bộ phần xử lý JSON sau khi gọi API:** Tìm đoạn code bắt đầu từ `var chapterData = await _mangaDexService.FetchChapterInfoAsync(chapterId);` và thay thế phần xử lý JSON phía sau nó bằng đoạn code sau:

    ```csharp
    public async Task<ChapterInfo> GetChapterInfoAsync(string chapterId)
    {
        if (string.IsNullOrEmpty(chapterId))
        {
            _logger.LogWarning("ChapterId không được cung cấp khi gọi GetChapterInfoAsync.");
            return null;
        }

        try
        {
            _logger.LogInformation($"Đang lấy thông tin chi tiết cho chapter ID: {chapterId}");
            // Gọi API và lấy dữ liệu dưới dạng dynamic hoặc JsonElement
            var chapterDynamicData = await _mangaDexService.FetchChapterInfoAsync(chapterId);

            if (chapterDynamicData == null)
            {
                _logger.LogWarning($"Không nhận được dữ liệu cho chapter ID: {chapterId}");
                return null;
            }

            // Deserialize lại thành JsonElement để sử dụng TryGetProperty một cách nhất quán
            // Điều này đảm bảo chúng ta không làm việc với kiểu dynamic nữa
            JsonElement chapterElement;
            try
            {
                 // Nếu chapterDynamicData đã là JsonElement thì dùng luôn
                 if (chapterDynamicData is JsonElement element) {
                     chapterElement = element;
                 }
                 // Nếu không, serialize lại rồi deserialize thành JsonElement
                 else {
                    string jsonString = JsonSerializer.Serialize(chapterDynamicData);
                    chapterElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
                 }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, $"Lỗi khi phân tích JSON nhận được cho chapter {chapterId}");
                return null;
            }


            // Sử dụng TryGetProperty để truy cập an toàn
            if (!chapterElement.TryGetProperty("attributes", out JsonElement attributesElement) || attributesElement.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning($"Chapter {chapterId} không có 'attributes' hoặc 'attributes' không phải là object.");
                return null;
            }

            // Lấy chapter number
            string chapterNumber = "?";
            if (attributesElement.TryGetProperty("chapter", out JsonElement chapterNumElement) && chapterNumElement.ValueKind == JsonValueKind.String)
            {
                chapterNumber = chapterNumElement.GetString() ?? "?";
            }
            else if (attributesElement.TryGetProperty("chapter", out chapterNumElement) && chapterNumElement.ValueKind == JsonValueKind.Number)
            {
                 chapterNumber = chapterNumElement.ToString(); // Chuyển số thành chuỗi
            }
            // Không cần kiểm tra null ở đây nữa vì TryGetProperty đã xử lý


            // Lấy chapter title
            string chapterTitleAttr = "";
            if (attributesElement.TryGetProperty("title", out JsonElement titleElement) && titleElement.ValueKind == JsonValueKind.String)
            {
                chapterTitleAttr = titleElement.GetString() ?? "";
            }
             // Không cần kiểm tra null

            // Lấy publishAt date
            DateTime publishedAt = DateTime.MinValue;
            if (attributesElement.TryGetProperty("publishAt", out JsonElement publishAtElement) && publishAtElement.ValueKind == JsonValueKind.String)
            {
                if (publishAtElement.TryGetDateTime(out var date))
                {
                    publishedAt = date;
                }
                else
                {
                    _logger.LogWarning($"Không thể parse ngày publishAt: {publishAtElement.GetString()} cho chapter {chapterId}");
                }
            }
             // Không cần kiểm tra null

            // Tạo tiêu đề hiển thị (Giữ nguyên logic này)
            string displayTitle = $"Chương {chapterNumber}";
            if (!string.IsNullOrEmpty(chapterTitleAttr) && chapterTitleAttr != chapterNumber)
            {
                displayTitle += $": {chapterTitleAttr}";
            }
            // Trường hợp đặc biệt cho Oneshot khi chapter number là null/rỗng
            else if (string.IsNullOrEmpty(chapterNumber) || chapterNumber == "?")
            {
                 displayTitle = !string.IsNullOrEmpty(chapterTitleAttr) ? chapterTitleAttr : "Oneshot"; // Nếu title có thì dùng title, không thì dùng Oneshot
            }


            return new ChapterInfo
            {
                Id = chapterId,
                Title = displayTitle,
                PublishedAt = publishedAt
            };
        }
        catch (Exception ex)
        {
            // Log lỗi cụ thể hơn nếu có thể
             if (ex is JsonException jsonEx) {
                 _logger.LogError(jsonEx, $"Lỗi JSON khi xử lý chapter ID: {chapterId}");
             } else if (ex is HttpRequestException httpEx) {
                  _logger.LogError(httpEx, $"Lỗi HTTP khi lấy chapter ID: {chapterId}");
             }
             else {
                 _logger.LogError(ex, $"Lỗi ngoại lệ không xác định khi lấy thông tin chi tiết cho chapter ID: {chapterId}");
             }
            return null; // Trả về null nếu có lỗi
        }
    }
    ```

5.  **Lưu file và chạy lại ứng dụng:** Kiểm tra lại trang lịch sử đọc truyện.

## Giải thích thay đổi (Lần 2)

-   **Loại bỏ `JsonConversionService`:** Chúng ta không còn chuyển đổi `attributes` thành `Dictionary<string, object>` nữa.
-   **Sử dụng `JsonElement` và `TryGetProperty`:** Giống như `GetChapterLanguageAsync`, chúng ta parse response thành `JsonElement` và dùng `TryGetProperty` để truy cập các thuộc tính (`attributes`, `chapter`, `title`, `publishAt`) một cách an toàn.
-   **Kiểm tra `ValueKind`:** Trước khi lấy giá trị, chúng ta kiểm tra `ValueKind` của `JsonElement` để đảm bảo nó đúng kiểu mong đợi (ví dụ: `JsonValueKind.String`, `JsonValueKind.Number`). Điều này giúp xử lý các trường hợp giá trị là `null` trong JSON hoặc có kiểu dữ liệu không mong đợi.
-   **Lấy giá trị:** Sử dụng các phương thức như `GetString()`, `TryGetDateTime()` để lấy giá trị một cách an toàn từ `JsonElement`.
-   **Xử lý `chapterNumber` là số:** Đã thêm xử lý trường hợp `chapter` trả về là số thay vì chuỗi.
-   **Xử lý `displayTitle` cho Oneshot:** Cập nhật logic tạo `displayTitle` để xử lý tốt hơn trường hợp `chapterNumber` là null hoặc rỗng (thường gặp ở Oneshot).

Cách tiếp cận này nhất quán hơn với phương thức `GetChapterLanguageAsync` đã hoạt động và giảm thiểu rủi ro gặp lỗi `RuntimeBinderException` do kiểu `dynamic` hoặc so sánh không tương thích.

Hãy thử lại với đoạn code mới này nhé!