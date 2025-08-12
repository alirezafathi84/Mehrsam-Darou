using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public int Version { get; set; }

    public Guid? TeamId { get; set; }

    public string? AvatarImg { get; set; }

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual ICollection<BatchTest> BatchTestCreatedByNavigations { get; set; } = new List<BatchTest>();

    public virtual ICollection<BatchTest> BatchTestLastModifiedByNavigations { get; set; } = new List<BatchTest>();

    public virtual ICollection<Certification> CertificationCreatedByNavigations { get; set; } = new List<Certification>();

    public virtual ICollection<Certification> CertificationLastModifiedByNavigations { get; set; } = new List<Certification>();

    public virtual ICollection<ChatMessage> ChatMessageReceivers { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatMessage> ChatMessageSenders { get; set; } = new List<ChatMessage>();

    public virtual ICollection<DailyAttendance> DailyAttendances { get; set; } = new List<DailyAttendance>();

    public virtual ICollection<DevelopmentProject> DevelopmentProjectCreatedByNavigations { get; set; } = new List<DevelopmentProject>();

    public virtual ICollection<DevelopmentProject> DevelopmentProjectLastModifiedByNavigations { get; set; } = new List<DevelopmentProject>();

    public virtual ICollection<FinancialReport> FinancialReportApprovedByNavigations { get; set; } = new List<FinancialReport>();

    public virtual ICollection<FinancialReport> FinancialReportCreatedByNavigations { get; set; } = new List<FinancialReport>();

    public virtual ICollection<Formula> FormulaCreatedByNavigations { get; set; } = new List<Formula>();

    public virtual ICollection<Formula> FormulaLastModifiedByNavigations { get; set; } = new List<Formula>();

    public virtual ICollection<JournalEntry> JournalEntryApprovedByNavigations { get; set; } = new List<JournalEntry>();

    public virtual ICollection<JournalEntry> JournalEntryCreatedByNavigations { get; set; } = new List<JournalEntry>();

    public virtual ICollection<MaterialRequest> MaterialRequestApprovedByNavigations { get; set; } = new List<MaterialRequest>();

    public virtual ICollection<MaterialRequest> MaterialRequestCreatedByNavigations { get; set; } = new List<MaterialRequest>();

    public virtual ICollection<MaterialRequest> MaterialRequestProcessedByNavigations { get; set; } = new List<MaterialRequest>();

    public virtual ICollection<MaterialRequest> MaterialRequestRequestedByNavigations { get; set; } = new List<MaterialRequest>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PaymentTransaction> PaymentTransactionApprovedByNavigations { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<PaymentTransaction> PaymentTransactionCreatedByNavigations { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<QaAudit> QaAuditCreatedByNavigations { get; set; } = new List<QaAudit>();

    public virtual ICollection<QaAudit> QaAuditLastModifiedByNavigations { get; set; } = new List<QaAudit>();

    public virtual ICollection<QaStandard> QaStandardCreatedByNavigations { get; set; } = new List<QaStandard>();

    public virtual ICollection<QaStandard> QaStandardLastModifiedByNavigations { get; set; } = new List<QaStandard>();

    public virtual ICollection<QcReport> QcReportCreatedByNavigations { get; set; } = new List<QcReport>();

    public virtual ICollection<QcReport> QcReportLastModifiedByNavigations { get; set; } = new List<QcReport>();

    public virtual ICollection<QcTest> QcTestCreatedByNavigations { get; set; } = new List<QcTest>();

    public virtual ICollection<QcTest> QcTestLastModifiedByNavigations { get; set; } = new List<QcTest>();

    public virtual ICollection<RequestApproval> RequestApprovals { get; set; } = new List<RequestApproval>();

    public virtual ICollection<RequestWorkflowHistory> RequestWorkflowHistoryAssignedToNavigations { get; set; } = new List<RequestWorkflowHistory>();

    public virtual ICollection<RequestWorkflowHistory> RequestWorkflowHistoryProcessedByNavigations { get; set; } = new List<RequestWorkflowHistory>();

    public virtual ICollection<ResearchProject> ResearchProjectCreatedByNavigations { get; set; } = new List<ResearchProject>();

    public virtual ICollection<ResearchProject> ResearchProjectLastModifiedByNavigations { get; set; } = new List<ResearchProject>();

    public virtual ICollection<SalaryInfo> SalaryInfos { get; set; } = new List<SalaryInfo>();

    public virtual Team? Team { get; set; }

    public virtual ICollection<UserEnterLog> UserEnterLogs { get; set; } = new List<UserEnterLog>();

    public virtual UserStatus? UserStatus { get; set; }

    public virtual ICollection<Vacation> VacationApprovedByNavigations { get; set; } = new List<Vacation>();

    public virtual ICollection<Vacation> VacationUsers { get; set; } = new List<Vacation>();
}
