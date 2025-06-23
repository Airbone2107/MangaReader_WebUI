Chào bạn,

Tôi đã sẵn sàng giúp bạn triển khai Roles và Policies để bảo vệ các endpoint chỉnh sửa Manga. Dựa trên yêu cầu của bạn, tôi sẽ thực hiện các bước sau:

1.  **Định nghĩa các quyền (Permissions)** mới dành riêng cho việc quản lý Manga.
2.  **Cập nhật Seeder** để tự động gán các quyền này cho vai trò `Admin` và `SuperAdmin` khi ứng dụng khởi chạy.
3.  **Áp dụng các Policy** tương ứng lên các endpoint `POST`, `PUT`, `DELETE` trong tất cả các Controller liên quan đến việc chỉnh sửa dữ liệu Manga (bao gồm Authors, Tags, Chapters, Covers, v.v.).

Các endpoint `GET` sẽ không được bảo vệ, đúng như yêu cầu của bạn.

Dưới đây là các tệp đã được cập nhật với đầy đủ mã nguồn.

### 1. Định nghĩa các quyền mới trong `Domain/Constants/Permissions.cs`

Chúng ta sẽ thêm một nhóm quyền mới cho việc quản lý Manga.

<file path="Domain\Constants\Permissions.cs">
```csharp
namespace Domain.Constants
{
    public static class Permissions
    {
        public static class Users
        {
            public const string View = "Permissions.Users.View";
            public const string Create = "Permissions.Users.Create";
            public const string Edit = "Permissions.Users.Edit";
            public const string Delete = "Permissions.Users.Delete";
        }

        public static class Roles
        {
            public const string View = "Permissions.Roles.View";
            public const string Create = "Permissions.Roles.Create";
            public const string Edit = "Permissions.Roles.Edit";
            public const string Delete = "Permissions.Roles.Delete";
        }

        // <<< THÊM MỚI BẮT ĐẦU >>>
        /// <summary>
        /// Các quyền liên quan đến quản lý nội dung Manga.
        /// </summary>
        public static class Manga
        {
            /// <summary>
            /// Quyền tạo mới các tài nguyên liên quan đến manga (manga, author, tag, chapter, v.v.).
            /// </summary>
            public const string Create = "Permissions.Manga.Create";

            /// <summary>
            /// Quyền chỉnh sửa các tài nguyên liên quan đến manga.
            /// </summary>
            public const string Edit = "Permissions.Manga.Edit";

            /// <summary>
            /// Quyền xóa các tài nguyên liên quan đến manga.
            /// </summary>
            public const string Delete = "Permissions.Manga.Delete";
        }
        // <<< THÊM MỚI KẾT THÚC >>>
    }
}
```
</file>

### 2. Cập nhật Seeder trong `Persistence/Data/SeedData.cs`

Chúng ta sẽ cập nhật `SeedData` để gán các quyền mới cho vai trò `Admin`. Vai trò `SuperAdmin` đã được cấu hình để tự động nhận tất cả các quyền nên không cần thay đổi.

<file path="Persistence\Data\SeedData.cs">
```csharp
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Persistence.Data
{
    public static class SeedData
    {
        public static async Task SeedEssentialsAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles
            await SeedRolesAsync(roleManager);

            // Seed SuperAdmin User
            await SeedSuperAdminAsync(userManager, roleManager);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Tạo các vai trò từ AppRoles constants
            await CreateRoleIfNotExists(roleManager, AppRoles.SuperAdmin);
            await CreateRoleIfNotExists(roleManager, AppRoles.Admin);
            await CreateRoleIfNotExists(roleManager, AppRoles.Moderator);
            await CreateRoleIfNotExists(roleManager, AppRoles.User);
        }

        private static async Task CreateRoleIfNotExists(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Tạo user SuperAdmin mặc định
            var defaultUser = new ApplicationUser 
            { 
                UserName = "superadmin", 
                Email = "superadmin@mangareader.com", 
                EmailConfirmed = true 
            };

            if (userManager.Users.All(u => u.Id != defaultUser.Id))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "123456");
                    await userManager.AddToRoleAsync(defaultUser, AppRoles.SuperAdmin);
                    await userManager.AddToRoleAsync(defaultUser, AppRoles.Admin);
                    await userManager.AddToRoleAsync(defaultUser, AppRoles.Moderator);
                    await userManager.AddToRoleAsync(defaultUser, AppRoles.User);
                }
            }
            
            // Gán tất cả quyền cho vai trò SuperAdmin
            await AddAllPermissionsToRole(roleManager, AppRoles.SuperAdmin);
            
            // <<< THÊM MỚI BẮT ĐẦU >>>
            // Gán các quyền quản lý Manga cho vai trò Admin
            await AddMangaPermissionsToAdminRole(roleManager);
            // <<< THÊM MỚI KẾT THÚC >>>
        }
        
        // <<< THÊM MỚI BẮT ĐẦU >>>
        private static async Task AddMangaPermissionsToAdminRole(RoleManager<IdentityRole> roleManager)
        {
            var adminRole = await roleManager.FindByNameAsync(AppRoles.Admin);
            if (adminRole == null) return;
            
            var adminClaims = await roleManager.GetClaimsAsync(adminRole);
            var mangaPermissions = new List<string>
            {
                Permissions.Manga.Create,
                Permissions.Manga.Edit,
                Permissions.Manga.Delete
            };
            
            foreach (var permission in mangaPermissions)
            {
                if (!adminClaims.Any(c => c.Type == "permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(adminRole, new Claim("permission", permission));
                }
            }
        }
        // <<< THÊM MỚI KẾT THÚC >>>

        public static async Task AddAllPermissionsToRole(RoleManager<IdentityRole> roleManager, string roleName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) return;
            
            var allClaims = await roleManager.GetClaimsAsync(role);
            var allPermissions = GetAllPermissions();
            
            foreach (var permission in allPermissions)
            {
                if (!allClaims.Any(c => c.Type == "permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(role, new Claim("permission", permission));
                }
            }
        }

        private static List<string> GetAllPermissions()
        {
            var permissions = new List<string>();
            var nestedTypes = typeof(Permissions).GetNestedTypes();
            foreach (var type in nestedTypes)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                permissions.AddRange(fields.Select(fi => (string)fi.GetValue(null)!));
            }
            return permissions;
        }
    }
}
```
</file>

