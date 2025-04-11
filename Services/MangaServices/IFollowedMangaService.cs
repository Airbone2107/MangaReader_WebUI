using System.Collections.Generic;
using System.Threading.Tasks;
using manga_reader_web.Services.MangaServices.Models;

namespace manga_reader_web.Services.MangaServices
{
    public interface IFollowedMangaService
    {
        Task<List<FollowedMangaViewModel>> GetFollowedMangaListAsync();
    }
} 