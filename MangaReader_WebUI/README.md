# Manga Reader Web Frontend

Đây là dự án frontend cho ứng dụng đọc truyện manga trực tuyến, được xây dựng bằng ASP.NET Core MVC. Frontend này tương tác với hai hệ thống backend:

1.  **MangaReaderLib API:** Nguồn dữ liệu chính cho tất cả thông tin liên quan đến manga, chapters, tác giả, tags, và ảnh bìa.
2.  **Auth API:** Dịch vụ riêng xử lý xác thực người dùng (Google OAuth) và quản lý dữ liệu người dùng (danh sách theo dõi, lịch sử đọc).

## Mục tiêu

- Cung cấp giao diện người dùng thân thiện, hiện đại để duyệt, tìm kiếm và đọc manga.
- Tích hợp với `MangaReaderLib API` để lấy dữ liệu manga.
- Tích hợp với `Auth API` để xử lý xác thực và dữ liệu người dùng.
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
  - `MangaReaderLib` API (cho dữ liệu truyện)
  - `Auth` API (cho người dùng và xác thực)

## Cài đặt và Chạy dự án

### Yêu cầu

- .NET 9 SDK hoặc phiên bản mới hơn.
- Một trình soạn thảo mã nguồn (Visual Studio, VS Code, Rider, ...).
- `MangaReaderLib` API đang chạy (Xem cấu hình `MangaReaderApiSettings:BaseUrl` trong `appsettings.json`).
- `Auth` API đang chạy (Xem cấu hình `BackendApi:BaseUrl` trong `appsettings.json`).

### Các bước cài đặt

1.  **Clone repository:**
    ```bash
    git clone <your-repository-url>
    cd MangaReader.WebUI
    ```
2.  **Chạy ứng dụng:**
    Sử dụng lệnh `dotnet run` hoặc chạy từ IDE của bạn.