### 3. Áp dụng Policies vào các Controllers

Bây giờ, chúng ta sẽ thêm attribute `[Authorize(Policy = "...")]` vào các action `POST`, `PUT`, `DELETE` của các controller liên quan đến Manga.

#### `MangaReaderDB/Controllers/AuthorsController.cs`

<file path="MangaReaderDB\Controllers\AuthorsController.cs">
```csharp
// MangaReaderDB/Controllers/AuthorsController.cs
using Application.Common.DTOs.Authors;
using Application.Common.Models;
using Application.Common.Responses;
using Application.Exceptions;
using Application.Features.Authors.Commands.CreateAuthor;
using Application.Features.Authors.Commands.DeleteAuthor;
using Application.Features.Authors.Commands.UpdateAuthor;
using Application.Features.Authors.Queries.GetAuthorById;
using Application.Features.Authors.Queries.GetAuthors;
using Domain.Constants; // <<< THÊM MỚI
using FluentValidation;
using Microsoft.AspNetCore.Authorization; // <<< THÊM MỚI
using Microsoft.AspNetCore.Mvc;

namespace MangaReaderDB.Controllers
{
    public class AuthorsController : BaseApiController
    {
        private readonly IValidator<CreateAuthorDto> _createAuthorDtoValidator;
        private readonly IValidator<UpdateAuthorDto> _updateAuthorDtoValidator;
        private readonly ILogger<AuthorsController> _logger;

        public AuthorsController(
            IValidator<CreateAuthorDto> createAuthorDtoValidator,
            IValidator<UpdateAuthorDto> updateAuthorDtoValidator,
            ILogger<AuthorsController> logger)
        {
            _createAuthorDtoValidator = createAuthorDtoValidator;
            _updateAuthorDtoValidator = updateAuthorDtoValidator;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<AuthorAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAuthor([FromBody] CreateAuthorDto createAuthorDto)
        {
            var validationResult = await _createAuthorDtoValidator.ValidateAsync(createAuthorDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new CreateAuthorCommand 
            { 
                Name = createAuthorDto.Name, 
                Biography = createAuthorDto.Biography 
            };
            var authorId = await Mediator.Send(command);
            
            var authorResource = await Mediator.Send(new GetAuthorByIdQuery { AuthorId = authorId });
            if (authorResource == null)
            {
                 _logger.LogError($"FATAL: Author with ID {authorId} was not found after creation!");
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Creation Error", "Failed to retrieve resource after creation.")));
            }
            return Created(nameof(GetAuthorById), new { id = authorId }, authorResource);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<AuthorAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuthorById(Guid id)
        {
            var query = new GetAuthorByIdQuery { AuthorId = id };
            var authorResource = await Mediator.Send(query);
            if (authorResource == null)
            {
                throw new NotFoundException(nameof(Domain.Entities.Author), id);
            }
            return Ok(authorResource);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<AuthorAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuthors([FromQuery] GetAuthorsQuery query)
        {
            var result = await Mediator.Send(query); // QueryHandler trả về PagedResult<ResourceObject<AuthorAttributesDto>>
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAuthor(Guid id, [FromBody] UpdateAuthorDto updateAuthorDto)
        {
            var validationResult = await _updateAuthorDtoValidator.ValidateAsync(updateAuthorDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new UpdateAuthorCommand
            {
                AuthorId = id,
                Name = updateAuthorDto.Name,
                Biography = updateAuthorDto.Biography
            };
            await Mediator.Send(command); 
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Delete)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAuthor(Guid id)
        {
            var command = new DeleteAuthorCommand { AuthorId = id };
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
```
</file>

#### `MangaReaderDB/Controllers/ChaptersController.cs` và `ChapterPagesController`

