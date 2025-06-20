using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiTenantAPI.Models
{

    public class ChangeLog
    {
        [Required]
        [ForeignKey("Tenant")]
        public string TenantID { get; set; }

        // Navigation property to the Tenant entity
        public Tenant Tenant { get; set; }

        [Required]
        [ForeignKey("AppUser")]
        public string UserId { get; set; }

        public AppUser AppUser { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime UpdatedAt { get; set; }
    }
}
