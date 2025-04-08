using manga_reader_web.Models.Auth;
using System.Net.Http.Headers;
using System.Text.Json;

namespace manga_reader_web.Services.AuthServices
{
    public class UserService : IUserService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserService> _logger;
        private const string TOKEN_KEY = "JwtToken";

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
            _httpContextAccessor.HttpContext?.Session.SetString(TOKEN_KEY, token);
        }

        public string GetToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(TOKEN_KEY);
        }

        public void RemoveToken()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(TOKEN_KEY);
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
                
                // Nếu token không hợp lệ (401), xóa token
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