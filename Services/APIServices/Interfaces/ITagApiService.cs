using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    public interface ITagApiService
    {
        Task<TagListResponse?> FetchTagsAsync();
    }
} 