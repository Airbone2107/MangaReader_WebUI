using MangaReaderLib.DTOs.Users;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Implementations
{
    public class RoleClient : IRoleClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<RoleClient> _logger;

        public RoleClient(IApiClient apiClient, ILogger<RoleClient> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<List<RoleDto>?> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting all roles.");
            return await _apiClient.GetAsync<List<RoleDto>>("api/Roles", cancellationToken);
        }

        public async Task<RoleDetailsDto?> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting permissions for role ID: {RoleId}", roleId);
            return await _apiClient.GetAsync<RoleDetailsDto>($"api/Roles/{roleId}/permissions", cancellationToken);
        }

        public async Task UpdateRolePermissionsAsync(string roleId, UpdateRolePermissionsRequestDto updatePermissionsDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating permissions for role ID: {RoleId}", roleId);
            await _apiClient.PutAsync($"api/Roles/{roleId}/permissions", updatePermissionsDto, cancellationToken);
        }
    }
} 