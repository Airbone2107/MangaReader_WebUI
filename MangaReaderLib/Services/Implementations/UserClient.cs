using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Users;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Implementations
{
    public class UserClient : IUserClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<UserClient> _logger;

        public UserClient(IApiClient apiClient, ILogger<UserClient> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<PagedResult<UserDto>?> GetUsersAsync(int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting users with offset: {Offset}, limit: {Limit}", offset, limit);
            var queryString = new StringBuilder("api/Users?");
            if (offset.HasValue) queryString.Append($"Offset={offset.Value}&");
            if (limit.HasValue) queryString.Append($"Limit={limit.Value}&");

            return await _apiClient.GetAsync<PagedResult<UserDto>>(queryString.ToString().TrimEnd('&', '?'), cancellationToken);
        }

        public async Task CreateUserAsync(CreateUserRequestDto createUserDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new user: {Username}", createUserDto.UserName);
            await _apiClient.PostAsync("api/Users", createUserDto, cancellationToken);
        }

        public async Task UpdateUserRolesAsync(string userId, UpdateUserRolesRequestDto updateUserRolesDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating roles for user ID: {UserId}", userId);
            await _apiClient.PutAsync($"api/Users/{userId}/roles", updateUserRolesDto, cancellationToken);
        }
    }
} 