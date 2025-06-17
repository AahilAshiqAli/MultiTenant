namespace AuthECAPI.DTO
{
    public class TenantDto
    {
        // Tenant-specific properties
        public string Name { get; set; }
        public string Provider { get; set; }
        public string Container { get; set; }
        public bool EnableVersioning { get; set; }
        public int RetentionDays { get; set; }
        public string DefaultBlobTier { get; set; }

        // Admin User (first user) registration properties
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } = "Admin";
        public string Gender { get; set; }
        public int Age { get; set; }
        public int? LibraryID { get; set; }
    }
}