namespace manga_reader_web.Services.MangaServices.MangaInformation
{
    public class MangaUtilityService
    {
        private readonly ILogger<MangaUtilityService> _logger;

        public MangaUtilityService(ILogger<MangaUtilityService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tính toán rating cho manga dựa trên ID
        /// </summary>
        /// <param name="mangaId">ID của manga</param>
        /// <returns>Giá trị rating (từ 6.0-10.0)</returns>
        public double GetMangaRating(string mangaId)
        {
            // MangaDex API không cung cấp thông tin rating trực tiếp
            // Trong thực tế, bạn có thể lưu và tính toán điểm đánh giá từ người dùng
            // Hoặc lấy từ nguồn API khác
            
            // Mô phỏng: Tạo rating giả dựa trên ID manga
            try
            {
                // Tạo một số giả từ 0-10 dựa trên mangaId
                var idSum = mangaId.Sum(c => c);
                return Math.Round((idSum % 40 + 60) / 10.0, 2); // Trả về số từ 6.0-10.0
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tính toán rating cho manga {mangaId}: {ex.Message}");
                return 7.5; // Giá trị mặc định nếu có lỗi
            }
        }
    }
}
