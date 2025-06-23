namespace MultiTenantAPI.DTO
{
    public class ContentMessage
    {
        public long Size { get; set; }              // Database ID of the content
        public string FilePath { get; set; } = string.Empty; // Local path where uploaded file is saved
        public string FileName { get; set; } = string.Empty; // The original filename from user
        public string ContentType { get; set; } = string.Empty;      // MIME type of the file
        public string UserId { get; set; } = string.Empty;           // For SignalR progress updates

        public string TenantId { get; set; } = string.Empty;
        
        public string uploadsFolder { get; set; } = string.Empty; // Folder where the file is uploaded

        public string dupFilePath { get; set; } = string.Empty; // Path to the duplicate file if it exists

        public string uniqueFileName { get; set; } = string.Empty; // Unique filename to avoid conflicts

        public bool isPrivate { get; set; } = false;

        public string RequiredRendition { get; set; }

    }
}
