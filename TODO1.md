Tài liệu này hướng dẫn chi tiết các bước cần thực hiện để cập nhật các dự án frontend (`MangaReaderLib`, `MangaReader_ManagerUI`, `MangaReader_WebUI`) nhằm phù hợp với các thay đổi mới từ API Backend.

Các thay đổi chính bao gồm:
1.  **Cập nhật `MangaReaderLib`:** Đồng bộ DTOs và các phương thức gọi API với đặc tả mới.
2.  **Mở rộng chức năng tìm kiếm:** Cho phép tìm kiếm manga theo Tác giả, Họa sĩ và Ngôn ngữ dịch có sẵn.
3.  **Hiển thị Ngôn ngữ dịch:** Hiển thị danh sách các ngôn ngữ có sẵn cho một manga trên trang chi tiết.

---

## Phần 1: Cập nhật Thư viện `MangaReaderLib`

Đây là bước quan trọng nhất, tạo nền tảng cho các thay đổi ở các lớp giao diện người dùng.

### Bước 1.1: Cập nhật DTOs

Cập nhật `MangaAttributesDto` để bao gồm trường `AvailableTranslatedLanguages`.

<br/>

<details>
<summary>Nội dung file `MangaReaderLib\DTOs\Mangas\MangaAttributesDto.cs`</summary>

```csharp
// MangaReaderLib\DTOs\Mangas\MangaAttributesDto.cs
using MangaReaderLib.Enums;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;

namespace MangaReaderLib.DTOs.Mangas
{
    public class MangaAttributesDto
    {
        public string Title { get; set; } = string.Empty;
        public List<string>? AvailableTranslatedLanguages { get; set; } // THÊM DÒNG NÀY
        public string OriginalLanguage { get; set; } = string.Empty;
        public PublicationDemographic? PublicationDemographic { get; set; }
        public MangaStatus Status { get; set; }
        public int? Year { get; set; }
        public ContentRating ContentRating { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public List<ResourceObject<TagInMangaAttributesDto>>? Tags { get; set; }
    }
}
```

</details>

<br/>

### Bước 1.2: Cập nhật Interface `IMangaReader`

Thay đổi phương thức `GetMangasAsync` để chấp nhận các tham số tìm kiếm mới (`authors`, `artists`, `availableTranslatedLanguage`) và loại bỏ tham số cũ `authorIdsFilter`.

<br/>

<details>
<summary>Nội dung file `MangaReaderLib\Services\Interfaces\IMangaReader.cs`</summary>

```csharp
// MangaReaderLib\Services\Interfaces\IMangaReader.cs
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for Manga endpoints.
    /// </summary>
    public interface IMangaReader : IReadClient
    {
        /// <summary>
        /// Lấy danh sách manga với các tùy chọn lọc và phân trang
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, 
            int? limit = null, 
            string? titleFilter = null, 
            string? statusFilter = null, 
            string? contentRatingFilter = null, 
            List<PublicationDemographic>? publicationDemographicsFilter = null,
            string? originalLanguageFilter = null,
            int? yearFilter = null,
            List<Guid>? authors = null, // THAY ĐỔI: từ authorIdsFilter thành authors
            List<Guid>? artists = null, // THÊM MỚI
            List<string>? availableTranslatedLanguage = null, // THÊM MỚI
            List<Guid>? includedTags = null,
            string? includedTagsMode = null,
            List<Guid>? excludedTags = null,
            string? excludedTagsMode = null,
            string? orderBy = null, 
            bool? ascending = null,
            List<string>? includes = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một manga dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId,
            List<string>? includes = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách bìa của một manga
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>?> GetMangaCoversAsync(
            Guid mangaId, 
            int? offset = null, 
            int? limit = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách các bản dịch của một manga
        /// </summary>
        Task<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetMangaTranslationsAsync(
            Guid mangaId, 
            int? offset = null, 
            int? limit = null,
            string? orderBy = null, 
            bool? ascending = null,
            CancellationToken cancellationToken = default);
    }
} 
```
</details>

<br/>

### Bước 1.3: Cập nhật Implementation `MangaClient`

Cập nhật logic trong `GetMangasAsync` để xây dựng chuỗi query string đúng với các tham số mới.

<br/>

<details>
<summary>Nội dung file `MangaReaderLib\Services\Implementations\MangaClient.cs`</summary>

