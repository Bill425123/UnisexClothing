using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnisexClothes.Models
{
    [Table("Fee")]
    public class Fee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FeedId { get; set; }

        [StringLength(200)]
        public string? Name { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Value { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Threshold { get; set; }
    }
}
