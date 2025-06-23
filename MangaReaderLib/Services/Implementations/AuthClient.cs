using MangaReaderLib.DTOs.Users;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Implementations
{
    public class AuthClient : IAuthClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<AuthClient> _logger;

        public AuthClient(IApiClient apiClient, ILogger<AuthClient> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to log in for user: {Username}", loginDto.Username);
            return await _apiClient.PostAsync<LoginDto, AuthResponseDto>("api/Auth/login", loginDto, cancellationToken);
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to refresh token.");
            return await _apiClient.PostAsync<RefreshTokenRequestDto, AuthResponseDto>("api/Auth/refresh", refreshTokenDto, cancellationToken);
        }

        public async Task<AuthResponseDto?> RevokeTokenAsync(RefreshTokenRequestDto refreshTokenDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to revoke token.");
            return await _apiClient.PostAsync<RefreshTokenRequestDto, AuthResponseDto>("api/Auth/revoke", refreshTokenDto, cancellationToken);
        }
    }
} 