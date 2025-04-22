using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices
{
    public interface ITagApiService
    {
        Task<TagListResponse?> FetchTagsAsync();
    }
} 