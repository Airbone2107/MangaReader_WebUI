using MangaReaderLib.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace MangaReader_ManagerUI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Trả về 200 OK với payload là một đối tượng đơn lẻ.
        /// </summary>
        protected ActionResult Ok<TData>(ApiResponse<TData> apiResponse) where TData : class
        {
            return base.Ok(apiResponse);
        }

        /// <summary>
        /// Trả về 200 OK với payload là một danh sách có phân trang.
        /// </summary>
        protected ActionResult Ok<TData>(ApiCollectionResponse<TData> apiCollectionResponse) where TData : class
        {
            return base.Ok(apiCollectionResponse);
        }
        
        /// <summary>
        /// Trả về 201 Created với payload là một đối tượng đơn lẻ và location header.
        /// </summary>
        protected ActionResult Created<TData>(string actionName, object? routeValues, ApiResponse<TData> value) where TData : class
        {
            return base.CreatedAtAction(actionName, routeValues, value);
        }
    }
} 