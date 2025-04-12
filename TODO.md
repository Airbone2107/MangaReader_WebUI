Chào bạn, tôi đã xem xét yêu cầu của bạn về việc loại bỏ danh sách theo dõi và lịch sử đọc khỏi trang Profile và menu người dùng, đồng thời tích hợp HTMX cho trang Login và Profile.

Dưới đây là các bước chi tiết để thực hiện những thay đổi này. Tôi sẽ tạo file `TODO.md` để bạn tiện theo dõi và thực hiện.

```markdown
# TODO.md: Đơn Giản Hóa Trang Profile và Tích Hợp HTMX cho Auth

## Mục tiêu

1.  Loại bỏ hoàn toàn tab "Đang theo dõi" và "Lịch sử đọc" khỏi trang Profile (`/Auth/Profile`). Trang Profile sẽ chỉ hiển thị thông tin cơ bản của người dùng.
2.  Loại bỏ các liên kết "Danh sách theo dõi" và "Lịch sử đọc" khỏi menu dropdown của người dùng trong header.
3.  Cập nhật các liên kết đến trang Login (`/Auth/Login`) và Profile (`/Auth/Profile`) để sử dụng HTMX, tải nội dung vào `#main-content`.
4.  Cập nhật `AuthController` để xử lý các request HTMX cho action `Login` và `Profile`.

## Các Bước Thực Hiện Chi Tiết

### 1. Frontend: Loại Bỏ Tabs và Nội Dung Khỏi Trang Profile

**File cần sửa:** `manga_reader_web\Views\Auth\Profile.cshtml`

**Công việc:**

1.  **Xóa bỏ cấu trúc Tabs:** Xóa toàn bộ phần `nav-tabs` (`<ul class="nav nav-tabs card-header-tabs"...>`) và `tab-content` (`<div class="tab-content"...>`).
2.  **Giữ lại phần thông tin người dùng:** Giữ lại cột chứa thông tin cơ bản (ảnh đại diện, tên, email, nút đăng xuất).
3.  **Điều chỉnh layout:** Đảm bảo phần thông tin người dùng còn lại hiển thị đúng trong layout mới (có thể cần điều chỉnh class `col-md-4` và `col-md-8`).

**Mã nguồn cần sửa (Xóa các phần được đánh dấu):**

