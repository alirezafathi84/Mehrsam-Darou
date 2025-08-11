using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;
using System.Globalization;

namespace Mehrsam_Darou.Controllers
{
    public class UserController : BaseController
    {
        private readonly ILogger<UserController> _logger;
        private readonly DarouAppContext _context;

        // Combine both constructors into one
        public UserController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> UserList(int? page, string SearchKey)
        {
            // Set common view data and get the page size
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10); // Default to 10 if setting.NumberPerPage is null
            int pageNumber = page ?? 1;

            // Base query for fetching users
            IQueryable<User> query = _context.Users.Include(u => u.Team);

            // Apply search filter if SearchKey is provided
            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(u => u.Username.Contains(SearchKey) || u.FirstName.Contains(SearchKey) || u.LastName.Contains(SearchKey)).OrderBy(e => e.DateCreated);
            }

            // Get total count after filtering
            int totalUsers = await query.CountAsync();

            // Fetch paginated results
            var users = await query
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create the paginated list
            var paginatedUsers = new PaginatedList<User>(users, totalUsers, pageNumber, pageSize);

            ViewBag.Teams = await _context.Teams.ToListAsync();

            // Pass paginated list to the view
            return View(paginatedUsers);
        }

        // New Report Action
        public async Task<IActionResult> UserReport(string format = "html")
        {
            // Get all users with their teams
            var allUsers = await _context.Users
                .Include(u => u.Team)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var reportData = new
            {
                Users = allUsers,
                GeneratedDate = DateTime.Now,
                TotalUsers = allUsers.Count,
                TeamsCount = allUsers.Where(u => u.Team != null).Select(u => u.Team.Name).Distinct().Count()
            };

            ViewBag.ReportData = reportData;

            if (format.ToLower() == "pdf")
            {
                // For PDF generation, you might want to use a library like iTextSharp or similar
                // For now, we'll return the HTML view that can be printed to PDF
                Response.Headers.Add("Content-Disposition", "attachment; filename=UserReport.html");
            }

            return View("UserReport", allUsers);
        }

        // Action to display a specific user's details by their ID
        [HttpGet("User/UserDetails/{id}")]
        public async Task<IActionResult> UserDetails(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                ViewData["Error"] = "رکوردی یافت نشد";
                return View(); // Return view with an error message
            }

            return View(user);  // Pass the user object as the model to the view
        }

        public async Task<IActionResult> AddNewUser()
        {
            // Set common view data and get the page size
            //  var setting = await ReadSettingAsync(_context);

            var Teams = await _context.Teams.OrderBy(e => e.Name).ToListAsync();

            return View("AddUser", Teams);
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(User user, string ConfirmPassword, Guid TeamDDL, IFormFile AvatarImg)
        {
            // Check if the password and confirm password match
            if (user.Password != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "رمز عبور و تایید آن یکسان نیستند.";
                return View("AddUser");
            }

            // Check if the username already exists
            bool isDuplicate = await _context.Users.AnyAsync(u => u.Username == user.Username);

            if (isDuplicate)
            {
                TempData["ErrorMessage"] = "نام کاربری قبلاً ثبت شده است.";
                return View("AddUser");
            }

            // Hash the password before saving it
            user.Password = Helper.Helper.HashPassword(user.Password);

            // Set the DateCreated to the current date and time
            user.DateCreated = DateTime.Now;
            user.DateModified = DateTime.Now;
            // Handle Avatar Upload
            if (AvatarImg != null && AvatarImg.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarImg.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/users/", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarImg.CopyToAsync(stream);
                }

                user.AvatarImg = @"\images\users\" + fileName; // Save the file name (not the full path) to the database
            }
            else
            {
                user.AvatarImg = null;  // Keep the old avatar if no new one is uploaded
            }
            user.TeamId = TeamDDL;

            // Add the user to the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "کاربر با موفقیت اضافه شد.";
            return RedirectToAction("UserList");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(Guid id)
        {
            // Validate session and get the user
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var teams = await _context.Teams.ToListAsync();
            ViewBag.User = user;
            return View(teams);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user, string ConfirmPassword, Guid TeamDDL, IFormFile AvatarImg)
        {
            if (!string.IsNullOrEmpty(user.Password) && user.Password != ConfirmPassword)
            {
                var teams = await _context.Teams.ToListAsync();
                ViewBag.User = user;
                TempData["ErrorMessage"] = "رمز عبور و تایید آن یکسان نیستند.";
                return View("EditUser", teams);
            }

            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "کاربر مورد نظر یافت نشد.";
                return View("EditUser");
            }

            _context.Entry(existingUser).State = EntityState.Detached;

            if (!string.IsNullOrEmpty(user.Password))
            {
                user.Password = HashPassword(user.Password); // Assuming HashPassword is your method for hashing
            }
            else
            {
                user.Password = existingUser.Password; // Keep the old password if not changed
            }

            // Handle Avatar Upload
            if (AvatarImg != null && AvatarImg.Length > 0)
            {
                // Delete the previous image if it exists
                if (!string.IsNullOrEmpty(existingUser.AvatarImg))
                {
                    var previousImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingUser.AvatarImg.TrimStart('\\'));
                    if (System.IO.File.Exists(previousImagePath))
                    {
                        System.IO.File.Delete(previousImagePath);
                    }
                }

                // Save the new image
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarImg.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/users/", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarImg.CopyToAsync(stream);
                }

                user.AvatarImg = @"\images\users\" + fileName; // Save the file path to the database
            }
            else
            {
                // Keep the old avatar if no new one is uploaded
                user.AvatarImg = existingUser.AvatarImg;
            }

            user.DateCreated = existingUser.DateCreated;
            user.DateModified = DateTime.Now;
            user.Version = existingUser.Version + 1;
            user.TeamId = TeamDDL; // Set the team ID from the dropdown
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "اطلاعات کاربر با موفقیت به‌روزرسانی شد.";
            return RedirectToAction("UserList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "کاربر مورد نظر یافت نشد.";
                    return RedirectToAction("UserList");
                }

                // Check if user has any related records that prevent deletion
                bool hasRelatedRecords = await CheckUserRelatedRecords(id);

                if (hasRelatedRecords)
                {
                    TempData["ErrorMessage"] = "این کاربر دارای رکوردهای مرتبط است و قابل حذف نیست.";
                    return RedirectToAction("UserList");
                }

                // Delete the user's image if it exists
                if (!string.IsNullOrEmpty(user.AvatarImg))
                {
                    // Construct the full path to the image file
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarImg.TrimStart('\\'));

                    // Check if the file exists and delete it
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Users.Remove(user);  // Remove the user from the database
                await _context.SaveChangesAsync();  // Save the changes to the database

                TempData["SuccessMessage"] = "کاربر با موفقیت حذف شد.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف کاربر: " + ex.Message;
            }

            return RedirectToAction("UserList");
        }

        /// <summary>
        /// Delete user with cascade deletion - removes user and all related records
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserCascade(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "کاربر مورد نظر یافت نشد.";
                    return RedirectToAction("UserList");
                }

                // Delete all related records in proper order (respecting foreign key constraints)
                await DeleteUserRelatedRecordsAsync(id);

                // Delete the user's image if it exists
                if (!string.IsNullOrEmpty(user.AvatarImg))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarImg.TrimStart('\\'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Finally delete the user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "کاربر و تمامی اطلاعات مرتبط با موفقیت حذف شد.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "خطا در حذف کاربر: " + ex.Message;
            }

            return RedirectToAction("UserList");
        }

        /// <summary>
        /// Get count of related records for a user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserRelatedRecordsCount(Guid id)
        {
            try
            {
                var counts = new
                {
                    AttendanceLogs = await _context.AttendanceLogs.CountAsync(a => a.UserId == id),
                    Notifications = await _context.Notifications.CountAsync(n => n.UserId == id),
                    ChatMessagesSent = await _context.ChatMessages.CountAsync(c => c.SenderId == id),
                    ChatMessagesReceived = await _context.ChatMessages.CountAsync(c => c.ReceiverId == id),
                    DailyAttendance = await _context.DailyAttendances.CountAsync(d => d.UserId == id),
                    UserEnterLogs = await _context.UserEnterLogs.CountAsync(u => u.UserId == id),
                    SalaryInfos = await _context.SalaryInfos.CountAsync(s => s.UserId == id),
                    VacationsRequested = await _context.Vacations.CountAsync(v => v.UserId == id),
                    VacationsApproved = await _context.Vacations.CountAsync(v => v.ApprovedBy == id),
                    UserStatus = await _context.UserStatuses.CountAsync(u => u.UserId == id),
                    BatchTestsCreated = await _context.BatchTests.CountAsync(b => b.CreatedBy == id),
                    BatchTestsModified = await _context.BatchTests.CountAsync(b => b.LastModifiedBy == id),
                    CertificationsCreated = await _context.Certifications.CountAsync(c => c.CreatedBy == id),
                    CertificationsModified = await _context.Certifications.CountAsync(c => c.LastModifiedBy == id),
                    DevelopmentProjectsCreated = await _context.DevelopmentProjects.CountAsync(d => d.CreatedBy == id),
                    DevelopmentProjectsModified = await _context.DevelopmentProjects.CountAsync(d => d.LastModifiedBy == id),
                    FormulasCreated = await _context.Formulas.CountAsync(f => f.CreatedBy == id),
                    FormulasModified = await _context.Formulas.CountAsync(f => f.LastModifiedBy == id),
                    JournalEntriesCreated = await _context.JournalEntries.CountAsync(j => j.CreatedBy == id),
                    JournalEntriesApproved = await _context.JournalEntries.CountAsync(j => j.ApprovedBy == id),
                    PaymentTransactionsCreated = await _context.PaymentTransactions.CountAsync(p => p.CreatedBy == id),
                    PaymentTransactionsApproved = await _context.PaymentTransactions.CountAsync(p => p.ApprovedBy == id),
                    QaAuditsCreated = await _context.QaAudits.CountAsync(q => q.CreatedBy == id),
                    QaAuditsModified = await _context.QaAudits.CountAsync(q => q.LastModifiedBy == id),
                    QaStandardsCreated = await _context.QaStandards.CountAsync(q => q.CreatedBy == id),
                    QaStandardsModified = await _context.QaStandards.CountAsync(q => q.LastModifiedBy == id),
                    QcReportsCreated = await _context.QcReports.CountAsync(q => q.CreatedBy == id),
                    QcReportsModified = await _context.QcReports.CountAsync(q => q.LastModifiedBy == id),
                    QcTestsCreated = await _context.QcTests.CountAsync(q => q.CreatedBy == id),
                    QcTestsModified = await _context.QcTests.CountAsync(q => q.LastModifiedBy == id),
                    ResearchProjectsCreated = await _context.ResearchProjects.CountAsync(r => r.CreatedBy == id),
                    ResearchProjectsModified = await _context.ResearchProjects.CountAsync(r => r.LastModifiedBy == id),
                    FinancialReportsCreated = await _context.FinancialReports.CountAsync(f => f.CreatedBy == id),
                    FinancialReportsApproved = await _context.FinancialReports.CountAsync(f => f.ApprovedBy == id)
                };

                var totalRecords = counts.AttendanceLogs + counts.Notifications +
                                 counts.ChatMessagesSent + counts.ChatMessagesReceived +
                                 counts.DailyAttendance + counts.UserEnterLogs +
                                 counts.SalaryInfos + counts.VacationsRequested +
                                 counts.VacationsApproved + counts.UserStatus +
                                 counts.BatchTestsCreated + counts.BatchTestsModified +
                                 counts.CertificationsCreated + counts.CertificationsModified +
                                 counts.DevelopmentProjectsCreated + counts.DevelopmentProjectsModified +
                                 counts.FormulasCreated + counts.FormulasModified +
                                 counts.JournalEntriesCreated + counts.JournalEntriesApproved +
                                 counts.PaymentTransactionsCreated + counts.PaymentTransactionsApproved +
                                 counts.QaAuditsCreated + counts.QaAuditsModified +
                                 counts.QaStandardsCreated + counts.QaStandardsModified +
                                 counts.QcReportsCreated + counts.QcReportsModified +
                                 counts.QcTestsCreated + counts.QcTestsModified +
                                 counts.ResearchProjectsCreated + counts.ResearchProjectsModified +
                                 counts.FinancialReportsCreated + counts.FinancialReportsApproved;

                return Json(new { success = true, counts, totalRecords });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private async Task<bool> CheckUserRelatedRecords(Guid userId)
        {
            // Check for related records that might prevent deletion
            try
            {
                // Check AttendanceLogs
                if (await _context.AttendanceLogs.AnyAsync(a => a.UserId == userId))
                    return true;

                // Check Notifications
                if (await _context.Notifications.AnyAsync(n => n.UserId == userId))
                    return true;

                // Check ChatMessages (both sender and receiver)
                if (await _context.ChatMessages.AnyAsync(c => c.SenderId == userId || c.ReceiverId == userId))
                    return true;

                // Check DailyAttendance
                if (await _context.DailyAttendances.AnyAsync(d => d.UserId == userId))
                    return true;

                // Check UserEnterLogs
                if (await _context.UserEnterLogs.AnyAsync(u => u.UserId == userId))
                    return true;

                // Check SalaryInfos
                if (await _context.SalaryInfos.AnyAsync(s => s.UserId == userId))
                    return true;

                // Check Vacations
                if (await _context.Vacations.AnyAsync(v => v.UserId == userId || v.ApprovedBy == userId))
                    return true;

                // Check UserStatus
                if (await _context.UserStatuses.AnyAsync(u => u.UserId == userId))
                    return true;

                // Check other related tables
                if (await _context.BatchTests.AnyAsync(b => b.CreatedBy == userId || b.LastModifiedBy == userId))
                    return true;

                if (await _context.Certifications.AnyAsync(c => c.CreatedBy == userId || c.LastModifiedBy == userId))
                    return true;

                if (await _context.DevelopmentProjects.AnyAsync(d => d.CreatedBy == userId || d.LastModifiedBy == userId))
                    return true;

                if (await _context.Formulas.AnyAsync(f => f.CreatedBy == userId || f.LastModifiedBy == userId))
                    return true;

                if (await _context.JournalEntries.AnyAsync(j => j.CreatedBy == userId || j.ApprovedBy == userId))
                    return true;

                if (await _context.PaymentTransactions.AnyAsync(p => p.CreatedBy == userId || p.ApprovedBy == userId))
                    return true;

                if (await _context.QaAudits.AnyAsync(q => q.CreatedBy == userId || q.LastModifiedBy == userId))
                    return true;

                if (await _context.QaStandards.AnyAsync(q => q.CreatedBy == userId || q.LastModifiedBy == userId))
                    return true;

                if (await _context.QcReports.AnyAsync(q => q.CreatedBy == userId || q.LastModifiedBy == userId))
                    return true;

                if (await _context.QcTests.AnyAsync(q => q.CreatedBy == userId || q.LastModifiedBy == userId))
                    return true;

                if (await _context.ResearchProjects.AnyAsync(r => r.CreatedBy == userId || r.LastModifiedBy == userId))
                    return true;

                if (await _context.FinancialReports.AnyAsync(f => f.CreatedBy == userId || f.ApprovedBy == userId))
                    return true;

                return false;
            }
            catch
            {
                // If there's an error checking, assume there are related records
                return true;
            }
        }

        /// <summary>
        /// Delete all related records for a user in proper order
        /// </summary>
        private async Task DeleteUserRelatedRecordsAsync(Guid userId)
        {
            // Delete in order to respect foreign key constraints
            // Delete child records first, then parent records

            // 1. Delete AttendanceLogs
            var attendanceLogs = await _context.AttendanceLogs.Where(a => a.UserId == userId).ToListAsync();
            if (attendanceLogs.Any())
            {
                _context.AttendanceLogs.RemoveRange(attendanceLogs);
                await _context.SaveChangesAsync();
            }

            // 2. Delete DailyAttendance
            var dailyAttendances = await _context.DailyAttendances.Where(d => d.UserId == userId).ToListAsync();
            if (dailyAttendances.Any())
            {
                _context.DailyAttendances.RemoveRange(dailyAttendances);
                await _context.SaveChangesAsync();
            }

            // 3. Delete UserEnterLogs
            var userEnterLogs = await _context.UserEnterLogs.Where(u => u.UserId == userId).ToListAsync();
            if (userEnterLogs.Any())
            {
                _context.UserEnterLogs.RemoveRange(userEnterLogs);
                await _context.SaveChangesAsync();
            }

            // 4. Delete UserStatus
            var userStatus = await _context.UserStatuses.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userStatus != null)
            {
                _context.UserStatuses.Remove(userStatus);
                await _context.SaveChangesAsync();
            }

            // 5. Delete SalaryInfos
            var salaryInfos = await _context.SalaryInfos.Where(s => s.UserId == userId).ToListAsync();
            if (salaryInfos.Any())
            {
                _context.SalaryInfos.RemoveRange(salaryInfos);
                await _context.SaveChangesAsync();
            }

            // 6. Delete Notifications
            var notifications = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
            if (notifications.Any())
            {
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
            }

            // 7. Delete ChatMessages (both sent and received)
            var chatMessages = await _context.ChatMessages
                .Where(c => c.SenderId == userId || c.ReceiverId == userId)
                .ToListAsync();
            if (chatMessages.Any())
            {
                _context.ChatMessages.RemoveRange(chatMessages);
                await _context.SaveChangesAsync();
            }

            // 8. Update Vacations where user was the approver (set to null or another user)
            var vacationsApproved = await _context.Vacations
                .Where(v => v.ApprovedBy == userId)
                .ToListAsync();
            foreach (var vacation in vacationsApproved)
            {
                vacation.ApprovedBy = null; // or set to another admin user
            }
            if (vacationsApproved.Any())
            {
                await _context.SaveChangesAsync();
            }

            // 9. Delete Vacations requested by user
            var vacations = await _context.Vacations.Where(v => v.UserId == userId).ToListAsync();
            if (vacations.Any())
            {
                _context.Vacations.RemoveRange(vacations);
                await _context.SaveChangesAsync();
            }

            // 10. Handle records in other tables that reference this user
            // For tables like batch_tests, certifications, etc., set the user references to null
            // or handle them according to your business rules

            // Update batch_tests created_by and last_modified_by
            var batchTestsCreated = await _context.BatchTests.Where(b => b.CreatedBy == userId).ToListAsync();
            var batchTestsModified = await _context.BatchTests.Where(b => b.LastModifiedBy == userId).ToListAsync();

            foreach (var test in batchTestsCreated)
                test.CreatedBy = null;
            foreach (var test in batchTestsModified)
                test.LastModifiedBy = null;

            if (batchTestsCreated.Any() || batchTestsModified.Any())
                await _context.SaveChangesAsync();

            // Update certifications
            var certificationsCreated = await _context.Certifications.Where(c => c.CreatedBy == userId).ToListAsync();
            var certificationsModified = await _context.Certifications.Where(c => c.LastModifiedBy == userId).ToListAsync();

            foreach (var cert in certificationsCreated)
                cert.CreatedBy = null;
            foreach (var cert in certificationsModified)
                cert.LastModifiedBy = null;

            if (certificationsCreated.Any() || certificationsModified.Any())
                await _context.SaveChangesAsync();

            // Update development_projects
            var devProjectsCreated = await _context.DevelopmentProjects.Where(d => d.CreatedBy == userId).ToListAsync();
            var devProjectsModified = await _context.DevelopmentProjects.Where(d => d.LastModifiedBy == userId).ToListAsync();

            foreach (var project in devProjectsCreated)
                project.CreatedBy = null;
            foreach (var project in devProjectsModified)
                project.LastModifiedBy = null;

            if (devProjectsCreated.Any() || devProjectsModified.Any())
                await _context.SaveChangesAsync();

            // Update formulas
            var formulasCreated = await _context.Formulas.Where(f => f.CreatedBy == userId).ToListAsync();
            var formulasModified = await _context.Formulas.Where(f => f.LastModifiedBy == userId).ToListAsync();

            foreach (var formula in formulasCreated)
                formula.CreatedBy = null;
            foreach (var formula in formulasModified)
                formula.LastModifiedBy = null;

            if (formulasCreated.Any() || formulasModified.Any())
                await _context.SaveChangesAsync();

            // Update journal_entries
            var journalEntriesCreated = await _context.JournalEntries.Where(j => j.CreatedBy == userId).ToListAsync();
            var journalEntriesApproved = await _context.JournalEntries.Where(j => j.ApprovedBy == userId).ToListAsync();

            foreach (var entry in journalEntriesCreated)
                entry.CreatedBy = null;
            foreach (var entry in journalEntriesApproved)
                entry.ApprovedBy = null;

            if (journalEntriesCreated.Any() || journalEntriesApproved.Any())
                await _context.SaveChangesAsync();

            // Update payment_transactions
            var paymentTransactionsCreated = await _context.PaymentTransactions.Where(p => p.CreatedBy == userId).ToListAsync();
            var paymentTransactionsApproved = await _context.PaymentTransactions.Where(p => p.ApprovedBy == userId).ToListAsync();

            foreach (var transaction in paymentTransactionsCreated)
                transaction.CreatedBy = null;
            foreach (var transaction in paymentTransactionsApproved)
                transaction.ApprovedBy = null;

            if (paymentTransactionsCreated.Any() || paymentTransactionsApproved.Any())
                await _context.SaveChangesAsync();

            // Update qa_audits
            var qaAuditsCreated = await _context.QaAudits.Where(q => q.CreatedBy == userId).ToListAsync();
            var qaAuditsModified = await _context.QaAudits.Where(q => q.LastModifiedBy == userId).ToListAsync();

            foreach (var audit in qaAuditsCreated)
                audit.CreatedBy = null;
            foreach (var audit in qaAuditsModified)
                audit.LastModifiedBy = null;

            if (qaAuditsCreated.Any() || qaAuditsModified.Any())
                await _context.SaveChangesAsync();

            // Update qa_standards
            var qaStandardsCreated = await _context.QaStandards.Where(q => q.CreatedBy == userId).ToListAsync();
            var qaStandardsModified = await _context.QaStandards.Where(q => q.LastModifiedBy == userId).ToListAsync();

            foreach (var standard in qaStandardsCreated)
                standard.CreatedBy = null;
            foreach (var standard in qaStandardsModified)
                standard.LastModifiedBy = null;

            if (qaStandardsCreated.Any() || qaStandardsModified.Any())
                await _context.SaveChangesAsync();

            // Update qc_reports
            var qcReportsCreated = await _context.QcReports.Where(q => q.CreatedBy == userId).ToListAsync();
            var qcReportsModified = await _context.QcReports.Where(q => q.LastModifiedBy == userId).ToListAsync();

            foreach (var report in qcReportsCreated)
                report.CreatedBy = null;
            foreach (var report in qcReportsModified)
                report.LastModifiedBy = null;

            if (qcReportsCreated.Any() || qcReportsModified.Any())
                await _context.SaveChangesAsync();

            // Update qc_tests
            var qcTestsCreated = await _context.QcTests.Where(q => q.CreatedBy == userId).ToListAsync();
            var qcTestsModified = await _context.QcTests.Where(q => q.LastModifiedBy == userId).ToListAsync();

            foreach (var test in qcTestsCreated)
                test.CreatedBy = null;
            foreach (var test in qcTestsModified)
                test.LastModifiedBy = null;

            if (qcTestsCreated.Any() || qcTestsModified.Any())
                await _context.SaveChangesAsync();

            // Update research_projects
            var researchProjectsCreated = await _context.ResearchProjects.Where(r => r.CreatedBy == userId).ToListAsync();
            var researchProjectsModified = await _context.ResearchProjects.Where(r => r.LastModifiedBy == userId).ToListAsync();

            foreach (var project in researchProjectsCreated)
                project.CreatedBy = null;
            foreach (var project in researchProjectsModified)
                project.LastModifiedBy = null;

            if (researchProjectsCreated.Any() || researchProjectsModified.Any())
                await _context.SaveChangesAsync();

            // Update financial_reports
            var financialReportsCreated = await _context.FinancialReports.Where(f => f.CreatedBy == userId).ToListAsync();
            var financialReportsApproved = await _context.FinancialReports.Where(f => f.ApprovedBy == userId).ToListAsync();

            foreach (var report in financialReportsCreated)
                report.CreatedBy = null;
            foreach (var report in financialReportsApproved)
                report.ApprovedBy = null;

            if (financialReportsCreated.Any() || financialReportsApproved.Any())
                await _context.SaveChangesAsync();
        }
    }
}