using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Generic;
using System.Linq;

namespace manga_reader_web.Infrastructure // Đảm bảo namespace phù hợp
{
    /// <summary>
    /// Mở rộng cách Razor View Engine tìm kiếm các file View và Partial View
    /// để phù hợp với cấu trúc thư mục theo feature.
    /// </summary>
    public class CustomViewLocationExpander : IViewLocationExpander
    {
        // Danh sách các thư mục "feature" bạn đã tạo trong Views/
        private static readonly string[] FeatureFolders = {
            "Auth",
            "ChapterRead",
            "Home",
            "MangaSearch",
            "Manga",
            "Shared"
            // Thêm các thư mục feature khác nếu bạn tạo thêm
        };

        // Danh sách các thư mục con của thư mục Manga
        private static readonly string[] MangaSubFolders = {
            "MangaDetails",
            "MangaFollowed",
            "MangaHistory"
        };

        /// <summary>
        /// Được gọi bởi View Engine để lấy danh sách các đường dẫn tìm kiếm View.
        /// </summary>
        /// <param name="context">Thông tin về View đang được tìm kiếm.</param>
        /// <param name="viewLocations">Danh sách các đường dẫn tìm kiếm mặc định.</param>
        /// <returns>Danh sách các đường dẫn tìm kiếm đã được mở rộng.</returns>
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            // {0} = Tên View (hoặc Partial View không có dấu _)
            // {1} = Tên Controller (ít dùng trong cấu trúc này)
            // {2} = Tên Area (không dùng trong dự án này)

            // Ưu tiên tìm kiếm trong các thư mục feature đã định nghĩa
            foreach (var folder in FeatureFolders)
            {
                // Đường dẫn cho View chính (ví dụ: Search.cshtml -> {0} = "Search")
                yield return $"~/Views/{folder}/{{0}}.cshtml";
                // Đường dẫn cho Partial View (ví dụ: _SearchFormPartial.cshtml -> {0} = "SearchFormPartial")
                yield return $"~/Views/{folder}/_{{0}}.cshtml";
                
                // Nếu là thư mục Manga, tìm thêm trong các thư mục con
                if (folder == "Manga")
                {
                    foreach (var subFolder in MangaSubFolders)
                    {
                        yield return $"~/Views/{folder}/{subFolder}/{{0}}.cshtml";
                        yield return $"~/Views/{folder}/{subFolder}/_{{0}}.cshtml";
                    }
                }
            }

            // Luôn tìm kiếm trong thư mục Shared (rất quan trọng)
            yield return "~/Views/Shared/{0}.cshtml";
            yield return "~/Views/Shared/_{0}.cshtml"; // Cho partials trong Shared

            // --- Tùy chọn: Giữ lại các đường dẫn mặc định ---
            // Bỏ comment phần dưới nếu bạn muốn View Engine vẫn tìm ở các vị trí cũ
            // (ví dụ: /Views/ControllerName/ActionName.cshtml) phòng trường hợp bạn chưa di chuyển hết.
            // Tuy nhiên, nếu đã di chuyển hết, việc này có thể không cần thiết và làm chậm quá trình tìm kiếm một chút.
            
            foreach (var location in viewLocations)
            {
                yield return location;
            }
        }

        /// <summary>
        /// Được gọi bởi View Engine để thêm các giá trị vào RouteData,
        /// thường dùng cho việc cache key. Không cần thiết cho trường hợp này.
        /// </summary>
        public void PopulateValues(ViewLocationExpanderContext context)
        {
            // Không cần thực hiện gì ở đây.
        }
    }
}
