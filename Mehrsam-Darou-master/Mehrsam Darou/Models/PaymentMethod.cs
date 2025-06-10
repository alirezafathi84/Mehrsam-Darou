using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class PaymentMethod
{
    public Guid PaymentMethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public string? MethodType { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
