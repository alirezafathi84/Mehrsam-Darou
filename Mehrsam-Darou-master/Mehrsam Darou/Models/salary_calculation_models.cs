using System;
using System.Collections.Generic;
using Mehrsam_Darou.Models;

namespace Mehrsam_Darou.Models
{
    public class MonthlySalaryCalculation
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string TeamName { get; set; }
        public string PersianMonth { get; set; }
        public int WorkedDays { get; set; }
        public int MissionDays { get; set; }
        public int PaidVacationDays { get; set; }
        public int UnpaidVacationDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal TotalWorkedHours { get; set; }
        public decimal MonthlyBaseSalary { get; set; }
        public decimal WorkedDaysPay { get; set; }
        public decimal PaidVacationPay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal AbsenceDeductions { get; set; }
        public decimal UnpaidVacationDeductions { get; set; }
        public decimal NetSalary { get; set; }
    }

    public class DetailedSalaryCalculation
    {
        public User User { get; set; }
        public SalaryInfo SalaryInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public MonthlySalaryCalculation Summary { get; set; }
        public List<DailyAttendanceDetail> DailyDetails { get; set; }
    }

    public class DailyAttendanceDetail
    {
        public DateTime Date { get; set; }
        public string PersianDate { get; set; }
        public string Status { get; set; }
        public string FirstEntryTime { get; set; }
        public string LastExitTime { get; set; }
        public decimal TotalHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public string VacationType { get; set; }
        public bool? IsVacationPaid { get; set; }
        public decimal DailySalaryImpact { get; set; }
        public decimal OvertimePay { get; set; }
    }

    public class DailyAttendanceWithSalaryImpact
    {
        public DateTime Date { get; set; }
        public string PersianDate { get; set; }
        public string FirstEntryTime { get; set; }
        public string LastExitTime { get; set; }
        public decimal TotalHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public string Status { get; set; }
        public string VacationType { get; set; }
        public bool? IsVacationPaid { get; set; }
        public decimal DailySalaryImpact { get; set; }
        public decimal OvertimePay { get; set; }
    }
}