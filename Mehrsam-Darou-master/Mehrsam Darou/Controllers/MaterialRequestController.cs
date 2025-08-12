using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mehrsam_Darou.Controllers
{
    public class MaterialRequestController : BaseController
    {
        private readonly DarouAppContext _context;

        public MaterialRequestController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: MaterialRequest/RequestList
        public async Task<IActionResult> RequestList(int? page, string searchKey, string status, string priority)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            // Note: After scaffold, use the exact property names generated
            IQueryable<MaterialRequest> query = _context.MaterialRequests
                .Include(r => r.RequestType)
                .Include(r => r.Category)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.ApprovedByNavigation);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(r => r.RequestTitle.Contains(searchKey) ||
                                       r.RequestNumber.Contains(searchKey) ||
                                       r.Department.Contains(searchKey));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority) && int.TryParse(priority, out int priorityLevel))
            {
                query = query.Where(r => r.PriorityLevel == priorityLevel);
            }

            query = query.OrderByDescending(r => r.RequestDate);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<MaterialRequest>(items, total, pageNumber, pageSize);

            // Populate filter dropdowns
            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "", Text = "همه وضعیت‌ها" },
                new { Value = "در انتظار بررسی", Text = "در انتظار بررسی" },
                new { Value = "در حال بررسی", Text = "در حال بررسی" },
                new { Value = "تأیید شده", Text = "تأیید شده" },
                new { Value = "رد شده", Text = "رد شده" },
                new { Value = "در حال تأمین", Text = "در حال تأمین" },
                new { Value = "تحویل شده", Text = "تحویل شده" },
                new { Value = "تکمیل شده", Text = "تکمیل شده" },
                new { Value = "منتظر تأیید مدیرعامل", Text = "منتظر تأیید مدیرعامل" }
            }, "Value", "Text", status);

            ViewBag.PriorityList = new SelectList(new[]
            {
                new { Value = "", Text = "همه اولویت‌ها" },
                new { Value = "1", Text = "بحرانی" },
                new { Value = "2", Text = "بالا" },
                new { Value = "3", Text = "متوسط" },
                new { Value = "4", Text = "پایین" },
                new { Value = "5", Text = "خیلی پایین" }
            }, "Value", "Text", priority);

            return View(paginatedList);
        }

        // GET: MaterialRequest/MyRequests
        public async Task<IActionResult> MyRequests(int? page, string searchKey, string status)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "کاربر یافت نشد";
                return RedirectToAction("Login", "Account");
            }

            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<MaterialRequest> query = _context.MaterialRequests
                .Where(r => r.RequestedBy == currentUserId)
                .Include(r => r.RequestType)
                .Include(r => r.Category)
                .Include(r => r.ApprovedByNavigation);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(r => r.RequestTitle.Contains(searchKey) ||
                                       r.RequestNumber.Contains(searchKey));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            query = query.OrderByDescending(r => r.RequestDate);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<MaterialRequest>(items, total, pageNumber, pageSize);

            // Populate status filter
            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "", Text = "همه وضعیت‌ها" },
                new { Value = "در انتظار بررسی", Text = "در انتظار بررسی" },
                new { Value = "در حال بررسی", Text = "در حال بررسی" },
                new { Value = "تأیید شده", Text = "تأیید شده" },
                new { Value = "رد شده", Text = "رد شده" },
                new { Value = "در حال تأمین", Text = "در حال تأمین" },
                new { Value = "تحویل شده", Text = "تحویل شده" },
                new { Value = "تکمیل شده", Text = "تکمیل شده" }
            }, "Value", "Text", status);

            return View(paginatedList);
        }

        // GET: MaterialRequest/AddRequest
        public async Task<IActionResult> AddRequest()
        {
            await PopulateDropdowns();

            var request = new MaterialRequest
            {
                RequestId = Guid.NewGuid(),
                RequestDate = DateTime.Now,
                PriorityLevel = 3,
                Currency = "IRR",
                IsSubstituteAllowed = true,
                IsActive = true,
                Status = "در انتظار بررسی" // Set default status
            };

            // Generate request number
            var lastRequest = await _context.MaterialRequests
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            var requestCount = await _context.MaterialRequests.CountAsync() + 1;
            request.RequestNumber = $"REQ-{DateTime.Now:yyyyMM}-{requestCount:000}";

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRequest(MaterialRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "کاربر یافت نشد";
                return RedirectToAction("Login", "Account");
            }

            // Remove problematic items from ModelState
            ModelState.Remove("Status");
            ModelState.Remove("Category");
            ModelState.Remove("RequestType");
            ModelState.Remove("WorkflowStage");
            ModelState.Remove("CreatedByNavigation");  // Add this line

            // Remove other navigation properties
            ModelState.Remove("RequestedByNavigation");
            ModelState.Remove("ApprovedByNavigation");
            ModelState.Remove("MaterialRequestItems");

            // Remove auto-generated fields
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("RequestedBy");

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Set default values
                    request.RequestedBy = currentUserId.Value;
                    request.CreatedBy = currentUserId.Value;
                    request.CreatedDate = DateTime.Now;
                    request.Status = "در انتظار بررسی";
                    request.WorkflowStage = "ثبت درخواست";

                    // Check for duplicate request number
                    if (await _context.MaterialRequests.AnyAsync(r => r.RequestNumber == request.RequestNumber))
                    {
                        var requestCount = await _context.MaterialRequests.CountAsync() + 1;
                        request.RequestNumber = $"REQ-{DateTime.Now:yyyyMM}-{requestCount:000}";
                    }

                    _context.MaterialRequests.Add(request);
                    await _context.SaveChangesAsync();

                    // Add workflow history
                    await AddWorkflowHistory(request.RequestId, "ثبت درخواست", "در انتظار بررسی",
                        "درخواست توسط کاربر ثبت شد", currentUserId.Value);

                    // Send notification to procurement team
                    await SendNotificationToProcurement(request);

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "درخواست شما با موفقیت ثبت شد";
                    return RedirectToAction(nameof(MyRequests));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "خطا در ثبت درخواست: " + ex.Message;
                }
            }
            else
            {
                // Debug remaining validation errors
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();

                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Field: {error.Field}, Errors: {string.Join(", ", error.Errors)}");
                }
            }

            await PopulateDropdowns();
            return View(request);
        }
        // GET: MaterialRequest/ProcessRequest/5
        public async Task<IActionResult> ProcessRequest(Guid id)
        {
            var request = await _context.MaterialRequests
                .Include(r => r.RequestType)
                .Include(r => r.Category)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Material)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Unit)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "درخواست یافت نشد";
                return RedirectToAction(nameof(RequestList));
            }

            // Get workflow history
            ViewBag.WorkflowHistory = await _context.RequestWorkflowHistories
                .Where(w => w.RequestId == id)
                .Include(w => w.ProcessedByNavigation)
                .OrderBy(w => w.ProcessedDate)
                .ToListAsync();

            await PopulateProcessingDropdowns();
            return View(request);
        }

        // POST: MaterialRequest/ProcessRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(Guid requestId, string action, string comments)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "کاربر یافت نشد";
                return RedirectToAction("Login", "Account");
            }

            var request = await _context.MaterialRequests
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Material)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "درخواست یافت نشد";
                return RedirectToAction(nameof(RequestList));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                switch (action.ToLower())
                {
                    case "check_inventory":
                        await ProcessCheckInventory(request, currentUserId.Value, comments);
                        break;

                    case "approve":
                        await ProcessApproval(request, currentUserId.Value, comments);
                        break;

                    case "reject":
                        await ProcessRejection(request, currentUserId.Value, comments);
                        break;

                    case "find_substitute":
                        await ProcessFindSubstitute(request, currentUserId.Value, comments);
                        break;

                    case "request_ceo_approval":
                        await ProcessRequestCeoApproval(request, currentUserId.Value, comments);
                        break;

                    case "deliver":
                        await ProcessDelivery(request, currentUserId.Value, comments);
                        break;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "عملیات با موفقیت انجام شد";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "خطا در پردازش درخواست: " + ex.Message;
            }

            return RedirectToAction(nameof(ProcessRequest), new { id = requestId });
        }

        // Private methods for processing workflow steps
        private async Task ProcessCheckInventory(MaterialRequest request, Guid processedBy, string comments)
        {
            bool allItemsAvailable = true;
            bool someItemsAvailable = false;

            foreach (var item in request.MaterialRequestItems)
            {
                if (item.Material != null)
                {
                    // Check material inventory
                    var availableStock = await _context.MaterialBatches
                        .Where(b => b.MaterialId == item.MaterialId &&
                                   b.Status == "آزاد شده" &&
                                   b.CurrentQuantity > 0)
                        .SumAsync(b => b.CurrentQuantity);

                    item.StockQuantity = availableStock;

                    if (availableStock >= item.QuantityRequested)
                    {
                        item.ItemStatus = "موجود";
                        item.AvailabilityStatus = "موجود";
                        someItemsAvailable = true;
                    }
                    else if (availableStock > 0)
                    {
                        item.ItemStatus = "موجود جزئی";
                        item.AvailabilityStatus = "موجود جزئی";
                        allItemsAvailable = false;
                        someItemsAvailable = true;
                    }
                    else
                    {
                        item.ItemStatus = "ناموجود";
                        item.AvailabilityStatus = "ناموجود";
                        allItemsAvailable = false;
                    }
                }
            }

            if (allItemsAvailable)
            {
                request.Status = "آماده تحویل";
                request.WorkflowStage = "تحویل";
                await SendNotificationToRequester(request, "درخواست شما آماده تحویل است");
            }
            else if (someItemsAvailable && request.IsSubstituteAllowed == true)
            {
                request.Status = "نیاز به جایگزین";
                request.WorkflowStage = "جستجوی جایگزین";
                await SendNotificationToRequester(request, "برخی اقلام درخواستی موجود نیستند، در حال جستجوی جایگزین");
            }
            else
            {
                request.Status = "نیاز به خرید";
                request.WorkflowStage = "درخواست خرید";
                await SendNotificationToProcurement(request, "درخواست نیاز به خرید دارد");
            }

            await AddWorkflowHistory(request.RequestId, "بررسی موجودی", request.Status,
                comments ?? "موجودی مواد بررسی شد", processedBy);
        }

        private async Task ProcessApproval(MaterialRequest request, Guid processedBy, string comments)
        {
            request.Status = "تأیید شده";
            request.ApprovedBy = processedBy;
            request.ApprovalDate = DateTime.Now;
            request.ApprovalStatus = "تأیید شده";

            // Move to next appropriate stage
            if (request.MaterialRequestItems.Any(i => i.ItemStatus == "ناموجود"))
            {
                request.WorkflowStage = "درخواست خرید";
                await SendNotificationToProcurement(request, "درخواست تأیید شده - نیاز به خرید");
            }
            else
            {
                request.WorkflowStage = "تحویل";
                await SendNotificationToRequester(request, "درخواست شما تأیید شد");
            }

            await AddWorkflowHistory(request.RequestId, "تأیید", "تأیید شده",
                comments ?? "درخواست تأیید شد", processedBy);
        }

        private async Task ProcessRejection(MaterialRequest request, Guid processedBy, string comments)
        {
            request.Status = "رد شده";
            request.ApprovalStatus = "رد شده";
            request.RejectionReason = comments;
            request.WorkflowStage = "تکمیل";

            await SendNotificationToRequester(request, $"درخواست شما رد شد. دلیل: {comments}");

            await AddWorkflowHistory(request.RequestId, "رد درخواست", "رد شده",
                comments ?? "درخواست رد شد", processedBy);
        }

        private async Task ProcessFindSubstitute(MaterialRequest request, Guid processedBy, string comments)
        {
            // This would involve complex logic to find substitutes
            // For now, we'll update status and notify
            request.Status = "در حال جستجوی جایگزین";
            request.WorkflowStage = "جستجوی جایگزین";

            await SendNotificationToRequester(request, "در حال جستجو برای مواد جایگزین");

            await AddWorkflowHistory(request.RequestId, "جستجوی جایگزین", request.Status,
                comments ?? "شروع جستجو برای مواد جایگزین", processedBy);
        }

        private async Task ProcessRequestCeoApproval(MaterialRequest request, Guid processedBy, string comments)
        {
            request.Status = "منتظر تأیید مدیرعامل";
            request.WorkflowStage = "تأیید مدیرعامل";

            // Find CEO and send notification
            var ceoUser = await _context.Users
                .Join(_context.Teams, u => u.TeamId, t => t.Id, (u, t) => new { User = u, Team = t })
                .Where(x => x.Team.ManagmentDashboard == true)
                .Select(x => x.User)
                .FirstOrDefaultAsync();

            if (ceoUser != null)
            {
                await SendNotificationToUser(ceoUser.Id, "درخواست نیاز به تأیید مدیرعامل",
                    $"درخواست شماره {request.RequestNumber} نیاز به تأیید شما دارد");
            }

            await AddWorkflowHistory(request.RequestId, "درخواست تأیید مدیرعامل", request.Status,
                comments ?? "درخواست به مدیرعامل ارسال شد", processedBy);
        }

        private async Task ProcessDelivery(MaterialRequest request, Guid processedBy, string comments)
        {
            request.Status = "تحویل شده";
            request.WorkflowStage = "تحویل";
            request.CompletionDate = DateTime.Now;

            await SendNotificationToRequester(request, "درخواست شما تحویل شد");

            await AddWorkflowHistory(request.RequestId, "تحویل", "تحویل شده",
                comments ?? "درخواست تحویل داده شد", processedBy);
        }

        // Helper methods
        private async Task AddWorkflowHistory(Guid requestId, string stage, string status, string comments, Guid processedBy)
        {
            var workflow = new RequestWorkflowHistory
            {
                WorkflowId = Guid.NewGuid(),
                RequestId = requestId,
                Stage = stage,
                Status = status,
                Comments = comments,
                ProcessedBy = processedBy,
                ProcessedDate = DateTime.Now,
                IsActive = true
            };

            _context.RequestWorkflowHistories.Add(workflow);
        }

        private async Task SendNotificationToRequester(MaterialRequest request, string message)
        {
            await SendNotificationToUser(request.RequestedBy,
                $"به‌روزرسانی درخواست {request.RequestNumber}", message);
        }

        private async Task SendNotificationToProcurement(MaterialRequest request, string customMessage = null)
        {
            // Find procurement team members
            var procurementUsers = await _context.Users
                .Join(_context.Teams, u => u.TeamId, t => t.Id, (u, t) => new { User = u, Team = t })
                .Where(x => x.Team.BuyCommercial == true)
                .Select(x => x.User)
                .ToListAsync();

            var message = customMessage ?? $"درخواست جدید شماره {request.RequestNumber} نیاز به بررسی دارد";

            foreach (var user in procurementUsers)
            {
                await SendNotificationToUser(user.Id, "درخواست جدید", message);
            }
        }

        private async Task SendNotificationToUser(Guid userId, string title, string message)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                Message = message,
                Type = "MaterialRequest",
                UserId = userId,
                CreatedDate = DateTime.Now,
                Seen = false
            };

            _context.Notifications.Add(notification);
        }

        private async Task PopulateDropdowns()
        {
            var categories = await _context.RequestCategories
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var requestTypes = await _context.RequestTypes
                .Where(t => t.IsActive == true)
                .OrderBy(t => t.TypeName)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
            ViewBag.RequestTypes = new SelectList(requestTypes, "TypeId", "TypeName"); // Make sure this matches your RequestType model

            ViewBag.PriorityLevels = new SelectList(new[]
            {
        new { Value = 1, Text = "بحرانی" },
        new { Value = 2, Text = "بالا" },
        new { Value = 3, Text = "متوسط" },
        new { Value = 4, Text = "پایین" },
        new { Value = 5, Text = "خیلی پایین" }
    }, "Value", "Text");
        }

        private async Task PopulateProcessingDropdowns()
        {
            var materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .ToListAsync();

            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            ViewBag.Materials = new SelectList(materials, "MaterialId", "MaterialName");
            ViewBag.Suppliers = new SelectList(suppliers, "SupplierId", "SupplierName");
        }

        // Dashboard method
        public async Task<IActionResult> RequestDashboard()
        {
            var currentUserId = GetCurrentUserId();

            // Get statistics
            var totalRequests = await _context.MaterialRequests.CountAsync();
            var pendingRequests = await _context.MaterialRequests.CountAsync(r => r.Status == "در انتظار بررسی");
            var approvedRequests = await _context.MaterialRequests.CountAsync(r => r.Status == "تأیید شده");
            var myRequests = currentUserId.HasValue ?
                await _context.MaterialRequests.CountAsync(r => r.RequestedBy == currentUserId) : 0;

            // Get recent requests
            var recentRequests = await _context.MaterialRequests
                .Include(r => r.RequestedByNavigation)
                .OrderByDescending(r => r.RequestDate)
                .Take(10)
                .ToListAsync();

            // Get requests by status
            var requestsByStatus = await _context.MaterialRequests
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.TotalRequests = totalRequests;
            ViewBag.PendingRequests = pendingRequests;
            ViewBag.ApprovedRequests = approvedRequests;
            ViewBag.MyRequests = myRequests;
            ViewBag.RecentRequests = recentRequests;
            ViewBag.RequestsByStatus = requestsByStatus;

            return View();
        }




    }
}