```html
@model manga_reader_web.Models.ProfileViewModel
@{
    ViewData["Title"] = "Trang cá nhân";
}

<div class="container mt-4">
    <div class="row justify-content-center"> @* Thay đổi thành justify-content-center nếu chỉ còn 1 cột *@
        <div class="col-md-6 col-lg-5"> @* Điều chỉnh kích thước cột cho phù hợp *@
            <div class="card shadow-sm mb-4">
                <div class="card-body text-center">
                    @if (!string.IsNullOrEmpty(Model.User.PhotoURL))
                    {
                        <img src="@Model.User.PhotoURL" alt="Ảnh đại diện" class="rounded-circle img-fluid mb-3" style="max-width: 150px;" />
                    }
                    else
                    {
                        <div class="bg-light rounded-circle d-inline-flex justify-content-center align-items-center mb-3" style="width: 150px; height: 150px;">
                            <i class="bi bi-person-fill" style="font-size: 4rem;"></i>
                        </div>
                    }
                    <h5 class="mb-1">@Model.User.DisplayName</h5>
                    <p class="text-muted">@Model.User.Email</p>
                    <a href="@Url.Action("Logout", "Auth")" class="btn btn-outline-danger mt-3"> @* Thêm margin top nếu cần *@
                        <i class="bi bi-box-arrow-right me-1"></i> Đăng xuất
                    </a>
                </div>
            </div>
        </div>

        @* ----- BẮT ĐẦU PHẦN CẦN XÓA ----- *@
        @*
        <div class="col-md-8">
            <div class="card shadow-sm">
                <div class="card-header">
                    <ul class="nav nav-tabs card-header-tabs" id="profileTabs" role="tablist">
                        <li class="nav-item" role="presentation">
                            <button class="nav-link active" id="following-tab" data-bs-toggle="tab" data-bs-target="#following" type="button" role="tab" aria-controls="following" aria-selected="true">
                                <i class="bi bi-heart-fill me-1"></i> Đang theo dõi (@Model.FollowingMangas.Count)
                            </button>
                        </li>
                        <li class="nav-item" role="presentation">
                            <button class="nav-link" id="history-tab" data-bs-toggle="tab" data-bs-target="#history" type="button" role="tab" aria-controls="history" aria-selected="false">
                                <i class="bi bi-clock-history me-1"></i> Lịch sử đọc
                            </button>
                        </li>
                    </ul>
                </div>
                <div class="card-body">
                    <div class="tab-content" id="profileTabsContent">
                        <div class="tab-pane fade show active" id="following" role="tabpanel" aria-labelledby="following-tab">
                            <!-- Nội dung tab Đang theo dõi -->
                        </div>

                        <div class="tab-pane fade" id="history" role="tabpanel" aria-labelledby="history-tab">
                            <!-- Nội dung tab Lịch sử đọc -->
                        </div>
                    </div>
                </div>
            </div>
        </div>
        *@
        @* ----- KẾT THÚC PHẦN CẦN XÓA ----- *@
    </div>
</div>

<!-- Toast thông báo (Giữ lại nếu cần cho các chức năng khác) -->
<div class="position-fixed bottom-0 end-0 p-3" style="z-index: 11">
    <div id="notificationToast" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="toast-header">
            <i class="bi bi-info-circle me-2"></i>
            <strong class="me-auto" id="toastTitle">Thông báo</strong>
            <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body" id="toastMessage">

        </div>
    </div>
</div>

@* Giữ lại Section Scripts nếu cần cho Toast hoặc chức năng khác *@
@section Scripts {
    <script>
        // Khởi tạo toast
        let toastElement = document.getElementById('notificationToast');
        let toast = toastElement ? new bootstrap.Toast(toastElement, { delay: 3000 }) : null;

        function showNotification(title, message, type = 'info') {
            // ... (giữ nguyên code showNotification)
        }

        // Xóa bỏ các hàm setupUnfollowButtons và setupContinueReadingButtons nếu không còn sử dụng
        // document.addEventListener('DOMContentLoaded', function () {
        //     // setupUnfollowButtons(); // Xóa hoặc comment dòng này
        //     // setupContinueReadingButtons(); // Xóa hoặc comment dòng này
        // });

        // Xóa bỏ hàm setupUnfollowButtons và unfollowManga nếu không còn nút unfollow
        // function setupUnfollowButtons() { ... }
        // function unfollowManga(mangaId, title, buttonElement) { ... }
        // function setupContinueReadingButtons() { ... }
    </script>
}
```

### 2. Backend: Cập Nhật `AuthController` và `ProfileViewModel`

**File cần sửa:** `manga_reader_web\Controllers\AuthController.cs`

**Công việc:**

1.  **Inject `ViewRenderService`:** Nếu chưa có, inject `ViewRenderService` vào constructor.
    *   **Nhắc nhở:** Bạn đã inject `ViewRenderService` trong các controller khác như `MangaController`, `ChapterController`. Hãy làm tương tự.
2.  **Cập nhật Action `Profile`:**
    *   Xóa bỏ logic lấy danh sách `FollowingMangas` (không cần gọi `_mangaDetailsService` nữa).
    *   Đảm bảo `ViewModel` chỉ chứa `User`.
    *   Sử dụng `_viewRenderService.RenderViewBasedOnRequest` để trả về `View` hoặc `PartialView`.
3.  **Cập nhật Action `Login`:**
    *   Sử dụng `_viewRenderService.RenderViewBasedOnRequest` để trả về `View` hoặc `PartialView`.

**Mã nguồn cần thêm/sửa trong `AuthController.cs`:**

