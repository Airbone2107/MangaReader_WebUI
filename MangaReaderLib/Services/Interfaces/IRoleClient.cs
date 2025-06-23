using MangaReaderLib.DTOs.Users;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    public interface IRoleClient
    {
        Task<List<RoleDto>?> GetRolesAsync(CancellationToken cancellationToken = default);
        Task<RoleDetailsDto?> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);
        Task UpdateRolePermissionsAsync(string roleId, UpdateRolePermissionsRequestDto updatePermissionsDto, CancellationToken cancellationToken = default);
    }
} 