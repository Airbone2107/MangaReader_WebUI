using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    /// <summary>
    /// Cung cấp việc triển khai cho <see cref="IApiStatusService"/>.
    /// </summary>
    /// <remarks>
    /// Sử dụng Primary Constructor để nhận dependency và gọi constructor lớp cơ sở.
    /// </remarks>
    /// <param name="httpClient">HttpClient đã được cấu hình.</param>
    /// <param name="logger">Logger cho ApiStatusService.</param>
    /// <param name="configuration">Đối tượng IConfiguration để lấy cấu hình.</param>
    /// <param name="apiRequestHandler">Service xử lý yêu cầu API.</param>
    public class ApiStatusService(
        HttpClient httpClient,
        ILogger<ApiStatusService> logger,
        IConfiguration configuration,
        IApiRequestHandler apiRequestHandler)
        : BaseApiService(httpClient, logger, configuration, apiRequestHandler),
          IApiStatusService
    {
        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync()
        {
            var url = BuildUrlWithParams("status");

            // Đặt thời gian chờ ngắn hơn cho request kiểm tra kết nối
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Gọi GetApiAsync để kiểm tra kết nối. Kiểu dữ liệu T không quan trọng.
            var result = await GetApiAsync<object>(url, cts.Token);

            // Nếu GetApiAsync trả về khác null, nghĩa là yêu cầu thành công (status code 2xx)
            return result != null;
        }
    }
} 