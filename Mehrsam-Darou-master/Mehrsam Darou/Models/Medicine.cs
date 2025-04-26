using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mehrsam_Darou.Models
{
    public partial class Medicine
    {
        public Medicine()
        {
            IsActive = true; // Set default value in constructor
        }

        [Key]
        public Guid MedicineId { get; set; }

        [Required(ErrorMessage = "کد دارو الزامی است")]
        [StringLength(50, ErrorMessage = "کد دارو نمی‌تواند بیش از 50 کاراکتر باشد")]
        [Display(Name = "کد دارو")]
        public string MedicineCode { get; set; } = null!;

        [Required(ErrorMessage = "نام برند الزامی است")]
        [StringLength(100, ErrorMessage = "نام برند نمی‌تواند بیش از 100 کاراکتر باشد")]
        [Display(Name = "نام برند")]
        public string BrandName { get; set; } = null!;

        [Required(ErrorMessage = "دسته‌بندی الزامی است")]
        [Display(Name = "دسته‌بندی")]
        public Guid CategoryId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "غلظت باید عددی مثبت باشد")]
        [Display(Name = "غلظت")]
        public decimal? Strength { get; set; }

        [Display(Name = "واحد غلظت")]
        public Guid? StrengthUnitId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "طول عمر باید عددی مثبت باشد")]
        [Display(Name = "طول عمر (ماه)")]
        public int? ShelfLifeMonths { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } // Changed to non-nullable with default value

        [ForeignKey(nameof(CategoryId))]
        public virtual MedicineCategory Category { get; set; } = null!;

        [ForeignKey(nameof(StrengthUnitId))]
        public virtual Unit? StrengthUnit { get; set; }

        public virtual ICollection<FinishedGoodsBatch> FinishedGoodsBatches { get; set; } = new List<FinishedGoodsBatch>();
        public virtual ICollection<MedicineBom> MedicineBoms { get; set; } = new List<MedicineBom>();
        public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
    }
}