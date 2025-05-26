using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Medicine
{
    public Guid MedicineId { get; set; }

    public string MedicineCode { get; set; } = null!;

    public string BrandName { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public decimal? Strength { get; set; }

    public Guid? StrengthUnitId { get; set; }

    public int? ShelfLifeMonths { get; set; }

    public bool? IsActive { get; set; }

    public virtual MedicineCategory Category { get; set; } = null!;

    public virtual ICollection<FinishedGoodsBatch> FinishedGoodsBatches { get; set; } = new List<FinishedGoodsBatch>();

    public virtual ICollection<MedicineBom> MedicineBoms { get; set; } = new List<MedicineBom>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } = new List<SalesInvoiceItem>();

    public virtual ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();

    public virtual ICollection<ShipmentItem> ShipmentItems { get; set; } = new List<ShipmentItem>();

    public virtual Unit? StrengthUnit { get; set; }
}
