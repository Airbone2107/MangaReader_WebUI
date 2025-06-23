using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.TagGroups;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với TagGroup API endpoints
    /// </summary>
    public interface ITagGroupClient : ITagGroupReader, ITagGroupWriter
    {
    }
} 