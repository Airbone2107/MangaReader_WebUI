namespace MangaReader.WebUI.Models
{
    public class SortManga
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Status { get; set; } = new List<string>();
        public string Safety { get; set; } = string.Empty;
        public List<string> Demographic { get; set; } = new List<string>();
        public List<string> IncludedTags { get; set; } = new List<string>();
        public List<string> ExcludedTags { get; set; } = new List<string>();
        public List<string> Languages { get; set; } = new List<string>();
        public string SortBy { get; set; } = "latest";
        public string TimeFrame { get; set; } = string.Empty;
        public List<string>? Genres { get; set; } = new List<string>();
        public string IncludedTagsMode { get; set; } = "AND";
        public string ExcludedTagsMode { get; set; } = "OR";
        public List<string> Authors { get; set; } = new List<string>();
        public List<string> Artists { get; set; } = new List<string>();
        public int? Year { get; set; }
        public List<string> ContentRating { get; set; } = new List<string>();
        public List<string> OriginalLanguage { get; set; } = new List<string>();
        public List<string> ExcludedOriginalLanguage { get; set; } = new List<string>();
        public string CreatedAtSince { get; set; } = string.Empty;
        public string UpdatedAtSince { get; set; } = string.Empty;
        public bool? HasAvailableChapters { get; set; }
        public string Group { get; set; } = string.Empty;

        public SortManga()
        {
            // Giá trị mặc định đã được thiết lập ở trên
        }

        public Dictionary<string, object> ToParams()
        {
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(Title))
                parameters["title"] = Title;
                
            if (Authors != null && Authors.Count > 0)
                parameters["authors[]"] = Authors;
                
            if (Artists != null && Artists.Count > 0)
                parameters["artists[]"] = Artists;
                
            if (Year.HasValue && Year > 0)
                parameters["year"] = Year.Value;

            if (Status != null && Status.Count > 0)
                parameters["status[]"] = Status;

            if (IncludedTags != null && IncludedTags.Count > 0)
            {
                parameters["includedTags[]"] = IncludedTags;
                parameters["includedTagsMode"] = IncludedTagsMode;
            }

            if (ExcludedTags != null && ExcludedTags.Count > 0)
            {
                parameters["excludedTags[]"] = ExcludedTags;
                parameters["excludedTagsMode"] = ExcludedTagsMode;
            }

            if (Languages != null && Languages.Count > 0)
            {
                var validLanguages = Languages.Where(lang => 
                    !string.IsNullOrWhiteSpace(lang) && 
                    System.Text.RegularExpressions.Regex.IsMatch(lang.Trim(), @"^[a-z]{2}(-[a-z]{2})?$")
                ).ToList();
                
                if (validLanguages.Count > 0)
                    parameters["availableTranslatedLanguage[]"] = validLanguages;
            }
            
            if (OriginalLanguage != null && OriginalLanguage.Count > 0)
                parameters["originalLanguage[]"] = OriginalLanguage;
                
            if (ExcludedOriginalLanguage != null && ExcludedOriginalLanguage.Count > 0)
                parameters["excludedOriginalLanguage[]"] = ExcludedOriginalLanguage;
                
            if (!string.IsNullOrEmpty(CreatedAtSince))
                parameters["createdAtSince"] = CreatedAtSince;
                
            if (!string.IsNullOrEmpty(UpdatedAtSince))
                parameters["updatedAtSince"] = UpdatedAtSince;
                
            if (HasAvailableChapters.HasValue)
                parameters["hasAvailableChapters"] = HasAvailableChapters.Value ? "true" : "false";
                
            if (!string.IsNullOrEmpty(Group))
                parameters["group"] = Group;

            if (SortBy == "latest") parameters["order[updatedAt]"] = "desc";
            else if (SortBy == "title") parameters["order[title]"] = "asc";
            else if (SortBy == "popular") parameters["order[followedCount]"] = "desc";
            else if (SortBy == "relevance") parameters["order[relevance]"] = "desc";
            else if (SortBy == "rating") parameters["order[rating]"] = "desc";
            else if (SortBy == "createdAt") parameters["order[createdAt]"] = "desc";
            else if (SortBy == "year") parameters["order[year]"] = "desc";
            else parameters["order[latestUploadedChapter]"] = "desc";

            if (ContentRating != null && ContentRating.Count > 0)
            {
                parameters["contentRating[]"] = ContentRating;
            }
            else if (string.IsNullOrEmpty(Safety) || Safety == "Tất cả")
            {
                parameters["contentRating[]"] = new[] { "safe", "suggestive" };
            }
            else
            {
                parameters["contentRating[]"] = new[] { Safety.ToLower() };
            }

            if (Demographic != null && Demographic.Count > 0)
            {
                parameters["publicationDemographic[]"] = Demographic;
            }
            else if (!string.IsNullOrEmpty(Safety) && Safety != "Tất cả")
            {
                parameters["publicationDemographic[]"] = new[] { Safety.ToLower() };
            }

            return parameters;
        }
    }
} 