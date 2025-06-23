using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.TranslatedMangas;
using MangaReaderLib.Enums;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Manga API endpoints
    /// </summary>
    public interface IMangaClient : IMangaReader, IMangaWriter
    {
    }
} 