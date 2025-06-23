using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace MangaDexLib.Services.APIServices
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho các service gọi API.
    /// Cung cấp các thành phần và phương thức dùng chung để tương tác với Backend API.
    /// </summary>
    public abstract class BaseApiService
    {
        /// <summary>
        /// HttpClient được cấu hình để gọi Backend API.
        /// Được truy cập bởi lớp kế thừa.
        /// </summary>
        protected readonly HttpClient HttpClient;

        /// <summary>
        /// Logger dành riêng cho lớp kế thừa.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// URL cơ sở của Backend API proxy cho MangaDex.
        /// </summary>
        protected readonly string BaseUrl;

        /// <summary>
        /// Tùy chọn cấu hình cho việc deserialize JSON.
        /// </summary>
        protected readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IApiRequestHandler _apiRequestHandler;
        private readonly TimeSpan _httpClientTimeout;

        protected BaseApiService(
            HttpClient httpClient,
            ILogger logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiRequestHandler = apiRequestHandler ?? throw new ArgumentNullException(nameof(apiRequestHandler));

            // URL này trỏ đến proxy backend, không phải MangaDex trực tiếp
            BaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/') + "/mangadex"
                      ?? throw new InvalidOperationException("BackendApi:BaseUrl/mangadex is not configured.");
            
            _httpClientTimeout = SetHttpClientTimeout(httpClient);
        }

        private static TimeSpan SetHttpClientTimeout(HttpClient client)
        {
            var timeout = TimeSpan.FromSeconds(60);
            client.Timeout = timeout;
            return timeout;
        }
        
        protected async Task<T?> GetApiAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
        {
            return await _apiRequestHandler.GetAsync<T>(
                this.HttpClient,
                url,
                this.Logger,
                this.JsonOptions,
                cancellationToken
            );
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
                if (param.Value == null) continue;

                foreach (var value in param.Value)
                {
                    if (!string.IsNullOrEmpty(value))
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
            }
            if (isFirst && sb[sb.Length - 1] == '?')
            {
                sb.Length--;
            }
            return sb.ToString();
        }
        
        protected void AddOrUpdateParam(Dictionary<string, List<string>> parameters, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!parameters.ContainsKey(key))
                {
                    parameters[key] = new List<string>();
                }
                parameters[key].Add(value);
            }
        }
    }
} 