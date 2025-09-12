using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

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

            // Populate filter dropdowns
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
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.CostCenter)
                .Include(r => r.Project);

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
                Status = "در انتظار بررسی",
                Urgency = "عادی"
            };

            // Generate request number with Persian calendar
            request.RequestNumber = await GenerateRequestNumber();

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRequest(MaterialRequest request, List<MaterialRequestItemViewModel> items)
        {
            try
            {
                // Debug: Log the start of the method
                System.Diagnostics.Debug.WriteLine("=== AddRequest POST Started ===");
                System.Diagnostics.Debug.WriteLine($"Request ID: {request?.RequestId}");
                System.Diagnostics.Debug.WriteLine($"Request Title: {request?.RequestTitle}");
                System.Diagnostics.Debug.WriteLine($"Items Count: {items?.Count ?? 0}");

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Current user ID is null");
                    TempData["ErrorMessage"] = "کاربر یافت نشد";
                    return RedirectToAction("Login", "Account");
                }

                System.Diagnostics.Debug.WriteLine($"Current User ID: {currentUserId}");

                // Remove navigation properties from ModelState to avoid validation errors
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

                // Debug: Log ModelState before validation
                System.Diagnostics.Debug.WriteLine($"ModelState IsValid: {ModelState.IsValid}");
                System.Diagnostics.Debug.WriteLine($"ModelState Error Count: {ModelState.ErrorCount}");

                // Manual validation for required fields
                if (string.IsNullOrWhiteSpace(request.RequestTitle))
                {
                    ModelState.AddModelError("RequestTitle", "عنوان درخواست الزامی است");
                    System.Diagnostics.Debug.WriteLine("ERROR: RequestTitle is empty");
                }

                //if (request.CategoryId!= null)
                //{
                //    ModelState.AddModelError("CategoryId", "انتخاب دسته‌بندی الزامی است");
                //    System.Diagnostics.Debug.WriteLine("ERROR: CategoryId is null");
                //}

                //if (request.RequestType!= null)
                //{
                //    ModelState.AddModelError("RequestTypeId", "انتخاب نوع درخواست الزامی است");
                //    System.Diagnostics.Debug.WriteLine("ERROR: RequestTypeId is null");
                //}

                if (!request.CostCenterId.HasValue)
                {
                    ModelState.AddModelError("CostCenterId", "انتخاب مرکز هزینه الزامی است");
                    System.Diagnostics.Debug.WriteLine("ERROR: CostCenterId is null");
                }

                if (request.PriorityLevel <= 0)
                {
                    ModelState.AddModelError("PriorityLevel", "انتخاب سطح اولویت الزامی است");
                    System.Diagnostics.Debug.WriteLine($"ERROR: PriorityLevel is {request.PriorityLevel}");
                }

                // Validate items if provided
                bool hasValidItems = false;
                if (items != null && items.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Validating {items.Count} items:");

                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        bool itemValid = true;

                        System.Diagnostics.Debug.WriteLine($"  Item {i}: Name='{item.ItemName}', Qty={item.QuantityRequested}, UnitId={item.UnitId}");

                        if (string.IsNullOrWhiteSpace(item.ItemName))
                        {
                            ModelState.AddModelError($"items[{i}].ItemName", "نام کالا الزامی است");
                            itemValid = false;
                            System.Diagnostics.Debug.WriteLine($"    ERROR: Item {i} name is empty");
                        }

                        if (item.QuantityRequested <= 0)
                        {
                            ModelState.AddModelError($"items[{i}].QuantityRequested", "مقدار باید بیشتر از صفر باشد");
                            itemValid = false;
                            System.Diagnostics.Debug.WriteLine($"    ERROR: Item {i} quantity is {item.QuantityRequested}");
                        }

                        if (!item.UnitId.HasValue)
                        {
                            ModelState.AddModelError($"items[{i}].UnitId", "انتخاب واحد الزامی است");
                            itemValid = false;
                            System.Diagnostics.Debug.WriteLine($"    ERROR: Item {i} UnitId is null");
                        }

                        if (itemValid)
                        {
                            hasValidItems = true;
                            System.Diagnostics.Debug.WriteLine($"    SUCCESS: Item {i} is valid");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No items provided");
                }

                if (!hasValidItems && (items == null || !items.Any()))
                {
                    ModelState.AddModelError("", "لطفاً حداقل یک کالای معتبر اضافه کنید");
                    System.Diagnostics.Debug.WriteLine("ERROR: No valid items found");
                }

                // Debug: Final ModelState check
                System.Diagnostics.Debug.WriteLine($"Final ModelState IsValid: {ModelState.IsValid}");

                if (!ModelState.IsValid)
                {
                    // Log all validation errors
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                        .ToList();

                    System.Diagnostics.Debug.WriteLine("=== VALIDATION ERRORS ===");
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field: {error.Field}");
                        foreach (var errorMsg in error.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Error: {errorMsg}");
                        }
                    }

                    // Repopulate dropdowns and return view
                    await PopulateDropdowns();
                    ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                    TempData["ErrorMessage"] = "لطفاً خطاهای موجود در فرم را بررسی کنید";

                    System.Diagnostics.Debug.WriteLine("Returning view due to validation errors");
                    return View(request);
                }

                System.Diagnostics.Debug.WriteLine("=== STARTING DATABASE TRANSACTION ===");

                // Process the valid request
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Set required fields
                    request.RequestedBy = currentUserId.Value;
                    request.CreatedBy = currentUserId.Value;
                    request.CreatedDate = DateTime.Now;
                    request.RequestDate = DateTime.Now;
                    request.Status = "در انتظار بررسی";
                    request.WorkflowStage = "ثبت درخواست";
                    request.IsActive = true;

                    // Ensure required properties have default values
                    if (string.IsNullOrWhiteSpace(request.Currency))
                        request.Currency = "IRR";

                    if (string.IsNullOrWhiteSpace(request.Urgency))
                        request.Urgency = "عادی";

                    // Generate unique request number
                    request.RequestNumber = await GenerateRequestNumber();
                    System.Diagnostics.Debug.WriteLine($"Generated request number: {request.RequestNumber}");

                    // Add the request
                    _context.MaterialRequests.Add(request);
                    System.Diagnostics.Debug.WriteLine("Added request to context");

                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine("Saved request to database");

                    // Add request items
                    if (items != null && items.Any())
                    {
                        decimal totalEstimated = 0;
                        int itemCount = 0;

                        foreach (var itemViewModel in items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName)))
                        {
                            var requestItem = new MaterialRequestItem
                            {
                                ItemId = Guid.NewGuid(),
                                RequestId = request.RequestId,
                                ItemType = "مواد",
                                MaterialId = itemViewModel.MaterialId,
                                ItemName = itemViewModel.ItemName?.Trim(),
                                ItemDescription = itemViewModel.ItemDescription?.Trim(),
                                QuantityRequested = itemViewModel.QuantityRequested,
                                UnitId = itemViewModel.UnitId,
                                UnitPriceEstimated = itemViewModel.UnitPriceEstimated,
                                TotalPriceEstimated = itemViewModel.QuantityRequested * (itemViewModel.UnitPriceEstimated ?? 0),
                                ItemGroupId = itemViewModel.ItemGroupId,
                                ItemStatus = "در انتظار بررسی",
                                IsCritical = itemViewModel.IsCritical,
                                CreatedDate = DateTime.Now
                            };

                            totalEstimated += requestItem.TotalPriceEstimated ?? 0;
                            _context.MaterialRequestItems.Add(requestItem);
                            itemCount++;

                            System.Diagnostics.Debug.WriteLine($"Added item {itemCount}: {requestItem.ItemName}");
                        }

                        System.Diagnostics.Debug.WriteLine($"Added {itemCount} items, total estimated: {totalEstimated}");

                        // Update total estimated cost
                        request.TotalEstimatedCost = totalEstimated;
                        _context.MaterialRequests.Update(request);
                    }

                    // Add workflow history
                    var workflow = new RequestWorkflowHistory
                    {
                        WorkflowId = Guid.NewGuid(),
                        RequestId = request.RequestId,
                        Stage = "ثبت درخواست",
                        Status = "در انتظار بررسی",
                        Comments = "درخواست توسط کاربر ثبت شد",
                        ProcessedBy = currentUserId.Value,
                        ProcessedDate = DateTime.Now,
                        IsActive = true
                    };
                    _context.RequestWorkflowHistories.Add(workflow);
                    System.Diagnostics.Debug.WriteLine("Added workflow history");

                    // Send notification to procurement team
                    try
                    {
                        await SendNotificationToProcurement(request);
                        System.Diagnostics.Debug.WriteLine("Sent notification to procurement team");
                    }
                    catch (Exception notificationEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to send notification: {notificationEx.Message}");
                        // Don't fail the entire transaction for notification issues
                    }

                    // Save all changes
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    System.Diagnostics.Debug.WriteLine("=== TRANSACTION COMMITTED SUCCESSFULLY ===");

                    TempData["SuccessMessage"] = $"درخواست شما با شماره {request.RequestNumber} با موفقیت ثبت شد";
                    return RedirectToAction(nameof(MyRequests));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"=== TRANSACTION ROLLED BACK ===");
                    System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                    TempData["ErrorMessage"] = "خطا در ثبت درخواست: " + ex.Message;

                    await PopulateDropdowns();
                    ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                    return View(request);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GENERAL ERROR IN ADDREQUEST ===");
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "خطای غیرمنتظره در سیستم: " + ex.Message;

                try
                {
                    await PopulateDropdowns();
                }
                catch (Exception populateEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error populating dropdowns: {populateEx.Message}");
                }

                ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                return View(request ?? new MaterialRequest
                {
                    RequestId = Guid.NewGuid(),
                    RequestDate = DateTime.Now,
                    PriorityLevel = 3,
                    Currency = "IRR",
                    IsSubstituteAllowed = true,
                    IsActive = true,
                    Status = "در انتظار بررسی",
                    Urgency = "عادی"
                });
            }
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

        // Private method to generate request number with Persian calendar
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

        // Fixed: Single version of ProcessCheckInventory (renamed main version)
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

        // GET: MaterialRequest/EditRequest/5
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

            // Check if user owns this request or has permission to edit
            if (request.RequestedBy != currentUserId.Value && !await HasEditPermission(currentUserId.Value))
            {
                TempData["ErrorMessage"] = "شما مجوز ویرایش این درخواست را ندارید";
                return RedirectToAction(nameof(ProcessRequest), new { id });
            }

            // Check if request is in editable status
            if (request.Status != "در انتظار بررسی")
            {
                TempData["WarningMessage"] = $"این درخواست در وضعیت '{request.Status}' قرار دارد و ممکن است قابل ویرایش نباشد";
            }

            await PopulateDropdowns();
            return View(request);
        }

        // POST: MaterialRequest/EditRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequest(MaterialRequest request, List<MaterialRequestItemViewModel> items)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== EditRequest POST Started ===");
                System.Diagnostics.Debug.WriteLine($"Request ID: {request?.RequestId}");
                System.Diagnostics.Debug.WriteLine($"Request Title: {request?.RequestTitle}");
                System.Diagnostics.Debug.WriteLine($"Items Count: {items?.Count ?? 0}");

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    TempData["ErrorMessage"] = "کاربر یافت نشد";
                    return RedirectToAction("Login", "Account");
                }

                // Get existing request from database
                var existingRequest = await _context.MaterialRequests
                    .Include(r => r.MaterialRequestItems)
                    .FirstOrDefaultAsync(r => r.RequestId == request.RequestId);

                if (existingRequest == null)
                {
                    TempData["ErrorMessage"] = "درخواست یافت نشد";
                    return RedirectToAction(nameof(MyRequests));
                }

                // Check ownership and permission
                if (existingRequest.RequestedBy != currentUserId.Value && !await HasEditPermission(currentUserId.Value))
                {
                    TempData["ErrorMessage"] = "شما مجوز ویرایش این درخواست را ندارید";
                    return RedirectToAction(nameof(ProcessRequest), new { id = request.RequestId });
                }

                // Check if request is still editable
                if (existingRequest.Status != "در انتظار بررسی")
                {
                    TempData["ErrorMessage"] = "این درخواست در وضعیت فعلی قابل ویرایش نیست";
                    return RedirectToAction(nameof(ProcessRequest), new { id = request.RequestId });
                }

                // Remove navigation properties from ModelState
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

                // Manual validation
                if (string.IsNullOrWhiteSpace(request.RequestTitle))
                {
                    ModelState.AddModelError("RequestTitle", "عنوان درخواست الزامی است");
                }

                if (!request.CostCenterId.HasValue)
                {
                    ModelState.AddModelError("CostCenterId", "انتخاب مرکز هزینه الزامی است");
                }

                if (request.PriorityLevel <= 0)
                {
                    ModelState.AddModelError("PriorityLevel", "انتخاب سطح اولویت الزامی است");
                }

                // Validate items
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
                    return View(existingRequest);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update main request properties
                    existingRequest.RequestTitle = request.RequestTitle?.Trim();
                    existingRequest.Description = request.Description?.Trim();
                    existingRequest.CategoryId = request.CategoryId;
                    existingRequest.RequestTypeId = request.RequestTypeId;
                    existingRequest.CostCenterId = request.CostCenterId;
                    existingRequest.ProjectId = request.ProjectId;
                    existingRequest.Department = request.Department?.Trim();
                    existingRequest.BudgetCode = request.BudgetCode?.Trim();
                    existingRequest.Currency = request.Currency;
                    existingRequest.RequiredDate = request.RequiredDate;
                    existingRequest.DeliveryLocation = request.DeliveryLocation?.Trim();
                    existingRequest.SpecialInstructions = request.SpecialInstructions?.Trim();
                    existingRequest.PriorityLevel = request.PriorityLevel;
                    existingRequest.Urgency = request.Urgency;
                    existingRequest.IsUrgent = request.IsUrgent;
                    existingRequest.IsSubstituteAllowed = request.IsSubstituteAllowed;

                    _context.MaterialRequests.Update(existingRequest);

                    // Handle request items - remove existing ones and add new ones
                    var existingItems = existingRequest.MaterialRequestItems.ToList();
                    _context.MaterialRequestItems.RemoveRange(existingItems);

                    // Add updated items
                    if (items != null && items.Any())
                    {
                        decimal totalEstimated = 0;
                        int itemCount = 0;

                        foreach (var itemViewModel in items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName)))
                        {
                            var requestItem = new MaterialRequestItem
                            {
                                ItemId = Guid.NewGuid(), // Always generate new ID for simplicity
                                RequestId = existingRequest.RequestId,
                                ItemType = "مواد",
                                MaterialId = itemViewModel.MaterialId,
                                ItemName = itemViewModel.ItemName?.Trim(),
                                ItemDescription = itemViewModel.ItemDescription?.Trim(),
                                QuantityRequested = itemViewModel.QuantityRequested,
                                UnitId = itemViewModel.UnitId,
                                UnitPriceEstimated = itemViewModel.UnitPriceEstimated,
                                TotalPriceEstimated = itemViewModel.QuantityRequested * (itemViewModel.UnitPriceEstimated ?? 0),
                                ItemGroupId = itemViewModel.ItemGroupId,
                                ItemStatus = "در انتظار بررسی",
                                IsCritical = itemViewModel.IsCritical,
                                CreatedDate = DateTime.Now
                            };

                            totalEstimated += requestItem.TotalPriceEstimated ?? 0;
                            _context.MaterialRequestItems.Add(requestItem);
                            itemCount++;
                        }

                        // Update total estimated cost
                        existingRequest.TotalEstimatedCost = totalEstimated;
                    }

                    // Add workflow history for the edit
                    await AddWorkflowHistory(existingRequest.RequestId, "ویرایش درخواست", "در انتظار بررسی",
                        "درخواست توسط کاربر ویرایش شد", currentUserId.Value);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    System.Diagnostics.Debug.WriteLine("=== EditRequest TRANSACTION COMMITTED ===");

                    TempData["SuccessMessage"] = $"درخواست {existingRequest.RequestNumber} با موفقیت ویرایش شد";
                    return RedirectToAction(nameof(ProcessRequest), new { id = request.RequestId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"Database error in EditRequest: {ex.Message}");

                    TempData["ErrorMessage"] = "خطا در ویرایش درخواست: " + ex.Message;

                    await PopulateDropdowns();
                    ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                    return View(existingRequest);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General error in EditRequest: {ex.Message}");
                TempData["ErrorMessage"] = "خطای غیرمنتظره در سیستم: " + ex.Message;

                await PopulateDropdowns();
                ViewBag.Items = items ?? new List<MaterialRequestItemViewModel>();
                return View(request);
            }
        }

        // Helper method to check if user has edit permission
        private async Task<bool> HasEditPermission(Guid userId)
        {
            // Check if user is in a team with edit permissions
            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Team != null)
            {
                // Allow editing for procurement team, managers
                return user.Team.BuyCommercial == true ||
                       user.Team.ManagmentDashboard == true;
            }

            return false;
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

            var costCenters = await _context.CostCenters
                .Where(cc => cc.IsActive == true)
                .OrderBy(cc => cc.CostCenterName)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => p.IsActive == true && (p.Status == "فعال" || p.Status == null))
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            var itemGroups = await _context.ItemGroups
                .Where(ig => ig.IsActive == true)
                .OrderBy(ig => ig.GroupName)
                .ToListAsync();

            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            var materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
            ViewBag.RequestTypes = new SelectList(requestTypes, "TypeId", "TypeName");
            ViewBag.CostCenters = new SelectList(costCenters, "CostCenterId", "CostCenterName");
            ViewBag.Projects = new SelectList(projects, "ProjectId", "ProjectName");
            ViewBag.ItemGroups = new SelectList(itemGroups, "ItemGroupId", "GroupName");
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
                new { Value = "", Text = "همه وضعیت‌ها" },
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
                new { Value = "", Text = "همه اولویت‌ها" },
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

            var itemGroups = await _context.ItemGroups
                .Where(ig => ig.IsActive == true)
                .OrderBy(ig => ig.GroupName)
                .ToListAsync();

            ViewBag.Materials = new SelectList(materials, "MaterialId", "MaterialName");
            ViewBag.Suppliers = new SelectList(suppliers, "SupplierId", "SupplierName");
            ViewBag.Units = new SelectList(units, "UnitId", "UnitName");
            ViewBag.ItemGroups = new SelectList(itemGroups, "ItemGroupId", "GroupName");
        }

        // Dashboard method
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

        // GET: MaterialRequest/GetMaterialsByGroup
        [HttpGet]
        public async Task<IActionResult> GetMaterialsByGroup(Guid itemGroupId)
        {
            var materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .Select(m => new { value = m.MaterialId, text = m.MaterialName })
                .ToListAsync();

            return Json(materials);
        }

        // GET: MaterialRequest/GetUnits
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

        // GET: MaterialRequest/ProcessRequest/5
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
                .Include(r => r.RequestedByNavigation)
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
                return RedirectToAction(nameof(RequestList));
            }

            // Check if user can process this request
            var canProcess = await CanUserProcessRequest(currentUserId.Value, request);
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

        // Helper method to check if user can process requests
        private async Task<bool> CanUserProcessRequest(Guid userId, MaterialRequest request)
        {
            // Check if user is in a team with processing permissions
            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Team != null)
            {
                // Allow processing for procurement team, managers, or warehouse staff
                bool hasProcessingRights = user.Team.BuyCommercial == true ||
                                          user.Team.ManagmentDashboard == true ||
                                          user.Team.Inventory == true;

                if (hasProcessingRights)
                {
                    return true;
                }

                // Allow request owner to perform limited actions
                if (request.RequestedBy == userId && request.Status == "در انتظار بررسی")
                {
                    return false; // Owner cannot process, but can edit
                }
            }

            return false;
        }

        // POST: MaterialRequest/UpdateItemStatus
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

                var requestItem = await _context.MaterialRequestItems
                    .Include(i => i.Request)
                    .FirstOrDefaultAsync(i => i.ItemId == model.ItemId);

                if (requestItem == null)
                {
                    return Json(new { success = false, message = "کالا یافت نشد" });
                }

                // Check permissions
                if (!await CanUserProcessRequest(currentUserId.Value, requestItem.Request))
                {
                    return Json(new { success = false, message = "شما مجوز به‌روزرسانی این کالا را ندارید" });
                }

                // Update item status
                requestItem.ItemStatus = model.Status;
                requestItem.AvailabilityStatus = model.Status;

                // Update stock quantity if available
                if (model.Status == "موجود" && requestItem.MaterialId.HasValue)
                {
                    var availableStock = await _context.MaterialBatches
                        .Where(b => b.MaterialId == requestItem.MaterialId &&
                                   b.Status == "آزاد شده" &&
                                   b.CurrentQuantity > 0)
                        .SumAsync(b => b.CurrentQuantity);

                    requestItem.StockQuantity = availableStock;
                }

                _context.MaterialRequestItems.Update(requestItem);
                await _context.SaveChangesAsync();

                // Add workflow history for item update
                await AddWorkflowHistory(requestItem.RequestId, "به‌روزرسانی وضعیت کالا",
                    $"وضعیت '{requestItem.ItemName}' به '{model.Status}' تغییر یافت",
                    $"وضعیت کالا توسط کاربر به‌روزرسانی شد", currentUserId.Value);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "وضعیت با موفقیت به‌روزرسانی شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در به‌روزرسانی وضعیت: " + ex.Message });
            }
        }

        // POST: MaterialRequest/CancelRequest
        [HttpPost]
        public async Task<IActionResult> CancelRequest([FromBody] CancelRequestModel model)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "کاربر یافت نشد" });
                }

                var request = await _context.MaterialRequests
                    .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

                if (request == null)
                {
                    return Json(new { success = false, message = "درخواست یافت نشد" });
                }

                // Check if user is the owner of the request
                if (request.RequestedBy != currentUserId.Value)
                {
                    return Json(new { success = false, message = "شما مجوز لغو این درخواست را ندارید" });
                }

                // Check if request can be canceled
                if (request.Status != "در انتظار بررسی" && request.Status != "در حال بررسی")
                {
                    return Json(new { success = false, message = "این درخواست در وضعیت فعلی قابل لغو نیست" });
                }

                // Update request status
                request.Status = "لغو شده";
                request.RejectionReason = "درخواست توسط کاربر لغو شد";

                _context.MaterialRequests.Update(request);

                // Add workflow history
                await AddWorkflowHistory(request.RequestId, "لغو درخواست", "لغو شده",
                    "درخواست توسط کاربر لغو شد", currentUserId.Value);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "درخواست با موفقیت لغو شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در لغو درخواست: " + ex.Message });
            }
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

        // GET: MaterialRequest/GetMaterialBatches
        [HttpGet]
        public async Task<IActionResult> GetMaterialBatches(Guid materialId)
        {
            try
            {
                var batches = await _context.MaterialBatches
                    .Where(b => b.MaterialId == materialId && b.CurrentQuantity > 0)
                    .Include(b => b.Unit)
                    .Include(b => b.Location)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new
                    {
                        batchId = b.BatchId,
                        batchNumber = b.BatchNumber,
                        currentQuantity = b.CurrentQuantity,
                        unit = b.Unit.UnitName,
                        location = b.Location.LocationName,
                        expiryDate = b.ExpiryDate,
                        status = b.Status
                    })
                    .ToListAsync();

                return Json(new { success = true, batches = batches });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در بارگیری اطلاعات بچ‌ها: " + ex.Message });
            }
        }

        // GET: MaterialRequest/GetWarehouseSummary
        [HttpGet]
        public async Task<IActionResult> GetWarehouseSummary()
        {
            try
            {
                var totalBatches = await _context.MaterialBatches.CountAsync();
                var availableBatches = await _context.MaterialBatches
                    .CountAsync(b => b.Status == "آزاد شده" && b.CurrentQuantity > 0);

                var quarantineBatches = await _context.MaterialBatches
                    .CountAsync(b => b.Status == "قرنطینه");

                // Fixed: Handle DateOnly to DateTime comparison properly
                var expiringSoon = await _context.MaterialBatches
                    .CountAsync(b => b.ExpiryDate.HasValue &&
                                    b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) <= DateTime.Now.AddMonths(3) &&
                                    b.Status == "آزاد شده");

                var lowStockMaterials = await _context.RawMaterials
                    .Where(m => m.IsActive == true && m.MinStockLevel.HasValue)
                    .Where(m => _context.MaterialBatches
                        .Where(b => b.MaterialId == m.MaterialId && b.Status == "آزاد شده")
                        .Sum(b => b.CurrentQuantity) < m.MinStockLevel)
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    totalBatches = totalBatches,
                    availableBatches = availableBatches,
                    quarantineBatches = quarantineBatches,
                    expiringSoon = expiringSoon,
                    lowStockMaterials = lowStockMaterials
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در بارگیری اطلاعات انبار: " + ex.Message });
            }
        }

        // POST: MaterialRequest/CheckSingleItemStock
        [HttpPost]
        public async Task<IActionResult> CheckSingleItemStock([FromBody] CheckStockModel model)
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
                    .Include(i => i.Material)
                    .Include(i => i.Request)
                    .FirstOrDefaultAsync(i => i.ItemId == model.ItemId);

                if (item == null)
                {
                    return Json(new { success = false, message = "قلم یافت نشد" });
                }

                decimal availableStock = 0;
                if (item.MaterialId.HasValue)
                {
                    availableStock = await _context.MaterialBatches
                        .Where(b => b.MaterialId == item.MaterialId &&
                                   b.Status == "آزاد شده" &&
                                   b.CurrentQuantity > 0)
                        .SumAsync(b => b.CurrentQuantity);
                }

                // Update item stock quantity
                item.StockQuantity = availableStock;

                // Determine status based on stock
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
                return Json(new { success = false, message = "خطا در بررسی موجودی: " + ex.Message });
            }
        }

        // POST: MaterialRequest/SetItemSubstitute
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
                    return Json(new { success = false, message = "جایگزینی برای این درخواست مجاز نیست" });
                }

                // Update substitute material
                item.SubstituteMaterialId = model.SubstituteMaterialId;
                item.SubstituteNotes = model.Notes;

                await _context.SaveChangesAsync();

                // Add workflow history
                await AddWorkflowHistory(item.RequestId, "تعیین ماده جایگزین",
                    "جایگزین تعیین شد",
                    $"ماده جایگزین برای '{item.ItemName}' تعیین شد: {model.Notes}",
                    currentUserId.Value);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "ماده جایگزین با موفقیت تعیین شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در تعیین جایگزین: " + ex.Message });
            }
        }

        // Enhanced ProcessCheckInventory method with better stock tracking (renamed to avoid duplication)
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
                    // Get available stock from all batches with proper DateOnly handling
                    availableStock = await _context.MaterialBatches
                        .Where(b => b.MaterialId == item.MaterialId &&
                                   b.Status == "آزاد شده" &&
                                   b.CurrentQuantity > 0 &&
                                   // Fixed: Handle DateOnly to DateTime conversion properly
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

            // Determine overall request status based on item availability
            if (allItemsAvailable)
            {
                request.Status = "آماده تحویل";
                request.WorkflowStage = "آماده برای تحویل";
                await SendNotificationToRequester(request, "درخواست شما آماده تحویل است");
            }
            else if (someItemsAvailable && request.IsSubstituteAllowed == true)
            {
                request.Status = "نیاز به جایگزین";
                request.WorkflowStage = "جستجوی ماده جایگزین";
                await SendNotificationToRequester(request, $"از {totalItems} قلم درخواستی، {availableItems} قلم موجود، {partialItems} قلم جزئی و {unavailableItems} قلم ناموجود است");
            }
            else if (someItemsAvailable)
            {
                request.Status = "آماده تحویل جزئی";
                request.WorkflowStage = "تحویل جزئی";
                await SendNotificationToRequester(request, $"تنها {availableItems} قلم از {totalItems} قلم درخواستی موجود است");
            }
            else
            {
                request.Status = "نیاز به خرید";
                request.WorkflowStage = "ارسال به واحد خرید";
                await SendNotificationToProcurement(request, $"تمام اقلام درخواست نیاز به خرید دارند");
            }

            var detailedComments = $"{comments}. نتیجه بررسی: {availableItems} موجود، {partialItems} جزئی، {unavailableItems} ناموجود";

            await AddWorkflowHistory(request.RequestId, "بررسی موجودی انبار", request.Status,
                detailedComments, processedBy);
        }

        // Model classes for API requests
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

        public class UpdateItemStatusModel
        {
            public Guid ItemId { get; set; }
            public string Status { get; set; }
        }

        public class CancelRequestModel
        {
            public Guid RequestId { get; set; }
        }
    }

    // ViewModel for MaterialRequestItem
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