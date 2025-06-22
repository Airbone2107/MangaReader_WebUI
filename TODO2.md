# TODO: Tái cấu trúc MangaReader_WebUI để sử dụng MangaReaderLib

Tài liệu này hướng dẫn chi tiết các bước để tái cấu trúc project `MangaReader_WebUI`, loại bỏ hoàn toàn nguồn dữ liệu MangaDex và tập trung sử dụng `MangaReaderLib` làm nguồn dữ liệu duy nhất, theo kế hoạch đã đề ra trong `Plan.md` (Bước 2 và 3).

## Bước 2: Dọn Dẹp Mã Nguồn MangaDex khỏi `MangaReader_WebUI`

Mục tiêu của bước này là loại bỏ tất cả các file, thư mục, và cấu hình liên quan đến việc lấy dữ liệu từ MangaDex để chuẩn bị cho việc tích hợp `MangaReaderLib`.

### 2.1. Xóa các File và Thư mục không cần thiết

Hãy xóa các file và thư mục sau khỏi project `MangaReader_WebUI`:

- **File OpenAPI của MangaDex:**
  - `MangaReader_WebUI\api.yaml`

- **Models của MangaDex:**
  - Toàn bộ thư mục: `MangaReader_WebUI\Models\MangaDex\` (bao gồm `Author.cs`, `Chapter.cs`, `Cover.cs`, `ErrorResponse.cs`, `Manga.cs`, `Relationship.cs`, `ScanlationGroup.cs`, `Tag.cs`).

- **Enum về nguồn truyện:**
  - `MangaReader_WebUI\Enums\MangaSource.cs`

- **Các Service gọi API MangaDex (qua proxy):**
  - Toàn bộ thư mục: `MangaReader_WebUI\Services\APIServices\Services\`
  - Toàn bộ thư mục: `MangaReader_WebUI\Services\APIServices\Interfaces\`
  - File: `MangaReader_WebUI\Services\APIServices\BaseApiService.cs`

- **Các Mappers dành cho MangaDex:**
  - Toàn bộ thư mục: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaMapper\`
  - Toàn bộ thư mục: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaMapper\`
  - File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\IMangaDataExtractor.cs`
  - File: `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaDataExtractorService.cs`

- **Logic quản lý đa nguồn:**
  - Toàn bộ thư mục: `MangaReader_WebUI\Services\MangaServices\MangaSourceManager\`

### 2.2. Cập nhật file `.csproj`

Mở file `MangaReader_WebUI\MangaReader.WebUI.csproj` và đảm bảo nó không chứa các tham chiếu không cần thiết. File này gần như không thay đổi, nhưng hãy đảm bảo nó trông như sau:

<!-- MangaReader_WebUI\MangaReader.WebUI.csproj -->
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="wwwroot\js\modules\common\**" />
    <Compile Remove="wwwroot\js\modules\components\**" />
    <Compile Remove="wwwroot\js\modules\pages\**" />
    <Content Remove="wwwroot\js\modules\common\**" />
    <Content Remove="wwwroot\js\modules\components\**" />
    <Content Remove="wwwroot\js\modules\pages\**" />
    <EmbeddedResource Remove="wwwroot\js\modules\common\**" />
    <EmbeddedResource Remove="wwwroot\js\modules\components\**" />
    <EmbeddedResource Remove="wwwroot\js\modules\pages\**" />
    <None Remove="wwwroot\js\modules\common\**" />
    <None Remove="wwwroot\js\modules\components\**" />
    <None Remove="wwwroot\js\modules\pages\**" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="9.0.5" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.TagHelpers" Version="2.3.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include="Models\ViewModels\" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\MangaReaderLib\MangaReaderLib.csproj" />
  </ItemGroup>

</Project>
```

### 2.3. Cập nhật file `appsettings.json`

Cấu hình này đã đúng, chỉ cần `MangaReaderApiSettings` cho dữ liệu truyện và `BackendApi` cho xác thực. Hãy đảm bảo nội dung file `MangaReader_WebUI\appsettings.json` như sau:

<!-- MangaReader_WebUI\appsettings.json -->
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "BackendApi": {
    "BaseUrl": "https://manga-reader-app-backend.onrender.com/api"
  },
  "MangaReaderApiSettings": {
    "BaseUrl": "https://localhost:7262",
    "CloudinaryBaseUrl": "https://res.cloudinary.com/dew5tpdko/image/upload/"
  }
}
```

Và file `MangaReader_WebUI\appsettings.Development.json`:

<!-- MangaReader_WebUI\appsettings.Development.json -->
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "BackendApi": {
    "BaseUrl": "https://manga-reader-app-backend.onrender.com/api"
  },
  "MangaReaderApiSettings": {
    "BaseUrl": "https://localhost:7262",
    "CloudinaryBaseUrl": "https://res.cloudinary.com/dew5tpdko/image/upload/"
  }
}
```

### 2.4. Dọn dẹp mã nguồn JavaScript

Loại bỏ logic và các file không cần thiết liên quan đến MangaDex.

#### 2.4.1. Xóa file `manga-tags.js`

File này đã lỗi thời và chức năng của nó được thay thế bằng `search-tags-dropdown.js`.
- **Xóa file:** `MangaReader_WebUI\wwwroot\js\modules\manga-tags.js`

#### 2.4.2. Cập nhật `ui-toggles.js`

Loại bỏ hoàn toàn logic liên quan đến việc chuyển đổi nguồn truyện.

<!-- MangaReader_WebUI\wwwroot\js\modules\ui-toggles.js -->
```javascript
/**
 * ui-toggles.js - Quản lý các chức năng chuyển đổi UI (chế độ sáng/tối)
 */

// --- Theme Switcher Constants ---
const THEME_KEY = 'theme';
const THEME_DARK = 'dark';
const THEME_LIGHT = 'light';

/**
 * Khởi tạo tất cả các nút chuyển đổi UI
 */
export function initUIToggles() {
    console.log("[UI Toggles] Initializing UI toggles...");
    initCustomThemeSwitcherInternal();
}

// --- Theme Switcher Logic ---

/**
 * Lưu chủ đề hiện tại vào localStorage
 * @param {string} theme - Chủ đề ('light' hoặc 'dark')
 */
function saveTheme(theme) {
    localStorage.setItem(THEME_KEY, theme);
}

/**
 * Lấy chủ đề đã lưu hoặc chủ đề hệ thống mặc định
 * @returns {string} - Chủ đề ('light' hoặc 'dark')
 */
function getSavedTheme() {
    const saved = localStorage.getItem(THEME_KEY);
    if (saved) {
        return saved;
    }
    // Fallback kiểm tra chế độ màu hệ thống
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return THEME_DARK;
    }
    return THEME_LIGHT; // Mặc định là light
}

/**
 * Áp dụng chủ đề cho trang web và cập nhật UI của switcher
 * @param {string} theme - Chủ đề ('light' hoặc 'dark')
 * @param {boolean} [showNotification=false] - Có hiển thị toast thông báo không
 */
function applyTheme(theme, showNotification = false) {
    document.documentElement.setAttribute('data-bs-theme', theme);

    // Cập nhật meta tag theme-color
    const metaThemeColor = document.querySelector('meta[name="theme-color"]');
    if (metaThemeColor) {
        metaThemeColor.setAttribute('content', theme === THEME_DARK ? '#121318' : '#0d6efd');
    }

    // Cập nhật UI của switcher
    updateThemeSwitcherUI(theme);

    // Hiển thị thông báo nếu cần
    if (showNotification && typeof window.showToast === 'function') {
        window.showToast('Thông báo', `Đã chuyển sang chế độ ${theme === THEME_DARK ? 'tối' : 'sáng'}!`, 'info');
    }
    // Xóa bỏ inline style color trên các nav-link active sau khi đổi theme
    if (typeof window.cleanupActiveLinks === 'function') {
        window.cleanupActiveLinks();
    }
}

/**
 * Cập nhật giao diện của custom theme switcher
 * @param {string} theme - Chủ đề hiện tại ('light' hoặc 'dark')
 */
function updateThemeSwitcherUI(theme) {
    const switcherItem = document.getElementById('customThemeSwitcherItem');
    const switcherText = document.getElementById('customThemeSwitcherText');
    const switcherIcon = document.getElementById('customThemeIcon');

    if (!switcherItem || !switcherText || !switcherIcon) {
        console.warn("[Theme] Không tìm thấy các thành phần của custom theme switcher.");
        return;
    }

    const isDark = theme === THEME_DARK;

    // Cập nhật class cho switch trực quan
    if (isDark) {
        switcherItem.classList.add('dark-mode');
    } else {
        switcherItem.classList.remove('dark-mode');
    }

    // Cập nhật icon và text
    if (isDark) {
        switcherIcon.className = 'bi bi-sun me-2';
        switcherText.childNodes[switcherText.childNodes.length - 1].nodeValue = ' Chế độ sáng';
    } else {
        switcherIcon.className = 'bi bi-moon-stars me-2';
        switcherText.childNodes[switcherText.childNodes.length - 1].nodeValue = ' Chế độ tối';
    }
    console.log(`[Theme] UI updated for ${theme} mode.`);
}

/**
 * Khởi tạo chức năng chuyển đổi chế độ tối/sáng tùy chỉnh (Internal)
 */
function initCustomThemeSwitcherInternal() {
    console.log("[Theme] Initializing custom theme switcher internal...");
    const switcherItem = document.getElementById('customThemeSwitcherItem');

    if (!switcherItem) {
        return;
    }

    const initialTheme = getSavedTheme();
    console.log(`[Theme] Initial theme: ${initialTheme}`);
    applyTheme(initialTheme, false);

    const handleSwitcherClick = (event) => {
        event.preventDefault();
        event.stopPropagation();

        const currentTheme = document.documentElement.getAttribute('data-bs-theme');
        const newTheme = currentTheme === THEME_DARK ? THEME_LIGHT : THEME_DARK;

        console.log(`[Theme] Switching from ${currentTheme} to ${newTheme}`);
        applyTheme(newTheme, true);
        saveTheme(newTheme);
    };

    if (switcherItem._themeClickListener) {
        switcherItem.removeEventListener('click', switcherItem._themeClickListener);
    }
    switcherItem._themeClickListener = handleSwitcherClick;
    switcherItem.addEventListener('click', handleSwitcherClick);
    console.log("[Theme] Custom theme switcher initialized and click listener attached.");

    if (!localStorage.getItem(THEME_KEY)) {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        const handleSystemThemeChange = (e) => {
            if (!localStorage.getItem(THEME_KEY)) {
                const systemTheme = e.matches ? THEME_DARK : THEME_LIGHT;
                console.log(`[Theme] System theme changed to: ${systemTheme}`);
                applyTheme(systemTheme, false);
            }
        };
        if (mediaQuery._systemThemeListener) {
            mediaQuery.removeEventListener('change', mediaQuery._systemThemeListener);
        }
        mediaQuery._systemThemeListener = handleSystemThemeChange;
        mediaQuery.addEventListener('change', handleSystemThemeChange);
        console.log("[Theme] Added listener for system theme changes.");
    }
}

// Export hàm khởi tạo chính
export default {
    initUIToggles
};
```

