using MangaReaderLib.DTOs.Users;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    public interface IAuthClient
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenDto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto?> RevokeTokenAsync(RefreshTokenRequestDto refreshTokenDto, CancellationToken cancellationToken = default);
    }
} 