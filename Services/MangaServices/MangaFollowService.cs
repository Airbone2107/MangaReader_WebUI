using MangaReader.WebUI.Services.AuthServices;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices
{
    /// <summary>
    /// Interface định nghĩa các phương thức cho dịch vụ theo dõi manga
    /// </summary>
    public interface IMangaFollowService
    {
        /// <summary>
        /// Kiểm tra xem một manga có đang được theo dõi hay không
        /// </summary>
        Task<bool> IsFollowingMangaAsync(string mangaId);

        /// <summary>
        /// Lấy danh sách ID các manga đang theo dõi
        /// </summary>
        Task<List<string>> GetFollowedMangasAsync();

        /// <summary>
        /// Theo dõi một manga
        /// </summary>
        Task<bool> FollowMangaAsync(string mangaId);

        /// <summary>
        /// Hủy theo dõi một manga
        /// </summary>
        Task<bool> UnfollowMangaAsync(string mangaId);

        /// <summary>
        /// Chuyển đổi trạng thái theo dõi (nếu đang theo dõi thì hủy, nếu chưa theo dõi thì thêm vào)
        /// </summary>
        Task<bool> ToggleFollowStatusAsync(string mangaId);
    }

    public class MangaFollowService : IMangaFollowService
    {
        private readonly ILogger<MangaFollowService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;

        public MangaFollowService(
            ILogger<MangaFollowService> logger,
            IHttpClientFactory httpClientFactory,
            IUserService userService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
        }

        /// <summary>
        /// Kiểm tra xem một manga có đang được theo dõi hay không
        /// </summary>
        /// <param name="mangaId">ID của manga cần kiểm tra</param>
        /// <returns>True nếu manga đang được theo dõi, ngược lại là False</returns>
        public async Task<bool> IsFollowingMangaAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                _logger.LogWarning("Không thể kiểm tra trạng thái theo dõi: mangaId trống");
                return false;
            }

            if (!_userService.IsAuthenticated())
            {
                _logger.LogInformation("Người dùng chưa đăng nhập, không thể kiểm tra trạng thái theo dõi");
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                var token = _userService.GetToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"/api/users/user/following/{mangaId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<FollowingStatusResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result?.IsFollowing ?? false;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _userService.RemoveToken();
                    _logger.LogWarning("Token không hợp lệ hoặc đã hết hạn");
                }

                _logger.LogError($"Lỗi khi kiểm tra trạng thái theo dõi: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kiểm tra trạng thái theo dõi manga {mangaId}");
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách ID các manga đang theo dõi
        /// </summary>
        /// <returns>Danh sách ID của các manga đang theo dõi</returns>
        public async Task<List<string>> GetFollowedMangasAsync()
        {
            if (!_userService.IsAuthenticated())
            {
                _logger.LogInformation("Người dùng chưa đăng nhập, không thể lấy danh sách manga đang theo dõi");
                return new List<string>();
            }

            try
            {
                // Lấy thông tin người dùng từ UserService, trong đó đã có danh sách manga đang theo dõi
                var userInfo = await _userService.GetUserInfoAsync();
                return userInfo?.FollowingManga ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách manga đang theo dõi");
                return new List<string>();
            }
        }

        /// <summary>
        /// Theo dõi một manga
        /// </summary>
        /// <param name="mangaId">ID của manga cần theo dõi</param>
        /// <returns>True nếu thành công, False nếu thất bại</returns>
        public async Task<bool> FollowMangaAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                _logger.LogWarning("Không thể theo dõi manga: mangaId trống");
                return false;
            }

            if (!_userService.IsAuthenticated())
            {
                _logger.LogInformation("Người dùng chưa đăng nhập, không thể theo dõi manga");
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                var token = _userService.GetToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    JsonSerializer.Serialize(new { mangaId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync("/api/users/follow", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Theo dõi manga {mangaId} thành công");
                    return true;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _userService.RemoveToken();
                    _logger.LogWarning("Token không hợp lệ hoặc đã hết hạn");
                }

                _logger.LogError($"Lỗi khi theo dõi manga: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi theo dõi manga {mangaId}");
                return false;
            }
        }

        /// <summary>
        /// Hủy theo dõi một manga
        /// </summary>
        /// <param name="mangaId">ID của manga cần hủy theo dõi</param>
        /// <returns>True nếu thành công, False nếu thất bại</returns>
        public async Task<bool> UnfollowMangaAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                _logger.LogWarning("Không thể hủy theo dõi manga: mangaId trống");
                return false;
            }

            if (!_userService.IsAuthenticated())
            {
                _logger.LogInformation("Người dùng chưa đăng nhập, không thể hủy theo dõi manga");
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendApiClient");
                var token = _userService.GetToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    JsonSerializer.Serialize(new { mangaId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync("/api/users/unfollow", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Hủy theo dõi manga {mangaId} thành công");
                    return true;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _userService.RemoveToken();
                    _logger.LogWarning("Token không hợp lệ hoặc đã hết hạn");
                }

                _logger.LogError($"Lỗi khi hủy theo dõi manga: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hủy theo dõi manga {mangaId}");
                return false;
            }
        }

        /// <summary>
        /// Chuyển đổi trạng thái theo dõi (nếu đang theo dõi thì hủy, nếu chưa theo dõi thì thêm vào)
        /// </summary>
        /// <param name="mangaId">ID của manga cần chuyển đổi trạng thái</param>
        /// <returns>Trạng thái theo dõi sau khi chuyển đổi (true: đang theo dõi, false: không theo dõi)</returns>
        public async Task<bool> ToggleFollowStatusAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                _logger.LogWarning("Không thể chuyển đổi trạng thái theo dõi: mangaId trống");
                return false;
            }

            if (!_userService.IsAuthenticated())
            {
                _logger.LogInformation("Người dùng chưa đăng nhập, không thể chuyển đổi trạng thái theo dõi");
                return false;
            }

            try
            {
                // Kiểm tra trạng thái hiện tại
                bool isCurrentlyFollowing = await IsFollowingMangaAsync(mangaId);
                bool success;

                if (isCurrentlyFollowing)
                {
                    // Nếu đang theo dõi, hủy theo dõi
                    success = await UnfollowMangaAsync(mangaId);
                    return success ? false : isCurrentlyFollowing; // Nếu thành công, trả về false; nếu thất bại, giữ nguyên trạng thái
                }
                else
                {
                    // Nếu chưa theo dõi, thêm vào danh sách theo dõi
                    success = await FollowMangaAsync(mangaId);
                    return success ? true : isCurrentlyFollowing; // Nếu thành công, trả về true; nếu thất bại, giữ nguyên trạng thái
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đổi trạng thái theo dõi manga {mangaId}");
                return false;
            }
        }
    }

    /// <summary>
    /// Model để parse response từ API kiểm tra trạng thái theo dõi
    /// </summary>
    public class FollowingStatusResponse
    {
        public bool IsFollowing { get; set; }
    }
}