<file path="MangaReaderDB\Controllers\ChaptersController.cs">
```csharp
// File: MangaReaderDB/Controllers/ChaptersController.cs
// comment
using Application.Common.DTOs.Chapters;
using Application.Common.Models; // For ResourceObject
using Application.Common.Responses;
// Application.Exceptions đã được using, nhưng ta sẽ chỉ định rõ ràng khi new ValidationException
using Application.Features.Chapters.Commands.CreateChapter;
using Application.Features.Chapters.Commands.CreateChapterPageEntry;
using Application.Features.Chapters.Commands.DeleteChapter;
using Application.Features.Chapters.Commands.DeleteChapterPage;
using Application.Features.Chapters.Commands.SyncChapterPages;
using Application.Features.Chapters.Commands.UpdateChapter;
using Application.Features.Chapters.Commands.UpdateChapterPageDetails;
using Application.Features.Chapters.Commands.UploadChapterPageImage;
using Application.Features.Chapters.Commands.UploadChapterPages;
using Application.Features.Chapters.Queries.GetChapterById;
using Application.Features.Chapters.Queries.GetChapterPages;
using Application.Features.Chapters.Queries.GetChaptersByTranslatedManga;
using Domain.Constants; // <<< THÊM MỚI
using Microsoft.AspNetCore.Authorization; // <<< THÊM MỚI
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MangaReaderDB.Controllers
{
    public class ChaptersController : BaseApiController
    {
        private readonly FluentValidation.IValidator<CreateChapterDto> _createChapterDtoValidator;
        private readonly FluentValidation.IValidator<UpdateChapterDto> _updateChapterDtoValidator;
        private readonly FluentValidation.IValidator<CreateChapterPageDto> _createChapterPageDtoValidator;
        private readonly ILogger<ChaptersController> _logger;

        public ChaptersController(
            FluentValidation.IValidator<CreateChapterDto> createChapterDtoValidator,
            FluentValidation.IValidator<UpdateChapterDto> updateChapterDtoValidator,
            FluentValidation.IValidator<CreateChapterPageDto> createChapterPageDtoValidator,
            ILogger<ChaptersController> logger)
        {
            _createChapterDtoValidator = createChapterDtoValidator;
            _updateChapterDtoValidator = updateChapterDtoValidator;
            _createChapterPageDtoValidator = createChapterPageDtoValidator;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<ChapterAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateChapter([FromBody] CreateChapterDto createDto)
        {
            var validationResult = await _createChapterDtoValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new CreateChapterCommand
            {
                TranslatedMangaId = createDto.TranslatedMangaId,
                UploadedByUserId = createDto.UploadedByUserId,
                Volume = createDto.Volume,
                ChapterNumber = createDto.ChapterNumber,
                Title = createDto.Title,
                PublishAt = createDto.PublishAt,
                ReadableAt = createDto.ReadableAt
            };
            var chapterId = await Mediator.Send(command);
            var chapterResource = await Mediator.Send(new GetChapterByIdQuery { ChapterId = chapterId });

            if (chapterResource == null)
            {
                _logger.LogError($"FATAL: Chapter with ID {chapterId} was not found after creation!");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(new ApiError(500, "Creation Error", "Failed to retrieve resource after creation.")));
            }
            return Created(nameof(GetChapterById), new { id = chapterId }, chapterResource);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<ChapterAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChapterById(Guid id)
        {
            var query = new GetChapterByIdQuery { ChapterId = id };
            var chapterResource = await Mediator.Send(query);
            if (chapterResource == null)
            {
                throw new Application.Exceptions.NotFoundException(nameof(Domain.Entities.Chapter), id);
            }
            return Ok(chapterResource);
        }

        [HttpGet("/translatedmangas/{translatedMangaId:guid}/chapters")]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<ChapterAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetChaptersByTranslatedManga(Guid translatedMangaId, [FromQuery] GetChaptersByTranslatedMangaQuery query)
        {
            query.TranslatedMangaId = translatedMangaId;
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] UpdateChapterDto updateDto)
        {
            var validationResult = await _updateChapterDtoValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new UpdateChapterCommand
            {
                ChapterId = id,
                Volume = updateDto.Volume,
                ChapterNumber = updateDto.ChapterNumber,
                Title = updateDto.Title,
                PublishAt = updateDto.PublishAt,
                ReadableAt = updateDto.ReadableAt
            };
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Delete)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteChapter(Guid id)
        {
            var command = new DeleteChapterCommand { ChapterId = id };
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpPost("{chapterId:guid}/pages/entry")]
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateChapterPageEntry(Guid chapterId, [FromBody] CreateChapterPageDto createPageDto)
        {
            var validationResult = await _createChapterPageDtoValidator.ValidateAsync(createPageDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new CreateChapterPageEntryCommand
            {
                ChapterId = chapterId,
                PageNumber = createPageDto.PageNumber
            };
            var pageId = await Mediator.Send(command);

            var responsePayload = new { PageId = pageId };

            return CreatedAtAction(
                actionName: nameof(ChapterPagesController.UploadChapterPageImage),
                controllerName: "ChapterPages", // Tên controller mà không có "Controller" ở cuối
                routeValues: new { pageId = pageId },
                value: new ApiResponse<object>(responsePayload)
            );
        }

        [HttpGet("{chapterId:guid}/pages")]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetChapterPages(Guid chapterId, [FromQuery] GetChapterPagesQuery query)
        {
            query.ChapterId = chapterId;
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("{chapterId:guid}/pages/batch")]
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<List<ChapterPageAttributesDto>>), StatusCodes.Status201Created)] // Sử dụng 200 OK để đơn giản, hoặc 201 nếu tất cả đều mới
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadChapterPages(Guid chapterId, [FromForm] List<IFormFile> files, [FromForm] List<int> pageNumbers)
        {
            if (files == null || !files.Any())
            {
                throw new Application.Exceptions.ValidationException("files", "At least one file is required.");
            }
            if (pageNumbers == null || !pageNumbers.Any())
            {
                throw new Application.Exceptions.ValidationException("pageNumbers", "Page numbers are required for all files.");
            }
            if (files.Count != pageNumbers.Count)
            {
                throw new Application.Exceptions.ValidationException("files/pageNumbers", "The number of files must match the number of page numbers provided.");
            }

            var filesToUpload = new List<FileToUpload>();
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var pageNumber = pageNumbers[i];

                if (file.Length == 0)
                    throw new Application.Exceptions.ValidationException($"files[{i}]", "File content cannot be empty.");
                if (file.Length > 10 * 1024 * 1024) // 10MB limit
                    throw new Application.Exceptions.ValidationException($"files[{i}]", "File size cannot exceed 10MB.");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    throw new Application.Exceptions.ValidationException($"files[{i}]", "Invalid file type. Allowed types are: " + string.Join(", ", allowedExtensions));
                }
                if (pageNumber <= 0)
                {
                    throw new Application.Exceptions.ValidationException($"pageNumbers[{i}]", "Page number must be greater than 0.");
                }

                var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                filesToUpload.Add(new FileToUpload
                {
                    ImageStream = memoryStream,
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    DesiredPageNumber = pageNumber
                });
            }

            var command = new UploadChapterPagesCommand
            {
                ChapterId = chapterId,
                Files = filesToUpload
            };

            var result = await Mediator.Send(command);
            return Ok(result); // BaseApiController.Ok sẽ wrap nó trong ApiResponse
        }

        [HttpPut("{chapterId:guid}/pages")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<List<ChapterPageAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SyncChapterPages(Guid chapterId, [FromForm] string pageOperationsJson /* Bỏ , [FromForm] IFormFileCollection files */)
        {
            // Lấy files trực tiếp từ Request.Form
            var formFiles = Request.Form.Files;

            _logger.LogInformation("SyncChapterPages called for ChapterId: {ChapterId}", chapterId);
            _logger.LogInformation("Received pageOperationsJson: {PageOperationsJson}", pageOperationsJson);

            if (formFiles != null && formFiles.Any())
            {
                _logger.LogInformation("Received {FilesCount} files in Request.Form.Files:", formFiles.Count);
                foreach (var f in formFiles)
                {
                    _logger.LogInformation("- File Name (from IFormFile.Name - should be FileIdentifier): '{FormFileName}', OriginalFileName: '{OriginalFileName}', ContentType: '{ContentType}', Length: {Length} bytes",
                        f.Name, f.FileName, f.ContentType, f.Length);
                }
            }
            else
            {
                _logger.LogWarning("No files received in Request.Form.Files.");
            }
            
            if (string.IsNullOrEmpty(pageOperationsJson))
            {
                throw new Application.Exceptions.ValidationException("pageOperationsJson", "Page operations JSON is required.");
            }

            List<PageOperationDto>? pageOperations;
            try
            {
                pageOperations = JsonSerializer.Deserialize<List<PageOperationDto>>(pageOperationsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize pageOperationsJson.");
                throw new Application.Exceptions.ValidationException("pageOperationsJson", "Invalid JSON format for page operations.");
            }

            if (pageOperations == null)
            {
                 _logger.LogError("pageOperationsJson deserialized to null.");
                throw new Application.Exceptions.ValidationException("pageOperationsJson", "Page operations cannot be null after deserialization.");
            }
             _logger.LogInformation("Deserialized {PageOperationsCount} page operations from JSON:", pageOperations.Count);
            foreach (var opLog in pageOperations)
            {
                _logger.LogInformation("- Operation: PageId='{PageId}', PageNumber={PageNumber}, FileIdentifier='{FileIdentifier}'",
                    opLog.PageId?.ToString() ?? "null", opLog.PageNumber, opLog.FileIdentifier ?? "null");
            }

            // Tạo fileMap một cách an toàn hơn, ưu tiên file đầu tiên nếu có trùng tên form field.
            // Tuy nhiên, client NÊN đảm bảo mỗi FileIdentifier là duy nhất cho mỗi file trong request.
            var fileMap = new Dictionary<string, IFormFile>();
            if (formFiles != null)
            {
                foreach (var file in formFiles)
                {
                    if (!fileMap.TryAdd(file.Name, file))
                    {
                        _logger.LogWarning("Duplicate form field name (FileIdentifier) detected: '{FileIdentifier}'. Only the first file with this identifier will be used.", file.Name);
                        // Cân nhắc: Có nên throw lỗi ở đây để client sửa không?
                        // throw new Application.Exceptions.ValidationException($"Duplicate FileIdentifier '{file.Name}' received. Each file must have a unique FileIdentifier as its form field name.");
                    }
                }
            }

            var instructions = new List<PageSyncInstruction>();
            foreach (var op in pageOperations)
            {
                if (op.PageNumber <= 0)
                {
                    string errorContext = op.PageId.HasValue ? $"PageId '{op.PageId.Value}'" : $"FileIdentifier '{op.FileIdentifier}'";
                    throw new Application.Exceptions.ValidationException($"PageOperation.PageNumber", $"Page number '{op.PageNumber}' for operation related to {errorContext} must be positive.");
                }
                FileToUploadInfo? fileToUploadInfo = null;
                if (!string.IsNullOrEmpty(op.FileIdentifier))
                {
                    _logger.LogInformation("Processing operation for FileIdentifier: '{FileIdentifier}'", op.FileIdentifier);
                    if (fileMap.TryGetValue(op.FileIdentifier, out var formFileFromMap))
                    {
                        _logger.LogInformation("Found matching file in IFormFileCollection for FileIdentifier: '{FileIdentifier}'. OriginalFileName: {OriginalFileName}", op.FileIdentifier, formFileFromMap.FileName);
                        
                        if (formFileFromMap.Length == 0) throw new Application.Exceptions.ValidationException(op.FileIdentifier, "File content cannot be empty.");
                        if (formFileFromMap.Length > 10 * 1024 * 1024) throw new Application.Exceptions.ValidationException(op.FileIdentifier, "File size cannot exceed 10MB.");
                        
                        var memoryStream = new MemoryStream();
                        await formFileFromMap.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        fileToUploadInfo = new FileToUploadInfo
                        {
                            ImageStream = memoryStream,
                            OriginalFileName = formFileFromMap.FileName,
                            ContentType = formFileFromMap.ContentType
                        };
                    }
                    else
                    {
                        _logger.LogWarning("File with identifier '{FileIdentifier}' was specified in pageOperationsJson but not found in the uploaded files (Request.Form.Files) for chapter {ChapterId}. PageId: {PageId}, PageNumber: {PageNumber}",
                            op.FileIdentifier, chapterId, op.PageId?.ToString() ?? "new", op.PageNumber);
                        throw new Application.Exceptions.ValidationException(op.FileIdentifier, $"File with identifier '{op.FileIdentifier}' was specified but not found in the uploaded files.");
                    }
                }
                else
                {
                     _logger.LogInformation("No FileIdentifier specified for operation with PageId: {PageId}, PageNumber: {PageNumber}. This page will not have its image updated/added unless it's an existing page and no image change is intended.",
                       op.PageId?.ToString() ?? "new", op.PageNumber);
                }

                instructions.Add(new PageSyncInstruction
                {
                    PageId = op.PageId ?? Guid.NewGuid(), // Nếu PageId null (trang mới), sẽ tạo Guid mới trong controller/handler
                    DesiredPageNumber = op.PageNumber,
                    ImageFileToUpload = fileToUploadInfo
                });
            }

            var command = new SyncChapterPagesCommand
            {
                ChapterId = chapterId,
                Instructions = instructions
            };

            var result = await Mediator.Send(command);
            return Ok(result); // BaseApiController.Ok sẽ wrap
        }
    }

    [Route("chapterpages")] // Đảm bảo controller này có route riêng
    public class ChapterPagesController : BaseApiController
    {
        private readonly FluentValidation.IValidator<UpdateChapterPageDto> _updateChapterPageDtoValidator;
        private readonly ILogger<ChapterPagesController> _logger;

        public ChapterPagesController(
            FluentValidation.IValidator<UpdateChapterPageDto> updateChapterPageDtoValidator,
            ILogger<ChapterPagesController> logger)
        {
            _updateChapterPageDtoValidator = updateChapterPageDtoValidator;
            _logger = logger;
        }

        [HttpPost("{pageId:guid}/image")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI (Coi upload ảnh là 1 hành động edit)
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)] // Trả về OK thay vì Created
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadChapterPageImage(Guid pageId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new Application.Exceptions.ValidationException("file", "File is required.");
            }
            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                throw new Application.Exceptions.ValidationException("file", "File size cannot exceed 5MB.");
            }
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                 throw new Application.Exceptions.ValidationException("file", "Invalid file type. Allowed types are: " + string.Join(", ", allowedExtensions));
            }

            using var stream = file.OpenReadStream();
            var command = new UploadChapterPageImageCommand
            {
                ChapterPageId = pageId,
                ImageStream = stream,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType
            };
            var publicId = await Mediator.Send(command);

            var responsePayload = new { PublicId = publicId };
            return Ok(responsePayload); // Sử dụng Ok từ BaseApiController
        }

        [HttpPut("{pageId:guid}/details")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateChapterPageDetails(Guid pageId, [FromBody] UpdateChapterPageDto updateDto)
        {
            var validationResult = await _updateChapterPageDtoValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new UpdateChapterPageDetailsCommand
            {
                PageId = pageId,
                PageNumber = updateDto.PageNumber
            };
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{pageId:guid}")]
        [Authorize(Policy = Permissions.Manga.Delete)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteChapterPage(Guid pageId)
        {
            var command = new DeleteChapterPageCommand { PageId = pageId };
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
```
</file>

