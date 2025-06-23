using MangaReader.WebUI.Models.ViewModels.Manga;

namespace MangaReader.WebUI.Services.MangaServices
{
    public interface IFollowedMangaService
    {
        Task<List<FollowedMangaViewModel>> GetFollowedMangaListAsync();
    }
} 