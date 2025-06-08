using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace Mehrsam_Darou.Services
{
    public class DailyAttendanceService
    {
        private readonly DarouAppContext _context;

        public DailyAttendanceService(DarouAppContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Calculate and update daily attendance for a specific user and date
        /// </summary>
        public async Task CalculateAndUpdateDailyAttendanceAsync(Guid userId, DateTime date)
        {
            try
            {
                var dateOnly = DateOnly.FromDateTime(date);

                // Get all attendance logs for this user on this date
                var dailyLogs = await _context.AttendanceLogs
                    .Where(al => al.UserId == userId && al.LogTime.Date == date)
                    .OrderBy(al => al.LogTime)
                    .ToListAsync();

                // Get or create DailyAttendance record
                var dailyAttendance = await _context.DailyAttendances
                    .FirstOrDefaultAsync(da => da.UserId == userId && da.Date == dateOnly);

                if (dailyAttendance == null)
                {
                    dailyAttendance = new DailyAttendance
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Date = dateOnly,
                        PersianDate = ConvertToPersianDate(date),
                        DateCreated = DateTime.Now,
                        IsWorkingDay = await IsWorkingDayAsync(date)
                    };
                    _context.DailyAttendances.Add(dailyAttendance);
                }

                // Calculate attendance summary
                await CalculateAttendanceSummary(dailyAttendance, dailyLogs, userId, dateOnly);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw to avoid breaking the main operation
                System.Diagnostics.Debug.WriteLine($"Error calculating daily attendance: {ex.Message}");
                throw; // Re-throw if you want the calling method to handle it
            }
        }

        /// <summary>
        /// Calculate attendance summary for a daily attendance record
        /// </summary>
        private async Task CalculateAttendanceSummary(DailyAttendance dailyAttendance, List<AttendanceLog> dailyLogs, Guid userId, DateOnly date)
        {
            if (dailyLogs.Any())
            {
                var entryLogs = dailyLogs.Where(dl => dl.LogType == "Entry").ToList();
                var exitLogs = dailyLogs.Where(dl => dl.LogType == "Exit").ToList();

                // Calculate times
                dailyAttendance.FirstEntryTime = entryLogs.Any() ?
                    TimeOnly.FromDateTime(entryLogs.First().LogTime) : null;

                dailyAttendance.LastExitTime = exitLogs.Any() ?
                    TimeOnly.FromDateTime(exitLogs.Last().LogTime) : null;

                // Calculate total hours worked
                dailyAttendance.TotalHours = CalculateTotalHours(entryLogs, exitLogs);

                // Determine status based on attendance and other factors
                dailyAttendance.Status = await DetermineAttendanceStatusAsync(dailyAttendance, dailyLogs, userId, date);
            }
            else
            {
                // No logs for this date - determine status based on other factors
                await HandleNoAttendanceLogsAsync(dailyAttendance, userId, date);
            }
        }

        /// <summary>
        /// Handle cases where there are no attendance logs for a date
        /// </summary>
        private async Task HandleNoAttendanceLogsAsync(DailyAttendance dailyAttendance, Guid userId, DateOnly date)
        {
            dailyAttendance.FirstEntryTime = null;
            dailyAttendance.LastExitTime = null;
            dailyAttendance.TotalHours = 0;

            // Check if there's an approved vacation for this date
            var hasVacation = await _context.Vacations
                .AnyAsync(v => v.UserId == userId &&
                              v.Status == "Approved" &&
                              v.StartDate <= date &&
                              v.EndDate >= date);

            if (hasVacation)
            {
                dailyAttendance.Status = "Vacation";
            }
            else if (await IsWorkingDayAsync(date.ToDateTime(TimeOnly.MinValue)))
            {
                dailyAttendance.Status = "Absent";
            }
            else
            {
                dailyAttendance.Status = "Holiday";
            }
        }

        /// <summary>
        /// Determine attendance status based on logs and other factors
        /// </summary>
        private async Task<string> DetermineAttendanceStatusAsync(DailyAttendance dailyAttendance, List<AttendanceLog> logs, Guid userId, DateOnly date)
        {
            if (!logs.Any())
                return dailyAttendance.IsWorkingDay ? "Absent" : "Holiday";

            // Check if there's an approved vacation for this date
            var hasVacation = await _context.Vacations
                .AnyAsync(v => v.UserId == userId &&
                              v.Status == "Approved" &&
                              v.StartDate <= date &&
                              v.EndDate >= date);

            if (hasVacation)
                return "Vacation";

            var hasEntry = logs.Any(l => l.LogType == "Entry");

            if (hasEntry)
            {
                // If there are attendance logs and it's a working day, consider it present
                // You can add more sophisticated logic here (minimum hours, etc.)
                return "Present";
            }

            return dailyAttendance.IsWorkingDay ? "Absent" : "Holiday";
        }

        /// <summary>
        /// Calculate total working hours from entry and exit logs
        /// </summary>
        private decimal CalculateTotalHours(List<AttendanceLog> entryLogs, List<AttendanceLog> exitLogs)
        {
            decimal totalHours = 0;
            var entryTimes = entryLogs.Select(e => e.LogTime).OrderBy(t => t).ToList();
            var exitTimes = exitLogs.Select(e => e.LogTime).OrderBy(t => t).ToList();

            // Pair each entry with the corresponding exit
            for (int i = 0; i < entryTimes.Count; i++)
            {
                var entryTime = entryTimes[i];

                // Find the next exit after this entry
                var exitTime = exitTimes.Where(e => e > entryTime).FirstOrDefault();

                if (exitTime != default)
                {
                    var duration = exitTime - entryTime;
                    totalHours += (decimal)duration.TotalHours;

                    // Remove this exit time so it's not used again
                    exitTimes.Remove(exitTime);
                }
                else if (i == entryTimes.Count - 1 && !exitTimes.Any(e => e > entryTime))
                {
                    // Last entry without corresponding exit - assume still working until reasonable time
                    var currentTime = DateTime.Now;
                    var entryDate = entryTime.Date;
                    var assumedExitTime = entryDate.AddHours(17); // Assume 5 PM end time

                    // If it's today and current time is before 5 PM, use current time
                    if (entryDate == DateTime.Today && currentTime < assumedExitTime)
                        assumedExitTime = currentTime;

                    // Only add hours if the assumed exit is after entry and reasonable
                    if (assumedExitTime > entryTime && (assumedExitTime - entryTime).TotalHours <= 12)
                    {
                        var duration = assumedExitTime - entryTime;
                        totalHours += (decimal)duration.TotalHours;
                    }
                }
            }

            return Math.Round(totalHours, 2);
        }

        /// <summary>
        /// Check if a date is a working day
        /// </summary>
        private async Task<bool> IsWorkingDayAsync(DateTime date)
        {
            var persianDate = await _context.PersianDateConverters
                .FirstOrDefaultAsync(pdc => pdc.GregorianDate == DateOnly.FromDateTime(date));

            return persianDate?.IsWorkingDay ??
                   (date.DayOfWeek != DayOfWeek.Friday); // Default: Friday is not working day in Iran
        }

        /// <summary>
        /// Convert DateTime to Persian date string
        /// </summary>
        private string ConvertToPersianDate(DateTime date)
        {
            var pc = new PersianCalendar();
            return $"{pc.GetYear(date)}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
        }

        /// <summary>
        /// Recalculate daily attendance for a date range and multiple users
        /// </summary>
        public async Task<int> RecalculateRangeAsync(DateTime startDate, DateTime endDate, List<Guid>? userIds = null)
        {
            var users = userIds ?? await _context.Users
                .Where(u => u.TeamId != null)
                .Select(u => u.Id)
                .ToListAsync();

            int processedDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                foreach (var userId in users)
                {
                    await CalculateAndUpdateDailyAttendanceAsync(userId, date);
                    processedDays++;
                }
            }

            return processedDays;
        }

        /// <summary>
        /// Recalculate daily attendance for a specific user in a date range
        /// </summary>
        public async Task RecalculateForUserAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                await CalculateAndUpdateDailyAttendanceAsync(userId, date);
            }
        }

        /// <summary>
        /// Get daily attendance summary for a user in a date range
        /// </summary>
        public async Task<List<DailyAttendance>> GetDailyAttendanceAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            var startDateOnly = DateOnly.FromDateTime(startDate);
            var endDateOnly = DateOnly.FromDateTime(endDate);

            return await _context.DailyAttendances
                .Include(da => da.User)
                .Where(da => da.UserId == userId &&
                            da.Date >= startDateOnly &&
                            da.Date <= endDateOnly)
                .OrderBy(da => da.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Get attendance statistics for a user in a date range
        /// </summary>
        public async Task<AttendanceStatistics> GetAttendanceStatisticsAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            var dailyAttendances = await GetDailyAttendanceAsync(userId, startDate, endDate);

            return new AttendanceStatistics
            {
                TotalDays = dailyAttendances.Count,
                PresentDays = dailyAttendances.Count(da => da.Status == "Present"),
                AbsentDays = dailyAttendances.Count(da => da.Status == "Absent"),
                VacationDays = dailyAttendances.Count(da => da.Status == "Vacation"),
                HolidayDays = dailyAttendances.Count(da => da.Status == "Holiday"),
                MissionDays = dailyAttendances.Count(da => da.Status == "Mission"),
                TotalWorkingHours = dailyAttendances.Sum(da => da.TotalHours ?? 0),
                AverageWorkingHours = dailyAttendances.Where(da => da.TotalHours > 0).Average(da => da.TotalHours ?? 0)
            };
        }

        /// <summary>
        /// Delete daily attendance record when all attendance logs for a date are removed
        /// </summary>
        public async Task CleanupEmptyDailyAttendanceAsync(Guid userId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);

            // Check if there are any attendance logs for this date
            var hasLogs = await _context.AttendanceLogs
                .AnyAsync(al => al.UserId == userId && al.LogTime.Date == date);

            if (!hasLogs)
            {
                // Check if there's a vacation for this date
                var hasVacation = await _context.Vacations
                    .AnyAsync(v => v.UserId == userId &&
                                  v.Status == "Approved" &&
                                  v.StartDate <= dateOnly &&
                                  v.EndDate >= dateOnly);

                // If no logs and no vacation, we might want to keep the record for absence tracking
                // Or delete it based on your business logic
                if (!hasVacation)
                {
                    var dailyAttendance = await _context.DailyAttendances
                        .FirstOrDefaultAsync(da => da.UserId == userId && da.Date == dateOnly);

                    if (dailyAttendance != null)
                    {
                        // Update status instead of deleting - you might want absence tracking
                        dailyAttendance.Status = await IsWorkingDayAsync(date) ? "Absent" : "Holiday";
                        dailyAttendance.FirstEntryTime = null;
                        dailyAttendance.LastExitTime = null;
                        dailyAttendance.TotalHours = 0;
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attendance statistics model
    /// </summary>
    public class AttendanceStatistics
    {
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int VacationDays { get; set; }
        public int HolidayDays { get; set; }
        public int MissionDays { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public decimal AverageWorkingHours { get; set; }
        public decimal AttendanceRate => TotalDays > 0 ? (decimal)PresentDays / (TotalDays - HolidayDays) * 100 : 0;
    }
}