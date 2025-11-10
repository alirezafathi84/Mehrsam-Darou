using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Collections.Generic;
using Mehrsam_Darou.Constants;

namespace Mehrsam_Darou.Controllers
{
    public class MaterialRequestController : BaseController
    {
        private readonly DarouAppContext _context;

        public MaterialRequestController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> RequestList(int? page, string searchKey, string status, string priority, string costCenter, string project)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<MaterialRequest> query = _context.MaterialRequests
                .Include(r => r.RequestType)
                .Include(r => r.Category)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.CostCenter)
                .Include(r => r.Project);

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

            if (!string.IsNullOrWhiteSpace(costCenter) && Guid.TryParse(costCenter, out Guid costCenterId))
            {
                query = query.Where(r => r.CostCenterId == costCenterId);
            }

            if (!string.IsNullOrWhiteSpace(project) && Guid.TryParse(project, out Guid projectId))
            {
                query = query.Where(r => r.ProjectId == projectId);
            }

            query = query.OrderByDescending(r => r.RequestDate);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<MaterialRequest>(items, total, pageNumber, pageSize);

            await PopulateFilterDropdowns();

            ViewBag.CurrentFilters = new
            {
                SearchKey = searchKey,
                Status = status,
                Priority = priority,
                CostCenter = costCenter,
                Project = project
            };