```csharp
// MangaReaderLib\Services\Implementations\MangaClient.cs
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using MangaReaderLib.Enums;

namespace MangaReaderLib.Services.Implementations
{
    public class MangaClient : IMangaClient
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<MangaClient> _logger;

        public MangaClient(IApiClient apiClient, ILogger<MangaClient> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string BuildQueryString(string baseUri, Dictionary<string, List<string>> queryParams)
        {
            var queryString = new StringBuilder();
            if (queryParams != null && queryParams.Any())
            {
                bool firstParam = true;
                foreach (var param in queryParams)
                {
                    if (param.Value != null && param.Value.Any())
                    {
                        foreach (var value in param.Value)
                        {
                            if (string.IsNullOrEmpty(value)) continue;

                            if (firstParam)
                            {
                                queryString.Append("?");
                                firstParam = false;
                            }
                            else
                            {
                                queryString.Append("&");
                            }
                            queryString.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value)}");
                        }
                    }
                }
            }
            return $"{baseUri}{queryString}";
        }
        
        private void AddQueryParam(Dictionary<string, List<string>> queryParams, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams[key] = new List<string>();
                }
                queryParams[key].Add(value);
            }
        }

        private void AddListQueryParam<TValue>(Dictionary<string, List<string>> queryParams, string key, List<TValue>? values)
        {
            if (values != null && values.Any())
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams[key] = new List<string>();
                }
                queryParams[key].AddRange(values.Select(v => v?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        public async Task<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>?> GetMangasAsync(
            int? offset = null, int? limit = null, string? titleFilter = null,
            string? statusFilter = null, string? contentRatingFilter = null,
            List<PublicationDemographic>? publicationDemographicsFilter = null,
            string? originalLanguageFilter = null, int? yearFilter = null,
            List<Guid>? authors = null, // THAY ĐỔI
            List<Guid>? artists = null, // THÊM MỚI
            List<string>? availableTranslatedLanguage = null, // THÊM MỚI
            List<Guid>? includedTags = null,
            string? includedTagsMode = null,
            List<Guid>? excludedTags = null,
            string? excludedTagsMode = null,
            string? orderBy = null, bool? ascending = null,
            List<string>? includes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting mangas with various filters.");

            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "titleFilter", titleFilter);
            AddQueryParam(queryParams, "statusFilter", statusFilter);
            AddQueryParam(queryParams, "contentRatingFilter", contentRatingFilter);

            if (publicationDemographicsFilter != null && publicationDemographicsFilter.Any())
            {
                 AddListQueryParam(queryParams, "publicationDemographicsFilter[]", publicationDemographicsFilter.Select(e => e.ToString()).ToList());
            }
            
            AddQueryParam(queryParams, "originalLanguageFilter", originalLanguageFilter);
            AddQueryParam(queryParams, "yearFilter", yearFilter?.ToString());

            // THÊM LOGIC MỚI
            if (authors != null && authors.Any())
            {
                AddListQueryParam(queryParams, "authors[]", authors.Select(id => id.ToString()).ToList());
            }
            if (artists != null && artists.Any())
            {
                AddListQueryParam(queryParams, "artists[]", artists.Select(id => id.ToString()).ToList());
            }
            if (availableTranslatedLanguage != null && availableTranslatedLanguage.Any())
            {
                AddListQueryParam(queryParams, "availableTranslatedLanguage[]", availableTranslatedLanguage);
            }
            
            if (includedTags != null && includedTags.Any())
            {
                AddListQueryParam(queryParams, "includedTags[]", includedTags.Select(id => id.ToString()).ToList());
                if (!string.IsNullOrEmpty(includedTagsMode))
                {
                    AddQueryParam(queryParams, "includedTagsMode", includedTagsMode);
                }
            }
            if (excludedTags != null && excludedTags.Any())
            {
                 AddListQueryParam(queryParams, "excludedTags[]", excludedTags.Select(id => id.ToString()).ToList());
                if (!string.IsNullOrEmpty(excludedTagsMode))
                {
                    AddQueryParam(queryParams, "excludedTagsMode", excludedTagsMode);
                }
            }

            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());

            if (includes != null && includes.Any())
            {
                AddListQueryParam(queryParams, "includes[]", includes);
            }

            string requestUri = BuildQueryString("Mangas", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<MangaAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> GetMangaByIdAsync(
            Guid mangaId, 
            List<string>? includes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting manga by ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            if (includes != null && includes.Any())
            {
                AddListQueryParam(queryParams, "includes[]", includes);
            }
            string requestUri = BuildQueryString($"Mangas/{mangaId}", queryParams);
            return await _apiClient.GetAsync<ApiResponse<ResourceObject<MangaAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> CreateMangaAsync(CreateMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new manga: {Title}", request.Title);
            return await _apiClient.PostAsync<CreateMangaRequestDto, ApiResponse<ResourceObject<MangaAttributesDto>>>("Mangas", request, cancellationToken);
        }

        public async Task UpdateMangaAsync(Guid mangaId, UpdateMangaRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating manga with ID: {MangaId}", mangaId);
            await _apiClient.PutAsync($"Mangas/{mangaId}", request, cancellationToken);
        }

        public async Task DeleteMangaAsync(Guid mangaId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting manga with ID: {MangaId}", mangaId);
            await _apiClient.DeleteAsync($"Mangas/{mangaId}", cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>?> GetMangaCoversAsync(Guid mangaId, int? offset = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting covers for manga with ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            string requestUri = BuildQueryString($"mangas/{mangaId}/covers", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<CoverArtAttributesDto>>>(requestUri, cancellationToken);
        }

        public async Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> UploadMangaCoverAsync(Guid mangaId, Stream imageStream, string fileName, string? volume = null, string? description = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Uploading cover for manga with ID: {MangaId}, Filename: {FileName}", mangaId, fileName);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(imageStream), "file", fileName);
            if (!string.IsNullOrEmpty(volume))
            {
                content.Add(new StringContent(volume), "volume");
            }
            if (!string.IsNullOrEmpty(description))
            {
                content.Add(new StringContent(description), "description");
            }
            
            return await _apiClient.PostAsync<ApiResponse<ResourceObject<CoverArtAttributesDto>>>($"mangas/{mangaId}/covers", content, cancellationToken);
        }

        public async Task<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>?> GetMangaTranslationsAsync(Guid mangaId, int? offset = null, int? limit = null, string? orderBy = null, bool? ascending = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting translations for manga with ID: {MangaId}", mangaId);
            var queryParams = new Dictionary<string, List<string>>();
            AddQueryParam(queryParams, "offset", offset?.ToString());
            AddQueryParam(queryParams, "limit", limit?.ToString());
            AddQueryParam(queryParams, "orderBy", orderBy);
            AddQueryParam(queryParams, "ascending", ascending?.ToString().ToLower());
            string requestUri = BuildQueryString($"mangas/{mangaId}/translations", queryParams);
            return await _apiClient.GetAsync<ApiCollectionResponse<ResourceObject<TranslatedMangaAttributesDto>>>(requestUri, cancellationToken);
        }
    }
}
```
</details>

---