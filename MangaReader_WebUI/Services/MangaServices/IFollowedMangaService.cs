using MangaReader.WebUI.Services.MangaServices.Models;

namespace MangaReader.WebUI.Services.MangaServices
{
    public interface IFollowedMangaService
    {
        Task<List<FollowedMangaViewModel>> GetFollowedMangaListAsync();
    }
} 