#### 2.4.3. Cập nhật `main.js`

Loại bỏ việc import và khởi tạo `initTagsInSearchForm`.

<!-- MangaReader_WebUI\wwwroot\js\main.js -->
```javascript
/**
 * main.js - File chính để import và khởi tạo tất cả các module
 */

// Import các module
import { initAuthUI } from './auth.js';
import { initCustomDropdowns } from './modules/custom-dropdown.js';
import { initErrorHandling } from './modules/error-handling.js';
import { initHtmxHandlers, reinitializeAfterHtmxLoad } from './modules/htmx-handlers.js';
import { initMangaDetailsPage } from './modules/manga-details.js';
import { initReadPage } from './modules/read-page.js';
import { initReadingState } from './modules/reading-state.js';
import SearchModule from './modules/search.js';
import { initSidebar } from './modules/sidebar.js';
import { initUIToggles } from './modules/ui-toggles.js';
import { initToasts } from './modules/toast.js';
import {
    adjustFooterPosition,
    adjustMangaTitles,
    cleanupActiveLinks,
    createDefaultImage,
    fixAccordionIssues,
    initBackToTop,
    initLazyLoading,
    initResponsive,
    initTooltips
} from './modules/ui.js';

// --- Xử lý Back/Forward và bfcache ---
window.addEventListener('pageshow', function(event) {
  // event.persisted là true nếu trang được tải từ bfcache (back/forward)
  if (event.persisted) {
    console.log('[pageshow] Page loaded from bfcache.');

    // Kiểm tra xem trang này có phải là trang được quản lý bởi HTMX không
    // (Dấu hiệu có thể là sự tồn tại của #main-content hoặc body có thuộc tính hx-boost)
    const mainContent = document.getElementById('main-content');
    const isHtmxManagedPage = mainContent || document.body.hasAttribute('hx-boost'); // Điều chỉnh điều kiện nếu cần

    if (isHtmxManagedPage) {
        console.log('[pageshow] This seems to be an HTMX-managed page restored from bfcache.');
        // Có vẻ như htmx:load không được kích hoạt đúng cách trong trường hợp này (non-HTMX -> HTMX back).
        // => Gọi lại hàm reinitializeAfterHtmxLoad một cách thủ công như một fallback.
        // Chúng ta truyền document.body vì toàn bộ trang đã được khôi phục.
        reinitializeAfterHtmxLoad(document.body);
    } else {
        console.log('[pageshow] This page does not seem to be HTMX-managed. No manual re-init needed.');
        // Đối với trang non-HTMX được khôi phục từ bfcache,
        // nếu cần chạy lại JS nào đó, bạn có thể thêm logic ở đây.
        // Ví dụ: Nếu trang Read cần khởi tạo lại gì đó khi back về nó.
    }
  } else {
      console.log('[pageshow] Page loaded normally (not from bfcache).');
  }
});
// --- Kết thúc xử lý Back/Forward ---

/**
 * Khởi tạo tất cả các module
 */
document.addEventListener('DOMContentLoaded', function() {
    // Xóa bỏ inline style của các active nav-link
    cleanupActiveLinks();
    
    // Khởi tạo tooltips
    initTooltips();
    
    // Khởi tạo lazy loading cho hình ảnh
    initLazyLoading();
    
    // Khởi tạo module tìm kiếm
    SearchModule.init();
    console.log('Search module registered');
    
    // Khởi tạo module quản lý thẻ manga
    if (window.initSearchTagsDropdown) {
        window.initSearchTagsDropdown();
        console.log('Search tags dropdown module registered');
    }
    
    // Tạo ảnh mặc định nếu chưa có
    createDefaultImage();
    
    // Khởi tạo chức năng hiển thị thông báo
    initToasts();
    
    // Khởi tạo chức năng lưu trạng thái đọc
    initReadingState();
    
    // Khởi tạo UI xác thực
    initAuthUI();
    console.log('Auth module registered');
    
    // Khởi tạo custom dropdowns
    initCustomDropdowns();
    console.log('Custom dropdowns initialized');
    
    // Khởi tạo chức năng chuyển đổi chế độ tối/sáng và nguồn truyện tùy chỉnh
    initUIToggles();
    
    // Khởi tạo nút back-to-top
    initBackToTop();
    
    // Khởi tạo chức năng xử lý lỗi
    initErrorHandling();
    
    // Khởi tạo chức năng responsive
    initResponsive();
    
    // Khắc phục vấn đề với accordion
    fixAccordionIssues();
    
    // Tự động điều chỉnh vị trí footer
    adjustFooterPosition();
    
    // Điều chỉnh kích thước chữ cho tiêu đề manga
    adjustMangaTitles();
    
    // Khởi tạo sidebar menu
    initSidebar();
    
    // Khởi tạo chức năng trang chi tiết manga có điều kiện
    if (document.querySelector('.details-manga-header-background') || document.querySelector('.details-manga-details-container')) {
        console.log('Main.js: Đang khởi tạo tính năng trang chi tiết manga khi tải trực tiếp.');
        initMangaDetailsPage();
    }
    
    // Khởi tạo chức năng cho trang đọc chapter có điều kiện
    if (document.querySelector('.chapter-reader-container') || document.getElementById('readingSidebar')) {
        console.log('Main.js: Đang khởi tạo tính năng trang đọc chapter khi tải trực tiếp.');
        initReadPage();
    }
    
    // Khởi tạo xử lý HTMX
    initHtmxHandlers();
    
    // Đánh dấu việc khởi tạo hoàn tất
    console.log('Manga Reader Web: Tất cả các module đã được khởi tạo thành công.');
});
```

#### 2.4.4. Cập nhật `_Layout.cshtml`

Loại bỏ nút chuyển đổi nguồn truyện trong `_Layout.cshtml`.

