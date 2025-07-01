namespace MultiTenantAPI.DTO
{
    public class ContentProcessingResult
    {
        public string ProcessedFilePath { get; set; } = string.Empty;
        public string FinalFileName { get; set; } = string.Empty;
        public string? ThumbnailPath { get; set; }
    }
}
