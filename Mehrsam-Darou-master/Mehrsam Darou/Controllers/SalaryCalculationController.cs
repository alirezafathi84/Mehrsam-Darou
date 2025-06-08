using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using System.Globalization;

namespace Mehrsam_Darou.Controllers
{
    public class SalaryCalculationController : BaseController
    {
        private readonly DarouAppContext _context;

        public SalaryCalculationController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: SalaryCalculation/Index
        public async Task<IActionResult> Index()
        {
            var currentDate = DateTime.Now;
            var persianCalendar = new PersianCalendar();
            var currentPersianYear = persianCalendar.GetYear(currentDate);
            var currentPersianMonth = persianCalendar.GetMonth(currentDate);

            ViewBag.CurrentPersianMonth = $"{currentPersianYear}/{currentPersianMonth:00}";
            ViewBag.Users = await GetUsersForDropdown();
            ViewBag.Teams = await GetTeamsForDropdown();

            return View();
        }

        // POST: SalaryCalculation/MonthlyCalculation
        [HttpPost]
        public async Task<IActionResult> MonthlyCalculation(string persianYearMonth, Guid? teamId)
        {
            try
            {
                // Get date range for Persian month
                var dateRange = await GetDateRangeForPersianMonth(persianYearMonth);
                if (!dateRange.HasValue)
                {
                    TempData["ErrorMessage"] = "ماه فارسی وارد شده معتبر نیست";
                    return RedirectToAction(nameof(Index));
                }

                var query = from u in _context.Users
                            join t in _context.Teams on u.TeamId equals t.Id
                            join si in _context.SalaryInfos on u.Id equals si.UserId
                            where si.EffectiveDate <= DateOnly.FromDateTime(dateRange.Value.EndDate) &&
                                  (si.EndDate == null || si.EndDate >= DateOnly.FromDateTime(dateRange.Value.StartDate))
                            select new
                            {
                                User = u,
                                Team = t,
                                SalaryInfo = si
                            };

                if (teamId.HasValue)
                {
                    query = query.Where(x => x.Team.Id == teamId.Value);
                }

                var employees = await query.ToListAsync();

                var salaryCalculations = new List<MonthlySalaryCalculation>();

                foreach (var emp in employees)
                {
                    var attendanceData = await _context.DailyAttendances
                        .Include(da => da.User)
                        .Where(da => da.UserId == emp.User.Id &&
                                    da.Date >= DateOnly.FromDateTime(dateRange.Value.StartDate) &&
                                    da.Date <= DateOnly.FromDateTime(dateRange.Value.EndDate))
                        .ToListAsync();

                    var vacations = await _context.Vacations
                        .Include(v => v.Type)
                        .Where(v => v.UserId == emp.User.Id &&
                                   v.Status == "Approved" &&
                                   v.StartDate <= DateOnly.FromDateTime(dateRange.Value.EndDate) &&
                                   v.EndDate >= DateOnly.FromDateTime(dateRange.Value.StartDate))
                        .ToListAsync();

                    var calculation = CalculateMonthlySalary(emp.User, emp.Team, emp.SalaryInfo,
                        attendanceData, vacations, persianYearMonth, dateRange.Value);

                    salaryCalculations.Add(calculation);
                }

                ViewBag.PersianYearMonth = persianYearMonth;
                ViewBag.DateRange = $"{dateRange.Value.StartDate:yyyy/MM/dd} - {dateRange.Value.EndDate:yyyy/MM/dd}";
                ViewBag.TeamName = teamId.HasValue ?
                    (await _context.Teams.FindAsync(teamId.Value))?.Name : "همه تیم‌ها";

                return View("MonthlyCalculationResult", salaryCalculations);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در محاسبه حقوق: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalaryCalculation/DetailedCalculation
        [HttpPost]
        public async Task<IActionResult> DetailedCalculation(Guid userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "کاربر یافت نشد";
                    return RedirectToAction(nameof(Index));
                }

                var salaryInfo = await _context.SalaryInfos
                    .Where(si => si.UserId == userId &&
                                si.EffectiveDate <= DateOnly.FromDateTime(endDate) &&
                                (si.EndDate == null || si.EndDate >= DateOnly.FromDateTime(startDate)))
                    .OrderByDescending(si => si.EffectiveDate)
                    .FirstOrDefaultAsync();

                if (salaryInfo == null)
                {
                    TempData["ErrorMessage"] = "اطلاعات حقوق و دستمزد برای این کاربر یافت نشد";
                    return RedirectToAction(nameof(Index));
                }

                var attendanceData = await _context.DailyAttendances
                    .Where(da => da.UserId == userId &&
                                da.Date >= DateOnly.FromDateTime(startDate) &&
                                da.Date <= DateOnly.FromDateTime(endDate))
                    .OrderBy(da => da.Date)
                    .ToListAsync();

                var vacations = await _context.Vacations
                    .Include(v => v.Type)
                    .Where(v => v.UserId == userId &&
                               v.Status == "Approved" &&
                               v.StartDate <= DateOnly.FromDateTime(endDate) &&
                               v.EndDate >= DateOnly.FromDateTime(startDate))
                    .ToListAsync();

                var detailedCalculation = CalculateDetailedSalary(user, salaryInfo,
                    attendanceData, vacations, startDate, endDate);

                return View("DetailedCalculationResult", detailedCalculation);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در محاسبه جزئیات حقوق: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SalaryCalculation/DailyAttendanceReport
        public async Task<IActionResult> DailyAttendanceReport(Guid userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Team)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound();
                }

                var salaryInfo = await _context.SalaryInfos
                    .Where(si => si.UserId == userId &&
                                si.EffectiveDate <= DateOnly.FromDateTime(endDate) &&
                                (si.EndDate == null || si.EndDate >= DateOnly.FromDateTime(startDate)))
                    .OrderByDescending(si => si.EffectiveDate)
                    .FirstOrDefaultAsync();

                var dailyReports = await GetDailyAttendanceWithSalaryImpact(userId, startDate, endDate, salaryInfo);

                ViewBag.User = user;
                ViewBag.SalaryInfo = salaryInfo;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;

                return View(dailyReports);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در تهیه گزارش: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<(DateTime StartDate, DateTime EndDate)?> GetDateRangeForPersianMonth(string persianYearMonth)
        {
            var persianDateConverters = await _context.PersianDateConverters
                .Where(pdc => pdc.PersianDate.StartsWith(persianYearMonth + "/"))
                .ToListAsync();

            if (!persianDateConverters.Any())
                return null;

            var minDate = persianDateConverters.Min(pdc => pdc.GregorianDate);
            var maxDate = persianDateConverters.Max(pdc => pdc.GregorianDate);

            return (minDate.ToDateTime(TimeOnly.MinValue),
                    maxDate.ToDateTime(TimeOnly.MinValue));
        }

        private MonthlySalaryCalculation CalculateMonthlySalary(User user, Team team, SalaryInfo salaryInfo,
            List<DailyAttendance> attendanceData, List<Vacation> vacations, string persianYearMonth,
            (DateTime StartDate, DateTime EndDate) dateRange)
        {
            var workedDays = attendanceData.Count(da => da.Status == "Present");
            var missionDays = attendanceData.Count(da => da.Status == "Mission");
            var absentDays = attendanceData.Count(da => da.Status == "Absent");
            var totalWorkedHours = attendanceData.Sum(da => da.TotalHours ?? 0);

            var paidVacationDays = 0;
            var unpaidVacationDays = 0;

            foreach (var vacation in vacations)
            {
                var vacationDaysInPeriod = GetVacationDaysInPeriod(vacation, dateRange);
                if (vacation.Type.IsPaid)
                    paidVacationDays += vacationDaysInPeriod;
                else
                    unpaidVacationDays += vacationDaysInPeriod;
            }

            var dailyBaseSalary = salaryInfo.BaseSalary / 30;
            var workedDaysPay = dailyBaseSalary * (workedDays + missionDays);
            var paidVacationPay = dailyBaseSalary * paidVacationDays;
            var overtimeHours = attendanceData.Sum(da => Math.Max(0, (da.TotalHours ?? 0) - 8));
            var overtimePay = salaryInfo.OvertimeRate * overtimeHours;
            var absenceDeductions = dailyBaseSalary * absentDays;
            var unpaidVacationDeductions = dailyBaseSalary * unpaidVacationDays;

            var netSalary = workedDaysPay + paidVacationPay + overtimePay - absenceDeductions - unpaidVacationDeductions;

            return new MonthlySalaryCalculation
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                TeamName = team.Name,
                PersianMonth = persianYearMonth,
                WorkedDays = workedDays,
                MissionDays = missionDays,
                PaidVacationDays = paidVacationDays,
                UnpaidVacationDays = unpaidVacationDays,
                AbsentDays = absentDays,
                TotalWorkedHours = totalWorkedHours,
                MonthlyBaseSalary = salaryInfo.BaseSalary,
                WorkedDaysPay = workedDaysPay,
                PaidVacationPay = paidVacationPay,
                OvertimePay = overtimePay,
                AbsenceDeductions = absenceDeductions,
                UnpaidVacationDeductions = unpaidVacationDeductions,
                NetSalary = netSalary
            };
        }

