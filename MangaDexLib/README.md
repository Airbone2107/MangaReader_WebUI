# MangaDexLib

`MangaDexLib` là một thư viện .NET độc lập được thiết kế để đóng gói tất cả các tương tác với API MangaDex, được tách ra từ dự án `MangaReader_WebUI`.

## Mục đích

-   **Tách biệt logic:** Tách biệt hoàn toàn logic gọi API MangaDex khỏi logic giao diện người dùng của ứng dụng chính.
-   **Tái sử dụng:** Tạo ra một thành phần có thể tái sử dụng trong các dự án khác nếu cần tương tác với MangaDex.
-   **Tổ chức code:** Giữ cho project chính (`MangaReader_WebUI`) gọn gàng và tập trung vào nguồn dữ liệu của `MangaReaderLib`.

## Cấu trúc

-   **`Models/`**: Chứa các lớp C# (DTOs) đại diện cho cấu trúc dữ liệu JSON trả về từ MangaDex API (ví dụ: `Manga`, `Chapter`, `Cover`...).
-   **`Services/APIServices/`**: Chứa các lớp service chịu trách nhiệm thực hiện các cuộc gọi HTTP đến backend proxy và deserialize phản hồi.
-   **`DataProcessing/`**: Chứa các lớp service xử lý và chuyển đổi dữ liệu thô từ API thành các định dạng dễ sử dụng hơn (Mappers, Extractors).
-   **`api.yaml`**: Tài liệu OpenAPI Specification của MangaDex API để tham khảo.

## Cách Hoạt Động

Thư viện này không gọi trực tiếp đến `api.mangadex.org`. Thay vào đó, nó tương tác với một **Backend API** (được cấu hình qua `BackendApi:BaseUrl`) đóng vai trò là một proxy. Điều này giúp:

-   Ẩn API key (nếu có).
-   Xử lý rate-limiting phía server.
-   Cache các phản hồi từ MangaDex để tăng hiệu suất.
-   Đơn giản hóa logic phía client. 