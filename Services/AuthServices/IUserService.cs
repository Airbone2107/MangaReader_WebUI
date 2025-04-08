namespace manga_reader_web.Services.AuthServices
{
    public interface IUserService
    {
        /// <summary>
        /// Lấy URL xác thực Google từ Backend API
        /// </summary>
        /// <returns>URL xác thực Google</returns>
        Task<string> GetGoogleAuthUrlAsync();

        /// <summary>
        /// Lưu JWT token nhận được từ callback
        /// </summary>
        /// <param name="token">JWT token</param>
        void SaveToken(string token);

        /// <summary>
        /// Lấy JWT token đã lưu
        /// </summary>
        /// <returns>JWT token</returns>
        string GetToken();

        /// <summary>
        /// Xóa JWT token khỏi lưu trữ
        /// </summary>
        void RemoveToken();

        /// <summary>
        /// Kiểm tra xem người dùng đã đăng nhập chưa
        /// </summary>
        /// <returns>True nếu đã đăng nhập, ngược lại False</returns>
        bool IsAuthenticated();

        /// <summary>
        /// Lấy thông tin người dùng từ backend
        /// </summary>
        /// <returns>Thông tin người dùng</returns>
        Task<Models.Auth.UserModel> GetUserInfoAsync();
    }
} 