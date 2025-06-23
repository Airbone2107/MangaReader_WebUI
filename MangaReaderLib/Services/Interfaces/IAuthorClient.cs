using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Author API endpoints
    /// </summary>
    public interface IAuthorClient : IAuthorReader, IAuthorWriter
    {
    }
} 