<!-- MangaReader_WebUI\Views\Shared\_Layout.cshtml -->
```html
<!DOCTYPE html>
<html lang="vi" data-bs-theme="light" id="htmlRoot">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Manga Reader</title>
    <partial name="_GlobalStyles" />
    <link rel="icon" type="image/png" href="~/favicon.png">
    <meta name="theme-color" content="#0d6efd">
    <meta name="description" content="Đọc manga online miễn phí với chất lượng cao">
    @await RenderSectionAsync("Styles", required: false)
    
    <!-- Script thiết lập theme ban đầu từ localStorage -->
    <script>
        // Tải theme từ localStorage và áp dụng trước khi trang tải hoàn tất
        const savedTheme = localStorage.getItem('theme');
        if (savedTheme) {
            document.documentElement.setAttribute('data-bs-theme', savedTheme);
        }
    </script>
</head>
<body class="manga-reader-app">
    <!-- Sidebar Menu -->
    <div id="sidebarMenu">
        <div class="sidebar-header">
            <h5 class="m-0">
                <i class="bi bi-book-half me-2"></i>Manga Reader
            </h5>
            <button type="button" class="btn-close" id="closeSidebar" aria-label="Close"></button>
        </div>
        <div class="sidebar-body">
            <ul class="navbar-nav flex-column p-3">
                <li class="nav-item mb-2">
                    <a class="nav-link sidebar-nav-link p-2" asp-area="" asp-controller="Home" asp-action="Index"
                       hx-get="@Url.Action("Index", "Home")" hx-target="#main-content" hx-push-url="true">
                        <i class="bi bi-house-door me-2"></i>Trang chủ
                    </a>
                </li>
                <li class="nav-item mb-2">
                    <a class="nav-link sidebar-nav-link p-2" asp-area="" asp-controller="Manga" asp-action="Search"
                       hx-get="@Url.Action("Search", "Manga")" hx-target="#main-content" hx-push-url="true"
                       hx-trigger="click">
                        <i class="bi bi-search me-2"></i>Tìm kiếm nâng cao
                    </a>
                </li>
                <li class="nav-item mb-2">
                    <a class="nav-link sidebar-nav-link p-2" asp-area="" asp-controller="Manga" asp-action="Followed"
                       hx-get="@Url.Action("Followed", "Manga")" hx-target="#main-content" hx-push-url="true">
                        <i class="bi bi-bookmark-heart me-2"></i>Truyện đang theo dõi
                    </a>
                </li>
                <li class="nav-item mb-2">
                    <a class="nav-link sidebar-nav-link p-2" asp-area="" asp-controller="Manga" asp-action="History"
                       hx-get="@Url.Action("History", "Manga")" hx-target="#main-content" hx-push-url="true">
                        <i class="bi bi-clock-history me-2"></i>Lịch sử đọc
                    </a>
                </li>
            </ul>
            
            <!-- Phần footer trong sidebar -->
            <div class="sidebar-footer text-muted">
                <hr>
                <h6><i class="bi bi-book-half me-2"></i>Manga Reader</h6>
                <p class="small">Trang web đọc truyện manga online miễn phí với chất lượng cao.</p>
                
                <div class="sidebar-social-icons">
                    <a href="#"><i class="bi bi-facebook"></i></a>
                    <a href="#"><i class="bi bi-twitter-x"></i></a>
                    <a href="#"><i class="bi bi-discord"></i></a>
                    <a href="#"><i class="bi bi-github"></i></a>
                </div>
                
                <p class="small mb-2">&copy; 2025 - Manga Reader</p>
            </div>
        </div>
    </div>
    
    <!-- Backdrop cho mobile -->
    <div class="sidebar-backdrop" id="sidebarBackdrop"></div>
    
    <!-- Main Container -->
    <div class="main-container">
        <!-- Content Area -->
        <div class="content-area">
            <!-- Header -->
            <div class="site-header">
                <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
                    <div class="container">
                        <!-- Nút menu bên trái -->
                        <button class="navbar-toggler sidebar-toggler me-2 htmx-preserve" type="button" id="sidebarToggler" 
                                hx-preserve="true">
                            <span class="navbar-toggler-icon"></span>
                        </button>
                        
                        <!-- Logo và tên trang web -->
                        <a class="navbar-brand text-transition" asp-area="" asp-controller="Home" asp-action="Index" 
                           hx-get="@Url.Action("Index", "Home")" hx-target="#main-content" hx-push-url="true">
                            <i class="bi bi-book-half me-2"></i>Manga Reader
                        </a>
                        
                        <!-- Phần phải: Thanh tìm kiếm và tài khoản -->
                        <div class="ms-auto d-flex align-items-center flex-grow-1 justify-content-end">
                            <!-- Thanh tìm kiếm -->
                            <div class="search-container d-none d-lg-block position-transition me-3">
                                <form class="d-flex" id="quickSearchForm" action="/Manga/Search" method="get" 
                                      hx-get="@Url.Action("Search", "Manga")" hx-target="#main-content" hx-push-url="true" 
                                      hx-trigger="submit">
                                    <div class="input-group">
                                        <input class="form-control" id="quickSearchInput" name="title" type="search" placeholder="Tìm truyện..." aria-label="Search">
                                        <button class="btn btn-light" type="submit"><i class="bi bi-search"></i></button>
                                    </div>
                                </form>
                            </div>
                            
                            <!-- Dropdown tài khoản -->
                            <div class="custom-user-dropdown" id="userDropdownContainer">
                                <button class="dropdown-toggle-btn" id="userDropdownToggle" aria-haspopup="true" aria-expanded="false">
                                    <i class="bi bi-person-circle user-icon"></i>
                                    <span class="user-name d-none" id="userNameDisplay"></span>
                                    <i class="bi bi-chevron-down dropdown-arrow-icon"></i>
                                </button>
                                <div class="dropdown-menu-content" id="userDropdownMenu">
                                    <!-- Menu cho người dùng chưa đăng nhập -->
                                    <div id="guestUserMenu">
                                        <a class="dropdown-item"
                                           href="@Url.Action("Login", "Auth")"
                                           hx-get="@Url.Action("Login", "Auth")"
                                           hx-target="#main-content"
                                           hx-push-url="true">
                                            <i class="bi bi-box-arrow-in-right me-2"></i>Đăng nhập
                                        </a>
                                    </div>

                                    <!-- Menu cho người dùng đã đăng nhập -->
                                    <div id="authenticatedUserMenu" class="d-none">
                                        <a class="dropdown-item"
                                           href="@Url.Action("Profile", "Auth")"
                                           hx-get="@Url.Action("Profile", "Auth")"
                                           hx-target="#main-content"
                                           hx-push-url="true">
                                            <i class="bi bi-person me-2"></i>Trang cá nhân
                                        </a>
                                        <hr class="dropdown-divider">
                                        <a class="dropdown-item" href="@Url.Action("Logout", "Auth")"><i class="bi bi-box-arrow-right me-2"></i>Đăng xuất</a>
                                    </div>

                                    <hr class="dropdown-divider">
                                    <!-- Custom Theme Switcher Item -->
                                    <div class="custom-dropdown-item custom-theme-switcher" id="customThemeSwitcherItem" role="button" tabindex="0">
                                        <span id="customThemeSwitcherText">
                                            <i id="customThemeIcon" class="bi bi-moon-stars me-2"></i>Chế độ tối
                                        </span>
                                        <div class="custom-theme-toggle-switch" aria-hidden="true">
                                            <div class="custom-theme-toggle-slider"></div>
                                        </div>
                                    </div>
                                    <!-- End Custom Theme Switcher Item -->
                                </div>
                            </div>
                        </div>
                    </div>
                </nav>
                
                <!-- Mobile Search -->
                <div class="container d-block d-lg-none py-2 mobile-search-container">
                    <form class="d-flex" id="mobileSearchForm" action="/Manga/Search" method="get"
                          hx-get="@Url.Action("Search", "Manga")" hx-target="#main-content" hx-push-url="true"
                          hx-trigger="submit">
                        <div class="input-group">
                            <input class="form-control" name="title" type="search" placeholder="Tìm truyện..." aria-label="Search">
                            <button class="btn btn-primary" type="submit"><i class="bi bi-search"></i></button>
                        </div>
                    </form>
                </div>
            </div>
            
            <!-- Main Content -->
            <main role="main" class="site-content">
                <div id="main-content">
                    @RenderBody()
                </div>
            </main>
        </div>
    </div>

    <!-- Back to top button -->
    <button id="backToTopBtn" class="btn btn-primary rounded-circle position-fixed bottom-0 end-0 m-4 d-none" title="Về đầu trang">
        <i class="bi bi-arrow-up"></i>
    </button>

    <!-- Toast container -->
    <div id="toastContainer" class="toast-container position-fixed bottom-0 end-0 p-3"></div>
    
    <partial name="_GlobalScripts" />
    
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

---

## Bước 3: Tái cấu trúc `MangaReader_WebUI` để sử dụng `MangaReaderLib` làm nguồn duy nhất

Mục tiêu của bước này là cập nhật lại toàn bộ logic trong `MangaReader_WebUI` để gọi trực tiếp đến các client của `MangaReaderLib`, thay vì thông qua các service API đa nguồn đã bị xóa.

### 3.1. Cập nhật `Program.cs`

File này sẽ được đơn giản hóa rất nhiều. Chúng ta sẽ xóa các đăng ký service không cần thiết và chỉ giữ lại các đăng ký cho client của `MangaReaderLib` và các service tầng cao.

<!-- MangaReader_WebUI\Program.cs -->
```csharp
using MangaReader.WebUI.Infrastructure;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.ChapterServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaPageService;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc.Razor;
using MangaReaderLib.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Thêm cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Cấu hình logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Đảm bảo HttpContextAccessor được đăng ký
builder.Services.AddHttpContextAccessor();

