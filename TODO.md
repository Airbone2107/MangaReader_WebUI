# TODO: Tối ưu hóa Chuyển đổi Grid/List View trong Trang Tìm Kiếm

**Mục tiêu:** Giảm số lần gọi API backend khi người dùng tìm kiếm/lọc/phân trang và khi chuyển đổi giữa chế độ xem Grid và List. Sử dụng Session phía server để lưu trữ tạm thời dữ liệu manga của trang hiện tại, tránh gọi lại API khi chỉ đổi chế độ xem.

**Giải pháp:** "Giải pháp cân bằng" - Server render view mặc định ban đầu, lưu dữ liệu vào Session. Khi người dùng chuyển view, gọi một action mới trên server chỉ để render lại partial view khác bằng dữ liệu từ Session.

---

## Backend (ASP.NET Core)

### 1. Controller (`MangaController.cs`)

-   **Action `Search`:**
    -   [ ] **Gọi API một lần:** Đảm bảo `_mangaSearchService.SearchMangaAsync()` chỉ được gọi **một lần** trong action này để lấy `MangaListViewModel`.
    -   [ ] **Lưu dữ liệu vào Session:**
        -   Sau khi lấy thành công `viewModel.Mangas`, lưu danh sách này vào Session.
        -   Sử dụng một key cố định và **luôn ghi đè** dữ liệu cũ trong Session mỗi khi action `Search` được thực thi thành công (ví dụ: `HttpContext.Session.SetString("CurrentSearchResultData", JsonSerializer.Serialize(viewModel.Mangas));`).
        -   Cân nhắc tạo một lớp `SessionKeys` để quản lý các key.
        -   **Quan trọng:** Chỉ lưu `List<MangaViewModel>`, không lưu toàn bộ `MangaListViewModel` để tiết kiệm dung lượng Session. Thông tin phân trang/sort sẽ được truyền qua tham số request khi cần.
    -   [ ] **Xác định View Mode ban đầu:**
        -   Quyết định view mode mặc định (ví dụ: "grid").
        *   *(Tùy chọn nâng cao)*: Có thể thử đọc cookie/header do client-side JS đặt để ưu tiên chế độ xem người dùng đã lưu, nhưng mặc định "grid" là đủ đơn giản.
    -   [ ] **Render View ban đầu:**
        -   Truyền `MangaListViewModel` (bao gồm cả `Mangas` đã lấy) vào view `Search.cshtml`.
        -   **Không** render cả hai partial view (`_MangaGridPartial` và `_MangaListPartial`) trong `Search.cshtml` nữa. Thay vào đó, `Search.cshtml` sẽ chứa một container (ví dụ: `#search-results-container`) và chỉ render partial view mặc định *bên trong* container đó.
        -   Ví dụ trong `Search.cshtml`:
            ```html
            <div id="search-results-container" class="@(initialViewMode)-view">
                @if (initialViewMode == "grid") {
                    <partial name="_MangaGridPartial" model="Model.Mangas" />
                } else {
                    <partial name="_MangaListPartial" model="Model.Mangas" />
                }
            </div>
            ```
-   **Action Mới `GetMangaViewPartial`:**
    -   [ ] Tạo một `IActionResult` mới, ví dụ: `public IActionResult GetMangaViewPartial(string viewMode = "grid")`.
    -   [ ] **Lấy dữ liệu từ Session:**
        -   Đọc dữ liệu `List<MangaViewModel>` từ Session bằng key đã định nghĩa.
        -   `var mangasJson = HttpContext.Session.GetString("CurrentSearchResultData");`
        -   Deserialize JSON: `var mangas = JsonSerializer.Deserialize<List<MangaViewModel>>(mangasJson ?? "[]");`
    -   [ ] **Xử lý Session rỗng/hết hạn:** Nếu không có dữ liệu trong Session (trả về `null` hoặc `[]`), trả về một PartialView thông báo lỗi hoặc nội dung trống (`return PartialView("_NoResultsPartial");` hoặc tương tự).
    -   [ ] **Render Partial View tương ứng:**
        -   Dựa vào tham số `viewMode`, chọn partial view đúng (`_MangaGridPartial` hoặc `_MangaListPartial`).
        -   Truyền danh sách `mangas` đã lấy từ Session làm model cho partial view.
        -   `return PartialView(viewMode == "grid" ? "_MangaGridPartial" : "_MangaListPartial", mangas);`
    -   [ ] Đảm bảo action này **không** gọi lại `_mangaSearchService.SearchMangaAsync()` hay bất kỳ hàm nào gọi API MangaDex.

### 2. Session Configuration (`Program.cs`)

-   [ ] Đảm bảo Session đã được cấu hình đúng:
    ```csharp
    builder.Services.AddDistributedMemoryCache(); // Hoặc cache khác nếu cần
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(20); // Đặt timeout hợp lý
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });
    // ...
    app.UseSession(); // Đảm bảo gọi UseSession() trước UseEndpoints/MapControllerRoute
    ```

### 3. Views

