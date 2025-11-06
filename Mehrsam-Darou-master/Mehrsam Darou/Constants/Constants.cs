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
}