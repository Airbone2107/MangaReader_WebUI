Ok, hãy mô hình hóa luồng xử lý cho chức năng theo dõi/hủy theo dõi truyện, bao gồm cả frontend (ASP.NET Core MVC) và backend (Node.js/Express).

**Mục tiêu:**

1.  Người dùng có thể bấm nút "Theo dõi" / "Đang theo dõi" trên trang chi tiết truyện.
2.  Trạng thái theo dõi được lưu trữ trong cơ sở dữ liệu của backend.
3.  Người dùng có thể xem danh sách các truyện đang theo dõi trên trang cá nhân.

**Các thành phần tham gia:**

1.  **Frontend (manga_reader_web):**
    *   View: `Views/Manga/Details.cshtml`, `Views/Auth/Profile.cshtml`
    *   JavaScript: Logic xử lý click nút, gọi API, cập nhật UI (`wwwroot/js/modules/manga-details.js`, `wwwroot/js/auth.js`)
    *   Controller: `MangaController` (để render trang Details), `AuthController` (để render trang Profile)
    *   Service: `IUserService` / `UserService` (để kiểm tra đăng nhập, lấy thông tin user), `HttpClient` ("BackendApiClient") để gọi API backend.
    *   Model: `MangaViewModel` (cần thêm thuộc tính `IsFollowing`), `UserModel` (đã có `FollowingManga`)
2.  **Backend (manga_reader_app_backend):**
    *   Routes: `routes/userRoutes.js` (cần thêm endpoint `/follow`, `/unfollow`)
    *   Model: `models/User.js` (đã có `followingManga`)
    *   Middleware: `authenticateToken`
    *   Database: MongoDB (`NhatDex_UserDB`)

**Luồng xử lý chi tiết:**

**Luồng 1: Hiển thị nút Theo dõi/Đang theo dõi trên trang chi tiết Manga**

1.  **Frontend - Request:** Người dùng truy cập trang `/Manga/Details/{mangaId}`.
2.  **Frontend - Controller (`MangaController.Details`):**
    *   Gọi `MangaDetailsService.GetMangaDetailsAsync(id)` để lấy thông tin chi tiết manga (bao gồm cả việc gọi `MangaDexService`).
    *   **Kiểm tra đăng nhập:** Gọi `IUserService.IsAuthenticated()`.
    *   **Nếu đã đăng nhập:**
        *   Gọi API Backend để kiểm tra trạng thái theo dõi: `GET /api/users/user/following/{mangaId}` (thông qua `HttpClient` trong một service mới hoặc `UserService`).
        *   **Backend API (`userRoutes.js` - `GET /user/following/:mangaId`):**
            *   Xác thực token (`authenticateToken`).
            *   Lấy `userId` từ `req.user`.
            *   Tìm `User` trong DB bằng `userId`.
            *   Kiểm tra xem `mangaId` có trong mảng `user.followingManga` không.
            *   Trả về JSON: `{ "isFollowing": true/false }`.
        *   **Frontend - Service:** Nhận response JSON, parse thành `boolean`.
        *   **Frontend - Controller:** Gán giá trị `isFollowing` vào `MangaViewModel.IsFollowing`.
    *   **Nếu chưa đăng nhập:** Gán `MangaViewModel.IsFollowing = false`.
    *   Truyền `MangaDetailViewModel` (chứa `MangaViewModel` đã cập nhật) vào View.
3.  **Frontend - View (`Details.cshtml`):**
    *   Render trang chi tiết.
    *   Dựa vào `@Model.Manga.IsFollowing`, hiển thị nút "Theo dõi" (nếu `false`) hoặc "Đang theo dõi" (nếu `true`).
    *   Gắn `data-id="@Model.Manga.Id"` và `data-following="@Model.Manga.IsFollowing.ToString().ToLower()"` vào nút.

**Luồng 2: Người dùng bấm nút Theo dõi / Hủy theo dõi**

1.  **Frontend - JavaScript (`manga-details.js`):**
    *   Bắt sự kiện click trên nút `#followBtn`.
    *   Kiểm tra xem người dùng đã đăng nhập chưa (có thể gọi `GET /Auth/GetCurrentUser` hoặc kiểm tra token đã lưu). Nếu chưa, chuyển hướng đến trang đăng nhập.
    *   Lấy `mangaId` từ `data-id`.
    *   Lấy trạng thái hiện tại `isFollowing` từ `data-following`.
    *   Xác định API endpoint cần gọi:
        *   Nếu `isFollowing` là `true` -> Gọi Hủy theo dõi (`/api/users/unfollow`).
        *   Nếu `isFollowing` là `false` -> Gọi Theo dõi (`/api/users/follow`).
    *   **Gọi API Backend:**
        *   Sử dụng `fetch` hoặc `axios`.
        *   Method: `POST`.
        *   URL: `https://manga-reader-app-backend.onrender.com/api/users/follow` hoặc `/unfollow`.
        *   Header: `Authorization: Bearer <JWT_TOKEN>` (lấy token từ `UserService.GetToken()` hoặc Session/LocalStorage).
        *   Body: `JSON.stringify({ mangaId: mangaId })`.
        *   Header: `Content-Type: application/json`.