// Cấu hình HttpClient để gọi Backend API (CHỈ DÙNG CHO XÁC THỰC)
builder.Services.AddHttpClient("BackendApiClient", client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "MangaReaderWeb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// Thêm HttpClient cho MangaReaderLib API
builder.Services.AddHttpClient("MangaReaderLibApiClient", client =>
{
    var baseUrl = builder.Configuration["MangaReaderApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "MangaReaderWeb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// Đăng ký các MangaReaderLib Mappers
builder.Services.AddScoped<IMangaReaderLibToMangaViewModelMapper, MangaReaderLibToMangaViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToMangaDetailViewModelMapper, MangaReaderLibToMangaDetailViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToChapterViewModelMapper, MangaReaderLibToChapterViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToSimpleChapterInfoMapper, MangaReaderLibToSimpleChapterInfoMapper>();
builder.Services.AddScoped<IMangaReaderLibToMangaInfoViewModelMapper, MangaReaderLibToMangaInfoViewModelMapper>();
builder.Services.AddScoped<IMangaReaderLibToChapterInfoMapper, MangaReaderLibToChapterInfoMapper>();
builder.Services.AddScoped<IMangaReaderLibToTagListResponseMapper, MangaReaderLibToTagListResponseMapper>();
builder.Services.AddScoped<IMangaReaderLibToAtHomeServerResponseMapper, MangaReaderLibToAtHomeServerResponseMapper>();

// Đăng ký các MangaReaderLib API Clients
builder.Services.AddScoped<IMangaReaderLibApiClient, MangaReaderLibApiClientService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("MangaReaderLibApiClient");
    var innerClientLogger = provider.GetRequiredService<ILogger<ApiClient>>();
    var wrapperLoggerForApiClient = provider.GetRequiredService<ILogger<MangaReaderLibApiClientService>>();
    return new MangaReaderLibApiClientService(httpClient, innerClientLogger, wrapperLoggerForApiClient);
});
builder.Services.AddScoped<IMangaReaderLibAuthorClient, MangaReaderLibAuthorClientService>();
builder.Services.AddScoped<IMangaReaderLibChapterClient, MangaReaderLibChapterClientService>();
builder.Services.AddScoped<IMangaReaderLibChapterPageClient, MangaReaderLibChapterPageClientService>();
builder.Services.AddScoped<IMangaReaderLibCoverApiService, MangaReaderLibCoverApiService>();
builder.Services.AddScoped<IMangaReaderLibMangaClient, MangaReaderLibMangaClientService>();
builder.Services.AddScoped<IMangaReaderLibTagClient, MangaReaderLibTagClientService>();
builder.Services.AddScoped<IMangaReaderLibTagGroupClient, MangaReaderLibTagGroupClientService>();
builder.Services.AddScoped<IMangaReaderLibTranslatedMangaClient, MangaReaderLibTranslatedMangaClientService>();

// Đăng ký các service liên quan đến xác thực
builder.Services.AddScoped<IUserService, UserService>();

// Đăng ký các service tiện ích
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<JsonConversionService>();
builder.Services.AddScoped<ViewRenderService>();

// Đăng ký các service tầng cao của ứng dụng
builder.Services.AddScoped<IMangaFollowService, MangaFollowService>();
builder.Services.AddScoped<IFollowedMangaService, FollowedMangaService>();
builder.Services.AddScoped<IMangaInfoService, MangaInfoService>();
builder.Services.AddScoped<IReadingHistoryService, ReadingHistoryService>();
builder.Services.AddScoped<ChapterService>();
builder.Services.AddScoped<MangaIdService>();
builder.Services.AddScoped<ChapterLanguageServices>();
builder.Services.AddScoped<ChapterReadingServices>();
builder.Services.AddScoped<MangaDetailsService>();
builder.Services.AddScoped<MangaSearchService>();

// Cấu hình Razor View Engine để sử dụng View Location Expander tùy chỉnh
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new CustomViewLocationExpander());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "auth_callback",
    pattern: "auth/callback",
    defaults: new { controller = "Auth", action = "Callback" });

app.Run();
```

### 3.2. Cập nhật các Services trong `MangaReader_WebUI`

Đây là phần cốt lõi của việc tái cấu trúc. Chúng ta sẽ cập nhật các service để chúng gọi trực tiếp đến các client của `MangaReaderLib` thay vì `IMangaApiService`.

#### 3.2.1. Cập nhật `HomeController.cs`

Loại bỏ logic đa nguồn, gọi trực tiếp đến `MangaReaderLib`.

<!-- MangaReader_WebUI\Controllers\HomeController.cs -->
```csharp
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MangaReaderLib.Enums;

