using MangaReader.WebUI.Models.Auth;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MangaReader.WebUI.Services.AuthServices
{
    public class UserService : IUserService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserService> _logger;
        private const string TOKEN_COOKIE_KEY = "JwtToken";

        public UserService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<string> GetGoogleAuthUrlAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                var response = await client.GetAsync("/api/users/auth/google/url");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var authUrlResponse = JsonSerializer.Deserialize<GoogleAuthUrlResponse>(content);
                    return authUrlResponse?.AuthUrl;
                }
                
                _logger.LogError("Error fetching Google auth URL: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching Google auth URL");
                return null;
            }
        }

        public void SaveToken(string token)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                };
                context.Response.Cookies.Append(TOKEN_COOKIE_KEY, token, cookieOptions);
                _logger.LogInformation("Đã lưu token vào cookie HttpOnly.");
            }
            else
            {
                _logger.LogWarning("HttpContext is null, không thể lưu token vào cookie.");
            }
        }

        public string GetToken()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Cookies.TryGetValue(TOKEN_COOKIE_KEY, out string token))
            {
                return token;
            }
            return null;
        }

        public void RemoveToken()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Cookies.ContainsKey(TOKEN_COOKIE_KEY))
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1)
                };
                context.Response.Cookies.Append(TOKEN_COOKIE_KEY, "", cookieOptions);
                _logger.LogInformation("Đã xóa token khỏi cookie.");
            }
            else
            {
                _logger.LogDebug("Không tìm thấy cookie token để xóa.");
            }
        }

        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(GetToken());
        }

        public async Task<UserModel> GetUserInfoAsync()
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                var response = await client.GetAsync("/api/users");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserModel>(content);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    RemoveToken();
                }
                
                _logger.LogError("Error fetching user info: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching user info");
                return null;
            }
        }
    }
} 