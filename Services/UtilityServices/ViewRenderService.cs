using Microsoft.AspNetCore.Mvc;

namespace MangaReader.WebUI.Services.UtilityServices
{
    /// <summary>
    /// Service xử lý việc render view dựa trên loại request (HTMX hoặc thông thường)
    /// </summary>
    public class ViewRenderService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public ViewRenderService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        /// <summary>
        /// Quyết định trả về View hay PartialView dựa vào loại request
        /// </summary>
        /// <param name="controller">Controller hiện tại</param>
        /// <param name="viewName">Tên view hoặc đường dẫn view</param>
        /// <param name="model">Model để truyền vào view</param>
        /// <returns>ActionResult phù hợp với loại request</returns>
        public IActionResult RenderViewBasedOnRequest(Controller controller, string viewName, object model)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            // Kiểm tra nếu là HTMX request
            if (httpContext != null && httpContext.Request.Headers.ContainsKey("HX-Request"))
            {
                // Nếu viewName bắt đầu bằng "~/Views/" thì đó là đường dẫn tuyệt đối
                if (!string.IsNullOrEmpty(viewName) && viewName.StartsWith("~/Views/"))
                {
                    return controller.PartialView(viewName, model);
                }
                return controller.PartialView(viewName, model);
            }
            
            // Tương tự với View
            if (!string.IsNullOrEmpty(viewName) && viewName.StartsWith("~/Views/"))
            {
                return controller.View(viewName, model);
            }
            return controller.View(viewName, model);
        }
        
        /// <summary>
        /// Overload của phương thức RenderViewBasedOnRequest không yêu cầu tên view cụ thể
        /// </summary>
        public IActionResult RenderViewBasedOnRequest(Controller controller, object model)
        {
            return RenderViewBasedOnRequest(controller, null, model);
        }
    }
}