-   **`Search.cshtml`:**
    -   [ ] Xóa bỏ `div` có `hx-trigger="load"` gọi `GetSearchResultsPartial`.
    -   [ ] Thay thế bằng container `#search-results-container` và render partial view mặc định bên trong như mô tả ở mục 1.
    -   [ ] **Cập nhật nút chuyển đổi View Mode:**
        -   Sửa thuộc tính `hx-get` của các nút trong `#view-mode-toggle`.
        -   Nút Grid: `hx-get="@Url.Action("GetMangaViewPartial", "Manga", new { viewMode = "grid" })"`
        -   Nút List: `hx-get="@Url.Action("GetMangaViewPartial", "Manga", new { viewMode = "list" })"`
        -   Đặt `hx-target="#search-results-container"` cho cả hai nút.
        -   Đặt `hx-swap="innerHTML"` (hoặc `outerHTML` nếu muốn thay thế cả container).
        -   *(Khuyến nghị)*: Thêm `hx-push-url="false"` để không thay đổi URL khi chỉ đổi view.
-   **`_SearchResultsWrapperPartial.cshtml`:**
    -   [ ] File này có thể **không cần thiết nữa** nếu `Search.cshtml` render trực tiếp partial grid/list vào `#search-results-container`. Xem xét xóa hoặc điều chỉnh nếu vẫn muốn giữ cấu trúc wrapper.
-   **`_MangaGridPartial.cshtml` & `_MangaListPartial.cshtml`:**
    -   [ ] Đảm bảo chúng nhận `List<MangaViewModel>` làm model và render đúng. Không cần thay đổi gì ở đây.

---

## Frontend (JavaScript)

### 1. `search.js`

-   **`initViewModeToggle()`:**
    -   [ ] **Xóa logic thay đổi class `.grid-view`/`.list-view` trên container:** HTMX sẽ thay thế toàn bộ nội dung của `#search-results-container`, nên không cần JS thay đổi class này nữa.
    -   [ ] **Giữ lại logic cập nhật trạng thái `active` cho các nút:** Sau khi HTMX swap thành công (sử dụng event `htmx:afterSwap` trong `htmx-handlers.js`), cập nhật class `active` cho nút tương ứng với `viewMode` vừa được tải.
    -   [ ] **Giữ lại logic lưu `viewMode` vào `localStorage`:** Khi người dùng click nút, lưu lựa chọn của họ.
-   **`applySavedViewMode()`:**
    -   [ ] **Thay đổi:** Hàm này không cần áp dụng class `.grid-view`/`.list-view` nữa.
    -   [ ] **Giữ lại:** Chỉ cần cập nhật trạng thái `active` của các nút toggle dựa trên giá trị đã lưu trong `localStorage` khi trang tải. *Không* cần trigger HTMX request ở đây, vì server đã render view mặc định (hoặc view ưu tiên nếu bạn triển khai nâng cao).

### 2. `htmx-handlers.js`

-   **`reinitializeAfterHtmxSwap(targetElement)`:**
    -   [ ] **Thêm logic cập nhật nút View Mode:**
        -   Kiểm tra nếu `targetElement` là `#search-results-container`.
        -   Nếu đúng, đọc `viewMode` từ `localStorage` (hoặc xác định từ nội dung vừa tải nếu dễ dàng hơn).
        -   Gọi hàm trong `search.js` (hoặc thực hiện trực tiếp) để cập nhật class `active` cho các nút trong `#view-mode-toggle`.

---

## Kiểm Thử

-   [ ] Kiểm tra tải trang Search lần đầu: Server render view mặc định (grid), API chỉ gọi 1 lần.
-   [ ] Kiểm tra chuyển đổi view: Click nút List -> HTMX gọi `GetMangaViewPartial?viewMode=list` -> Server trả về HTML của List View (dùng dữ liệu Session) -> Nội dung container được thay thế. API **không** được gọi lại. Click nút Grid -> tương tự.
-   [ ] Kiểm tra tìm kiếm/lọc: Nhập từ khóa/chọn filter -> Submit form -> Action `Search` chạy -> API gọi 1 lần -> Dữ liệu Session được **ghi đè** -> Server render view mặc định (grid) với dữ liệu mới.
-   [ ] Kiểm tra phân trang: Click link phân trang -> Action `Search` chạy (với tham số `page` mới) -> API gọi 1 lần -> Dữ liệu Session được **ghi đè** -> Server render view mặc định (grid) với dữ liệu trang mới.
-   [ ] Kiểm tra Session Timeout: Để trang Search một lúc cho Session hết hạn -> Click nút chuyển view -> Server nên trả về thông báo lỗi/trống thay vì crash.
-   [ ] Kiểm tra lưu trạng thái view: Chọn List View -> Tải lại trang Search (hoặc tìm kiếm lại) -> View mặc định (Grid) được hiển thị, nhưng nút List View vẫn nên được đánh dấu `active` (do đọc từ `localStorage`). Người dùng có thể click lại nếu muốn List View.

---

Hoàn thành các bước trên sẽ giúp tối ưu hiệu suất trang tìm kiếm đáng kể. Chúc bạn thành công!