        private DetailedSalaryCalculation CalculateDetailedSalary(User user, SalaryInfo salaryInfo,
            List<DailyAttendance> attendanceData, List<Vacation> vacations, DateTime startDate, DateTime endDate)
        {
            var dailyDetails = new List<DailyAttendanceDetail>();

            foreach (var attendance in attendanceData.OrderBy(a => a.Date))
            {
                var vacation = vacations.FirstOrDefault(v =>
                    attendance.Date >= v.StartDate && attendance.Date <= v.EndDate);

                var overtimeHours = Math.Max(0, (attendance.TotalHours ?? 0) - 8);
                var dailySalaryImpact = CalculateDailySalaryImpact(attendance.Status,
                    vacation?.Type?.IsPaid ?? false, salaryInfo.BaseSalary);
                var dailyOvertimePay = salaryInfo.OvertimeRate * overtimeHours;

                dailyDetails.Add(new DailyAttendanceDetail
                {
                    Date = attendance.Date.ToDateTime(TimeOnly.MinValue),
                    PersianDate = attendance.PersianDate,
                    Status = attendance.Status,
                    FirstEntryTime = attendance.FirstEntryTime?.ToString(),
                    LastExitTime = attendance.LastExitTime?.ToString(),
                    TotalHours = attendance.TotalHours ?? 0,
                    OvertimeHours = overtimeHours,
                    VacationType = vacation?.Type?.Name,
                    IsVacationPaid = vacation?.Type?.IsPaid,
                    DailySalaryImpact = dailySalaryImpact,
                    OvertimePay = dailyOvertimePay
                });
            }

            var summary = CalculateMonthlySalary(user, user.Team, salaryInfo, attendanceData, vacations,
                $"{startDate:yyyy/MM}", (startDate, endDate));

            return new DetailedSalaryCalculation
            {
                User = user,
                SalaryInfo = salaryInfo,
                StartDate = startDate,
                EndDate = endDate,
                Summary = summary,
                DailyDetails = dailyDetails
            };
        }

