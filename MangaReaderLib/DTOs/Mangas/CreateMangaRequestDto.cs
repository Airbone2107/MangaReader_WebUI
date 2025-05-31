using MangaReaderLib.Enums;

namespace MangaReaderLib.DTOs.Mangas
{
    public class CreateMangaRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string OriginalLanguage { get; set; } = string.Empty;
        public PublicationDemographic? PublicationDemographic { get; set; }
        public MangaStatus Status { get; set; }
        public int? Year { get; set; }
        public ContentRating ContentRating { get; set; }
        public List<Guid>? TagIds { get; set; }
        public List<MangaAuthorInputDto>? Authors { get; set; }
    }
} 