namespace MangaReader.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<HomeController> _logger;
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;

        public HomeController(
            IMangaReaderLibMangaClient mangaClient,
            ILogger<HomeController> logger,
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper)
        {
            _mangaClient = mangaClient;
            _logger = logger;
            _mangaViewModelMapper = mangaViewModelMapper;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["PageType"] = "home";

                var recentMangaResponse = await _mangaClient.GetMangasAsync(
                    limit: 10,
                    orderBy: "updatedAt",
                    ascending: false,
                    includes: new List<string> { "cover_art", "author" }
                );

                if (recentMangaResponse?.Data == null || !recentMangaResponse.Data.Any())
                {
                    _logger.LogWarning("API không trả về dữ liệu manga mới nhất.");
                    ViewBag.ErrorMessage = "Không có dữ liệu manga. Vui lòng thử lại sau.";
                    return View("Index", new List<MangaViewModel>());
                }

                var viewModels = new List<MangaViewModel>();
                foreach (var mangaDto in recentMangaResponse.Data)
                {
                    try
                    {
                        var viewModel = await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaDto);
                        viewModels.Add(viewModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi map manga ID: {mangaDto?.Id} trên trang chủ.");
                    }
                }
                
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_MangaGridPartial", viewModels);
                }

                return View("Index", viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang chủ.");
                ViewBag.ErrorMessage = $"Không thể tải danh sách manga: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View("Index", new List<MangaViewModel>());
            }
        }

        public async Task<IActionResult> GetLatestMangaPartial()
        {
            try
            {
                 var recentMangaResponse = await _mangaClient.GetMangasAsync(
                    limit: 10,
                    orderBy: "updatedAt",
                    ascending: false,
                    includes: new List<string> { "cover_art", "author" }
                );

                var viewModels = new List<MangaViewModel>();
                if (recentMangaResponse?.Data != null)
                {
                    foreach (var mangaDto in recentMangaResponse.Data)
                    {
                        try
                        {
                            viewModels.Add(await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaDto));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi map manga ID: {mangaDto?.Id} trong partial.");
                        }
                    }
                }
                
                return PartialView("_MangaGridPartial", viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách manga mới nhất cho partial.");
                return PartialView("_ErrorPartial", "Không thể tải danh sách manga mới nhất.");
            }
        }

        public IActionResult Privacy()
        {
            ViewData["PageType"] = "home";
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView("Privacy");
            }
            return View("Privacy");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            ViewData["PageType"] = "home";
            var model = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView("Error", model);
            }
            return View(model);
        }
    }
}
```

#### 3.2.2. Cập nhật `MangaController.cs`

Chỉnh sửa `GetTags` và các DI để phản ánh kiến trúc mới.

<!-- MangaReader_WebUI\Controllers\MangaController.cs -->
```csharp
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.MangaPageService;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaReader.WebUI.Controllers
{
    public class MangaController : Controller
    {
        private readonly IMangaReaderLibTagClient _tagClient;
        private readonly IMangaReaderLibToTagListResponseMapper _tagListResponseMapper;
        private readonly ILogger<MangaController> _logger;
        private readonly MangaDetailsService _mangaDetailsService;
        private readonly MangaSearchService _mangaSearchService;
        private readonly ViewRenderService _viewRenderService;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFollowedMangaService _followedMangaService;
        private readonly IReadingHistoryService _readingHistoryService;

        public MangaController(
            IMangaReaderLibTagClient tagClient,
            IMangaReaderLibToTagListResponseMapper tagListResponseMapper,
            ILogger<MangaController> logger,
            MangaDetailsService mangaDetailsService,
            MangaSearchService mangaSearchService,
            ViewRenderService viewRenderService,
            IMangaFollowService mangaFollowService,
            IUserService userService,
            IHttpClientFactory httpClientFactory,
            IFollowedMangaService followedMangaService,
            IReadingHistoryService readingHistoryService)
        {
            _tagClient = tagClient;
            _tagListResponseMapper = tagListResponseMapper;
            _logger = logger;
            _mangaDetailsService = mangaDetailsService;
            _mangaSearchService = mangaSearchService;
            _viewRenderService = viewRenderService;
            _mangaFollowService = mangaFollowService;
            _userService = userService;
            _httpClientFactory = httpClientFactory;
            _followedMangaService = followedMangaService;
            _readingHistoryService = readingHistoryService;
        }

        private static class SessionKeys
        {
            public const string CurrentSearchResultData = "CurrentSearchResultData";
        }

        [HttpGet]
        [Route("api/manga/tags")]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                _logger.LogInformation("Đang lấy danh sách tags từ MangaReaderLib API");
                var tagsDataFromLib = await _tagClient.GetTagsAsync(limit: 500); // Lấy tối đa 500 tags
                if (tagsDataFromLib == null)
                {
                    throw new Exception("API không trả về dữ liệu tags.");
                }
                
                // Map kết quả từ MangaReaderLib DTO sang MangaDex DTO mà frontend đang sử dụng
                var tagsForFrontend = _tagListResponseMapper.MapToTagListResponse(tagsDataFromLib);
                
                return Json(new { success = true, data = tagsForFrontend });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách tags từ MangaReaderLib API.");
                return Json(new { success = false, error = "Không thể tải danh sách tags." });
            }
        }
        
        // GET: Manga/Details/5
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                ViewData["PageType"] = "manga-details";
                var viewModel = await _mangaDetailsService.GetMangaDetailsAsync(id);

                if (_userService.IsAuthenticated())
                {
                    bool isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                    if (viewModel.Manga != null)
                    {
                        viewModel.Manga.IsFollowing = isFollowing;
                    }
                }
                else
                {
                    if (viewModel.Manga != null)
                    {
                        viewModel.Manga.IsFollowing = false;
                    }
                }

                if (viewModel.AlternativeTitlesByLanguage != null && viewModel.AlternativeTitlesByLanguage.Any())
                {
                    ViewData["AlternativeTitlesByLanguage"] = viewModel.AlternativeTitlesByLanguage;
                }

                return _viewRenderService.RenderViewBasedOnRequest(this, "Details", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết manga: {Message}", ex.Message);
                ViewBag.ErrorMessage = "Không thể tải chi tiết manga. Vui lòng thử lại sau.";
                return View("Details", new MangaDetailViewModel { AlternativeTitlesByLanguage = new Dictionary<string, List<string>>() });
            }
        }
        
        // GET: Manga/Search
        public async Task<IActionResult> Search(
            string title = "", 
            List<string>? status = null, 
            string sortBy = "latest",
            string authors = "",
            string artists = "",
            int? year = null,
            List<string>? availableTranslatedLanguage = null,
            List<string>? publicationDemographic = null,
            List<string>? contentRating = null,
            string includedTagsMode = "AND",
            string excludedTagsMode = "OR",
            List<string>? genres = null,
            string includedTagsStr = "",
            string excludedTagsStr = "",
            int page = 1, 
            int pageSize = 24)
        {
            try
            {
                _logger.LogInformation("[SEARCH_VIEW] Bắt đầu action Search với page={Page}, pageSize={PageSize}", page, pageSize);
                ViewData["PageType"] = "home";

                var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
                    title, status, sortBy, authors, artists, year, 
                    availableTranslatedLanguage, publicationDemographic, contentRating,
                    includedTagsMode, excludedTagsMode, genres, includedTagsStr, excludedTagsStr);

                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                if (viewModel.Mangas != null && viewModel.Mangas.Any())
                {
                    HttpContext.Session.SetString(SessionKeys.CurrentSearchResultData, 
                        JsonSerializer.Serialize(viewModel.Mangas));
                }

                string initialViewMode = Request.Cookies.TryGetValue("MangaViewMode", out string? cookieViewMode) && (cookieViewMode == "grid" || cookieViewMode == "list")
                    ? cookieViewMode : "grid";

                ViewData["InitialViewMode"] = initialViewMode;

                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    string hxTarget = Request.Headers["HX-Target"].FirstOrDefault() ?? "";
                    string referer = Request.Headers["Referer"].FirstOrDefault() ?? "";
                    
                    if (!string.IsNullOrEmpty(referer) && !referer.Contains("/Manga/Search"))
                    {
                        return PartialView("Search", viewModel);
                    }
                    
                    if (hxTarget == "search-results-and-pagination" || hxTarget == "main-content")
                    {
                        return PartialView("_SearchResultsWrapperPartial", viewModel);
                    }
                    else
                    {
                        return PartialView("Search", viewModel);
                    }
                }

                return View("Search", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách manga.");
                ViewBag.ErrorMessage = $"Không thể tải danh sách manga. Chi tiết: {ex.Message}";
                return View("Search", new MangaListViewModel
                {
                    Mangas = new List<MangaViewModel>(),
                    CurrentPage = 1,
                    PageSize = pageSize,
                    TotalCount = 0,
                    MaxPages = 0,
                    SortOptions = new SortManga { Title = title, Status = status ?? new List<string>(), SortBy = sortBy ?? "latest" }
                });
            }
        }
        
        [HttpPost]
        [Route("api/proxy/toggle-follow")]
        public async Task<IActionResult> ToggleFollowProxy([FromBody] MangaActionRequest request)
        {
            if (string.IsNullOrEmpty(request?.MangaId))
            {
                return BadRequest(new { success = false, message = "Manga ID không hợp lệ" });
            }

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Toggle follow attempt failed: User not authenticated.");
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
            }

            string backendEndpoint;
            bool isCurrentlyFollowing;

            try
            {
                var checkClient = _httpClientFactory.CreateClient("BackendApiClient");
                var checkToken = _userService.GetToken();
                checkClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", checkToken);
                var checkResponse = await checkClient.GetAsync($"/api/users/user/following/{request.MangaId}");

                if (checkResponse.IsSuccessStatusCode)
                {
                    var checkContent = await checkResponse.Content.ReadAsStringAsync();
                    var statusResponse = JsonSerializer.Deserialize<FollowingStatusResponse>(checkContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    isCurrentlyFollowing = statusResponse?.IsFollowing ?? false;
                }
                else if (checkResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                     _userService.RemoveToken();
                     return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "Không thể kiểm tra trạng thái theo dõi hiện tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking following status for manga {MangaId}", request.MangaId);
                return StatusCode(500, new { success = false, message = "Lỗi khi kiểm tra trạng thái theo dõi." });
            }

            backendEndpoint = isCurrentlyFollowing ? "/api/users/unfollow" : "/api/users/follow";
            bool targetFollowingState = !isCurrentlyFollowing;
            string successMessage = targetFollowingState ? "Đã theo dõi truyện" : "Đã hủy theo dõi truyện";

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                var token = _userService.GetToken();
                 if (string.IsNullOrEmpty(token)) {
                     return Unauthorized(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
                 }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new { mangaId = request.MangaId };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(backendEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true, isFollowing = targetFollowingState, message = successMessage });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken();
                        return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
                    }
                    return StatusCode((int)response.StatusCode, new { success = false, message = $"Lỗi từ backend: {response.ReasonPhrase}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong proxy action {Endpoint} cho manga {MangaId}", backendEndpoint, request.MangaId);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi xử lý yêu cầu" });
            }
        }

        public async Task<IActionResult> GetSearchResultsPartial(
            string title = "", List<string>? status = null, string sortBy = "latest",
            string authors = "", string artists = "", int? year = null,
            List<string>? availableTranslatedLanguage = null, List<string>? publicationDemographic = null,
            List<string>? contentRating = null, string includedTagsMode = "AND", string excludedTagsMode = "OR",
            List<string>? genres = null, string includedTagsStr = "", string excludedTagsStr = "",
            int page = 1, int pageSize = 24)
        {
            try
            {
                var sortManga = _mangaSearchService.CreateSortMangaFromParameters(
                    title, status, sortBy, authors, artists, year, 
                    availableTranslatedLanguage, publicationDemographic, contentRating,
                    includedTagsMode, excludedTagsMode, genres, includedTagsStr, excludedTagsStr);

                var viewModel = await _mangaSearchService.SearchMangaAsync(page, pageSize, sortManga);

                if (viewModel.Mangas.Count == 0)
                {
                    return PartialView("_NoResultsPartial");
                }
                return PartialView("_SearchResultsWrapperPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải kết quả tìm kiếm.");
                return PartialView("_NoResultsPartial");
            }
        }

        [HttpGet]
        public IActionResult GetMangaViewPartial(string viewMode = "grid")
        {
            try
            {
                var mangasJson = HttpContext.Session.GetString(SessionKeys.CurrentSearchResultData);
                if (string.IsNullOrEmpty(mangasJson))
                {
                    return PartialView("_NoResultsPartial");
                }
                
                var mangas = JsonSerializer.Deserialize<List<MangaViewModel>>(mangasJson);
                ViewData["InitialViewMode"] = viewMode;
                
                var viewModel = new MangaListViewModel
                {
                    Mangas = mangas ?? new List<MangaViewModel>(),
                    CurrentPage = 1, PageSize = mangas?.Count ?? 0,
                    TotalCount = mangas?.Count ?? 0, MaxPages = 1
                };
                
                return PartialView("_SearchResultsPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu manga từ Session.");
                return PartialView("_NoResultsPartial");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Followed()
        {
            if (!_userService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Followed", "Manga") });
            }

            try
            {
                var followedMangas = await _followedMangaService.GetFollowedMangaListAsync();
                return _viewRenderService.RenderViewBasedOnRequest(this, "Followed", followedMangas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang truyện đang theo dõi.");
                ViewBag.ErrorMessage = "Không thể tải danh sách truyện đang theo dõi. Vui lòng thử lại sau.";
                return View("Followed", new List<FollowedMangaViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            if (!_userService.IsAuthenticated())
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_UnauthorizedPartial");
                }
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("History", "Manga") });
            }

            try
            {
                var history = await _readingHistoryService.GetReadingHistoryAsync();
                return _viewRenderService.RenderViewBasedOnRequest(this, "History", history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang lịch sử đọc truyện.");
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    ViewBag.ErrorMessage = "Không thể tải lịch sử đọc. Vui lòng thử lại sau.";
                    return PartialView("_ErrorPartial");
                }
                ViewBag.ErrorMessage = "Không thể tải lịch sử đọc. Vui lòng thử lại sau.";
                return View("History", new List<LastReadMangaViewModel>());
            }
        }
    }

    public class MangaActionRequest
    {
        public string? MangaId { get; set; }
    }

    public class FollowingStatusResponse
    {
        public bool IsFollowing { get; set; }
    }
}
```

#### 3.2.3. Cập nhật `MangaInfoService.cs`

Service này giờ đây sẽ gọi đến `MangaReaderLib`.

<!-- MangaReader_WebUI\Services\MangaServices\MangaInfoService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;

namespace MangaReader.WebUI.Services.MangaServices
{
    public class MangaInfoService : IMangaInfoService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<MangaInfoService> _logger;
        private readonly IMangaReaderLibToMangaInfoViewModelMapper _mangaToInfoViewModelMapper;

        public MangaInfoService(
            IMangaReaderLibMangaClient mangaClient,
            ILogger<MangaInfoService> logger,
            IMangaReaderLibToMangaInfoViewModelMapper mangaToInfoViewModelMapper)
        {
            _mangaClient = mangaClient;
            _logger = logger;
            _mangaToInfoViewModelMapper = mangaToInfoViewModelMapper;
        }

        public async Task<MangaInfoViewModel?> GetMangaInfoAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId) || !Guid.TryParse(mangaId, out var mangaGuid))
            {
                _logger.LogWarning("MangaId không hợp lệ khi gọi GetMangaInfoAsync: {MangaId}", mangaId);
                return null;
            }

            try
            {
                _logger.LogInformation("Bắt đầu lấy thông tin cơ bản cho manga ID: {MangaId}", mangaId);

                // Yêu cầu include cover_art để có publicId
                var mangaResponse = await _mangaClient.GetMangaByIdAsync(mangaGuid, new List<string> { "cover_art" });

                if (mangaResponse?.Data == null)
                {
                    _logger.LogWarning("Không thể lấy chi tiết manga {MangaId} trong MangaInfoService. API trả về null hoặc không có dữ liệu.", mangaId);
                    return new MangaInfoViewModel
                    {
                        MangaId = mangaId,
                        MangaTitle = $"Lỗi tải tiêu đề ({mangaId})",
                        CoverUrl = "/images/cover-placeholder.jpg"
                    };
                }

                var mangaInfoViewModel = _mangaToInfoViewModelMapper.MapToMangaInfoViewModel(mangaResponse.Data);
                
                _logger.LogInformation("Lấy thông tin cơ bản thành công cho manga ID: {MangaId}", mangaId);
                return mangaInfoViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin cơ bản cho manga ID: {MangaId}", mangaId);
                return new MangaInfoViewModel
                {
                    MangaId = mangaId,
                    MangaTitle = $"Lỗi lấy tiêu đề ({mangaId})",
                    CoverUrl = "/images/cover-placeholder.jpg"
                };
            }
        }
    }
}
```

