# TODO.md: Triển khai IViewLocationExpander để tùy chỉnh đường dẫn tìm kiếm View

## Mục tiêu

Cấu hình ASP.NET Core Razor View Engine để tự động tìm kiếm các file View (`.cshtml`) và Partial View (`_.cshtml`) trong các thư mục con mới được tạo trong `Views/` (ví dụ: `Views/MangaSearch/`, `Views/ChapterRead/`, `Views/Auth/`, v.v.). Điều này cho phép gọi `View("ActionName")` hoặc `PartialView("_PartialName")` từ Controller và `<partial name="_PartialName" />` từ View mà không cần chỉ định đường dẫn đầy đủ.

## Tại sao cần làm điều này?

Sau khi tái cấu trúc thư mục `Views`, cơ chế tìm kiếm View mặc định của ASP.NET Core (dựa trên tên Controller và thư mục `Shared`) không còn hoạt động hiệu quả cho cấu trúc mới. `IViewLocationExpander` cho phép chúng ta "dạy" cho View Engine biết những nơi mới cần tìm kiếm.

## Các bước thực hiện

1.  **Tạo thư mục Infrastructure (Nếu chưa có):**
    *   Trong thư mục gốc của dự án (`manga_reader_web`), tạo một thư mục mới tên là `Infrastructure`. (Hoặc bạn có thể đặt lớp Expander trong thư mục `Helpers` hoặc `Services` tùy ý).

2.  **Tạo lớp `CustomViewLocationExpander.cs`:**
    *   Trong thư mục `Infrastructure` (hoặc thư mục bạn đã chọn), tạo một file mới tên là `CustomViewLocationExpander.cs`.
    *   Dán đoạn code sau vào file:

    ```csharp
    // Infrastructure/CustomViewLocationExpander.cs
    using Microsoft.AspNetCore.Mvc.Razor;
    using System.Collections.Generic;
    using System.Linq;

    namespace manga_reader_web.Infrastructure // Đảm bảo namespace phù hợp
    {
        /// <summary>
        /// Mở rộng cách Razor View Engine tìm kiếm các file View và Partial View
        /// để phù hợp với cấu trúc thư mục theo feature.
        /// </summary>
        public class CustomViewLocationExpander : IViewLocationExpander
        {
            // Danh sách các thư mục "feature" bạn đã tạo trong Views/
            private static readonly string[] FeatureFolders = {
                "Auth",
                "ChapterRead",
                "Home",
                "MangaDetails",
                "MangaFollowed",
                "MangaHistory",
                "MangaSearch"
                // Thêm các thư mục feature khác nếu bạn tạo thêm
            };

            /// <summary>
            /// Được gọi bởi View Engine để lấy danh sách các đường dẫn tìm kiếm View.
            /// </summary>
            /// <param name="context">Thông tin về View đang được tìm kiếm.</param>
            /// <param name="viewLocations">Danh sách các đường dẫn tìm kiếm mặc định.</param>
            /// <returns>Danh sách các đường dẫn tìm kiếm đã được mở rộng.</returns>
            public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
            {
                // {0} = Tên View (hoặc Partial View không có dấu _)
                // {1} = Tên Controller (ít dùng trong cấu trúc này)
                // {2} = Tên Area (không dùng trong dự án này)

                // Ưu tiên tìm kiếm trong các thư mục feature đã định nghĩa
                foreach (var folder in FeatureFolders)
                {
                    // Đường dẫn cho View chính (ví dụ: Search.cshtml -> {0} = "Search")
                    yield return $"~/Views/{folder}/{{0}}.cshtml";
                    // Đường dẫn cho Partial View (ví dụ: _SearchFormPartial.cshtml -> {0} = "SearchFormPartial")
                    yield return $"~/Views/{folder}/_{{0}}.cshtml";
                }

                // Luôn tìm kiếm trong thư mục Shared (rất quan trọng)
                yield return "~/Views/Shared/{0}.cshtml";
                yield return "~/Views/Shared/_{0}.cshtml"; // Cho partials trong Shared

                // --- Tùy chọn: Giữ lại các đường dẫn mặc định ---
                // Bỏ comment phần dưới nếu bạn muốn View Engine vẫn tìm ở các vị trí cũ
                // (ví dụ: /Views/ControllerName/ActionName.cshtml) phòng trường hợp bạn chưa di chuyển hết.
                // Tuy nhiên, nếu đã di chuyển hết, việc này có thể không cần thiết và làm chậm quá trình tìm kiếm một chút.
                /*
                foreach (var location in viewLocations)
                {
                    yield return location;
                }
                */
            }

            /// <summary>
            /// Được gọi bởi View Engine để thêm các giá trị vào RouteData,
            /// thường dùng cho việc cache key. Không cần thiết cho trường hợp này.
            /// </summary>
            public void PopulateValues(ViewLocationExpanderContext context)
            {
                // Không cần thực hiện gì ở đây.
            }
        }
    }
    ```

