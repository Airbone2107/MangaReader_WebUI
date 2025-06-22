using MangaDexLib.Models;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    public interface ITagApiService
    {
        Task<TagListResponse?> FetchTagsAsync();
    }
} 