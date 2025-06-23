using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với ChapterPage API endpoints
    /// </summary>
    public interface IChapterPageClient : IChapterPageReader, IChapterPageWriter
    {
    }
} 