#### `MangaReaderDB/Controllers/CoverArtsController.cs`

<file path="MangaReaderDB\Controllers\CoverArtsController.cs">
```csharp
using Application.Common.DTOs.CoverArts;
using Application.Common.Models; // For ResourceObject
using Application.Common.Responses;
using Application.Exceptions;
using Application.Features.CoverArts.Commands.DeleteCoverArt;
using Application.Features.CoverArts.Commands.UploadCoverArtImage;
using Application.Features.CoverArts.Queries.GetCoverArtById;
using Application.Features.CoverArts.Queries.GetCoverArtsByManga;
using Domain.Constants; // <<< THÊM MỚI
using FluentValidation;
using Microsoft.AspNetCore.Authorization; // <<< THÊM MỚI
using Microsoft.AspNetCore.Mvc;

namespace MangaReaderDB.Controllers
{
    public class CoverArtsController : BaseApiController
    {
        // Validator for CreateCoverArtDto (used in UploadCoverArtImageCommand)
        private readonly IValidator<CreateCoverArtDto> _createCoverArtDtoValidator;
        private readonly ILogger<CoverArtsController> _logger; // Thêm logger

        public CoverArtsController(
            IValidator<CreateCoverArtDto> createCoverArtDtoValidator,
            ILogger<CoverArtsController> logger) // Inject logger
        {
            _createCoverArtDtoValidator = createCoverArtDtoValidator;
            _logger = logger; // Gán logger
        }

        [HttpPost("/mangas/{mangaId:guid}/covers")] // Custom route to associate with manga
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<CoverArtAttributesDto>>), StatusCodes.Status201Created)] // Sửa ProducesResponseType
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadCoverArtImage(Guid mangaId, IFormFile file, [FromForm] string? volume, [FromForm] string? description) // Sử dụng FromForm cho metadata
        {
            if (file == null || file.Length == 0)
            {
                throw new Application.Exceptions.ValidationException("file", "File is required.");
            }
             if (file.Length > 5 * 1024 * 1024) 
            {
                throw new Application.Exceptions.ValidationException("file", "File size cannot exceed 5MB.");
            }
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                 throw new Application.Exceptions.ValidationException("file", "Invalid file type. Allowed types are: " + string.Join(", ", allowedExtensions));
            }

            var metadataDto = new CreateCoverArtDto { MangaId = mangaId, Volume = volume, Description = description };
            var validationResult = await _createCoverArtDtoValidator.ValidateAsync(metadataDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            using var stream = file.OpenReadStream();
            var command = new UploadCoverArtImageCommand
            {
                MangaId = mangaId,
                Volume = volume,
                Description = description,
                ImageStream = stream,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType
            };

            var coverId = await Mediator.Send(command);
            var coverArtResource = await Mediator.Send(new GetCoverArtByIdQuery { CoverId = coverId });

            if (coverArtResource == null)
            {
                _logger.LogError($"FATAL: CoverArt with ID {coverId} was not found after creation! This indicates a critical issue.");
                throw new InvalidOperationException($"Could not retrieve CoverArt with ID {coverId} after creation. This is an unexpected error.");
            }
            return Created(nameof(GetCoverArtById), new { id = coverId }, coverArtResource);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<CoverArtAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCoverArtById(Guid id)
        {
            var query = new GetCoverArtByIdQuery { CoverId = id };
            var coverArtResource = await Mediator.Send(query);
            if (coverArtResource == null)
            {
                 throw new NotFoundException(nameof(Domain.Entities.CoverArt), id);
            }
            return Ok(coverArtResource);
        }

        [HttpGet("/mangas/{mangaId:guid}/covers")] // Custom route
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCoverArtsByManga(Guid mangaId, [FromQuery] GetCoverArtsByMangaQuery query)
        {
            query.MangaId = mangaId;
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Delete)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCoverArt(Guid id)
        {
            var command = new DeleteCoverArtCommand { CoverId = id };
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
```
</file>

