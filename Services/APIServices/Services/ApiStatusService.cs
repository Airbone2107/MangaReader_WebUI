using MangaReader.WebUI.Services.APIServices.Interfaces;

namespace MangaReader.WebUI.Services.APIServices.Services
{
    public class ApiStatusService : BaseApiService, IApiStatusService
    {
        public ApiStatusService(HttpClient httpClient, ILogger<ApiStatusService> logger, IConfiguration configuration)
            : base(httpClient, logger, configuration)
        {
        }

        public async Task<bool> TestConnectionAsync()
        {
            Logger.LogInformation("Testing connection to MangaDex API...");
            var url = BuildUrlWithParams("status");
            Logger.LogInformation($"Sending request to: {url}");

            try
            {
                // Đặt thời gian chờ ngắn hơn cho request kiểm tra kết nối
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                
                var response = await HttpClient.SendAsync(requestMessage, cts.Token);
                var content = await response.Content.ReadAsStringAsync();
                
                Logger.LogInformation($"Connection test result: {(int)response.StatusCode} - {content}");
                
                return response.IsSuccessStatusCode;
            }
            catch (TaskCanceledException ex)
            {
                // Xử lý timeout
                Logger.LogWarning($"Timeout during connection test: {ex.Message}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                // Xử lý lỗi HTTP
                Logger.LogWarning($"HTTP error during connection test: {ex.Message}");
                return false;
            }
            catch (OperationCanceledException ex)
            {
                // Xử lý hủy thao tác
                Logger.LogWarning($"Connection test was canceled: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                Logger.LogError($"Unexpected error during connection test: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }
} 