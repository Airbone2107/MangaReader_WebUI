using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    public interface IUserClient
    {
        Task<PagedResult<UserDto>?> GetUsersAsync(int? offset = null, int? limit = null, CancellationToken cancellationToken = default);
        Task CreateUserAsync(CreateUserRequestDto createUserDto, CancellationToken cancellationToken = default);
        Task UpdateUserRolesAsync(string userId, UpdateUserRolesRequestDto updateUserRolesDto, CancellationToken cancellationToken = default);
    }
} 