            return View(paginatedList);
        }

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
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.CostCenter)
                .Include(r => r.Project);

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
                Status = "در انتظار بررسی",
                Urgency = "عادی"
            };

            request.RequestNumber = await GenerateRequestNumber();

            return View(request);
        }
        // Add this method to your MaterialRequestController

        [HttpPost]
        public async Task<IActionResult> UpdateItemStatus([FromBody] UpdateItemStatusModel model)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "کاربر یافت نشد" });
                }

                // Check permissions
                if (!await CanUserProcessInventoryRequest(currentUserId.Value))
                {
                    return Json(new { success = false, message = "عدم دسترسی به این عملیات" });
                }

                var item = await _context.MaterialRequestItems
                    .Include(i => i.Request)
                    .FirstOrDefaultAsync(i => i.ItemId == model.ItemId);

                if (item == null)
                {
                    return Json(new { success = false, message = "قلم یافت نشد" });
                }

                // Validate status against CHECK constraint
                var allowedStatuses = new[] { "موجود", "ناموجود", "موجود جزئی", "نیاز به بررسی", "جایگزین موجود" };
                if (!allowedStatuses.Contains(model.Status))
                {
                    return Json(new { success = false, message = "وضعیت نامعتبر است" });
                }

                // Update both ItemStatus and AvailabilityStatus
                item.ItemStatus = model.Status;
                item.AvailabilityStatus = model.Status;

                await _context.SaveChangesAsync();

                // Add workflow history
                await AddWorkflowHistory(
                    item.RequestId,
                    "به‌روزرسانی وضعیت قلم",
                    model.Status,
                    $"وضعیت قلم '{item.ItemName}' به '{model.Status}' تغییر کرد",
                    currentUserId.Value
                );

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "وضعیت با موفقیت به‌روزرسانی شد",
                    newStatus = model.Status
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "خطا در به‌روزرسانی: " + ex.Message
                });
            }
        }

        // Model class (add at the bottom of your controller)
        public class UpdateItemStatusModel
        {
            public Guid? ItemId { get; set; }
            public string? Status { get; set; }
        }

        // Complete AddRequest POST Method - Copy this entire method

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRequest(MaterialRequest request, List<MaterialRequestItemViewModel> items)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    TempData["ErrorMessage"] = "کاربر یافت نشد";
                    return RedirectToAction("Login", "Account");
                }

                var fieldsToRemove = new[] {
            "Status", "Category", "RequestType", "WorkflowStage",
            "CreatedByNavigation", "RequestedByNavigation", "ApprovedByNavigation",
            "MaterialRequestItems", "CostCenter", "Project", "CreatedBy",
            "CreatedDate", "RequestedBy", "ApprovedBy", "ApprovalDate"
        };

                foreach (var field in fieldsToRemove)
                {
                    ModelState.Remove(field);
                }

                if (string.IsNullOrWhiteSpace(request.RequestTitle))
                {
                    ModelState.AddModelError("RequestTitle", "عنوان درخواست الزامی است");
                }

                if (request.CategoryId == Guid.Empty)
                {
                    ModelState.AddModelError("CategoryId", "انتخاب دسته‌بندی الزامی است");
                }

                if (request.RequestTypeId == Guid.Empty)
                {
                    ModelState.AddModelError("RequestTypeId", "انتخاب نوع درخواست الزامی است");
                }

                if (request.CostCenterId == Guid.Empty)
                {
                    ModelState.AddModelError("CostCenterId", "انتخاب مرکز هزینه الزامی است");
                }

                if (request.PriorityLevel <= 0)
                {
                    ModelState.AddModelError("PriorityLevel", "انتخاب سطح اولویت الزامی است");
                }

                bool hasValidItems = false;
                if (items != null && items.Any())
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        bool itemValid = true;

                        if (string.IsNullOrWhiteSpace(item.ItemName))
                        {
                            ModelState.AddModelError($"items[{i}].ItemName", "نام کالا الزامی است");
                            itemValid = false;
                        }

                        if (item.QuantityRequested <= 0)
                        {
                            ModelState.AddModelError($"items[{i}].QuantityRequested", "مقدار باید بیشتر از صفر باشد");
                            itemValid = false;
                        }

                        if (!item.UnitId.HasValue)
                        {
                            ModelState.AddModelError($"items[{i}].UnitId", "انتخاب واحد الزامی است");
                            itemValid = false;
                        }

                        if (itemValid)
                        {
                            hasValidItems = true;
                        }
                    }
                }

                if (!hasValidItems && (items == null || !items.Any()))
                {
                    ModelState.AddModelError("", "لطفاً حداقل یک کالای معتبر اضافه کنید");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateDropdowns();
                    ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                    TempData["ErrorMessage"] = "لطفاً خطاهای موجود در فرم را بررسی کنید";
                    return View(request);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    request.RequestedBy = currentUserId.Value;
                    request.CreatedBy = currentUserId.Value;
                    request.CreatedDate = DateTime.Now;
                    request.RequestDate = DateTime.Now;
                    request.Status = "در انتظار بررسی";
                    request.WorkflowStage = "ثبت درخواست";
                    request.IsActive = true;

                    if (string.IsNullOrWhiteSpace(request.Currency))
                        request.Currency = "IRR";

                    if (string.IsNullOrWhiteSpace(request.Urgency))
                        request.Urgency = "عادی";

                    request.RequestNumber = await GenerateRequestNumber();

                    _context.MaterialRequests.Add(request);

                    if (items != null && items.Any())
                    {
                        foreach (var itemVm in items)
                        {
                            // FIXED: Validate ItemGroupId before using it
                            Guid? itemGroupId = null;
                            if (itemVm.ItemGroupId.HasValue && itemVm.ItemGroupId.Value != Guid.Empty)
                            {
                                var itemGroupExists = await _context.ItemGroups
                                    .AnyAsync(ig => ig.ItemGroupId == itemVm.ItemGroupId.Value);

                                if (itemGroupExists)
                                {
                                    itemGroupId = itemVm.ItemGroupId;
                                }
                            }

                            var item = new MaterialRequestItem
                            {
                                ItemId = Guid.NewGuid(),
                                RequestId = request.RequestId,
                                MaterialId = itemVm.MaterialId,
                                ItemName = itemVm.ItemName,
                                ItemDescription = itemVm.ItemDescription,
                                QuantityRequested = itemVm.QuantityRequested,
                                UnitId = itemVm.UnitId,
                                UnitPriceEstimated = itemVm.UnitPriceEstimated,
                                ItemGroupId = itemGroupId, // Validated or null
                                IsCritical = itemVm.IsCritical,
                                ItemStatus = "در انتظار بررسی",
                                AvailabilityStatus = "نیاز به بررسی", // CHECK constraint value
                                ItemType = itemVm.MaterialId.HasValue ? "مواد" : "ملزومات" // CHECK constraint values
                            };

                            _context.MaterialRequestItems.Add(item);
                        }
                    }

                    var workflow = new RequestWorkflowHistory
                    {
                        WorkflowId = Guid.NewGuid(),
                        RequestId = request.RequestId,
                        Stage = "ثبت درخواست",
                        Status = "در انتظار بررسی",
                        Comments = "درخواست با موفقیت ثبت شد",
                        ProcessedBy = currentUserId.Value,
                        ProcessedDate = DateTime.Now,
                        IsActive = true
                    };

                    _context.RequestWorkflowHistories.Add(workflow);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"درخواست شماره {request.RequestNumber} با موفقیت ثبت شد";
                    return RedirectToAction(nameof(MyRequests));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "خطا در ثبت درخواست: " + ex.Message;
                    await PopulateDropdowns();
                    ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                    return View(request);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطای غیرمنتظره: " + ex.Message;
                await PopulateDropdowns();
                ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                return View(request);
            }
        }



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
                        await ProcessCheckInventoryMain(request, currentUserId.Value, comments);
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
                    default:
                        TempData["ErrorMessage"] = "عملیات نامعتبر";
                        await transaction.RollbackAsync();
                        return RedirectToAction(nameof(ProcessRequest), new { id = requestId });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "عملیات با موفقیت انجام شد";
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                var errorDetails = $"Status: '{request?.Status}', WorkflowStage: '{request?.WorkflowStage}', Action: '{action}'";
                TempData["ErrorMessage"] = $"خطا در پردازش: {ex.InnerException?.Message ?? ex.Message}";

             //   _logger?.LogError(ex, $"DB Update Error - {errorDetails}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"خطا در پردازش: {ex.Message}";
             //   _logger?.LogError(ex, $"Unexpected error processing request {requestId}");
            }

            return RedirectToAction(nameof(ProcessRequest), new { id = requestId });
        }




        // GET: MaterialRequest/InventoryRequestList
        public async Task<IActionResult> InventoryRequestList(int? page, string searchKey, string status, string priority, bool urgentOnly = false)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<MaterialRequest> query = _context.MaterialRequests
                .Include(r => r.RequestType)
                .Include(r => r.Category)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.CostCenter)
                .Include(r => r.Project)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Material)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Unit);

            // Filter to show requests that need inventory attention
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

            if (urgentOnly)
            {
                query = query.Where(r => r.IsUrgent == true || r.PriorityLevel <= 2);
            }

            // Order by priority and urgency first, then by date
            query = query.OrderByDescending(r => r.RequestDate);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<MaterialRequest>(items, total, pageNumber, pageSize);

            ViewBag.CurrentFilters = new
            {
                SearchKey = searchKey,
                Status = status,
                Priority = priority,
                UrgentOnly = urgentOnly
            };

            return View(paginatedList);
        }

        // GET: MaterialRequest/InventoryProcessRequest/5
        public async Task<IActionResult> InventoryProcessRequest(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "کاربر یافت نشد";
                return RedirectToAction("Login", "Account");
            }

            var request = await _context.MaterialRequests
                .Include(r => r.RequestType)
                .Include(r => r.Category)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.CostCenter)
                .Include(r => r.Project)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Material)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Unit)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.ItemGroup)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.SubstituteMaterial)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "درخواست یافت نشد";
                return RedirectToAction(nameof(InventoryRequestList));
            }

            // Check if user can process this request (inventory team)
            var canProcess = await CanUserProcessInventoryRequest(currentUserId.Value);
            ViewBag.CanProcess = canProcess;

            // Get workflow history
            ViewBag.WorkflowHistory = await _context.RequestWorkflowHistories
                .Where(w => w.RequestId == id)
                .Include(w => w.ProcessedByNavigation)
                .OrderBy(w => w.ProcessedDate)
                .ToListAsync();

            await PopulateProcessingDropdowns();
            return View(request);
        }

        // Helper method to check inventory processing permissions
        private async Task<bool> CanUserProcessInventoryRequest(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Team != null)
            {
                // Allow processing for inventory team or managers
                return user.Team.Inventory == true ||
                       user.Team.ManagmentDashboard == true;
            }

            return false;
        }

        // POST: MaterialRequest/BulkInventoryCheck
        [HttpPost]
        public async Task<IActionResult> BulkInventoryCheck([FromBody] List<Guid> requestIds)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "کاربر یافت نشد" });
                }

                if (!await CanUserProcessInventoryRequest(currentUserId.Value))
                {
                    return Json(new { success = false, message = "عدم دسترسی" });
                }

                int processed = 0;
                var errors = new List<string>();

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var requestId in requestIds)
                    {
                        var request = await _context.MaterialRequests
                            .Include(r => r.MaterialRequestItems)
                                .ThenInclude(i => i.Material)
                            .FirstOrDefaultAsync(r => r.RequestId == requestId);

                        if (request != null && (request.Status == "در انتظار بررسی" || request.Status == "در حال بررسی"))
                        {
                            await ProcessCheckInventoryDetailed(request, currentUserId.Value, "بررسی گروهی موجودی");
                            processed++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = $"{processed} درخواست پردازش شد", processed = processed });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "خطا در پردازش گروهی: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطای سیستم: " + ex.Message });
            }
        }




        private async Task<string> GenerateRequestNumber()
        {
            var persianCalendar = new PersianCalendar();
            var currentDate = DateTime.Now;
            var persianYear = persianCalendar.GetYear(currentDate);
            var yearSuffix = (persianYear % 100).ToString("00");
            var yearPattern = $"REQ-WH-{yearSuffix}";

            var lastRequestNumber = await _context.MaterialRequests
                .Where(r => r.RequestNumber.StartsWith(yearPattern))
                .OrderByDescending(r => r.RequestNumber)
                .Select(r => r.RequestNumber)
                .FirstOrDefaultAsync();

            int nextSequentialNumber = 1;
            if (!string.IsNullOrEmpty(lastRequestNumber))
            {
                var sequentialPart = lastRequestNumber.Substring(lastRequestNumber.Length - 5);
                if (int.TryParse(sequentialPart, out int lastNumber))
                {
                    nextSequentialNumber = lastNumber + 1;
                }
            }

            return $"REQ-WH-{yearSuffix}{nextSequentialNumber:00000}";
        }


        private async Task ProcessCheckInventoryMain(MaterialRequest request, Guid processedBy, string comments)
        {
            bool allItemsAvailable = true;
            bool someItemsAvailable = false;

            foreach (var item in request.MaterialRequestItems)
            {
                if (item.Material != null)
                {
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

            // ✅ استفاده از مقادیر صحیح
            if (allItemsAvailable)
            {
                request.Status = MaterialRequestStatus.Approved;
                request.WorkflowStage = MaterialRequestWorkflowStage.Delivery; // "تحویل"
                await SendNotificationToRequester(request, "درخواست شما آماده تحویل است");
            }
            else if (someItemsAvailable && request.IsSubstituteAllowed == true)
            {
                request.Status = MaterialRequestStatus.NeedSubstitute;
                request.WorkflowStage = MaterialRequestWorkflowStage.FindingSubstitute; // "جستجوی جایگزین"
                await SendNotificationToRequester(request, "برخی اقلام موجود نیستند");
            }
            else
            {
                request.Status = MaterialRequestStatus.InProcurement;
                request.WorkflowStage = MaterialRequestWorkflowStage.PurchaseRequest; // "درخواست خرید"
                await SendNotificationToProcurement(request, "درخواست نیاز به خرید دارد");
            }

            await AddWorkflowHistory(request.RequestId, "بررسی موجودی", request.Status,
                comments ?? "موجودی بررسی شد", processedBy);
        }

        private async Task ProcessApproval(MaterialRequest request, Guid processedBy, string comments)
        {
            request.Status = MaterialRequestStatus.Approved;
            request.ApprovedBy = processedBy;
            request.ApprovalDate = DateTime.Now;
            request.ApprovalStatus = "تأیید شده";

            if (request.MaterialRequestItems.Any(i => i.ItemStatus == "ناموجود"))
            {
                request.WorkflowStage = MaterialRequestWorkflowStage.PurchaseRequest; // "درخواست خرید"
                await SendNotificationToProcurement(request, "درخواست تأیید شده");
            }
            else
            {
                request.WorkflowStage = MaterialRequestWorkflowStage.Delivery; // "تحویل"
                await SendNotificationToRequester(request, "درخواست تأیید شد");
            }

            await AddWorkflowHistory(request.RequestId, "تأیید", MaterialRequestStatus.Approved,
                comments ?? "تأیید شد", processedBy);
        }

        private async Task ProcessRejection(MaterialRequest request, Guid processedBy, string comments)
        {
            request.Status = MaterialRequestStatus.Rejected;
            request.ApprovedBy = processedBy;
            request.ApprovalDate = DateTime.Now;
            request.ApprovalStatus = "رد شده";
            // ✅ "رد شده" در constraint نیست، پس از یکی از مقادیر موجود استفاده می‌کنیم
            request.WorkflowStage = MaterialRequestWorkflowStage.Completed; // "تکمیل"

            await SendNotificationToRequester(request, "درخواست رد شد");
            await AddWorkflowHistory(request.RequestId, "رد", MaterialRequestStatus.Rejected,
                comments ?? "رد شد", processedBy);
        }

        private async Task ProcessFindSubstitute(MaterialRequest request, Guid processedBy, string comments)
        {
            request.Status = MaterialRequestStatus.NeedSubstitute;
            request.WorkflowStage = MaterialRequestWorkflowStage.FindingSubstitute; // "جستجوی جایگزین"

            await SendNotificationToRequester(request, "در حال جستجوی جایگزین");
            await AddWorkflowHistory(request.RequestId, "جستجوی جایگزین", request.Status,
                comments ?? "شروع جستجو", processedBy);
        }

        private async Task ProcessRequestCeoApproval(MaterialRequest request, Guid processedBy, string comments)
        {
            request.Status = MaterialRequestStatus.WaitingCeoApproval;
            request.WorkflowStage = MaterialRequestWorkflowStage.CeoApproval; // "تأیید مدیرعامل"

            var ceoUser = await _context.Users
                .Join(_context.Teams, u => u.TeamId, t => t.Id, (u, t) => new { User = u, Team = t })
                .Where(x => x.Team.ManagmentDashboard == true)
                .Select(x => x.User)
                .FirstOrDefaultAsync();

            if (ceoUser != null)
            {
                await SendNotificationToUser(ceoUser.Id, "تأیید مدیرعامل",
                    $"درخواست {request.RequestNumber} نیاز به تأیید دارد");
            }

            await AddWorkflowHistory(request.RequestId, "تأیید مدیرعامل", request.Status,
                comments ?? "ارسال به مدیرعامل", processedBy);
        }

        //private async Task ProcessDelivery(MaterialRequest request, Guid processedBy, string comments)
        //{
        //    request.Status = MaterialRequestStatus.Delivered;
        //    request.WorkflowStage = MaterialRequestWorkflowStage.Delivery; // "تحویل"
        //    request.CompletionDate = DateTime.Now;

        //    await SendNotificationToRequester(request, "تحویل شد");
        //    await AddWorkflowHistory(request.RequestId, "تحویل", MaterialRequestStatus.Delivered,
        //        comments ?? "تحویل داده شد", processedBy);
        //}


        private async Task ProcessDelivery(MaterialRequest request, Guid processedBy, string comments)
        {
            bool allItemsDelivered = true;
            bool someItemsDelivered = false;
            var deliveryNotes = new List<string>();

            // Calculate date for expiry check (30 days from now)
            var minExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));

            // Process each item and reduce inventory
            foreach (var item in request.MaterialRequestItems)
            {
                if (item.MaterialId.HasValue && item.QuantityRequested > 0)
                {
                    // Get the requested unit's conversion factor
                    var requestedUnit = await _context.Units
                        .FirstOrDefaultAsync(u => u.UnitId == item.UnitId);

                    if (requestedUnit == null)
                    {
                        deliveryNotes.Add($"{item.ItemName}: خطا - واحد یافت نشد");
                        allItemsDelivered = false;
                        continue;
                    }

                    // Get available batches
                    var availableBatches = await _context.MaterialBatches
                        .Include(b => b.Unit) // Include unit for conversion
                        .Where(b => b.MaterialId == item.MaterialId &&
                                   (b.Status == "قرنطینه" || b.Status == "آزاد شده") &&
                                   b.CurrentQuantity > 0 &&
                                   (!b.ExpiryDate.HasValue || b.ExpiryDate.Value > minExpiryDate))
                        .OrderBy(b => b.BatchId)
                        .ThenBy(b => b.ExpiryDate)
                        .ToListAsync();

                    // Convert requested quantity to base unit for comparison
                    decimal requestedQuantityInBaseUnit = item.QuantityRequested * (requestedUnit.ConversionFactor ?? 1);
                    decimal remainingQuantity = requestedQuantityInBaseUnit;
                    decimal deliveredQuantity = 0;

                    foreach (var batch in availableBatches)
                    {
                        if (remainingQuantity <= 0)
                            break;

                        // Get batch unit's conversion factor
                        var batchUnit = batch.Unit;
                        if (batchUnit == null)
                        {
                            continue;
                        }

                        // Convert batch quantity to base unit
                        decimal batchQuantityInBaseUnit = batch.CurrentQuantity * (batchUnit.ConversionFactor ?? 1);

                        // Calculate how much to deduct in base unit
                        decimal quantityToDeductInBaseUnit = Math.Min(batchQuantityInBaseUnit, remainingQuantity);

                        if (quantityToDeductInBaseUnit > 0)
                        {
                            // Convert back to batch unit for deduction
                            decimal quantityToDeductInBatchUnit = quantityToDeductInBaseUnit / (batchUnit.ConversionFactor ?? 1);

                            // Reduce batch quantity
                            batch.CurrentQuantity -= quantityToDeductInBatchUnit;
                            remainingQuantity -= quantityToDeductInBaseUnit;
                            deliveredQuantity += quantityToDeductInBaseUnit;

                            // Update batch status if fully consumed
                            if (batch.CurrentQuantity <= 0.001m) // Small threshold for decimal precision
                            {
                                batch.Status = "مصرف شده";
                                batch.CurrentQuantity = 0;
                            }
                        }
                    }

                    // Convert delivered quantity back to requested unit for display
                    decimal deliveredInRequestedUnit = deliveredQuantity / (requestedUnit.ConversionFactor ?? 1);

                    // Update item status
                    if (remainingQuantity > 0.001m) // Partial delivery
                    {
                        item.ItemStatus = "تحویل جزئی";
                        item.AvailabilityStatus = "تحویل جزئی";
                        deliveryNotes.Add($"{item.ItemName}: {deliveredInRequestedUnit:F3} {requestedUnit.UnitSymbol} از {item.QuantityRequested} {requestedUnit.UnitSymbol} تحویل داده شد");
                        allItemsDelivered = false;
                        someItemsDelivered = true;
                    }
                    else // Full delivery
                    {
                        item.ItemStatus = "تحویل شده";
                        item.AvailabilityStatus = "موجود";
                        deliveryNotes.Add($"{item.ItemName}: {deliveredInRequestedUnit:F3} {requestedUnit.UnitSymbol} تحویل داده شد");
                        someItemsDelivered = true;
                    }
                }
                else
                {
                    // Items without MaterialId (non-inventory items)
                    item.ItemStatus = "تحویل شده";
                    someItemsDelivered = true;
                }
            }

            // Update request status
            request.Status = MaterialRequestStatus.Delivered;
            request.WorkflowStage = MaterialRequestWorkflowStage.Delivery;
            request.CompletionDate = DateTime.Now;

            // Send notification with delivery details
            var deliveryMessage = allItemsDelivered
                ? "تمام اقلام درخواست شما تحویل داده شد"
                : $"تحویل: {string.Join(", ", deliveryNotes)}";

            await SendNotificationToRequester(request, deliveryMessage);

            // Add workflow history with detailed notes
            var historyComments = comments ?? "تحویل داده شد و موجودی کسر گردید";
            if (deliveryNotes.Any())
            {
                historyComments += "\n" + string.Join("\n", deliveryNotes);
            }

            await AddWorkflowHistory(
                request.RequestId,
                "تحویل",
                MaterialRequestStatus.Delivered,
                historyComments,
                processedBy
            );
        }


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
                $"درخواست {request.RequestNumber}", message);
        }

        private async Task SendNotificationToProcurement(MaterialRequest request, string customMessage = null)
        {
            var procurementUsers = await _context.Users
                .Join(_context.Teams, u => u.TeamId, t => t.Id, (u, t) => new { User = u, Team = t })
                .Where(x => x.Team.BuyCommercial == true)
                .Select(x => x.User)
                .ToListAsync();

            var message = customMessage ?? $"درخواست {request.RequestNumber}";

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

        // ========== FIXED: EditRequest GET ==========
        public async Task<IActionResult> EditRequest(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "کاربر یافت نشد";
                return RedirectToAction("Login", "Account");
            }

            var request = await _context.MaterialRequests
                .Include(r => r.RequestType)
                .Include(r => r.Category)
                .Include(r => r.CostCenter)
                .Include(r => r.Project)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Material)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Unit)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.ItemGroup)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "درخواست یافت نشد";
                return RedirectToAction(nameof(MyRequests));
            }

            if (request.RequestedBy != currentUserId.Value && !await HasEditPermission(currentUserId.Value))
            {
                TempData["ErrorMessage"] = "عدم دسترسی";
                return RedirectToAction(nameof(ProcessRequest), new { id });
            }

            if (request.Status != "در انتظار بررسی")
            {
                TempData["WarningMessage"] = $"وضعیت: {request.Status}";
            }

            // FIXED: Pass selections
            await PopulateDropdowns(
                selectedCategoryId: request.CategoryId,
                selectedRequestTypeId: request.RequestTypeId,
                selectedCostCenterId: request.CostCenterId,
                selectedProjectId: request.ProjectId
            );

            return View(request);
        }

        // ========== FIXED: EditRequest POST ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequest(MaterialRequest request, List<MaterialRequestItemViewModel> items)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    TempData["ErrorMessage"] = "کاربر یافت نشد";
                    return RedirectToAction("Login", "Account");
                }

                var existingRequest = await _context.MaterialRequests
                    .Include(r => r.MaterialRequestItems)
                    .FirstOrDefaultAsync(r => r.RequestId == request.RequestId);

                if (existingRequest == null)
                {
                    TempData["ErrorMessage"] = "درخواست یافت نشد";
                    return RedirectToAction(nameof(MyRequests));
                }

                if (existingRequest.RequestedBy != currentUserId.Value && !await HasEditPermission(currentUserId.Value))
                {
                    TempData["ErrorMessage"] = "عدم دسترسی";
                    return RedirectToAction(nameof(ProcessRequest), new { id = request.RequestId });
                }

                if (existingRequest.Status != "در انتظار بررسی")
                {
                    TempData["ErrorMessage"] = "قابل ویرایش نیست";
                    return RedirectToAction(nameof(ProcessRequest), new { id = request.RequestId });
                }

                var fieldsToRemove = new[] {
                    "Status", "Category", "RequestType", "WorkflowStage",
                    "CreatedByNavigation", "RequestedByNavigation", "ApprovedByNavigation",
                    "MaterialRequestItems", "CostCenter", "Project", "CreatedBy",
                    "CreatedDate", "RequestedBy", "ApprovedBy", "ApprovalDate", "RequestNumber"
                };

                foreach (var field in fieldsToRemove)
                {
                    ModelState.Remove(field);
                }

                if (string.IsNullOrWhiteSpace(request.RequestTitle))
                {
                    ModelState.AddModelError("RequestTitle", "عنوان الزامی است");
                }

                if (request.CategoryId == Guid.Empty)
                {
                    ModelState.AddModelError("CategoryId", "دسته‌بندی الزامی است");
                }

                if (request.RequestTypeId == Guid.Empty)
                {
                    ModelState.AddModelError("RequestTypeId", "نوع درخواست الزامی است");
                }

                if (request.CostCenterId == Guid.Empty)
                {
                    ModelState.AddModelError("CostCenterId", "مرکز هزینه الزامی است");
                }

                if (request.PriorityLevel <= 0)
                {
                    ModelState.AddModelError("PriorityLevel", "اولویت الزامی است");
                }

                bool hasValidItems = false;
                if (items != null && items.Any())
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        bool itemValid = true;

                        if (string.IsNullOrWhiteSpace(item.ItemName))
                        {
                            ModelState.AddModelError($"items[{i}].ItemName", "نام کالا الزامی است");
                            itemValid = false;
                        }

                        if (item.QuantityRequested <= 0)
                        {
                            ModelState.AddModelError($"items[{i}].QuantityRequested", "مقدار باید بیشتر از صفر باشد");
                            itemValid = false;
                        }

                        if (!item.UnitId.HasValue)
                        {
                            ModelState.AddModelError($"items[{i}].UnitId", "واحد الزامی است");
                            itemValid = false;
                        }

                        if (itemValid)
                        {
                            hasValidItems = true;
                        }
                    }
                }

                if (!hasValidItems)
                {
                    ModelState.AddModelError("", "حداقل یک کالا الزامی است");
                }

                if (!ModelState.IsValid)
                {
                    // FIXED: Maintain selections on error
                    await PopulateDropdowns(
                        selectedCategoryId: request.CategoryId,
                        selectedRequestTypeId: request.RequestTypeId,
                        selectedCostCenterId: request.CostCenterId,
                        selectedProjectId: request.ProjectId
                    );

                    ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                    TempData["ErrorMessage"] = "خطا در اعتبارسنجی";
                    return View(request);
                }

                existingRequest.RequestTitle = request.RequestTitle;
                existingRequest.Description = request.Description;
                existingRequest.CategoryId = request.CategoryId;
                existingRequest.RequestTypeId = request.RequestTypeId;
                existingRequest.CostCenterId = request.CostCenterId;
                existingRequest.ProjectId = request.ProjectId;
                existingRequest.Department = request.Department;
                existingRequest.BudgetCode = request.BudgetCode;
                existingRequest.PriorityLevel = request.PriorityLevel;
                existingRequest.RequiredDate = request.RequiredDate;
                existingRequest.Urgency = request.Urgency;
                existingRequest.IsSubstituteAllowed = request.IsSubstituteAllowed;
                existingRequest.Currency = request.Currency;

                _context.MaterialRequestItems.RemoveRange(existingRequest.MaterialRequestItems);

                foreach (var itemVm in items)
                {
                    var item = new MaterialRequestItem
                    {
                        ItemId = Guid.NewGuid(),
                        RequestId = existingRequest.RequestId,
                        MaterialId = itemVm.MaterialId,
                        ItemName = itemVm.ItemName,
                        ItemDescription = itemVm.ItemDescription,
                        QuantityRequested = itemVm.QuantityRequested,
                        UnitId = itemVm.UnitId,
                        UnitPriceEstimated = itemVm.UnitPriceEstimated,
                        ItemGroupId = itemVm.ItemGroupId,
                        IsCritical = itemVm.IsCritical,
                        ItemStatus = "در انتظار بررسی",
                        AvailabilityStatus = "نیاز به بررسی", // FIXED: Using allowed constraint value
                        ItemType = itemVm.MaterialId.HasValue ? "مواد" : "ملزومات"
                    };

                    _context.MaterialRequestItems.Add(item);
                }

                await _context.SaveChangesAsync();

                await AddWorkflowHistory(existingRequest.RequestId, "ویرایش", existingRequest.Status,
                    "ویرایش شد", currentUserId.Value);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "به‌روزرسانی شد";
                return RedirectToAction(nameof(MyRequests));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا: " + ex.Message;

                // FIXED: Maintain selections on exception
                await PopulateDropdowns(
                    selectedCategoryId: request.CategoryId,
                    selectedRequestTypeId: request.RequestTypeId,
                    selectedCostCenterId: request.CostCenterId,
                    selectedProjectId: request.ProjectId
                );

                ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                return View(request);
            }
        }

        private async Task<bool> HasEditPermission(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Team != null)
            {
                return user.Team.BuyCommercial == true ||
                       user.Team.ManagmentDashboard == true;
            }

            return false;
        }

        // FIXED: PopulateDropdowns with proper ItemGroups from MaterialCategories
        private async Task PopulateDropdowns(
            Guid? selectedCategoryId = null,
            Guid? selectedRequestTypeId = null,
            Guid? selectedCostCenterId = null,
            Guid? selectedProjectId = null)
        {
            var categories = await _context.RequestCategories
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var requestTypes = await _context.RequestTypes
                .Where(t => t.IsActive == true)
                .OrderBy(t => t.TypeName)
                .ToListAsync();

            var costCenters = await _context.CostCenters
                .Where(cc => cc.IsActive == true)
                .OrderBy(cc => cc.CostCenterName)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => p.IsActive == true && (p.Status == "فعال" || p.Status == null))
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            // FIXED: ItemGroups should load from MaterialCategories (not ItemGroups table)
            var itemGroups = await _context.MaterialCategories
                .Where(ig => ig.IsActive == true)
                .OrderBy(ig => ig.CategoryName)
                .ToListAsync();

            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            var materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .ToListAsync();

            // FIXED: Pass selected values as 4th parameter
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", selectedCategoryId);
            ViewBag.RequestTypes = new SelectList(requestTypes, "TypeId", "TypeName", selectedRequestTypeId);
            ViewBag.CostCenters = new SelectList(costCenters, "CostCenterId", "CostCenterName", selectedCostCenterId);
            ViewBag.Projects = new SelectList(projects, "ProjectId", "ProjectName", selectedProjectId);
            // FIXED: ItemGroups now uses MaterialCategories with CategoryId and CategoryName
            ViewBag.ItemGroups = new SelectList(itemGroups, "CategoryId", "CategoryName");
            ViewBag.Units = new SelectList(units, "UnitId", "UnitName");
            ViewBag.Materials = new SelectList(materials, "MaterialId", "MaterialName");

            ViewBag.PriorityLevels = new SelectList(new[]
            {
                new { Value = 1, Text = "بحرانی" },
                new { Value = 2, Text = "بالا" },
                new { Value = 3, Text = "متوسط" },
                new { Value = 4, Text = "پایین" },
                new { Value = 5, Text = "خیلی پایین" }
            }, "Value", "Text");
        }

        private async Task PopulateFilterDropdowns()
        {
            var costCenters = await _context.CostCenters
                .Where(cc => cc.IsActive == true)
                .OrderBy(cc => cc.CostCenterName)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "", Text = "همه" },
                new { Value = "در انتظار بررسی", Text = "در انتظار بررسی" },
                new { Value = "در حال بررسی", Text = "در حال بررسی" },
                new { Value = "تأیید شده", Text = "تأیید شده" },
                new { Value = "رد شده", Text = "رد شده" },
                new { Value = "در حال تأمین", Text = "در حال تأمین" },
                new { Value = "تحویل شده", Text = "تحویل شده" },
                new { Value = "تکمیل شده", Text = "تکمیل شده" },
                new { Value = "منتظر تأیید مدیرعامل", Text = "منتظر تأیید مدیرعامل" }
            }, "Value", "Text");

            ViewBag.PriorityList = new SelectList(new[]
            {
                new { Value = "", Text = "همه" },
                new { Value = "1", Text = "بحرانی" },
                new { Value = "2", Text = "بالا" },
                new { Value = "3", Text = "متوسط" },
                new { Value = "4", Text = "پایین" },
                new { Value = "5", Text = "خیلی پایین" }
            }, "Value", "Text");

            ViewBag.CostCentersList = new SelectList(costCenters, "CostCenterId", "CostCenterName");
            ViewBag.ProjectsList = new SelectList(projects, "ProjectId", "ProjectName");
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

            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            // FIXED: ItemGroups from MaterialCategories
            var itemGroups = await _context.MaterialCategories
                .Where(ig => ig.IsActive == true)
                .OrderBy(ig => ig.CategoryName)
                .ToListAsync();

            ViewBag.Materials = new SelectList(materials, "MaterialId", "MaterialName");
            ViewBag.Suppliers = new SelectList(suppliers, "SupplierId", "SupplierName");
            ViewBag.Units = new SelectList(units, "UnitId", "UnitName");
            // FIXED: Use CategoryId and CategoryName from MaterialCategories
            ViewBag.ItemGroups = new SelectList(itemGroups, "CategoryId", "CategoryName");
        }

        public async Task<IActionResult> RequestDashboard()
        {
            var currentUserId = GetCurrentUserId();

            var totalRequests = await _context.MaterialRequests.CountAsync();
            var pendingRequests = await _context.MaterialRequests.CountAsync(r => r.Status == "در انتظار بررسی");
            var approvedRequests = await _context.MaterialRequests.CountAsync(r => r.Status == "تأیید شده");
            var myRequests = currentUserId.HasValue ?
                await _context.MaterialRequests.CountAsync(r => r.RequestedBy == currentUserId) : 0;

            var recentRequests = await _context.MaterialRequests
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.CostCenter)
                .Include(r => r.Project)
                .OrderByDescending(r => r.RequestDate)
                .Take(10)
                .ToListAsync();

            var requestsByStatus = await _context.MaterialRequests
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var requestsByCostCenter = await _context.MaterialRequests
                .Where(r => r.CostCenterId.HasValue)
                .Include(r => r.CostCenter)
                .GroupBy(r => r.CostCenter.CostCenterName)
                .Select(g => new { CostCenter = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.TotalRequests = totalRequests;
            ViewBag.PendingRequests = pendingRequests;
            ViewBag.ApprovedRequests = approvedRequests;
            ViewBag.MyRequests = myRequests;
            ViewBag.RecentRequests = recentRequests;
            ViewBag.RequestsByStatus = requestsByStatus;
            ViewBag.RequestsByCostCenter = requestsByCostCenter;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMaterialsByGroup(Guid itemGroupId)
        {
            // Get materials filtered by category (ItemGroup is actually MaterialCategory)
            var materials = await _context.RawMaterials
                .Where(m => m.IsActive == true && m.CategoryId == itemGroupId)
                .OrderBy(m => m.MaterialName)
                .Select(m => new { value = m.MaterialId, text = m.MaterialName })
                .ToListAsync();

            return Json(materials);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnits()
        {
            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .Select(u => new { value = u.UnitId, text = u.UnitName })
                .ToListAsync();

            return Json(units);
        }

        public async Task<IActionResult> ProcessRequest(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "کاربر یافت نشد";
                return RedirectToAction("Login", "Account");
            }

            var request = await _context.MaterialRequests
                .Include(r => r.RequestType)
                .Include(r => r.Category)
                .Include(r => r.CostCenter)
                .Include(r => r.Project)
                .Include(r => r.RequestedByNavigation)
                    .ThenInclude(u => u.Team)
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.Unit)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.ItemGroup)
                .Include(r => r.MaterialRequestItems)
                    .ThenInclude(i => i.SubstituteMaterial)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "درخواست یافت نشد";
                return RedirectToAction(nameof(RequestList));
            }

            var workflowHistory = await _context.RequestWorkflowHistories
                .Where(w => w.RequestId == id)
                .Include(w => w.ProcessedByNavigation)
                .OrderByDescending(w => w.ProcessedDate)
                .ToListAsync();

            ViewBag.WorkflowHistory = workflowHistory;

            var canProcess = await CanUserProcessRequest(currentUserId.Value, request);
            ViewBag.CanProcess = canProcess;

            var canCheckInventory = await CanUserProcessInventoryRequest(currentUserId.Value);
            ViewBag.CanCheckInventory = canCheckInventory;

            await PopulateProcessingDropdowns();

            return View(request);
        }

        private async Task<bool> CanUserProcessRequest(Guid userId, MaterialRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Team == null)
                return false;

            if (user.Team.ManagmentDashboard == true)
                return true;

            if (user.Team.BuyCommercial == true &&
                (request.Status == "نیاز به خرید" || request.Status == "در حال تأمین"))
                return true;

            if (user.Team.Inventory == true &&
                (request.Status == "در انتظار بررسی" || request.Status == "نیاز به جایگزین"))
                return true;

            return false;
        }

        //private async Task<bool> CanUserProcessInventoryRequest(Guid userId)
        //{
        //    var user = await _context.Users
        //        .Include(u => u.Team)
        //        .FirstOrDefaultAsync(u => u.Id == userId);

        //    if (user?.Team == null)
        //        return false;

        //    return user.Team.Inventory == true || user.Team.ManagmentDashboard == true;
        //}

        [HttpPost]
        public async Task<IActionResult> CheckItemStock([FromBody] CheckStockModel model)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "کاربر یافت نشد" });
                }

                if (!await CanUserProcessInventoryRequest(currentUserId.Value))
                {
                    return Json(new { success = false, message = "عدم دسترسی" });
                }

                var item = await _context.MaterialRequestItems
                    .Include(i => i.Request)
                    .FirstOrDefaultAsync(i => i.ItemId == model.ItemId);

                if (item == null)
                {
                    return Json(new { success = false, message = "قلم یافت نشد" });
                }

                if (!item.MaterialId.HasValue)
                {
                    return Json(new { success = false, message = "ماده مشخص نشده" });
                }

                var availableStock = await _context.MaterialBatches
                    .Where(b => b.MaterialId == item.MaterialId &&
                               b.Status == "آزاد شده" &&
                               b.CurrentQuantity > 0 &&
                               (!b.ExpiryDate.HasValue || b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) > DateTime.Now.AddDays(30)))
                    .SumAsync(b => b.CurrentQuantity);

                item.StockQuantity = availableStock;

                string newStatus;
                if (availableStock >= item.QuantityRequested)
                {
                    newStatus = "موجود";
                }
                else if (availableStock > 0)
                {
                    newStatus = "موجود جزئی";
                }
                else
                {
                    newStatus = "ناموجود";
                }

                item.ItemStatus = newStatus;
                item.AvailabilityStatus = newStatus;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    stockQuantity = availableStock,
                    status = newStatus,
                    message = "موجودی بررسی شد"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetItemSubstitute([FromBody] SetSubstituteModel model)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "کاربر یافت نشد" });
                }

                if (!await CanUserProcessInventoryRequest(currentUserId.Value))
                {
                    return Json(new { success = false, message = "عدم دسترسی" });
                }

                var item = await _context.MaterialRequestItems
                    .Include(i => i.Request)
                    .FirstOrDefaultAsync(i => i.ItemId == model.ItemId);

                if (item == null)
                {
                    return Json(new { success = false, message = "قلم یافت نشد" });
                }

                if (item.Request.IsSubstituteAllowed != true)
                {
                    return Json(new { success = false, message = "جایگزینی مجاز نیست" });
                }

                item.SubstituteMaterialId = model.SubstituteMaterialId;
                item.SubstituteNotes = model.Notes;

                await _context.SaveChangesAsync();

                await AddWorkflowHistory(item.RequestId, "جایگزین",
                    "تعیین شد",
                    $"جایگزین برای '{item.ItemName}': {model.Notes}",
                    currentUserId.Value);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "جایگزین تعیین شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا: " + ex.Message });
            }
        }

        private async Task ProcessCheckInventoryDetailed(MaterialRequest request, Guid processedBy, string comments)
        {
            bool allItemsAvailable = true;
            bool someItemsAvailable = false;
            int totalItems = request.MaterialRequestItems.Count;
            int availableItems = 0;
            int partialItems = 0;
            int unavailableItems = 0;

            foreach (var item in request.MaterialRequestItems)
            {
                decimal availableStock = 0;

                if (item.MaterialId.HasValue)
                {
                    availableStock = await _context.MaterialBatches
                        .Where(b => b.MaterialId == item.MaterialId &&
                                   b.Status == "آزاد شده" &&
                                   b.CurrentQuantity > 0 &&
                                   (!b.ExpiryDate.HasValue || b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) > DateTime.Now.AddDays(30)))
                        .SumAsync(b => b.CurrentQuantity);
                }

                item.StockQuantity = availableStock;

                if (availableStock >= item.QuantityRequested)
                {
                    item.ItemStatus = "موجود";
                    item.AvailabilityStatus = "موجود";
                    availableItems++;
                    someItemsAvailable = true;
                }
                else if (availableStock > 0)
                {
                    item.ItemStatus = "موجود جزئی";
                    item.AvailabilityStatus = "موجود جزئی";
                    partialItems++;
                    allItemsAvailable = false;
                    someItemsAvailable = true;
                }
                else
                {
                    item.ItemStatus = "ناموجود";
                    item.AvailabilityStatus = "ناموجود";
                    unavailableItems++;
                    allItemsAvailable = false;
                }
            }

            if (allItemsAvailable)
            {
                request.Status = "آماده تحویل";
                request.WorkflowStage = "آماده برای تحویل";
                await SendNotificationToRequester(request, "آماده تحویل");
            }
            else if (someItemsAvailable && request.IsSubstituteAllowed == true)
            {
                request.Status = "نیاز به جایگزین";
                request.WorkflowStage = "جستجوی ماده جایگزین";
                await SendNotificationToRequester(request, $"{availableItems} موجود، {partialItems} جزئی، {unavailableItems} ناموجود");
            }
            else if (someItemsAvailable)
            {
                request.Status = "آماده تحویل جزئی";
                request.WorkflowStage = "تحویل جزئی";
                await SendNotificationToRequester(request, $"{availableItems} از {totalItems} موجود");
            }
            else
            {
                request.Status = "نیاز به خرید";
                request.WorkflowStage = "ارسال به واحد خرید";
                await SendNotificationToProcurement(request, "نیاز به خرید");
            }

            var detailedComments = $"{comments}. {availableItems} موجود، {partialItems} جزئی، {unavailableItems} ناموجود";

            await AddWorkflowHistory(request.RequestId, "بررسی موجودی", request.Status,
                detailedComments, processedBy);
        }

        public class CheckStockModel
        {
            public Guid ItemId { get; set; }
        }

        public class SetSubstituteModel
        {
            public Guid ItemId { get; set; }
            public Guid SubstituteMaterialId { get; set; }
            public string Notes { get; set; }
        }

      

        public class CancelRequestModel
        {
            public Guid RequestId { get; set; }
        }
    }

    public class MaterialRequestItemViewModel
    {
        public Guid? MaterialId { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public decimal QuantityRequested { get; set; }
        public Guid? UnitId { get; set; }
        public decimal? UnitPriceEstimated { get; set; }
        public Guid? ItemGroupId { get; set; }
        public bool IsCritical { get; set; }
    }
}