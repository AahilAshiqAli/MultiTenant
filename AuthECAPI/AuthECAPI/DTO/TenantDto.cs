namespace AuthECAPI.DTO
{
    public class TenantDto
    {
        public string Name { get; set; }
        public string Provider { get; set; }
        public string Container { get; set; }
        public bool EnableVersioning { get; set; }
        public int RetentionDays { get; set; }
        public string DefaultBlobTier { get; set; }
    }
}
