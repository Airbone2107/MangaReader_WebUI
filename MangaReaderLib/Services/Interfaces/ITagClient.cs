using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Tags;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Tag API endpoints
    /// </summary>
    public interface ITagClient : ITagReader, ITagWriter
    {
    }
} 