using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mehrsam_Darou.Models
{
    public partial class MedicineCategory
    {
        public MedicineCategory()
        {
            IsActive = true; // Default value
        }

        [Key]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
        [StringLength(100, ErrorMessage = "نام دسته‌بندی نمی‌تواند بیش از 100 کاراکتر باشد")]
        [Display(Name = "نام دسته‌بندی")]
        public string CategoryName { get; set; } = null!;

        [StringLength(500, ErrorMessage = "توضیحات نمی‌تواند بیش از 500 کاراکتر باشد")]
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; }

        public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
    }
}