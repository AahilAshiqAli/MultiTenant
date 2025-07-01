namespace MultiTenantAPI.DTO
{
    public class ContentMessage
    {
        public long Size { get; set; }              // Database ID of the content
        public string FileName { get; set; } = string.Empty; // The original filename from user
        public string ContentType { get; set; } = string.Empty;      // MIME type of the file
        public string UserId { get; set; }            // For SignalR progress updates
        public Guid TenantId { get; set; }
        public string uniqueFileName { get; set; } = string.Empty; // Unique filename to avoid conflicts
        public bool isPrivate { get; set; } = false;
        public string RequiredRendition { get; set; }

    }
}