#### 3.2.4. Cập nhật `ReadingHistoryService.cs`

Chỉnh sửa logic lấy thông tin chapter để phù hợp với `MangaReaderLib`.

<!-- MangaReader_WebUI\Services\MangaServices\ReadingHistoryService.cs -->
```csharp
using MangaReader.WebUI.Models.ViewModels.Chapter;
using MangaReader.WebUI.Models.ViewModels.History;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangaReader.WebUI.Services.MangaServices
{
    // Model để deserialize response từ backend /reading-history
    public class BackendHistoryItem
    {
        [JsonPropertyName("mangaId")]
        public string MangaId { get; set; }

        [JsonPropertyName("chapterId")]
        public string ChapterId { get; set; }

        [JsonPropertyName("lastReadAt")]
        public DateTime LastReadAt { get; set; }
    }

    public class ReadingHistoryService : IReadingHistoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly IMangaInfoService _mangaInfoService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReadingHistoryService> _logger;
        private readonly TimeSpan _rateLimitDelay;
        private readonly ILastReadMangaViewModelMapper _lastReadMapper;
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly IMangaReaderLibToChapterInfoMapper _chapterInfoMapper;
        private readonly IMangaReaderLibTranslatedMangaClient _translatedMangaClient;

        public ReadingHistoryService(
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            IMangaInfoService mangaInfoService,
            IConfiguration configuration,
            ILogger<ReadingHistoryService> logger,
            ILastReadMangaViewModelMapper lastReadMapper,
            IMangaReaderLibChapterClient chapterClient,
            IMangaReaderLibToChapterInfoMapper chapterInfoMapper,
            IMangaReaderLibTranslatedMangaClient translatedMangaClient)
        {
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _mangaInfoService = mangaInfoService;
            _configuration = configuration;
            _logger = logger;
            _rateLimitDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("ApiRateLimitDelayMs", 250));
            _lastReadMapper = lastReadMapper;
            _chapterClient = chapterClient;
            _chapterInfoMapper = chapterInfoMapper;
            _translatedMangaClient = translatedMangaClient;
        }

        public async Task<List<LastReadMangaViewModel>> GetReadingHistoryAsync()
        {
            var historyViewModels = new List<LastReadMangaViewModel>();

            if (!_userService.IsAuthenticated())
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể lấy lịch sử đọc.");
                return historyViewModels;
            }

            var token = _userService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Không thể lấy token người dùng đã đăng nhập.");
                return historyViewModels;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Đang gọi API backend /api/users/reading-history");
                var response = await client.GetAsync("/api/users/reading-history");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi khi gọi API backend lấy lịch sử đọc. Status: {response.StatusCode}, Content: {errorContent}");
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _userService.RemoveToken();
                    }
                    return historyViewModels;
                }

                var content = await response.Content.ReadAsStringAsync();
                var backendHistory = JsonSerializer.Deserialize<List<BackendHistoryItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (backendHistory == null || !backendHistory.Any())
                {
                    _logger.LogInformation("Không có lịch sử đọc nào từ backend.");
                    return historyViewModels;
                }

                _logger.LogInformation($"Nhận được {backendHistory.Count} mục lịch sử từ backend. Bắt đầu lấy chi tiết...");

                foreach (var item in backendHistory)
                {
                    await Task.Delay(_rateLimitDelay);
                    var mangaInfo = await _mangaInfoService.GetMangaInfoAsync(item.MangaId);
                    if (mangaInfo == null)
                    {
                        _logger.LogWarning("Không thể lấy thông tin cho MangaId: {MangaId} trong lịch sử đọc. Bỏ qua.", item.MangaId);
                        continue;
                    }

                    ChapterInfoViewModel chapterInfoViewModel;
                    try
                    {
                        if (!Guid.TryParse(item.ChapterId, out var chapterGuid))
                        {
                            _logger.LogWarning("ChapterId không hợp lệ: {ChapterId}. Bỏ qua.", item.ChapterId);
                            continue;
                        }

                        var chapterResponse = await _chapterClient.GetChapterByIdAsync(chapterGuid);
                        if (chapterResponse?.Data == null)
                        {
                            _logger.LogWarning("Không tìm thấy chapter với ID: {ChapterId} trong lịch sử đọc. Bỏ qua.", item.ChapterId);
                            continue;
                        }

                        var tmRel = chapterResponse.Data.Relationships?.FirstOrDefault(r => r.Type == "translated_manga");
                        string langKey = "en"; // Mặc định
                        if (tmRel != null && Guid.TryParse(tmRel.Id, out var tmGuid))
                        {
                            var tmResponse = await _translatedMangaClient.GetTranslatedMangaByIdAsync(tmGuid);
                            if (tmResponse?.Data?.Attributes?.LanguageKey != null)
                            {
                                langKey = tmResponse.Data.Attributes.LanguageKey;
                            }
                        }

                        chapterInfoViewModel = _chapterInfoMapper.MapToChapterInfo(chapterResponse.Data, langKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi lấy thông tin chapter {ChapterId} trong lịch sử đọc.", item.ChapterId);
                        continue;
                    }

                    var historyViewModel = _lastReadMapper.MapToLastReadMangaViewModel(mangaInfo, chapterInfoViewModel, item.LastReadAt);
                    historyViewModels.Add(historyViewModel);
                }

                _logger.LogInformation("Hoàn tất xử lý {Count} mục lịch sử đọc.", historyViewModels.Count);
                return historyViewModels;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Lỗi khi deserialize lịch sử đọc từ backend.");
                return historyViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ngoại lệ khi lấy và xử lý lịch sử đọc.");
                return historyViewModels;
            }
        }
    }
}
```

