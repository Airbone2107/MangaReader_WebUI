using MangaReaderLib.DTOs.Users;
using MangaReaderLib.Services.Exceptions;
using MangaReaderLib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaReader_ManagerUI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthClient _authClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthClient authClient, ILogger<AuthController> logger)
        {
            _authClient = authClient;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var response = await _authClient.LoginAsync(loginDto, HttpContext.RequestAborted);
                if (response == null || !response.IsSuccess)
                {
                    return Unauthorized(response != null 
                        ? response 
                        : new AuthResponseDto { IsSuccess = false, Message = "Invalid username or password." });
                }
                return Ok(response);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API exception during login for user {Username}.", loginDto.Username);
                return StatusCode((int)ex.StatusCode, ex.ApiErrorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during login for user {Username}.", loginDto.Username);
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
} 