2.  **Backend - API (`userRoutes.js` - `POST /follow` hoặc `POST /unfollow`):**
    *   **Middleware:** `authenticateToken` xác thực JWT.
    *   **Input:** `req.body` chứa `{ mangaId: "string" }`.
    *   **Logic:**
        *   Lấy `userId` từ `req.user.userId`.
        *   Lấy `mangaId` từ `req.body`.
        *   Kiểm tra `mangaId` có hợp lệ không.
        *   Tìm `User` trong MongoDB bằng `userId`.
        *   **Nếu `/follow`:**
            *   Kiểm tra nếu `mangaId` *chưa* có trong `user.followingManga`.
            *   Nếu chưa có, dùng `$addToSet` để thêm `mangaId` vào mảng `followingManga` (đảm bảo không trùng lặp).
            *   `await User.findByIdAndUpdate(userId, { $addToSet: { followingManga: mangaId } });`
        *   **Nếu `/unfollow`:**
            *   Kiểm tra nếu `mangaId` *đã* có trong `user.followingManga`.
            *   Nếu có, dùng `$pull` để xóa `mangaId` khỏi mảng `followingManga`.
            *   `await User.findByIdAndUpdate(userId, { $pull: { followingManga: mangaId } });`
        *   Lưu thay đổi (nếu dùng `user.save()`).
    *   **Output:**
        *   Thành công: `res.status(200).json({ message: "Thao tác thành công", isFollowing: new_status })` (trả về trạng thái mới).
        *   Lỗi: `400` (Bad Request - thiếu mangaId), `401` (Unauthorized), `404` (User not found), `500` (Server error).
3.  **Frontend - JavaScript:**
    *   Nhận response từ Backend.
    *   **Nếu thành công (status 200):**
        *   Parse JSON response để lấy trạng thái mới `isFollowing`.
        *   Cập nhật thuộc tính `data-following` của nút.
        *   Thay đổi text và icon của nút (`<i class="bi bi-bookmark-check-fill"></i>Đang theo dõi` hoặc `<i class="bi bi-bookmark-plus"></i>Theo dõi`).
        *   Hiển thị toast thông báo thành công (ví dụ: "Đã theo dõi truyện!", "Đã hủy theo dõi truyện.").
    *   **Nếu thất bại:**
        *   Hiển thị toast thông báo lỗi (ví dụ: "Lỗi! Không thể thực hiện thao tác.", "Vui lòng đăng nhập để theo dõi.").
        *   Nếu lỗi là 401, có thể xóa token và yêu cầu đăng nhập lại.

**Luồng 3: Hiển thị danh sách truyện đang theo dõi trên trang cá nhân**

1.  **Frontend - Request:** Người dùng truy cập trang `/Auth/Profile`.
2.  **Frontend - Controller (`AuthController.Profile`):**
    *   Kiểm tra đăng nhập (`IUserService.IsAuthenticated()`). Nếu chưa, chuyển hướng đến Login.
    *   Gọi `IUserService.GetUserInfoAsync()` để lấy thông tin người dùng.
    *   **Backend API (`userRoutes.js` - `GET /`):**
        *   Xác thực token.
        *   Lấy `userId`.
        *   Tìm `User` trong DB, trả về thông tin user bao gồm mảng `followingManga` (chứa các `mangaId`).
        *   **Output:** JSON object tương ứng `UserModel`.
    *   **Frontend - Controller:** Nhận `UserModel` từ `UserService`.
    *   Truyền `UserModel` vào View `Profile.cshtml`.
3.  **Frontend - View (`Profile.cshtml`):**
    *   Hiển thị thông tin cơ bản của người dùng (`DisplayName`, `Email`, `PhotoURL`).
    *   Lặp qua danh sách `Model.FollowingManga` (đây là danh sách các `mangaId`).
    *   **Với mỗi `mangaId`:**
        *   **Cách 1 (Client-side fetch):** Dùng JavaScript để gọi API lấy chi tiết từng manga.
            *   Trong JS, lặp qua các phần tử có `data-id`.
            *   Gọi API Backend proxy: `fetch('/api/mangadex/manga/' + mangaId)` hoặc gọi API lấy nhiều manga một lúc: `fetch('/api/mangadex/manga-by-ids?ids[]=' + mangaId1 + '&ids[]=' + mangaId2 + ...)` (hiệu quả hơn).
            *   Nhận thông tin chi tiết (title, cover).
            *   Cập nhật DOM để hiển thị ảnh bìa và tên truyện cho từng `mangaId`.
        *   **Cách 2 (Server-side fetch - Khuyến nghị nếu danh sách dài):**
            *   Trong `AuthController.Profile`, sau khi lấy được `UserModel`, lấy danh sách `followingManga` (các ID).
            *   Gọi một service (ví dụ `MangaFollowService` hoặc `MangaDexService`) để gọi API Backend `/api/mangadex/manga-by-ids?ids[]=...` và lấy chi tiết các manga này.
            *   Tạo một `ProfileViewModel` mới chứa cả `UserModel` và danh sách `List<MangaViewModel>` của các truyện đang theo dõi.
            *   Truyền `ProfileViewModel` vào View.
            *   View sẽ lặp qua danh sách `MangaViewModel` để hiển thị trực tiếp.
    *   Hiển thị các truyện đang theo dõi dưới dạng card hoặc danh sách.

