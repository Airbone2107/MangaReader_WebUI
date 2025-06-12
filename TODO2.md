```markdown
# TODO.md

## Hotfix lỗi lặp tên chapter trong trang chi tiết manga

**Vấn đề:** Tên chapter bị lặp dạng (Chương 1: Chương 1: test).
**Yêu cầu:** Hotfix bằng cách loại bỏ việc gán chữ "Chương" nếu tên chapter đã có sẵn.

**File cần cập nhật:**

1.  `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaDataExtractorService.cs`

---

### 1. Cập nhật `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaDataExtractorService.cs`

**Nội dung code:**

```csharp
// MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaDataExtractorService.cs
// ...
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Services; // Cần cho CoverApiService static helper
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.UtilityServices; // Cần cho LocalizationService
using Microsoft.Extensions.Configuration; // Thêm using này
using Microsoft.AspNetCore.Http;        // Thêm using này
using System.Diagnostics;
using System.Text.Json; // Cần cho JsonException và JsonSerializer
using System.Text.RegularExpressions; // THÊM USING NÀY

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;

/// <summary>
/// Triển khai IMangaDataExtractor, chịu trách nhiệm trích xuất dữ liệu cụ thể từ Model MangaDex.
/// </summary>
public class MangaDataExtractorService : IMangaDataExtractor
{
// ... (giữ nguyên các phần khác của class) ...

    public string ExtractChapterDisplayTitle(ChapterAttributes attributes)
    {
        Debug.Assert(attributes != null, "ChapterAttributes không được null khi trích xuất Display Title.");

        string chapterNumberString = attributes.ChapterNumber ?? "?"; 
        string specificChapterTitle = attributes.Title?.Trim() ?? "";

        // Nếu không có số chương hoặc chapterNumber là "?", chỉ hiển thị title (nếu có), hoặc "Oneshot"
        if (string.IsNullOrEmpty(attributes.ChapterNumber) || attributes.ChapterNumber == "?")
        {
            return !string.IsNullOrEmpty(specificChapterTitle) ? specificChapterTitle : "Oneshot";
        }

        // Logic hotfix:
        // Kiểm tra xem specificChapterTitle đã bắt đầu bằng "Chương X" hoặc "Chapter X" (X là chapterNumberString)
        // một cách không phân biệt hoa thường và có thể có dấu hai chấm hoặc khoảng trắng theo sau.
        string patternChapterVn = $"^Chương\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";
        string patternChapterEn = $"^Chapter\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";
        // Có thể thêm các biến thể khác như "Ch.", "Chap." nếu cần
        // string patternCh = $"^Ch\\.\\s*{Regex.Escape(chapterNumberString)}([:\\s-]|$)";

        bool startsWithChapterInfo = Regex.IsMatch(specificChapterTitle, patternChapterVn, RegexOptions.IgnoreCase) ||
                                     Regex.IsMatch(specificChapterTitle, patternChapterEn, RegexOptions.IgnoreCase);
                                     // || Regex.IsMatch(specificChapterTitle, patternCh, RegexOptions.IgnoreCase)

        if (startsWithChapterInfo)
        {
            // specificChapterTitle đã bao gồm "Chương X" hoặc "Chapter X" rồi, dùng nó
            // Ta cần đảm bảo rằng nếu nó chỉ là "Chương X" (không có tên riêng) thì vẫn chuẩn
            // Ví dụ: title là "Chương 1", chapterNumber là "1" -> return "Chương 1"
            // Ví dụ: title là "Chương 1: Tên gì đó", chapterNumber là "1" -> return "Chương 1: Tên gì đó"
            return specificChapterTitle;
        }
        else if (!string.IsNullOrEmpty(specificChapterTitle))
        {
            // specificChapterTitle là tên riêng (không chứa "Chương X" ở đầu), ghép "Chương X: " vào
            return $"Chương {chapterNumberString}: {specificChapterTitle}";
        }
        else
        {
            // specificChapterTitle rỗng, chỉ hiển thị "Chương X"
            return $"Chương {chapterNumberString}";
        }
    }

// ... (giữ nguyên các phần khác của class) ...
}
```

---
**Hoàn tất.**