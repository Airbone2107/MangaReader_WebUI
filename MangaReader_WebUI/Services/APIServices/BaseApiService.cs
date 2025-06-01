using MangaReader.WebUI.Services.APIServices.Interfaces;
using System.Text;
using System.Text.Json;
namespace MangaReader.WebUI.Services.APIServices
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho các service gọi API.
    /// Cung cấp các thành phần và phương thức dùng chung để tương tác với Backend API.
    /// </summary>
    /// <remarks>
    /// Sử dụng Primary Constructor để nhận dependency.
    /// </remarks>
    /// <param name="httpClient">HttpClient đã được cấu hình.</param>
    /// <param name="logger">Logger cho lớp kế thừa.</param>
    /// <param name="configuration">Đối tượng IConfiguration để lấy cấu hình.</param>
    /// <param name="apiRequestHandler">Service xử lý yêu cầu API.</param>
    public abstract class BaseApiService(
        HttpClient httpClient,
        ILogger logger,
        IConfiguration configuration,
        IApiRequestHandler apiRequestHandler)
    {
        /// <summary>
        /// HttpClient được cấu hình để gọi Backend API.
        /// Được truy cập bởi lớp kế thừa.
        /// </summary>
        protected readonly HttpClient HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        /// <summary>
        /// Logger dành riêng cho lớp kế thừa.
        /// </summary>
        protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// URL cơ sở của Backend API proxy cho MangaDex.
        /// </summary>
        protected readonly string BaseUrl = configuration?["BackendApi:BaseUrl"]?.TrimEnd('/') + "/mangadex"
                                         ?? throw new InvalidOperationException("BackendApi:BaseUrl/mangadex is not configured.");

        /// <summary>
        /// Tùy chọn cấu hình cho việc deserialize JSON.
        /// </summary>
        protected readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly TimeSpan _httpClientTimeout = SetHttpClientTimeout(httpClient); // Gọi hàm helper để set timeout

        // Hàm helper để đặt timeout
        private static TimeSpan SetHttpClientTimeout(HttpClient client)
        {
            var timeout = TimeSpan.FromSeconds(60);
            client.Timeout = timeout;
            return timeout;
        }

        /// <summary>
        /// Gửi yêu cầu GET đến API và deserialize phản hồi.
        /// Sử dụng <see cref="IApiRequestHandler"/> để đóng gói logic gọi và xử lý lỗi.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu mong đợi của phản hồi API.</typeparam>
        /// <param name="url">URL đầy đủ của endpoint API.</param>
        /// <param name="cancellationToken">Token để hủy yêu cầu (tùy chọn).</param>
        /// <returns>Một đối tượng kiểu <typeparamref name="T"/> đã được deserialize hoặc null nếu có lỗi.</returns>
        protected async Task<T?> GetApiAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
        {
            return await apiRequestHandler.GetAsync<T>(
                this.HttpClient,
                url,
                this.Logger,
                this.JsonOptions,
                cancellationToken
            );
        }

        /// <summary>
        /// Xây dựng URL đầy đủ với các tham số truy vấn từ một Dictionary.
        /// Hỗ trợ các tham số có nhiều giá trị (ví dụ: `includes[]=a&includes[]=b`).
        /// </summary>
        /// <param name="endpointPath">Đường dẫn tương đối của endpoint (ví dụ: "manga", "chapter/{id}/feed").</param>
        /// <param name="parameters">Dictionary chứa các tham số truy vấn. Key là tên tham số, Value là danh sách các giá trị.</param>
        /// <returns>URL đầy đủ đã bao gồm các tham số truy vấn.</returns>
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

        /// <summary>
        /// Thêm hoặc cập nhật một tham số vào Dictionary các tham số truy vấn.
        /// Nếu key chưa tồn tại, tạo mới danh sách giá trị.
        /// Nếu value không null hoặc rỗng, thêm value vào danh sách của key tương ứng.
        /// </summary>
        /// <param name="parameters">Dictionary chứa các tham số.</param>
        /// <param name="key">Tên tham số.</param>
        /// <param name="value">Giá trị của tham số (có thể là null).</param>
        protected void AddOrUpdateParam(Dictionary<string, List<string>> parameters, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!parameters.ContainsKey(key))
                {
                    parameters[key] = [];
                }
                parameters[key].Add(value);
            }
        }
    }
} 