#### `MangaReaderDB/Controllers/MangasController.cs`

<file path="MangaReaderDB\Controllers\MangasController.cs">
```csharp
using Application.Common.DTOs.Mangas;
using Application.Common.Models; // For ResourceObject
using Application.Common.Responses;
using Application.Exceptions;
using Application.Features.Mangas.Commands.CreateManga;
using Application.Features.Mangas.Commands.DeleteManga;
using Application.Features.Mangas.Commands.UpdateManga;
using Application.Features.Mangas.Queries.GetMangaById;
using Application.Features.Mangas.Queries.GetMangas;
using Domain.Constants; // <<< THÊM MỚI
using FluentValidation;
using Microsoft.AspNetCore.Authorization; // <<< THÊM MỚI
using Microsoft.AspNetCore.Mvc;

namespace MangaReaderDB.Controllers
{
    public class MangasController : BaseApiController
    {
        private readonly IValidator<CreateMangaDto> _createMangaDtoValidator;
        private readonly IValidator<UpdateMangaDto> _updateMangaDtoValidator;
        private readonly ILogger<MangasController> _logger; // Thêm logger

        public MangasController(
            IValidator<CreateMangaDto> createMangaDtoValidator,
            IValidator<UpdateMangaDto> updateMangaDtoValidator,
            ILogger<MangasController> logger) // Inject logger
        {
            _createMangaDtoValidator = createMangaDtoValidator;
            _updateMangaDtoValidator = updateMangaDtoValidator;
            _logger = logger; // Gán logger
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateManga([FromBody] CreateMangaDto createDto)
        {
            var validationResult = await _createMangaDtoValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new CreateMangaCommand
            {
                Title = createDto.Title,
                OriginalLanguage = createDto.OriginalLanguage,
                PublicationDemographic = createDto.PublicationDemographic,
                Status = createDto.Status,
                Year = createDto.Year,
                ContentRating = createDto.ContentRating,
                TagIds = createDto.TagIds,
                Authors = createDto.Authors
            };
            var mangaId = await Mediator.Send(command);
            var mangaResource = await Mediator.Send(new GetMangaByIdQuery { MangaId = mangaId });

            if (mangaResource == null)
            {
                 _logger.LogError($"FATAL: Manga with ID {mangaId} was not found after creation!");
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Creation Error", "Failed to retrieve resource after creation.")));
            }
            return Created(nameof(GetMangaById), new { id = mangaId }, mangaResource);
        }

        // ---------- BẮT ĐẦU THAY ĐỔI TẠI ĐÂY ----------
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMangaById([FromRoute] Guid id, [FromQuery] GetMangaByIdQuery query)
        {
            // Gán ID từ route vào query object đã được binding từ query string
            query.MangaId = id;

            var mangaResource = await Mediator.Send(query);
            if (mangaResource == null)
            {
                throw new NotFoundException(nameof(Domain.Entities.Manga), id);
            }
            return Ok(mangaResource);
        }
        // ---------- KẾT THÚC THAY ĐỔI ----------

        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<MangaAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMangas([FromQuery] GetMangasQuery query)
        {
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateManga(Guid id, [FromBody] UpdateMangaDto updateDto)
        {
            var validationResult = await _updateMangaDtoValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new UpdateMangaCommand
            {
                MangaId = id,
                Title = updateDto.Title,
                OriginalLanguage = updateDto.OriginalLanguage,
                PublicationDemographic = updateDto.PublicationDemographic,
                Status = updateDto.Status,
                Year = updateDto.Year,
                ContentRating = updateDto.ContentRating,
                IsLocked = updateDto.IsLocked,
                TagIds = updateDto.TagIds,
                Authors = updateDto.Authors
            };
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Delete)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteManga(Guid id)
        {
            var command = new DeleteMangaCommand { MangaId = id };
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
```
</file>

