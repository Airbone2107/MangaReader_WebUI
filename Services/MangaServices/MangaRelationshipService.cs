namespace manga_reader_web.Services.MangaServices
{
    /// <summary>
    /// Service xử lý các mối quan hệ (relationships) của manga
    /// </summary>
    public class MangaRelationshipService
    {
        private readonly ILogger<MangaRelationshipService> _logger;

        public MangaRelationshipService(ILogger<MangaRelationshipService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin tác giả và họa sĩ từ relationships của manga
        /// </summary>
        /// <param name="mangaDict">Dictionary chứa thông tin manga</param>
        /// <returns>Tuple chứa thông tin tác giả và họa sĩ</returns>
        public (string author, string artist) GetAuthorArtist(Dictionary<string, object> mangaDict)
        {
            string author = "Không rõ";
            string artist = "Không rõ";

            try
            {
                var relationships = (List<object>)mangaDict["relationships"];
                
                foreach (var rel in relationships)
                {
                    var relDict = (Dictionary<string, object>)rel;               
                    string relType = relDict["type"].ToString();
                    string relId = relDict["id"].ToString();
                    
                    // Xử lý tác giả và họa sĩ từ relationships
                    if (relType == "author" || relType == "artist")
                    {                       
                            var attrs = (Dictionary<string, object>)relDict["attributes"];                          
                            if (relType == "author")
                                author = attrs["name"].ToString();
                            else if (relType == "artist")
                                artist = attrs["name"].ToString();                    
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý relationships: {ex.Message}");
            }
            
            return (author, artist);
        }
    }
}
