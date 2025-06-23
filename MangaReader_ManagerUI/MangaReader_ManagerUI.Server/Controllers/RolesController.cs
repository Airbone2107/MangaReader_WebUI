using MangaReaderLib.DTOs.Users;
using MangaReaderLib.Services.Exceptions;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaReader_ManagerUI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleClient _roleClient;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRoleClient roleClient, ILogger<RolesController> logger)
        {
            _roleClient = roleClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var result = await _roleClient.GetRolesAsync(HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API exception while getting roles.");
                return StatusCode((int)ex.StatusCode, ex.ApiErrorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while getting roles.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
        
        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetRolePermissions(string id)
        {
            try
            {
                var result = await _roleClient.GetRolePermissionsAsync(id, HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API exception while getting permissions for role {RoleId}.", id);
                return StatusCode((int)ex.StatusCode, ex.ApiErrorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while getting permissions for role {RoleId}.", id);
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPut("{id}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(string id, [FromBody] UpdateRolePermissionsRequestDto updateDto)
        {
            try
            {
                await _roleClient.UpdateRolePermissionsAsync(id, updateDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API exception while updating permissions for role {RoleId}.", id);
                return StatusCode((int)ex.StatusCode, ex.ApiErrorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating permissions for role {RoleId}.", id);
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
} 