#### `MangaReaderDB/Controllers/TagGroupsController.cs`

<file path="MangaReaderDB\Controllers\TagGroupsController.cs">
```csharp
using Application.Common.DTOs.TagGroups;
using Application.Common.Models; // For ResourceObject
using Application.Common.Responses;
using Application.Exceptions;
using Application.Features.TagGroups.Commands.CreateTagGroup;
using Application.Features.TagGroups.Commands.DeleteTagGroup;
using Application.Features.TagGroups.Commands.UpdateTagGroup;
using Application.Features.TagGroups.Queries.GetTagGroupById;
using Application.Features.TagGroups.Queries.GetTagGroups;
using Domain.Constants; // <<< THÊM MỚI
using FluentValidation;
using Microsoft.AspNetCore.Authorization; // <<< THÊM MỚI
using Microsoft.AspNetCore.Mvc;

namespace MangaReaderDB.Controllers
{
    public class TagGroupsController : BaseApiController
    {
        private readonly IValidator<CreateTagGroupDto> _createTagGroupDtoValidator;
        private readonly IValidator<UpdateTagGroupDto> _updateTagGroupDtoValidator;
        private readonly ILogger<TagGroupsController> _logger; // Thêm logger

        public TagGroupsController(
            IValidator<CreateTagGroupDto> createTagGroupDtoValidator,
            IValidator<UpdateTagGroupDto> updateTagGroupDtoValidator,
            ILogger<TagGroupsController> logger) // Inject logger
        {
            _createTagGroupDtoValidator = createTagGroupDtoValidator;
            _updateTagGroupDtoValidator = updateTagGroupDtoValidator;
            _logger = logger; // Gán logger
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TagGroupAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTagGroup([FromBody] CreateTagGroupDto createDto)
        {
            var validationResult = await _createTagGroupDtoValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new CreateTagGroupCommand { Name = createDto.Name };
            var tagGroupId = await Mediator.Send(command);
            var tagGroupResource = await Mediator.Send(new GetTagGroupByIdQuery { TagGroupId = tagGroupId });

            if (tagGroupResource == null)
            {
                _logger.LogError($"FATAL: TagGroup with ID {tagGroupId} was not found after creation!");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Creation Error", "Failed to retrieve resource after creation.")));
            }
            return Created(nameof(GetTagGroupById), new { id = tagGroupId }, tagGroupResource);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TagGroupAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTagGroupById(Guid id)
        {
            var query = new GetTagGroupByIdQuery { TagGroupId = id };
            var tagGroupResource = await Mediator.Send(query);
            if (tagGroupResource == null)
            {
                throw new NotFoundException(nameof(Domain.Entities.TagGroup), id);
            }
            return Ok(tagGroupResource);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<TagGroupAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTagGroups([FromQuery] GetTagGroupsQuery query)
        {
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTagGroup(Guid id, [FromBody] UpdateTagGroupDto updateDto)
        {
            var validationResult = await _updateTagGroupDtoValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new UpdateTagGroupCommand { TagGroupId = id, Name = updateDto.Name };
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Delete)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)] 
        public async Task<IActionResult> DeleteTagGroup(Guid id)
        {
            var command = new DeleteTagGroupCommand { TagGroupId = id };
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
```
</file>

