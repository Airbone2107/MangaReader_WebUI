using System.Text.Json;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    /// <summary>
    /// Định nghĩa interface cho một service xử lý các yêu cầu API HTTP.
    /// Đóng gói logic gửi yêu cầu, xử lý phản hồi, ghi log và deserialize kết quả.
    /// </summary>
    public interface IApiRequestHandler
    {
        /// <summary>
        /// Thực hiện một yêu cầu HTTP GET đến URL được chỉ định và deserialize nội dung phản hồi thành kiểu <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu mong đợi của đối tượng trả về sau khi deserialize.</typeparam>
        /// <param name="httpClient">Đối tượng <see cref="HttpClient"/> để thực hiện yêu cầu.</param>
        /// <param name="url">URL đầy đủ của endpoint API.</param>
        /// <param name="logger">Đối tượng <see cref="ILogger"/> để ghi log (thường là logger của service gọi đến).</param>
        /// <param name="options">Đối tượng <see cref="JsonSerializerOptions"/> để cấu hình quá trình deserialize.</param>
        /// <param name="cancellationToken">Token để hủy yêu cầu (tùy chọn).</param>
        /// <returns>
        /// Một <see cref="Task{TResult}"/> đại diện cho hoạt động bất đồng bộ.
        /// Kết quả của task là một đối tượng kiểu <typeparamref name="T"/> đã được deserialize,
        /// hoặc <c>null</c> nếu yêu cầu thất bại, phản hồi không thành công, hoặc có lỗi trong quá trình deserialize.
        /// </returns>
        Task<T?> GetAsync<T>(
            HttpClient httpClient,
            string url,
            ILogger logger,
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default
        ) where T : class;
    }
}