```csharp
// Thêm using nếu cần
using manga_reader_web.Services.UtilityServices;
using manga_reader_web.Models; // Namespace của ProfileViewModel
using manga_reader_web.Models.Auth; // Namespace của UserModel

namespace manga_reader_web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;
        // private readonly IMangaFollowService _mangaFollowService; // Không cần nữa nếu không dùng
        // private readonly MangaDetailsService _mangaDetailsService; // Không cần nữa
        private readonly ViewRenderService _viewRenderService; // THÊM INJECTION

        public AuthController(
            IUserService userService,
            ILogger<AuthController> logger,
            // IMangaFollowService mangaFollowService, // Bỏ injection này
            // MangaDetailsService mangaDetailsService, // Bỏ injection này
            ViewRenderService viewRenderService // THÊM VÀO CONSTRUCTOR
            )
        {
            _userService = userService;
            _logger = logger;
            // _mangaFollowService = mangaFollowService; // Bỏ gán
            // _mangaDetailsService = mangaDetailsService; // Bỏ gán
            _viewRenderService = viewRenderService; // GÁN GIÁ TRỊ
        }

        // GET: /Auth/Login
        public IActionResult Login(string returnUrl = null)
        {
            _logger.LogInformation("Hiển thị trang Login.");
            ViewBag.ReturnUrl = returnUrl;
            // SỬ DỤNG VIEWRENDERSERVICE
            return _viewRenderService.RenderViewBasedOnRequest(this, "Login", null); // Truyền null vì view Login không cần model
        }

        // ... (Các action GoogleLogin, Callback, Logout giữ nguyên) ...

        // GET: /Auth/Profile
        public async Task<IActionResult> Profile()
        {
            _logger.LogInformation("Yêu cầu trang Profile.");
            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, chuyển hướng đến Login.");
                // Nếu là HTMX request, trả về partial yêu cầu đăng nhập
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                     // Có thể trả về Unauthorized() để HTMX xử lý hoặc một partial view
                     // return Unauthorized();
                     return PartialView("_UnauthorizedPartial"); // Tạo partial này nếu muốn
                }
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile", "Auth") });
            }

            try
            {
                // Lấy thông tin người dùng
                var user = await _userService.GetUserInfoAsync();

                if (user == null)
                {
                    _logger.LogError("Không thể lấy thông tin người dùng đã đăng nhập.");
                    TempData["ErrorMessage"] = "Không thể lấy thông tin người dùng. Vui lòng đăng nhập lại.";
                     if (Request.Headers.ContainsKey("HX-Request"))
                     {
                         ViewBag.ErrorMessage = "Không thể lấy thông tin người dùng.";
                         return PartialView("_ErrorPartial"); // Tạo partial này nếu muốn
                     }
                    return RedirectToAction("Login");
                }

                // Tạo ViewModel chỉ với thông tin User
                var viewModel = new ProfileViewModel
                {
                    User = user
                    // Không cần FollowingMangas nữa
                };

                _logger.LogInformation($"Hiển thị trang Profile cho user: {user.Email}");
                // SỬ DỤNG VIEWRENDERSERVICE
                return _viewRenderService.RenderViewBasedOnRequest(this, "Profile", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang profile người dùng");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang profile.";
                 if (Request.Headers.ContainsKey("HX-Request"))
                 {
                     ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải trang profile.";
                     return PartialView("_ErrorPartial");
                 }
                return RedirectToAction("Index", "Home");
            }
        }

         // ... (Action GetCurrentUser giữ nguyên) ...
    }
}

// File: manga_reader_web\Models\ProfileViewModel.cs
namespace manga_reader_web.Models
{
    public class ProfileViewModel
    {
        public UserModel User { get; set; }
        // public List<MangaViewModel> FollowingMangas { get; set; } = new List<MangaViewModel>(); // XÓA DÒNG NÀY
    }
}
```

**File cần tạo (nếu chưa có):**

*   `manga_reader_web\Views\Shared\_UnauthorizedPartial.cshtml` (Bạn có thể copy từ file này trong project hoặc tạo mới)
*   `manga_reader_web\Views\Shared\_ErrorPartial.cshtml` (Bạn có thể copy từ file này trong project hoặc tạo mới)

### 3. Frontend: Loại Bỏ Links Khỏi User Dropdown

**File cần sửa:** `manga_reader_web\Views\Shared\_Layout.cshtml`

**Công việc:**

1.  Tìm đến phần `#authenticatedUserMenu`.
2.  Xóa các thẻ `<a>` của "Danh sách theo dõi" và "Lịch sử đọc".

**Mã nguồn cần sửa (Xóa các dòng được đánh dấu `-`):**

```html
<!-- Menu cho người dùng đã đăng nhập -->
<div id="authenticatedUserMenu" class="d-none">
    <a class="dropdown-item" href="@Url.Action("Profile", "Auth")"><i class="bi bi-person me-2"></i>Trang cá nhân</a>
-   <a class="dropdown-item" href="@Url.Action("Profile", "Auth")#following"><i class="bi bi-bookmark me-2"></i>Danh sách theo dõi</a>
-   <a class="dropdown-item" href="@Url.Action("Profile", "Auth")#history"><i class="bi bi-clock-history me-2"></i>Lịch sử đọc</a>
    <hr class="dropdown-divider">
    <a class="dropdown-item" href="@Url.Action("Logout", "Auth")"><i class="bi bi-box-arrow-right me-2"></i>Đăng xuất</a>
</div>
```