**Chuyển đổi kiểu dữ liệu:**

*   **Manga ID:** Luôn là `string` ở cả frontend và backend.
*   **User ID:** Là `ObjectId` trong MongoDB, nhưng thường được chuyển thành `string` khi gửi/nhận qua API JSON (`req.user.userId`, `UserModel.Id`).
*   **Following List:**
    *   Backend (MongoDB): Mảng các `string` (`followingManga: [String]`).
    *   Backend (API Response `GET /`): Mảng các `string` trong JSON (`"followingManga": ["id1", "id2"]`).
    *   Frontend (C# Model `UserModel`): `List<string> FollowingManga`.
    *   Frontend (JavaScript): Mảng các `string`.
*   **API Request Body (POST /follow, /unfollow):**
    *   Frontend JS: `JSON.stringify({ mangaId: "string" })`.
    *   Backend Express: `req.body` (object JavaScript `{ mangaId: "string" }`) sau khi `express.json()` middleware xử lý.
*   **API Response (GET /user/following/:mangaId):**
    *   Backend Express: `res.json({ isFollowing: boolean })`.
    *   Frontend JS: Object JavaScript `{ isFollowing: true/false }`.
*   **API Response (GET /):**
    *   Backend Express: `res.json(userDocument)` (có thể lọc bớt trường).
    *   Frontend C#: Deserialize JSON thành `UserModel`.
*   **Manga Details (từ MangaDex qua Backend Proxy):**
    *   MangaDex API: JSON phức tạp.
    *   Backend Proxy: Chuyển tiếp JSON.
    *   Frontend C# (`MangaDexService`): Deserialize JSON thành `dynamic` hoặc `JsonElement`.
    *   Frontend C# (Services xử lý): Chuyển đổi `dynamic`/`JsonElement` thành `Dictionary<string, object>` hoặc các Model cụ thể (`MangaViewModel`, `ChapterViewModel`).

**Lưu ý:**

*   Cần xử lý lỗi cẩn thận ở cả frontend và backend (API không phản hồi, token hết hạn, mangaId không hợp lệ, lỗi DB,...).
*   Sử dụng `async/await` cho tất cả các thao tác bất đồng bộ (gọi API, truy vấn DB).
*   Tối ưu hóa việc lấy chi tiết nhiều manga trên trang Profile (sử dụng endpoint lấy theo danh sách ID thay vì gọi lặp lại).

Quy trình thực hiện

**Quy trình thực hiện chi tiết:**

## 1. Chuẩn bị và phân tích
- [x] Đánh giá cấu trúc hiện tại của dự án
- [x] Kiểm tra models đã có (`UserModel`, `MangaViewModel`)
- [x] Kiểm tra các API endpoint hiện có ở backend

## 2. Phát triển Backend (Node.js/Express)
- [x] Tạo endpoint kiểm tra trạng thái theo dõi: `GET /api/users/user/following/:mangaId`
- [x] Tạo endpoint theo dõi truyện: `POST /api/users/follow`
- [x] Tạo endpoint hủy theo dõi truyện: `POST /api/users/unfollow`
- [x] Kiểm thử các API bằng Postman hoặc công cụ tương tự

## 3. Cập nhật Frontend Models và Services (ASP.NET Core)
- [x] Cập nhật `MangaViewModel` thêm thuộc tính `IsFollowing`
- [x] Tạo hoặc cập nhật service để gọi API backend mới

## 4. Cập nhật Frontend Controllers
- [x] Cập nhật `MangaController.Details` để kiểm tra trạng thái theo dõi
- [x] Cập nhật `AuthController.Profile` để lấy danh sách truyện đang theo dõi

## 5. Cập nhật Frontend Views
- [x] Cập nhật `Views/Manga/Details.cshtml` thêm nút theo dõi/hủy theo dõi
- [x] Cập nhật `Views/Auth/Profile.cshtml` hiển thị danh sách truyện đang theo dõi

## 6. Triển khai JavaScript Client-side
- [x] Cập nhật `wwwroot/js/modules/manga-details.js` để xử lý sự kiện click nút
- [x] Thêm logic gọi API và cập nhật UI sau khi theo dõi/hủy theo dõi
- [x] Thêm logic client-side để tải thông tin chi tiết truyện ở trang Profile

## 7. Kiểm thử và Tối ưu
- [ ] Kiểm thử toàn bộ luồng theo dõi/hủy theo dõi
- [ ] Kiểm thử hiển thị danh sách truyện đang theo dõi
- [ ] Xử lý các trường hợp lỗi và ngoại lệ
- [ ] Tối ưu hiệu suất tải truyện đang theo dõi

## 8. Triển khai và Giám sát
- [ ] Triển khai các thay đổi lên môi trường production
- [ ] Giám sát lỗi và hiệu suất
- [ ] Thu thập phản hồi và cải thiện nếu cần
