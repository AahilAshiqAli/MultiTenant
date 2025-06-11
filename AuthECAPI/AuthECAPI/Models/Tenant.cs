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

        // Add more config afterwards if needed
    }
}
