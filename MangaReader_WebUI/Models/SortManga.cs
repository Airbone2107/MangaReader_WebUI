using MangaReaderLib.Enums;

namespace MangaReader.WebUI.Models
{
    public class SortManga
    {
        public string Title { get; set; } = string.Empty;
        public List<string>? Status { get; set; }
        public List<string>? ContentRating { get; set; }
        public List<string>? PublicationDemographic { get; set; }
        public List<string>? OriginalLanguage { get; set; }
        public int? Year { get; set; }

        public List<string>? Authors { get; set; } // Tìm theo ID tác giả
        public List<string>? Artists { get; set; } // Tìm theo ID họa sĩ
        public List<string>? AvailableTranslatedLanguage { get; set; } // Tìm theo ngôn ngữ có sẵn

        public string IncludedTagsStr { get; set; } = string.Empty;
        public string ExcludedTagsStr { get; set; } = string.Empty;
        public string IncludedTagsMode { get; set; } = "AND";
        public string ExcludedTagsMode { get; set; } = "OR";
        
        public string SortBy { get; set; } = "updatedAt";
        public bool Ascending { get; set; } = false;

        // Dưới đây là các phương thức helper để lấy giá trị đã được parse
        public List<Guid>? GetIncludedTags() => 
            IncludedTagsStr?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(tagId => Guid.TryParse(tagId, out var guid) ? guid : (Guid?)null)
                            .Where(guid => guid.HasValue)
                            .Select(guid => guid!.Value)
                            .ToList();

        public List<Guid>? GetExcludedTags() =>
            ExcludedTagsStr?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                           .Select(tagId => Guid.TryParse(tagId, out var guid) ? guid : (Guid?)null)
                           .Where(guid => guid.HasValue)
                           .Select(guid => guid!.Value)
                           .ToList();

        public List<Guid>? GetAuthorGuids() =>
            Authors?.Where(id => Guid.TryParse(id, out _)).Select(Guid.Parse).ToList();

        public List<Guid>? GetArtistGuids() =>
            Artists?.Where(id => Guid.TryParse(id, out _)).Select(Guid.Parse).ToList();
        
        public List<PublicationDemographic>? GetPublicationDemographics() =>
            PublicationDemographic?.Select(d => Enum.TryParse<PublicationDemographic>(d, true, out var demo) ? (PublicationDemographic?)demo : null)
                                .Where(d => d.HasValue)
                                .Select(d => d.Value)
                                .ToList();
        
        public string? GetFirstStatus() => Status?.FirstOrDefault();
        
        public string? GetFirstContentRating() => ContentRating?.FirstOrDefault();
        
        public string? GetFirstOriginalLanguage() => OriginalLanguage?.FirstOrDefault();

        public SortManga()
        {
            // Giá trị mặc định đã được thiết lập ở trên
        }
    }
} 