3.  **Đăng ký `CustomViewLocationExpander` trong `Program.cs`:**
    *   Mở file `Program.cs`.
    *   Thêm dòng `using` cho namespace của lớp expander ở đầu file:
        ```csharp
        using manga_reader_web.Infrastructure; // Hoặc namespace bạn đã đặt
        using Microsoft.AspNetCore.Mvc.Razor; // Cần thêm using này
        ```
    *   Tìm đến phần cấu hình services (`builder.Services...`).
    *   Thêm đoạn code sau **trước** dòng `var app = builder.Build();`:

        ```csharp
        // Cấu hình Razor View Engine để sử dụng View Location Expander tùy chỉnh
        builder.Services.Configure<RazorViewEngineOptions>(options =>
        {
            // Thêm expander của bạn vào danh sách.
            // Nó sẽ được gọi để cung cấp các đường dẫn tìm kiếm bổ sung (hoặc thay thế).
            options.ViewLocationExpanders.Add(new CustomViewLocationExpander());
        });
        ```

4.  **Đơn giản hóa các lệnh gọi `View()` và `PartialView()` (Quan trọng!):**
    *   Bây giờ View Engine đã biết cách tìm View trong cấu trúc mới, bạn có thể (và nên) **quay lại các Controller và View để đơn giản hóa các lệnh gọi**:
    *   **Trong Controllers:**
        *   Thay thế các lệnh gọi `View("~/Views/MangaSearch/Search.cshtml", model)` thành `View("Search", model)`.
        *   Thay thế các lệnh gọi `PartialView("~/Views/MangaSearch/_SearchResultsWrapperPartial.cshtml", viewModel)` thành `PartialView("_SearchResultsWrapperPartial", viewModel)`.
        *   Thay thế các lệnh gọi `_viewRenderService.RenderViewBasedOnRequest(this, "~/Views/MangaDetails/Details.cshtml", viewModel)` thành `_viewRenderService.RenderViewBasedOnRequest(this, "Details", viewModel)`.
        *   **Áp dụng tương tự cho tất cả các Controller khác (`Home`, `Auth`, `Chapter`) và các Action trả về `View` hoặc `PartialView`.**
    *   **Trong Views (`.cshtml` files):**
        *   Thay thế các thẻ `<partial name="~/Views/MangaSearch/_SearchFormPartial.cshtml" ... />` thành `<partial name="_SearchFormPartial" ... />`.
        *   Thay thế các lệnh gọi `@Html.Partial("~/Views/MangaFollowed/_FollowedMangaItemPartial.cshtml", manga)` thành `@Html.Partial("_FollowedMangaItemPartial", manga)`.
        *   **Áp dụng tương tự cho tất cả các file `.cshtml` có gọi Partial View nằm trong các thư mục feature.** (Các partial trong `Shared` thường không cần thay đổi).

5.  **Build và Chạy thử:**
    *   Build lại dự án (`dotnet build`).
    *   Chạy ứng dụng (`dotnet run`).
    *   Truy cập tất cả các trang và chức năng, đặc biệt là những trang có View hoặc Partial View đã được di chuyển.
    *   Kiểm tra xem tất cả các trang có hiển thị đúng không và không còn lỗi `InvalidOperationException: The view '...' was not found`.
    *   Kiểm tra kỹ các chức năng sử dụng HTMX để đảm bảo các partial view được tải chính xác khi chỉ dùng tên.

## Giải thích cách hoạt động

*   `IViewLocationExpander` cho phép bạn can thiệp vào quá trình tìm kiếm View của Razor.
*   Phương thức `ExpandViewLocations` trả về một danh sách các mẫu đường dẫn mà View Engine sẽ thử.
*   Chúng ta đã thêm các mẫu đường dẫn trỏ đến các thư mục feature mới (`~/Views/FeatureFolder/{0}.cshtml` và `~/Views/FeatureFolder/_{0}.cshtml`).
*   Chúng ta cũng giữ lại đường dẫn tìm kiếm trong `~/Views/Shared/`.
*   Do đó, khi Controller gọi `View("Search")` hoặc View gọi `<partial name="_SearchFormPartial" />`, View Engine sẽ thử các đường dẫn chúng ta đã cung cấp (bao gồm `~/Views/MangaSearch/Search.cshtml` và `~/Views/MangaSearch/_SearchFormPartial.cshtml`) và tìm thấy file chính xác.

Bằng cách này, bạn có thể giữ code trong Controller và View gọn gàng hơn trong khi vẫn duy trì cấu trúc thư mục `Views` có tổ chức.