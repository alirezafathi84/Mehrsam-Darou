namespace Mehrsam_Darou.Constants
{
    /// <summary>
    /// مقادیر دقیق Status از CHECK CONSTRAINT پایگاه داده
    /// هرگز این مقادیر را تغییر ندهید مگر اینکه constraint هم تغییر کند
    /// </summary>
    public static class MaterialRequestStatus
    {
        public const string Pending = "در انتظار بررسی";
        public const string Reviewing = "در حال بررسی";
        public const string Approved = "تأیید شده";
        public const string Rejected = "رد شده";
        public const string InProcurement = "در حال تأمین";
        public const string Delivered = "تحویل شده";
        public const string Completed = "تکمیل شده";
        public const string Cancelled = "لغو شده";
        public const string NeedSubstitute = "نیاز به جایگزین";
        public const string WaitingCeoApproval = "منتظر تأیید مدیرعامل";
    }


    /// <summary>
    /// مقادیر دقیق WorkflowStage از CHECK CONSTRAINT پایگاه داده
    /// این مقادیر هرگز نباید تغییر کنند مگر اینکه constraint هم تغییر کند
    /// </summary>
    public static class MaterialRequestWorkflowStage
    {
        public const string RequestRegistration = "ثبت درخواست";
        public const string InventoryCheck = "بررسی موجودی";
        public const string FindingSubstitute = "جستجوی جایگزین";
        public const string PurchaseRequest = "درخواست خرید";
        public const string ManagerApproval = "تأیید مدیر";
        public const string CeoApproval = "تأیید مدیرعامل";
        public const string Procurement = "تأمین";
        public const string Delivery = "تحویل";
        public const string Completed = "تکمیل";
    }




}