#### `MangaReaderDB/Controllers/TagsController.cs`

<file path="MangaReaderDB\Controllers\TagsController.cs">
```csharp
// MangaReaderDB/Controllers/TagsController.cs
using Application.Common.DTOs.Tags;
using Application.Common.Models; // For ResourceObject
using Application.Common.Responses;
using Application.Exceptions;
using Application.Features.Tags.Commands.CreateTag;
using Application.Features.Tags.Commands.DeleteTag;
using Application.Features.Tags.Commands.UpdateTag;
using Application.Features.Tags.Queries.GetTagById;
using Application.Features.Tags.Queries.GetTags;
using Domain.Constants; // <<< THÊM MỚI
using FluentValidation;
using Microsoft.AspNetCore.Authorization; // <<< THÊM MỚI
using Microsoft.AspNetCore.Mvc;

namespace MangaReaderDB.Controllers
{
    public class TagsController : BaseApiController
    {
        private readonly IValidator<CreateTagDto> _createTagDtoValidator;
        private readonly IValidator<UpdateTagDto> _updateTagDtoValidator;
        private readonly ILogger<TagsController> _logger; // Thêm logger

        public TagsController(
            IValidator<CreateTagDto> createTagDtoValidator,
            IValidator<UpdateTagDto> updateTagDtoValidator,
            ILogger<TagsController> logger) // Inject logger
        {
            _createTagDtoValidator = createTagDtoValidator;
            _updateTagDtoValidator = updateTagDtoValidator;
            _logger = logger; // Gán logger
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TagAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)] 
        public async Task<IActionResult> CreateTag([FromBody] CreateTagDto createDto)
        {
            var validationResult = await _createTagDtoValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new CreateTagCommand { Name = createDto.Name, TagGroupId = createDto.TagGroupId };
            var tagId = await Mediator.Send(command);
            var tagResource = await Mediator.Send(new GetTagByIdQuery { TagId = tagId });
            
            if (tagResource == null)
            {
                _logger.LogError($"FATAL: Tag with ID {tagId} was not found after creation!");
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Creation Error", "Failed to retrieve resource after creation.")));
            }
            return Created(nameof(GetTagById), new { id = tagId }, tagResource);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TagAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTagById(Guid id)
        {
            var query = new GetTagByIdQuery { TagId = id };
            var tagResource = await Mediator.Send(query);
            if (tagResource == null)
            {
                throw new NotFoundException(nameof(Domain.Entities.Tag), id);
            }
            return Ok(tagResource);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<TagAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTags([FromQuery] GetTagsQuery query)
        {
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagDto updateDto)
        {
            var validationResult = await _updateTagDtoValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new UpdateTagCommand { TagId = id, Name = updateDto.Name, TagGroupId = updateDto.TagGroupId };
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Delete)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTag(Guid id)
        {
            var command = new DeleteTagCommand { TagId = id };
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
```
</file>

