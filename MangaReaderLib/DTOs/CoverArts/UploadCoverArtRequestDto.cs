namespace MangaReaderLib.DTOs.CoverArts
{
    // DTO này chỉ chứa metadata, file sẽ được gửi riêng trong multipart/form-data
    public class UploadCoverArtRequestDto
    {
        public string? Volume { get; set; }
        public string? Description { get; set; }
    }
} 