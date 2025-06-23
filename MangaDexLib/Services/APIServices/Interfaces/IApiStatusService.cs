namespace MangaDexLib.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho service kiểm tra trạng thái kết nối của API.
    /// </summary>
    public interface IApiStatusService
    {
        /// <summary>
        /// Kiểm tra kết nối đến Backend API proxy.
        /// </summary>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là <c>true</c> nếu kết nối thành công (API trả về mã trạng thái thành công);
        /// ngược lại, trả về <c>false</c>.
        /// </returns>
        Task<bool> TestConnectionAsync();
    }
} 