#### 3.2.5. Cập nhật `MangaSearchService.cs`

<!-- MangaReader_WebUI\Services\MangaServices\MangaPageService\MangaSearchService.cs -->
```csharp
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReaderLib.Enums;
using Microsoft.Extensions.Logging;

namespace MangaReader.WebUI.Services.MangaServices.MangaPageService
{
    public class MangaSearchService
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly ILogger<MangaSearchService> _logger;
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;

        public MangaSearchService(
            IMangaReaderLibMangaClient mangaClient,
            ILogger<MangaSearchService> logger,
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper)
        {
            _mangaClient = mangaClient;
            _logger = logger;
            _mangaViewModelMapper = mangaViewModelMapper;
        }

        public SortManga CreateSortMangaFromParameters(
            string title = "", List<string>? status = null, string sortBy = "latest",
            string authors = "", string artists = "", int? year = null,
            List<string>? availableTranslatedLanguage = null, List<string>? publicationDemographic = null,
            List<string>? contentRating = null, string includedTagsMode = "AND",
            string excludedTagsMode = "OR", List<string>? genres = null,
            string includedTagsStr = "", string excludedTagsStr = "")
        {
            var sortManga = new SortManga
            {
                Title = title,
                Status = status ?? new List<string>(),
                SortBy = sortBy ?? "latest",
                Year = year,
                Demographic = publicationDemographic ?? new List<string>(),
                IncludedTagsMode = includedTagsMode ?? "AND",
                ExcludedTagsMode = excludedTagsMode ?? "OR",
                Genres = genres
            };

            if (!string.IsNullOrEmpty(authors))
            {
                sortManga.Authors = authors.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
            }
            if (!string.IsNullOrEmpty(artists))
            {
                // Note: MangaReaderLib API uses authorIdsFilter for both authors and artists.
                // We merge them here for simplicity. The API should handle filtering by role.
                sortManga.Authors.AddRange(artists.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)));
                sortManga.Authors = sortManga.Authors.Distinct().ToList();
            }
            
            if (availableTranslatedLanguage != null && availableTranslatedLanguage.Any())
            {
                sortManga.OriginalLanguage = availableTranslatedLanguage;
            }
            
            if (contentRating != null && contentRating.Any())
            {
                sortManga.ContentRating = contentRating;
            }
            
            if (!string.IsNullOrEmpty(includedTagsStr))
            {
                sortManga.IncludedTags = includedTagsStr.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            }
            
            if (!string.IsNullOrEmpty(excludedTagsStr))
            {
                sortManga.ExcludedTags = excludedTagsStr.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            }

            return sortManga;
        }

        public async Task<MangaListViewModel> SearchMangaAsync(int page, int pageSize, SortManga sortManga)
        {
            try
            {
                int offset = (page - 1) * pageSize;

                List<PublicationDemographic>? demographics = null;
                if (sortManga.Demographic != null && sortManga.Demographic.Any())
                {
                    demographics = sortManga.Demographic
                        .Select(d => Enum.TryParse<PublicationDemographic>(d, true, out var demo) ? (PublicationDemographic?)demo : null)
                        .Where(d => d.HasValue)
                        .Select(d => d.Value)
                        .ToList();
                }

                var result = await _mangaClient.GetMangasAsync(
                    offset: offset,
                    limit: pageSize,
                    titleFilter: sortManga.Title,
                    statusFilter: sortManga.Status?.FirstOrDefault(),
                    contentRatingFilter: sortManga.ContentRating?.FirstOrDefault(),
                    publicationDemographicsFilter: demographics,
                    originalLanguageFilter: sortManga.OriginalLanguage?.FirstOrDefault(),
                    yearFilter: sortManga.Year,
                    authorIdsFilter: sortManga.Authors?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                    includedTags: sortManga.IncludedTags?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                    includedTagsMode: sortManga.IncludedTagsMode,
                    excludedTags: sortManga.ExcludedTags?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                    excludedTagsMode: sortManga.ExcludedTagsMode,
                    orderBy: sortManga.SortBy,
                    ascending: sortManga.SortBy == "title",
                    includes: new List<string> { "cover_art", "author", "tag" }
                );

                int totalCount = result?.Total ?? 0;
                int maxPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var mangaViewModels = new List<MangaViewModel>();
                if (result?.Data != null)
                {
                    foreach (var mangaDto in result.Data)
                    {
                        if (mangaDto != null)
                        {
                            mangaViewModels.Add(await _mangaViewModelMapper.MapToMangaViewModelAsync(mangaDto));
                        }
                    }
                }

                return new MangaListViewModel
                {
                    Mangas = mangaViewModels,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    MaxPages = maxPages,
                    SortOptions = sortManga
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách manga.");
                return new MangaListViewModel
                {
                    Mangas = new List<MangaViewModel>(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    MaxPages = 0,
                    SortOptions = sortManga
                };
            }
        }
    }
}
```

### 3.3. Cập nhật các file README

Cuối cùng, cập nhật các file README để phản ánh cấu trúc mới, loại bỏ các tham chiếu đến MangaDex và proxy.

#### 3.3.1. Cập nhật `MangaReader_WebUI\README.md`

<!-- MangaReader_WebUI\README.md -->
```markdown
# Manga Reader Web Frontend

Đây là dự án frontend cho ứng dụng đọc truyện manga trực tuyến, được xây dựng bằng ASP.NET Core MVC. Frontend này tương tác với hai hệ thống backend:

1.  **MangaReaderLib API:** Nguồn dữ liệu chính cho tất cả thông tin liên quan đến manga, chapters, tác giả, tags, và ảnh bìa.
2.  **Backend Auth API:** Dịch vụ riêng xử lý xác thực người dùng (Google OAuth) và quản lý dữ liệu người dùng (danh sách theo dõi, lịch sử đọc).

## Mục tiêu

- Cung cấp giao diện người dùng thân thiện, hiện đại để duyệt, tìm kiếm và đọc manga.
- Tích hợp với `MangaReaderLib API` để lấy dữ liệu manga.
- Tích hợp với `Backend Auth API` để xử lý xác thực và dữ liệu người dùng.
- Sử dụng HTMX để cải thiện trải nghiệm người dùng với các cập nhật trang một phần (partial page updates).

## Công nghệ sử dụng

- **Backend Framework:** ASP.NET Core 9.0 MVC
- **Ngôn ngữ:** C#
- **Frontend Framework/Libraries:**
  - Bootstrap 5.3
  - jQuery 3.7.1
  - HTMX 1.9.12
- **Kiến trúc:** Model-View-Controller (MVC)
- **API Tương tác:**
  - `MangaReaderLib` API
  - Backend Auth API

## Cài đặt và Chạy dự án

### Yêu cầu

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) hoặc phiên bản mới hơn.
- Một trình soạn thảo mã nguồn (Visual Studio, VS Code, Rider, ...).
- `MangaReaderLib` API đang chạy (Xem cấu hình `MangaReaderApiSettings:BaseUrl` trong `appsettings.json`).
- Backend Auth API đang chạy (Xem cấu hình `BackendApi:BaseUrl` trong `appsettings.json`).

### Các bước cài đặt

1.  **Clone repository:**
    ```bash
    git clone <your-repository-url>
    cd MangaReader.WebUI
    ```
2.  **Chạy ứng dụng:**
    Sử dụng lệnh `dotnet run` hoặc chạy từ IDE của bạn.
```

