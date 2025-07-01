namespace MultiTenantAPI.DTO
{
    public class ContentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public Guid TenantId { get; set; }
        public string thumbnail { get; set; }
        public Guid UserId { get; set; }
        public bool Status { get; set; } = false;
    }

}
