namespace MultiTenantAPI.DTO
{
    public class ContentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public string TenantId { get; set; }
        public string thumbnail { get; set; }
        public string UserId { get; set; }
    }

}