        private async Task<List<DailyAttendanceWithSalaryImpact>> GetDailyAttendanceWithSalaryImpact(
            Guid userId, DateTime startDate, DateTime endDate, SalaryInfo salaryInfo)
        {
            var attendanceData = await _context.DailyAttendances
                .Where(da => da.UserId == userId &&
                            da.Date >= DateOnly.FromDateTime(startDate) &&
                            da.Date <= DateOnly.FromDateTime(endDate))
                .OrderBy(da => da.Date)
                .ToListAsync();

            var vacations = await _context.Vacations
                .Include(v => v.Type)
                .Where(v => v.UserId == userId &&
                           v.Status == "Approved" &&
                           v.StartDate <= DateOnly.FromDateTime(endDate) &&
                           v.EndDate >= DateOnly.FromDateTime(startDate))
                .ToListAsync();

            var result = new List<DailyAttendanceWithSalaryImpact>();

            foreach (var attendance in attendanceData)
            {
                var vacation = vacations.FirstOrDefault(v =>
                    attendance.Date >= v.StartDate && attendance.Date <= v.EndDate);

                var overtimeHours = Math.Max(0, (attendance.TotalHours ?? 0) - 8);
                var dailySalaryImpact = CalculateDailySalaryImpact(attendance.Status,
                    vacation?.Type?.IsPaid ?? false, salaryInfo?.BaseSalary ?? 0);
                var dailyOvertimePay = (salaryInfo?.OvertimeRate ?? 0) * overtimeHours;

                result.Add(new DailyAttendanceWithSalaryImpact
                {
                    Date = attendance.Date.ToDateTime(TimeOnly.MinValue),
                    PersianDate = attendance.PersianDate,
                    FirstEntryTime = attendance.FirstEntryTime?.ToString(),
                    LastExitTime = attendance.LastExitTime?.ToString(),
                    TotalHours = attendance.TotalHours ?? 0,
                    OvertimeHours = overtimeHours,
                    Status = attendance.Status,
                    VacationType = vacation?.Type?.Name,
                    IsVacationPaid = vacation?.Type?.IsPaid,
                    DailySalaryImpact = dailySalaryImpact,
                    OvertimePay = dailyOvertimePay
                });
            }

            return result;
        }

        private decimal CalculateDailySalaryImpact(string status, bool isVacationPaid, decimal baseSalary)
        {
            var dailyBaseSalary = baseSalary / 30;

            return status switch
            {
                "Present" => dailyBaseSalary,
                "Mission" => dailyBaseSalary,
                "Vacation" when isVacationPaid => dailyBaseSalary,
                "Vacation" when !isVacationPaid => -dailyBaseSalary,
                "Absent" => -dailyBaseSalary,
                _ => 0
            };
        }

        private int GetVacationDaysInPeriod(Vacation vacation, (DateTime StartDate, DateTime EndDate) period)
        {
            var vacationStart = vacation.StartDate.ToDateTime(TimeOnly.MinValue);
            var vacationEnd = vacation.EndDate.ToDateTime(TimeOnly.MinValue);

            var overlapStart = vacationStart > period.StartDate ? vacationStart : period.StartDate;
            var overlapEnd = vacationEnd < period.EndDate ? vacationEnd : period.EndDate;

            if (overlapStart <= overlapEnd)
            {
                return (int)(overlapEnd - overlapStart).TotalDays + 1;
            }

            return 0;
        }

        private async Task<List<dynamic>> GetUsersForDropdown()
        {
            return await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .Cast<dynamic>()
                .ToListAsync();
        }

        private async Task<List<dynamic>> GetTeamsForDropdown()
        {
            return await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .Cast<dynamic>()
                .ToListAsync();
        }
    }
}