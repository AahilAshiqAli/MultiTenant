using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiTenantAPI.Models
{
    [Table("Contents")]
    public class Content : ChangeLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string FileName { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public string ContentType { get; set; }

        [Column(TypeName = "bigint")]
        public long Size { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string FilePath { get; set; }

        [Column(TypeName = "bit")]
        public bool IsPrivate { get; set; } = false;

        [Column(TypeName = "nvarchar(500)")]
        public string? thumbnail { get; set; }

        [Column(TypeName = "bit")]
        public bool Status { get; set; } = false;


    }
}