#### `MangaReaderDB/Controllers/TranslatedMangasController.cs`

<file path="MangaReaderDB\Controllers\TranslatedMangasController.cs">
```csharp
using Application.Common.DTOs.TranslatedMangas;
using Application.Common.Models; // For ResourceObject
using Application.Common.Responses;
using Application.Exceptions;
using Application.Features.TranslatedMangas.Commands.CreateTranslatedManga;
using Application.Features.TranslatedMangas.Commands.DeleteTranslatedManga;
using Application.Features.TranslatedMangas.Commands.UpdateTranslatedManga;
using Application.Features.TranslatedMangas.Queries.GetTranslatedMangaById;
using Application.Features.TranslatedMangas.Queries.GetTranslatedMangasByManga;
using Domain.Constants; // <<< THÊM MỚI
using FluentValidation;
using Microsoft.AspNetCore.Authorization; // <<< THÊM MỚI
using Microsoft.AspNetCore.Mvc;

namespace MangaReaderDB.Controllers
{
    public class TranslatedMangasController : BaseApiController
    {
        private readonly IValidator<CreateTranslatedMangaDto> _createDtoValidator;
        private readonly IValidator<UpdateTranslatedMangaDto> _updateDtoValidator;
        private readonly ILogger<TranslatedMangasController> _logger; // Thêm logger

        public TranslatedMangasController(
            IValidator<CreateTranslatedMangaDto> createDtoValidator,
            IValidator<UpdateTranslatedMangaDto> updateDtoValidator,
            ILogger<TranslatedMangasController> logger) // Inject logger
        {
            _createDtoValidator = createDtoValidator;
            _updateDtoValidator = updateDtoValidator;
            _logger = logger; // Gán logger
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Manga.Create)] // <<< THÊM MỚI
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)] 
        public async Task<IActionResult> CreateTranslatedManga([FromBody] CreateTranslatedMangaDto createDto)
        {
            var validationResult = await _createDtoValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new CreateTranslatedMangaCommand
            {
                MangaId = createDto.MangaId,
                LanguageKey = createDto.LanguageKey,
                Title = createDto.Title,
                Description = createDto.Description
            };
            var translatedMangaId = await Mediator.Send(command);
            var translatedMangaResource = await Mediator.Send(new GetTranslatedMangaByIdQuery { TranslatedMangaId = translatedMangaId });
            
            if(translatedMangaResource == null)
            {
                _logger.LogError($"FATAL: TranslatedManga with ID {translatedMangaId} was not found after creation!");
                 return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(new ApiError(500, "Creation Error", "Failed to retrieve resource after creation.")));
            }
            return Created(nameof(GetTranslatedMangaById), new { id = translatedMangaId }, translatedMangaResource);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ResourceObject<TranslatedMangaAttributesDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTranslatedMangaById(Guid id)
        {
            var query = new GetTranslatedMangaByIdQuery { TranslatedMangaId = id };
            var translatedMangaResource = await Mediator.Send(query);
            if (translatedMangaResource == null)
            {
                 throw new NotFoundException(nameof(Domain.Entities.TranslatedManga), id);
            }
            return Ok(translatedMangaResource);
        }

        [HttpGet("/mangas/{mangaId:guid}/translations")] 
        [ProducesResponseType(typeof(ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTranslatedMangasByManga(Guid mangaId, [FromQuery] GetTranslatedMangasByMangaQuery query)
        {
            query.MangaId = mangaId;
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Edit)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTranslatedManga(Guid id, [FromBody] UpdateTranslatedMangaDto updateDto)
        {
            var validationResult = await _updateDtoValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            var command = new UpdateTranslatedMangaCommand
            {
                TranslatedMangaId = id,
                LanguageKey = updateDto.LanguageKey,
                Title = updateDto.Title,
                Description = updateDto.Description
            };
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Manga.Delete)] // <<< THÊM MỚI
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTranslatedManga(Guid id)
        {
            var command = new DeleteTranslatedMangaCommand { TranslatedMangaId = id };
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
```
</file>

### Hoàn tất

Như vậy, tất cả các endpoint chỉnh sửa (tạo, sửa, xóa) liên quan đến Manga và các tài nguyên phụ thuộc của nó đã được bảo vệ. Người dùng cần phải đăng nhập và có các quyền `Permissions.Manga.Create`, `Permissions.Manga.Edit`, hoặc `Permissions.Manga.Delete` tương ứng trong vai trò của mình để có thể thực hiện các thao tác này. Các endpoint `GET` vẫn được công khai như yêu cầu.