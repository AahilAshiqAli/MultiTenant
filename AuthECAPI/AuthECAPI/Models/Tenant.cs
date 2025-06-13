using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthECAPI.Models
{
    public class Tenant
    {
        [Key]
        public Guid TenantID { get; set; } = Guid.NewGuid();

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Name { get; set; }


        [Required]
        [Column(TypeName = "varchar(50)")]
        public string Provider { get; set; } = "azure";

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string Container { get; set; }

        public bool EnableVersioning { get; set; }
        public int RetentionDays { get; set; } = 30;
        public string DefaultBlobTier { get; set; } = "Hot"; // Hot/Cool/Archive

    }
}
