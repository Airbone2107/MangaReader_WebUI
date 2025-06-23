using System.Net.Http.Headers;

namespace MangaReader_ManagerUI.Server
{
    /// <summary>
    /// Một DelegatingHandler để chặn các yêu cầu HTTP đi ra,
    /// đọc token xác thực từ HttpContext của yêu cầu đến,
    /// và gắn nó vào yêu cầu đang được gửi đến API backend.
    /// </summary>
    public class AuthenticationHeaderHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationHeaderHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Lấy HttpContext của request hiện tại từ client (React) đến server này.
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) == true)
            {
                // Sao chép header "Authorization" từ request gốc sang request đang được gửi đi.
                // Điều này đảm bảo token được chuyển tiếp đến API backend.
                if (AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
                {
                    request.Headers.Authorization = headerValue;
                }
            }

            // Tiếp tục gửi request đi sau khi đã thêm header (nếu có).
            return await base.SendAsync(request, cancellationToken);
        }
    }
} 