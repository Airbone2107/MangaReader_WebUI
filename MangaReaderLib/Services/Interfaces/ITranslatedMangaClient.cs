using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TranslatedMangas;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với TranslatedManga API endpoints
    /// </summary>
    public interface ITranslatedMangaClient : ITranslatedMangaReader, ITranslatedMangaWriter
    {
    }
} 