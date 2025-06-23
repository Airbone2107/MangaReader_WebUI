using MangaReaderLib.DTOs.Users;
using MangaReaderLib.Services.Exceptions;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaReader_ManagerUI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserClient _userClient;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserClient userClient, ILogger<UsersController> logger)
        {
            _userClient = userClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int? offset, [FromQuery] int? limit)
        {
            try
            {
                var result = await _userClient.GetUsersAsync(offset, limit, HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API exception while getting users.");
                return StatusCode((int)ex.StatusCode, ex.ApiErrorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while getting users.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto createUserDto)
        {
            try
            {
                await _userClient.CreateUserAsync(createUserDto, HttpContext.RequestAborted);
                return StatusCode(201);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API exception while creating user.");
                return StatusCode((int)ex.StatusCode, ex.ApiErrorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating user.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPut("{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] UpdateUserRolesRequestDto updateUserRolesDto)
        {
            try
            {
                await _userClient.UpdateUserRolesAsync(id, updateUserRolesDto, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API exception while updating roles for user {UserId}.", id);
                return StatusCode((int)ex.StatusCode, ex.ApiErrorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating roles for user {UserId}.", id);
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
} 