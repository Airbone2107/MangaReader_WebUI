# Manga Reader Web Frontend

Đây là dự án frontend cho ứng dụng đọc truyện manga trực tuyến, được xây dựng bằng ASP.NET Core MVC. Frontend này tương tác với một Backend API riêng (đóng vai trò proxy và xử lý logic nghiệp vụ) và gián tiếp lấy dữ liệu từ MangaDex API.

## Mục tiêu

- Cung cấp giao diện người dùng thân thiện, hiện đại để duyệt, tìm kiếm và đọc manga.
- Tích hợp với Backend API để xử lý xác thực người dùng, quản lý danh sách theo dõi, và lấy dữ liệu manga/chapter.
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
  - Backend API (Proxy & Business Logic)
  - MangaDex API (thông qua Backend API)

## Cài đặt và Chạy dự án

### Yêu cầu

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) hoặc phiên bản mới hơn.
- Một trình soạn thảo mã nguồn (Visual Studio, VS Code, Rider, ...).
- Backend API đang chạy (Xem cấu hình `BackendApi:BaseUrl` trong `appsettings.json`).

### Các bước cài đặt

1.  **Clone repository:**
    ```bash
    git clone <your-repository-url>
    cd manga_reader_web
    ```
