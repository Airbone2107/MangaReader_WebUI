using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MangaReader.WebUI.Services.APIServices
{
    public abstract class BaseApiService
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;
        protected readonly string BaseUrl;
        protected readonly JsonSerializerOptions JsonOptions;

        protected BaseApiService(HttpClient httpClient, ILogger logger, IConfiguration configuration)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            BaseUrl = configuration["BackendApi:BaseUrl"]?.TrimEnd('/') + "/mangadex"
                      ?? throw new InvalidOperationException("BackendApi:BaseUrl/mangadex is not configured.");

            // Cấu hình timeout nếu cần, ví dụ 60 giây
            HttpClient.Timeout = TimeSpan.FromSeconds(60);

            JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Ghi log lỗi với thông tin chi tiết
        protected void LogApiError(string functionName, HttpResponseMessage response, string content)
        {
            string errorMessage = $"Lỗi trong hàm {functionName} ({this.GetType().Name}):\n" +
                                $"URL: {response.RequestMessage?.RequestUri}\n" +
                                $"Mã trạng thái: {(int)response.StatusCode}\n" +
                                $"Nội dung phản hồi: {content}";
            Logger.LogError(errorMessage);
            Console.WriteLine(errorMessage); // Giữ lại log console nếu cần debug nhanh
            Console.WriteLine("Stack trace:");
            Console.WriteLine(new StackTrace().ToString());
        }

        protected string BuildUrlWithParams(string endpointPath, Dictionary<string, List<string>>? parameters = null)
        {
            var fullUrl = BaseUrl.TrimEnd('/') + "/" + endpointPath.TrimStart('/');

            if (parameters == null || parameters.Count == 0)
                return fullUrl;

            var sb = new StringBuilder(fullUrl);
            sb.Append('?');

            bool isFirst = true;
            foreach (var param in parameters)
            {
                foreach (var value in param.Value)
                {
                    if (!isFirst)
                        sb.Append('&');
                    else
                        isFirst = false;

                    sb.Append(Uri.EscapeDataString(param.Key));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(value));
                }
            }
            return sb.ToString();
        }

        protected void AddOrUpdateParam(Dictionary<string, List<string>> parameters, string key, string value)
        {
            if (!parameters.ContainsKey(key))
            {
                parameters[key] = new List<string>();
            }
            parameters[key].Add(value);
        }
    }
} 