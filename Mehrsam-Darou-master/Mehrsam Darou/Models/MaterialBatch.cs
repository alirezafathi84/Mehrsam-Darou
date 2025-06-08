//using System;
//using System.Collections.Generic;

//namespace Mehrsam_Darou.Models;

//public partial class MaterialBatch
//{
//    public Guid BatchId { get; set; }

//    public Guid MaterialId { get; set; }

//    public string BatchNumber { get; set; } = null!;

//    public decimal InitialQuantity { get; set; }

//    public decimal CurrentQuantity { get; set; }

//    public Guid UnitId { get; set; }

//    public Guid? LocationId { get; set; }

//    public string? Status { get; set; }

//    public DateOnly? ExpiryDate { get; set; }

//    public virtual StorageLocation? Location { get; set; }

//    public virtual RawMaterial Material { get; set; } = null!;

//    public virtual ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new List<PurchaseInvoiceItem>();

//    public virtual Unit Unit { get; set; } = null!;
//}


using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mehrsam_Darou.Models
{
    [Table("material_batches")]
    public partial class MaterialBatch
    {
        [Key]
        [Column("batch_id")]
        public Guid BatchId { get; set; }

        [Required]
        [Column("material_id")]
        [Display(Name = "ماده اولیه")]
        public Guid MaterialId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("batch_number")]
        [Display(Name = "شماره بچ")]
        public string BatchNumber { get; set; } = null!;

        [Required]
        [Column("initial_quantity", TypeName = "decimal(10, 3)")]
        [Display(Name = "مقدار اولیه")]
        [Range(0.001, double.MaxValue, ErrorMessage = "مقدار اولیه باید بیشتر از صفر باشد")]
        public decimal InitialQuantity { get; set; }

        [Required]
        [Column("current_quantity", TypeName = "decimal(10, 3)")]
        [Display(Name = "مقدار فعلی")]
        [Range(0, double.MaxValue, ErrorMessage = "مقدار فعلی نمی‌تواند منفی باشد")]
        public decimal CurrentQuantity { get; set; }

        [Required]
        [Column("unit_id")]
        [Display(Name = "واحد")]
        public Guid UnitId { get; set; }

        [Column("location_id")]
        [Display(Name = "محل نگهداری")]
        public Guid? LocationId { get; set; }

        [StringLength(20)]
        [Column("status")]
        [Display(Name = "وضعیت")]
        public string? Status { get; set; }

        [Column("expiry_date", TypeName = "date")]
        [Display(Name = "تاریخ انقضا")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        // Navigation properties
        [ForeignKey("MaterialId")]
        [InverseProperty("MaterialBatches")]
        public virtual RawMaterial Material { get; set; } = null!;

        [ForeignKey("UnitId")]
        [InverseProperty("MaterialBatches")]
        public virtual Unit Unit { get; set; } = null!;

        [ForeignKey("LocationId")]
        [InverseProperty("MaterialBatches")]
        public virtual StorageLocation? Location { get; set; }

        // Inverse navigation properties
        [InverseProperty("Batch")]
        public virtual ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new List<PurchaseInvoiceItem>();
    }
}