using MangaReaderLib.Enums;

namespace MangaReaderLib.DTOs.Mangas
{
    public class MangaAuthorInputDto
    {
        public Guid AuthorId { get; set; }
        public MangaStaffRole Role { get; set; }
    }
} 