### 4. Frontend: Cập Nhật Links Để Sử Dụng HTMX

**File cần sửa:** `manga_reader_web\Views\Shared\_Layout.cshtml`

**Công việc:**

1.  **Link Đăng nhập:** Tìm thẻ `<a>` trong `#guestUserMenu` trỏ đến `Auth/Login`. Thêm các thuộc tính HTMX `hx-get`, `hx-target`, `hx-push-url`.
2.  **Link Trang cá nhân:** Tìm thẻ `<a>` trong `#authenticatedUserMenu` trỏ đến `Auth/Profile`. Thêm các thuộc tính HTMX `hx-get`, `hx-target`, `hx-push-url`.

**Mã nguồn cần sửa (Thêm các thuộc tính được đánh dấu `+`):**

```html
<!-- Menu cho người dùng chưa đăng nhập -->
<div id="guestUserMenu">
    <a class="dropdown-item"
       href="@Url.Action("Login", "Auth")"
+      hx-get="@Url.Action("Login", "Auth")"
+      hx-target="#main-content"
+      hx-push-url="true">
        <i class="bi bi-box-arrow-in-right me-2"></i>Đăng nhập
    </a>
</div>

<!-- Menu cho người dùng đã đăng nhập -->
<div id="authenticatedUserMenu" class="d-none">
    <a class="dropdown-item"
       href="@Url.Action("Profile", "Auth")"
+      hx-get="@Url.Action("Profile", "Auth")"
+      hx-target="#main-content"
+      hx-push-url="true">
        <i class="bi bi-person me-2"></i>Trang cá nhân
    </a>
    @* Các link đã xóa ở bước trước *@
    <hr class="dropdown-divider">
    <a class="dropdown-item" href="@Url.Action("Logout", "Auth")"><i class="bi bi-box-arrow-right me-2"></i>Đăng xuất</a>
</div>
```

### 5. Backend: Đăng Ký `ViewRenderService` (Nếu Chưa Có)

**File cần sửa:** `manga_reader_web\Program.cs`

**Công việc:** Đảm bảo rằng `ViewRenderService` đã được đăng ký.

**Mã nguồn cần kiểm tra/thêm:**

```csharp
// ... các đăng ký services khác ...

// Đăng ký ViewRenderService (Đảm bảo dòng này tồn tại)
builder.Services.AddScoped<manga_reader_web.Services.UtilityServices.ViewRenderService>();

// ... các đăng ký services khác ...
```

### 6. Kiểm Tra

1.  Chạy ứng dụng (`dotnet run`).
2.  **Kiểm tra Trang Profile:**
    *   Đăng nhập vào tài khoản.
    *   Click vào "Trang cá nhân" trong menu người dùng.
    *   **Xác nhận:** Nội dung trang Profile chỉ hiển thị thông tin người dùng (ảnh, tên, email, nút đăng xuất), không còn các tab "Đang theo dõi" hay "Lịch sử đọc".
    *   **Xác nhận:** Trang Profile được tải vào `#main-content` mà không load lại toàn bộ trang. URL trên thanh địa chỉ thay đổi thành `/Auth/Profile`.
3.  **Kiểm tra Menu Người Dùng:**
    *   Click vào menu người dùng ở góc trên bên phải.
    *   **Xác nhận:** Menu chỉ còn các mục "Trang cá nhân" và "Đăng xuất", không còn "Danh sách theo dõi" và "Lịch sử đọc".
4.  **Kiểm tra Trang Login:**
    *   Đăng xuất (nếu đang đăng nhập).
    *   Click vào "Đăng nhập" trong menu người dùng.
    *   **Xác nhận:** Trang Login được tải vào `#main-content` mà không load lại toàn bộ trang. URL trên thanh địa chỉ thay đổi thành `/Auth/Login`.
5.  **Kiểm tra Console:** Mở Developer Tools (F12) và kiểm tra tab Console xem có lỗi JavaScript nào không, đặc biệt là sau khi HTMX tải nội dung.
6.  **Kiểm tra Network:** Kiểm tra tab Network để xem các request HTMX đến `/Auth/Profile` và `/Auth/Login` có được gửi đúng và nhận về partial view không.

Chúc bạn thực hiện thành công!
```

Bạn hãy lưu nội dung trên vào file `TODO.md` và thực hiện theo các bước đã hướng dẫn nhé. Nếu có bất kỳ vấn đề gì, đừng ngần ngại hỏi lại!