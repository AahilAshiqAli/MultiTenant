using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
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

    [Column(TypeName = "nvarchar(100)")]
    public string TenantId { get; set; }

    [Column(TypeName = "bit")]
    public bool IsPrivate { get; set; } = false;
}
