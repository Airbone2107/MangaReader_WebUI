# Hướng Dẫn API Quản Lý User & Role

Tài liệu này tập trung vào các API endpoint dành cho việc quản lý Người dùng (Users), Vai trò (Roles), và Quyền hạn (Permissions) trong hệ thống MangaReader. Đây là tài liệu cốt lõi cho các nhà phát triển xây dựng trang quản trị (Admin Panel) hoặc các công cụ tương tác với hệ thống phân quyền của API.

## Mục Lục

1.  [Giới Thiệu](#1-giới-thiệu)
2.  [Xác Thực & Phân Quyền (Authentication & Authorization)](#2-xác-thực--phân-quyền-authentication--authorization)
    *   [2.1. Luồng Xác Thực](#21-luồng-xác-thực)
    *   [2.2. Phân Quyền Dựa Trên Claims & Policies](#22-phân-quyền-dựa-trên-claims--policies)
    *   [2.3. Danh Sách Permissions (Quyền Hạn)](#23-danh-sách-permissions-quyền-hạn)
    *   [2.4. Danh Sách Roles (Vai Trò)](#24-danh-sách-roles-vai-trò)
3.  [Các Quy Ước Chung & Cấu Trúc Response](#3-các-quy-ước-chung--cấu-trúc-response)
4.  [Chi Tiết Các API Endpoints](#4-chi-tiết-các-api-endpoints)
    *   [4.1. Authentication (`/api/auth`)](#41-authentication-apiauth)
    *   [4.2. User Management (`/api/users`)](#42-user-management-apiusers)
    *   [4.3. Role Management (`/api/roles`)](#43-role-management-apiroles)

---

## 1. Giới Thiệu

API quản lý người dùng cung cấp các phương tiện để:
*   Đăng nhập và làm mới phiên làm việc.
*   Tạo, xem, và cập nhật người dùng.
*   Xem và quản lý các vai trò.
*   Gán quyền hạn (permissions) cho các vai trò.

Đối tượng sử dụng chính của các API này là các quản trị viên hệ thống có thẩm quyền.

## 2. Xác Thực & Phân Quyền (Authentication & Authorization)

### 2.1. Luồng Xác Thực

Tất cả các endpoint quản lý (trong `/api/users` và `/api/roles`) đều yêu cầu **xác thực**. Hệ thống sử dụng **JWT Bearer Token**.

1.  **Lấy Token:** Client gửi yêu cầu đến `POST /api/auth/login` với `username` và `password`.
2.  **Sử dụng Token:** Nếu đăng nhập thành công, API sẽ trả về một `accessToken`. Client phải đính kèm token này vào header của mỗi request tiếp theo tới các endpoint được bảo vệ.
    ```
    Authorization: Bearer <your_access_token>
    ```

### 2.2. Phân Quyền Dựa Trên Claims & Policies

Quyền truy cập vào từng endpoint được kiểm soát bởi **Policies**. Mỗi policy tương ứng với một **Permission** (quyền hạn) cụ thể.

*   Khi một người dùng đăng nhập, JWT token của họ sẽ chứa tất cả các permission mà họ có, dựa trên các vai trò (roles) mà họ được gán.
*   Để gọi một endpoint, người dùng phải có permission tương ứng với policy bảo vệ endpoint đó.
*   Nếu không có token hợp lệ, API trả về `401 Unauthorized`.
*   Nếu có token hợp lệ nhưng không có đủ quyền, API trả về `403 Forbidden`.

### 2.3. Danh Sách Permissions (Quyền Hạn)

Đây là danh sách các chuỗi permission được sử dụng trong hệ thống. Chúng được dùng để định nghĩa Policy và gán cho Roles.

*   **Users**
    *   `Permissions.Users.View`: Quyền xem danh sách và chi tiết người dùng.
    *   `Permissions.Users.Create`: Quyền tạo người dùng mới.
    *   `Permissions.Users.Edit`: Quyền chỉnh sửa thông tin người dùng (ví dụ: gán vai trò).
    *   `Permissions.Users.Delete`: Quyền xóa người dùng.
*   **Roles**
    *   `Permissions.Roles.View`: Quyền xem danh sách vai trò và các quyền của chúng.
    *   `Permissions.Roles.Create`: Quyền tạo vai trò mới.
    *   `Permissions.Roles.Edit`: Quyền chỉnh sửa vai trò (ví dụ: cập nhật danh sách quyền).
    *   `Permissions.Roles.Delete`: Quyền xóa vai trò.

### 2.4. Danh Sách Roles (Vai Trò)

Hệ thống được khởi tạo với các vai trò mặc định sau:

*   `SuperAdmin`: Có tất cả các quyền hạn trong hệ thống. Vai trò này không thể bị chỉnh sửa.
*   `Admin`: Vai trò quản trị viên cấp cao.
*   `Moderator`: Vai trò điều hành viên.
*   `User`: Vai trò người dùng cơ bản.

Vai trò `SuperAdmin` được gán tất cả các permission một cách tự động khi khởi chạy ứng dụng.

## 3. Các Quy Ước Chung & Cấu Trúc Response

API tuân thủ các quy ước về HTTP Methods, Status Codes, và cấu trúc JSON response như đã mô tả trong tài liệu `MangaReaderAPI.md`. Vui lòng tham khảo tài liệu đó để biết chi tiết. Các mã lỗi quan trọng cho phần này bao gồm:

*   `401 Unauthorized`: Yêu cầu thiếu hoặc có token không hợp lệ.
*   `403 Forbidden`: Token hợp lệ nhưng người dùng không có quyền truy cập tài nguyên.
*   `404 Not Found`: Không tìm thấy user hoặc role được yêu cầu.

## 4. Chi Tiết Các API Endpoints

### 4.1. Authentication (`/api/auth`)

Các endpoint này dùng để quản lý phiên đăng nhập.

#### `POST /api/auth/login`
*   **Mô tả:** Đăng nhập vào hệ thống để nhận Access Token và Refresh Token.
*   **Request Body:** (`LoginDto`)
    ```json
    {
      "username": "superadmin",
      "password": "123456"
    }
    ```
*   **Response (200 OK):** (`AuthResponseDto`)
    ```json
    {
      "isSuccess": true,
      "message": "Login successful!",
      "accessToken": "eyJhbGciOiJIUz...",
      "refreshToken": "AbCdEfGhIjKlMnOp..."
    }
    ```

#### `POST /api/auth/refresh`
*   **Mô tả:** Dùng Refresh Token hợp lệ để lấy một cặp Access Token và Refresh Token mới.
*   **Request Body:** (`RefreshTokenRequestDto`)
    ```json
    {
      "refreshToken": "AbCdEfGhIjKlMnOp..."
    }
    ```
*   **Response (200 OK):** (`AuthResponseDto`)

#### `POST /api/auth/revoke`
*   **Mô tả:** Thu hồi một Refresh Token. Endpoint này yêu cầu xác thực (phải dùng Access Token).
*   **Authorization:** `Bearer <token>`
*   **Request Body:** (`RefreshTokenRequestDto`)
*   **Response (200 OK):**
    ```json
    {
      "isSuccess": true,
      "message": "Token revoked successfully.",
      "accessToken": "",
      "refreshToken": ""
    }
    ```

#### Ghi chú về `/api/auth/register`
Endpoint `POST /api/auth/register` đã bị **loại bỏ** hoàn toàn. Việc tạo người dùng mới hiện được quản lý bởi các quản trị viên thông qua endpoint `POST /api/users`. Thay đổi này nhằm tăng cường bảo mật và đảm bảo chỉ những người có thẩm quyền mới có thể thêm người dùng vào hệ thống.

### 4.2. User Management (`/api/users`)

Các endpoint này yêu cầu xác thực và phân quyền.

#### `GET /api/users`
*   **Mô tả:** Lấy danh sách người dùng trong hệ thống có phân trang.
*   **Authorization:** Yêu cầu `permission` là `Permissions.Users.View`.
*   **Query Parameters:**
    *   `offset` (int, optional, default: 0)
    *   `limit` (int, optional, default: 20)
*   **Response (200 OK):**
    ```json
    {
      "items": [
        {
          "id": "user-guid-1",
          "userName": "superadmin",
          "email": "superadmin@mangareader.com",
          "roles": ["SuperAdmin", "Admin", "User"]
        }
      ],
      "limit": 20,
      "offset": 0,
      "total": 1
    }
    ```

#### `POST /api/users`
*   **Mô tả:** Tạo một người dùng mới. Người dùng mới sẽ được gán các vai trò được chỉ định.
*   **Authorization:** Yêu cầu `permission` là `Permissions.Users.Create`.
*   **Request Body:** (`CreateUserRequestDto`)
    ```json
    {
      "userName": "newmod",
      "email": "moderator@example.com",
      "password": "password123",
      "roles": ["Moderator", "User"]
    }
    ```
*   **Response (201 Created):** Trả về `Location` header và ID của user mới tạo.

#### `PUT /api/users/{id}/roles`
*   **Mô tả:** Cập nhật (thay thế hoàn toàn) danh sách vai trò cho một người dùng.
*   **Authorization:** Yêu cầu `permission` là `Permissions.Users.Edit`.
*   **Path Parameter:**
    *   `id`: (string, required) ID của người dùng cần cập nhật.
*   **Request Body:** (`UpdateUserRolesRequestDto`)
    ```json
    {
      "roles": ["User"]
    }
    ```
*   **Response (204 No Content):** Cập nhật thành công.

### 4.3. Role Management (`/api/roles`)

Các endpoint này yêu cầu xác thực và phân quyền.

#### `GET /api/roles`
*   **Mô tả:** Lấy danh sách tất cả các vai trò có sẵn trong hệ thống.
*   **Authorization:** Yêu cầu `permission` là `Permissions.Roles.View`.
*   **Response (200 OK):**
    ```json
    [
      { "id": "role-guid-1", "name": "SuperAdmin" },
      { "id": "role-guid-2", "name": "Admin" },
      { "id": "role-guid-3", "name": "Moderator" },
      { "id": "role-guid-4", "name": "User" }
    ]
    ```

#### `GET /api/roles/{id}/permissions`
*   **Mô tả:** Lấy thông tin chi tiết của một vai trò, bao gồm danh sách các permission đã được gán.
*   **Authorization:** Yêu cầu `permission` là `Permissions.Roles.View`.
*   **Path Parameter:**
    *   `id`: (string, required) ID của vai trò cần xem.
*   **Response (200 OK):** (`RoleDetailsDto`)
    ```json
    {
      "id": "role-guid-1",
      "name": "SuperAdmin",
      "permissions": [
        "Permissions.Users.View",
        "Permissions.Users.Create",
        "Permissions.Users.Edit",
        "Permissions.Users.Delete",
        "Permissions.Roles.View",
        "Permissions.Roles.Create",
        "Permissions.Roles.Edit",
        "Permissions.Roles.Delete"
      ]
    }
    ```

#### `PUT /api/roles/{id}/permissions`
*   **Mô tả:** Cập nhật (thay thế hoàn toàn) danh sách permission cho một vai trò.
*   **Authorization:** Yêu cầu `permission` là `Permissions.Roles.Edit`.
*   **Path Parameter:**
    *   `id`: (string, required) ID của vai trò cần cập nhật.
*   **Request Body:** (`UpdateRolePermissionsRequestDto`)
    ```json
    {
      "permissions": [
        "Permissions.Users.View",
        "Permissions.Users.Create"
      ]
    }
    ```
*   **Response (204 No Content):** Cập nhật thành công.
*   **Lưu ý:** Bạn không thể chỉnh sửa vai trò `SuperAdmin`. API sẽ trả về lỗi nếu cố gắng làm vậy.
```