#### 3.3.2. Cập nhật `MangaReader_WebUI\Controllers\README.md`

<!-- MangaReader_WebUI\Controllers\README.md -->
```markdown
# Controllers

Thư mục `Controllers` chứa các lớp Controller theo kiến trúc Model-View-Controller (MVC) của ASP.NET Core. Vai trò chính của các Controller là:

1.  **Tiếp nhận yêu cầu HTTP:** Xử lý các request từ trình duyệt của người dùng (GET, POST, ...).
2.  **Điều phối logic:** Gọi các `Services` tương ứng để thực hiện logic nghiệp vụ, lấy hoặc xử lý dữ liệu.
3.  **Chuẩn bị dữ liệu cho View:** Tạo hoặc lấy các đối tượng `ViewModel` từ `Services` và truyền chúng đến `View`.
4.  **Trả về phản hồi:** Quyết định và trả về kết quả cho người dùng, thường là một `View` (trang HTML) hoặc `JsonResult` (cho các API endpoint hoặc HTMX request).

## Danh sách Controllers

- **`AuthController.cs`**:
  - Xử lý các action liên quan đến xác thực người dùng.
  - `Login`: Hiển thị trang đăng nhập.
  - `GoogleLogin`: Bắt đầu luồng đăng nhập bằng Google OAuth bằng cách gọi `UserService` để lấy URL xác thực và chuyển hướng người dùng.
  - `Callback`: Xử lý callback từ Backend Auth API sau khi xác thực Google thành công, nhận JWT token và lưu trữ nó.
  - `Logout`: Xóa thông tin đăng nhập (token).
  - `GetCurrentUser`: API endpoint (AJAX/Fetch) để kiểm tra trạng thái đăng nhập và lấy thông tin người dùng hiện tại.
  - `Profile`: Hiển thị trang thông tin cá nhân của người dùng đã đăng nhập.
- **`ChapterController.cs`**:
  - Xử lý các action liên quan đến việc đọc chapter.
  - `Read(string id)`: Hiển thị trang đọc của một chapter cụ thể. Gọi `ChapterReadingServices` để lấy toàn bộ thông tin cần thiết cho trang đọc.
  - `SaveReadingProgress`: API endpoint để lưu tiến độ đọc của người dùng vào Backend Auth API.
- **`HomeController.cs`**:
  - Xử lý các action cho các trang cơ bản của ứng dụng.
  - `Index`: Hiển thị trang chủ, lấy danh sách manga mới cập nhật từ `MangaReaderLib API`.
  - `Privacy`: Hiển thị trang chính sách bảo mật.
  - `Error`: Hiển thị trang lỗi chung của ứng dụng.
- **`MangaController.cs`**:
  - Xử lý các action liên quan đến thông tin và danh sách manga.
  - `Details(string id)`: Hiển thị trang chi tiết của một manga. Sử dụng `MangaDetailsService` để lấy toàn bộ thông tin cần thiết.
  - `Search(...)`: Hiển thị trang tìm kiếm manga và xử lý kết quả tìm kiếm. Sử dụng `MangaSearchService` để phân tích tham số, gọi `MangaReaderLib API` và xử lý phân trang.
  - `GetTags()`: API endpoint để lấy danh sách tags từ `MangaReaderLib API`.
  - `ToggleFollowProxy(...)`: Proxy action để xử lý việc theo dõi/hủy theo dõi manga thông qua Backend Auth API.
  - `Followed()`: Hiển thị danh sách truyện đang theo dõi của người dùng.
  - `History()`: Hiển thị lịch sử đọc của người dùng.

## Tích hợp HTMX

Một số action trong các controller (đặc biệt là `HomeController`, `MangaController`) sử dụng `ViewRenderService` để trả về `PartialView` thay vì `View` đầy đủ khi nhận được request từ HTMX (có header `HX-Request`). Điều này cho phép cập nhật chỉ một phần của trang web, mang lại trải nghiệm mượt mà hơn cho người dùng.
```

#### 3.3.3. Cập nhật `MangaReader_WebUI\Services\README.md`

<!-- MangaReader_WebUI\Services\README.md -->
```markdown
# Services

Thư mục `Services` chứa các lớp logic nghiệp vụ cốt lõi của ứng dụng frontend. Các service này chịu trách nhiệm xử lý dữ liệu, tương tác với các API bên ngoài (`MangaReaderLib API` và `Backend Auth API`), và cung cấp dữ liệu đã được xử lý cho các Controllers.

## Cấu trúc

Các service được tổ chức thành các thư mục con dựa trên chức năng chính:

- **`AuthServices/`**: Chứa các service liên quan đến xác thực và quản lý người dùng.
  - `IUserService.cs`, `UserService.cs`: Giao tiếp với **Backend Auth API** để xử lý đăng nhập Google OAuth, quản lý JWT token và lấy thông tin người dùng.

- **`MangaServices/`**: Tập hợp các service xử lý logic liên quan đến manga, lấy dữ liệu từ **MangaReaderLib API**.
  - **`ChapterServices/`**:
    - `ChapterService.cs`: Lấy và xử lý thông tin chi tiết về các chapter, bao gồm việc định dạng tiêu đề, sắp xếp và phân loại theo ngôn ngữ.
    - `ChapterReadingServices.cs`: Tổng hợp tất cả dữ liệu cần thiết để hiển thị trang đọc truyện.
    - `MangaIdService.cs`, `ChapterLanguageServices.cs`: Các service helper để lấy thông tin liên quan đến chapter.
  - **`MangaPageService/`**:
    - `MangaDetailsService.cs`: Tổng hợp thông tin chi tiết của một manga và danh sách chapters để hiển thị trên trang chi tiết.
    - `MangaSearchService.cs`: Xử lý logic tìm kiếm manga, gọi API tìm kiếm và xử lý kết quả trả về.
  - `IMangaFollowService.cs`, `MangaFollowService.cs`: Quản lý trạng thái theo dõi manga của người dùng, tương tác với **Backend Auth API**.
  - `IFollowedMangaService.cs`, `FollowedMangaService.cs`: Lấy danh sách manga đang theo dõi và các chapter mới nhất của chúng.
  - `IReadingHistoryService.cs`, `ReadingHistoryService.cs`: Lấy lịch sử đọc của người dùng từ **Backend Auth API**.
  - `IMangaInfoService.cs`, `MangaInfoService.cs`: Cung cấp thông tin cơ bản của một manga.

- **`APIServices/MangaReaderLibApiClients/`**: Chứa các client service (và interface của chúng) được tạo ra từ project `MangaReaderLib`, chịu trách nhiệm thực hiện các lệnh gọi HTTP trực tiếp đến `MangaReaderLib API`.

- **`MangaServices/DataProcessing/`**: Chứa các Mappers chịu trách nhiệm chuyển đổi các đối tượng DTO từ `MangaReaderLib` thành các `ViewModel` mà `View` có thể sử dụng.

- **`UtilityServices/`**: Chứa các service tiện ích dùng chung trong ứng dụng.
  - `JsonConversionService.cs`: Cung cấp các hàm để chuyển đổi dữ liệu JSON.
  - `LocalizationService.cs`: Cung cấp các hàm dịch thuật (ví dụ: trạng thái manga).
  - `ViewRenderService.cs`: Giúp quyết định render `View` hay `PartialView` dựa trên request HTMX.

## Nguyên tắc thiết kế

- **Dependency Injection:** Tất cả các service đều được đăng ký trong `Program.cs` và được inject vào các lớp cần sử dụng.
- **Single Responsibility Principle:** Mỗi service tập trung vào một nhiệm vụ hoặc một nhóm nhiệm vụ liên quan chặt chẽ.
- **Abstraction:** Sử dụng interface (ví dụ: `IUserService`) để tăng tính linh hoạt và khả năng kiểm thử.
- **Error Handling:** Các service cố gắng xử lý lỗi một cách hợp lý (ví dụ: logging lỗi, trả về giá trị mặc định) để tránh làm crash ứng dụng.
```