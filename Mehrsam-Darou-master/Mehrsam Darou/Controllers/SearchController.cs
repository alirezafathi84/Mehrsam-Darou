using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System.Text.Json;

namespace Mehrsam_Darou.Controllers
{
    public class SearchController : BaseController
    {
        private readonly IServiceProvider _serviceProvider;

        // IMPORTANT: Updated constructor to inject IServiceProvider
        public SearchController(DarouAppContext context, IServiceProvider serviceProvider) : base(context)
        {
            _serviceProvider = serviceProvider;
        }

        // GET: Search/GlobalSearch
        public async Task<IActionResult> GlobalSearch(string q, int? page)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return View(new GlobalSearchResult());
            }

            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting?.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            var searchResults = new GlobalSearchResult
            {
                SearchQuery = q.Trim(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var searchTerm = q.Trim().ToLower();
            var hasSystemUsersPermission = ViewData["SystemUsersMenu"] as bool? ?? false;

            // Create all search tasks with separate DbContext instances
            var searchTasks = new List<Task<List<SearchResultItem>>>();

            // Users search (if permission exists)
            searchTasks.Add(hasSystemUsersPermission ?
                SearchUsersAsync(searchTerm) :
                Task.FromResult(new List<SearchResultItem>()));

            // Add other search tasks
            searchTasks.Add(SearchCustomersAsync(searchTerm));
            searchTasks.Add(SearchMedicinesAsync(searchTerm));
            searchTasks.Add(SearchSuppliersAsync(searchTerm));
            searchTasks.Add(SearchRawMaterialsAsync(searchTerm));
            searchTasks.Add(SearchProductionOrdersAsync(searchTerm));
            searchTasks.Add(SearchPurchaseOrdersAsync(searchTerm));
            searchTasks.Add(SearchSalesOrdersAsync(searchTerm));
            searchTasks.Add(SearchBatchTestsAsync(searchTerm));
            searchTasks.Add(SearchQcTestsAsync(searchTerm));
            searchTasks.Add(SearchQaAuditsAsync(searchTerm));
            searchTasks.Add(SearchCertificationsAsync(searchTerm));
            searchTasks.Add(SearchQcReportsAsync(searchTerm));
            searchTasks.Add(SearchMaterialBatchesAsync(searchTerm));
            searchTasks.Add(SearchFinishedGoodsBatchesAsync(searchTerm));
            searchTasks.Add(SearchTeamsAsync(searchTerm));
            searchTasks.Add(SearchShipmentsAsync(searchTerm));
            searchTasks.Add(SearchStorageLocationsAsync(searchTerm));
            searchTasks.Add(SearchOrganizationsAsync(searchTerm));

            // Execute all searches in parallel
            var allResults = await Task.WhenAll(searchTasks);

            // Combine all results
            foreach (var result in allResults)
            {
                searchResults.Results.AddRange(result);
            }

            // Group results by category
            searchResults.GroupedResults = searchResults.Results
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            searchResults.TotalResults = searchResults.Results.Count;

            return View(searchResults);
        }

        // Individual search methods with separate DbContext instances
        private async Task<List<SearchResultItem>> SearchUsersAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.Users
                .Include(u => u.Team)
                .Where(u => (u.FirstName + " " + u.LastName).Contains(searchTerm) ||
                           (u.Username != null && u.Username.Contains(searchTerm)))
                .Select(u => new SearchResultItem
                {
                    Id = u.Id.ToString(),
                    Title = (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                    Subtitle = (u.Username ?? "") + (u.Team != null ? " - " + u.Team.Name : ""),
                    Description = "",
                    Category = "کاربران",
                    Icon = "solar:user-id-bold-duotone",
                    Url = Url.Action("EditUser", "User", new { id = u.Id }) ?? "#",
                    Badge = "کاربر",
                    BadgeClass = "bg-primary"
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchCustomersAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.Customers
                .Where(c => c.CustomerName.Contains(searchTerm) ||
                           c.CustomerCode.Contains(searchTerm) ||
                           (c.ContactPerson != null && c.ContactPerson.Contains(searchTerm)) ||
                           (c.Email != null && c.Email.Contains(searchTerm)))
                .Select(c => new SearchResultItem
                {
                    Id = c.CustomerId.ToString(),
                    Title = c.CustomerName,
                    Subtitle = c.CustomerCode + (c.ContactPerson != null ? " - " + c.ContactPerson : ""),
                    Description = c.Email ?? "",
                    Category = "مشتریان",
                    Icon = "solar:user-rounded-bold-duotone",
                    Url = Url.Action("EditCustomer", "Customer", new { id = c.CustomerId }) ?? "#",
                    Badge = c.IsActive == true ? "فعال" : "غیرفعال",
                    BadgeClass = c.IsActive == true ? "bg-success" : "bg-secondary"
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchMedicinesAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.Medicines
                .Include(m => m.Category)
                .Where(m => m.BrandName.Contains(searchTerm) ||
                           m.MedicineCode.Contains(searchTerm))
                .Select(m => new SearchResultItem
                {
                    Id = m.MedicineId.ToString(),
                    Title = m.BrandName,
                    Subtitle = m.MedicineCode + (m.Category != null ? " - " + m.Category.CategoryName : ""),
                    Description = m.Strength != null ? $"قدرت: {m.Strength}" : "",
                    Category = "داروها",
                    Icon = "solar:leaf-bold-duotone",
                    Url = Url.Action("EditMedicine", "Medicine", new { id = m.MedicineId }) ?? "#",
                    Badge = m.IsActive == true ? "فعال" : "غیرفعال",
                    BadgeClass = m.IsActive == true ? "bg-success" : "bg-secondary"
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchSuppliersAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.Suppliers
                .Where(s => s.SupplierName.Contains(searchTerm) ||
                           s.SupplierCode.Contains(searchTerm) ||
                           (s.ContactPerson != null && s.ContactPerson.Contains(searchTerm)) ||
                           (s.Email != null && s.Email.Contains(searchTerm)))
                .Select(s => new SearchResultItem
                {
                    Id = s.SupplierId.ToString(),
                    Title = s.SupplierName,
                    Subtitle = s.SupplierCode + (s.ContactPerson != null ? " - " + s.ContactPerson : ""),
                    Description = s.Email ?? "",
                    Category = "تأمین‌کنندگان",
                    Icon = "solar:case-bold-duotone",
                    Url = Url.Action("EditSupplier", "Supplier", new { id = s.SupplierId }) ?? "#",
                    Badge = s.IsActive == true ? "فعال" : "غیرفعال",
                    BadgeClass = s.IsActive == true ? "bg-success" : "bg-secondary"
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchRawMaterialsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.RawMaterials
                .Include(m => m.Category)
                .Where(m => m.MaterialName.Contains(searchTerm) ||
                           m.MaterialCode.Contains(searchTerm))
                .Select(m => new SearchResultItem
                {
                    Id = m.MaterialId.ToString(),
                    Title = m.MaterialName,
                    Subtitle = m.MaterialCode + (m.Category != null ? " - " + m.Category.CategoryName : ""),
                    Description = m.MinStockLevel != null ? $"حداقل موجودی: {m.MinStockLevel}" : "",
                    Category = "مواد اولیه",
                    Icon = "solar:box-bold-duotone",
                    Url = Url.Action("EditRawMaterial", "RawMaterial", new { id = m.MaterialId }) ?? "#",
                    Badge = m.IsActive == true ? "فعال" : "غیرفعال",
                    BadgeClass = m.IsActive == true ? "bg-success" : "bg-secondary"
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchProductionOrdersAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.ProductionOrders
                .Include(po => po.Medicine)
                .Where(po => po.OrderNumber.Contains(searchTerm) ||
                            (po.Medicine != null && po.Medicine.BrandName.Contains(searchTerm)))
                .Select(po => new SearchResultItem
                {
                    Id = po.OrderId.ToString(),
                    Title = "سفارش تولید " + po.OrderNumber,
                    Subtitle = po.Medicine != null ? po.Medicine.BrandName : "",
                    Description = po.Status ?? "",
                    Category = "سفارشات تولید",
                    Icon = "solar:document-text-bold-duotone",
                    Url = Url.Action("EditProductionOrder", "ProductionOrder", new { id = po.OrderId }) ?? "#",
                    Badge = po.Status ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(po.Status)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchPurchaseOrdersAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.PurchaseOrders
                .Include(po => po.Supplier)
                .Where(po => po.PoNumber.Contains(searchTerm) ||
                            (po.Supplier != null && po.Supplier.SupplierName.Contains(searchTerm)))
                .Select(po => new SearchResultItem
                {
                    Id = po.PurchaseOrderId.ToString(),
                    Title = "سفارش خرید " + po.PoNumber,
                    Subtitle = po.Supplier != null ? po.Supplier.SupplierName : "",
                    Description = po.TotalAmount != null ? $"مبلغ: {po.TotalAmount:N0} ریال" : "",
                    Category = "سفارشات خرید",
                    Icon = "solar:document-text-bold-duotone",
                    Url = Url.Action("EditPurchaseOrder", "PurchaseOrder", new { id = po.PurchaseOrderId }) ?? "#",
                    Badge = po.Status ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(po.Status)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchSalesOrdersAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.SalesOrders
                .Include(so => so.Customer)
                .Where(so => so.SoNumber.Contains(searchTerm) ||
                            (so.Customer != null && so.Customer.CustomerName.Contains(searchTerm)))
                .Select(so => new SearchResultItem
                {
                    Id = so.SalesOrderId.ToString(),
                    Title = "سفارش فروش " + so.SoNumber,
                    Subtitle = so.Customer != null ? so.Customer.CustomerName : "",
                    Description = so.TotalAmount != null ? $"مبلغ: {so.TotalAmount:N0} ریال" : "",
                    Category = "سفارشات فروش",
                    Icon = "solar:gift-bold-duotone",
                    Url = Url.Action("EditSalesOrder", "SalesOrder", new { id = so.SalesOrderId }) ?? "#",
                    Badge = so.Status ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(so.Status)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchBatchTestsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.BatchTests
                .Include(bt => bt.Product)
                .Include(bt => bt.Test)
                .Where(bt => bt.TestNumber.Contains(searchTerm) ||
                            bt.BatchNumber.Contains(searchTerm) ||
                            (bt.Product != null && bt.Product.BrandName.Contains(searchTerm)))
                .Select(bt => new SearchResultItem
                {
                    Id = bt.BatchTestId.ToString(),
                    Title = "تست " + bt.TestNumber,
                    Subtitle = (bt.Product != null ? bt.Product.BrandName : "") + " - بچ " + bt.BatchNumber,
                    Description = bt.TestStatus ?? "",
                    Category = "آزمایشات",
                    Icon = "solar:test-tube-bold-duotone",
                    Url = Url.Action("EditBatchTest", "BatchTest", new { id = bt.BatchTestId }) ?? "#",
                    Badge = bt.TestStatus ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(bt.TestStatus)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchQcTestsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.QcTests
                .Where(qt => qt.TestName.Contains(searchTerm) ||
                            qt.TestCode.Contains(searchTerm))
                .Select(qt => new SearchResultItem
                {
                    Id = qt.TestId.ToString(),
                    Title = qt.TestName,
                    Subtitle = qt.TestCode + " - " + qt.TestType,
                    Description = "",
                    Category = "آزمایشات کیفی",
                    Icon = "solar:checklist-bold-duotone",
                    Url = Url.Action("EditQCTest", "QCTest", new { id = qt.TestId }) ?? "#",
                    Badge = qt.TestType ?? "نامشخص",
                    BadgeClass = "bg-info"
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchQaAuditsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.QaAudits
                .Where(qa => qa.AuditTitle.Contains(searchTerm) ||
                            qa.AuditCode.Contains(searchTerm))
                .Select(qa => new SearchResultItem
                {
                    Id = qa.AuditId.ToString(),
                    Title = qa.AuditTitle,
                    Subtitle = qa.AuditCode + " - " + qa.AuditStatus,
                    Description = "",
                    Category = "ممیزی کیفیت",
                    Icon = "solar:checklist-minimalistic-bold-duotone",
                    Url = Url.Action("EditQAAudit", "QAAudit", new { id = qa.AuditId }) ?? "#",
                    Badge = qa.AuditStatus ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(qa.AuditStatus)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchCertificationsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.Certifications
                .Where(c => c.CertificationName.Contains(searchTerm) ||
                           c.CertificationCode.Contains(searchTerm))
                .Select(c => new SearchResultItem
                {
                    Id = c.CertificationId.ToString(),
                    Title = c.CertificationName,
                    Subtitle = c.CertificationCode + " - " + c.CertificationStatus,
                    Description = "",
                    Category = "گواهی‌نامه‌ها",
                    Icon = "solar:medal-ribbons-star-bold-duotone",
                    Url = Url.Action("EditCertification", "Certification", new { id = c.CertificationId }) ?? "#",
                    Badge = c.CertificationStatus ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(c.CertificationStatus)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchQcReportsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.QcReports
                .Where(qr => qr.ReportTitle.Contains(searchTerm) ||
                            qr.ReportNumber.Contains(searchTerm))
                .Select(qr => new SearchResultItem
                {
                    Id = qr.ReportId.ToString(),
                    Title = qr.ReportTitle,
                    Subtitle = qr.ReportNumber + " - " + qr.ReportStatus,
                    Description = "",
                    Category = "گزارشات کنترل کیفیت",
                    Icon = "solar:file-text-bold-duotone",
                    Url = Url.Action("EditQCReport", "QCReport", new { id = qr.ReportId }) ?? "#",
                    Badge = qr.ReportStatus ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(qr.ReportStatus)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchMaterialBatchesAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.MaterialBatches
                .Include(mb => mb.Material)
                .Where(mb => mb.BatchNumber.Contains(searchTerm) ||
                            (mb.Material != null && mb.Material.MaterialName.Contains(searchTerm)))
                .Select(mb => new SearchResultItem
                {
                    Id = mb.BatchId.ToString(),
                    Title = "بچ " + mb.BatchNumber,
                    Subtitle = mb.Material != null ? mb.Material.MaterialName : "",
                    Description = mb.Status ?? "",
                    Category = "بچ‌های مواد اولیه",
                    Icon = "solar:archive-bold-duotone",
                    Url = Url.Action("EditMaterialBatch", "MaterialBatch", new { id = mb.BatchId }) ?? "#",
                    Badge = mb.Status ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(mb.Status)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchFinishedGoodsBatchesAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.FinishedGoodsBatches
                .Include(fgb => fgb.Medicine)
                .Where(fgb => fgb.BatchNumber.Contains(searchTerm) ||
                             (fgb.Medicine != null && fgb.Medicine.BrandName.Contains(searchTerm)))
                .Select(fgb => new SearchResultItem
                {
                    Id = fgb.BatchId.ToString(),
                    Title = "بچ " + fgb.BatchNumber,
                    Subtitle = fgb.Medicine != null ? fgb.Medicine.BrandName : "",
                    Description = fgb.Status ?? "",
                    Category = "بچ‌های محصول نهایی",
                    Icon = "solar:archive-check-bold-duotone",
                    Url = Url.Action("EditFinishedGoodsBatch", "FinishedGoodsBatch", new { id = fgb.BatchId }) ?? "#",
                    Badge = fgb.Status ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(fgb.Status)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchTeamsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.Teams
                .Where(t => t.Name.Contains(searchTerm))
                .Select(t => new SearchResultItem
                {
                    Id = t.Id.ToString(),
                    Title = t.Name,
                    Subtitle = t.DefaultPageForTeam ?? "بدون صفحه پیش‌فرض",
                    Description = "",
                    Category = "تیم‌ها",
                    Icon = "solar:users-group-rounded-bold-duotone",
                    Url = Url.Action("EditTeam", "Team", new { id = t.Id }) ?? "#",
                    Badge = "تیم",
                    BadgeClass = "bg-primary"
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchShipmentsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.Shipments
                .Include(s => s.Customer)
                .Where(s => s.ShipmentNumber.Contains(searchTerm) ||
                           (s.Customer != null && s.Customer.CustomerName.Contains(searchTerm)))
                .Select(s => new SearchResultItem
                {
                    Id = s.ShipmentId.ToString(),
                    Title = "حمل " + s.ShipmentNumber,
                    Subtitle = s.Customer != null ? s.Customer.CustomerName : "",
                    Description = s.Status ?? "",
                    Category = "حمل و نقل",
                    Icon = "solar:delivery-bold-duotone",
                    Url = Url.Action("EditShipment", "Shipment", new { id = s.ShipmentId }) ?? "#",
                    Badge = s.Status ?? "نامشخص",
                    BadgeClass = GetStatusBadgeClass(s.Status)
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchStorageLocationsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.StorageLocations
                .Where(sl => sl.LocationName.Contains(searchTerm) ||
                            sl.LocationCode.Contains(searchTerm))
                .Select(sl => new SearchResultItem
                {
                    Id = sl.LocationId.ToString(),
                    Title = sl.LocationName,
                    Subtitle = sl.LocationCode + " - " + sl.LocationType,
                    Description = "",
                    Category = "مکان‌های انبار",
                    Icon = "solar:box-bold-duotone",
                    Url = Url.Action("EditStorageLocation", "StorageLocation", new { id = sl.LocationId }) ?? "#",
                    Badge = sl.LocationType ?? "نامشخص",
                    BadgeClass = "bg-info"
                })
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SearchResultItem>> SearchOrganizationsAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DarouAppContext>();

            return await context.Organizations
                .Where(o => o.Name.Contains(searchTerm))
                .Select(o => new SearchResultItem
                {
                    Id = o.Id.ToString(),
                    Title = o.Name,
                    Subtitle = "اولویت: " + o.Priority,
                    Description = "",
                    Category = "سازمان‌ها",
                    Icon = "solar:buildings-bold-duotone",
                    Url = Url.Action("EditOrganization", "Organization", new { id = o.Id }) ?? "#",
                    Badge = "سازمان",
                    BadgeClass = "bg-primary"
                })
                .Take(5)
                .ToListAsync();
        }

        // Enhanced AJAX Search for autocomplete - Using sequential execution to avoid threading issues
        [HttpGet]
        public async Task<IActionResult> QuickSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new { results = new List<object>() });
            }

            var searchTerm = q.Trim().ToLower();
            var results = new List<object>();

            try
            {
                // For QuickSearch, we'll execute sequentially to keep it simple and fast
                // Search in Users
                var users = await _context.Users
                    .Include(u => u.Team)
                    .Where(u => u.FirstName.Contains(searchTerm) ||
                               u.LastName.Contains(searchTerm) ||
                               (u.Username != null && u.Username.Contains(searchTerm)))
                    .Select(u => new
                    {
                        id = u.Id.ToString(),
                        title = u.FirstName + " " + u.LastName,
                        subtitle = u.Username + " - " + (u.Team != null ? u.Team.Name : "بدون تیم"),
                        category = "کاربران",
                        icon = "solar:user-bold-duotone",
                        url = Url.Action("EditUser", "User", new { id = u.Id }) ?? "#"
                    })
                    .Take(2)
                    .ToListAsync();

                // Search in Customers
                var customers = await _context.Customers
                    .Where(c => c.CustomerName.Contains(searchTerm) ||
                               c.CustomerCode.Contains(searchTerm) ||
                               (c.ContactPerson != null && c.ContactPerson.Contains(searchTerm)))
                    .Select(c => new
                    {
                        id = c.CustomerId.ToString(),
                        title = c.CustomerName,
                        subtitle = c.CustomerCode + " - " + (c.ContactPerson ?? ""),
                        category = "مشتریان",
                        icon = "solar:user-rounded-bold-duotone",
                        url = Url.Action("EditCustomer", "Customer", new { id = c.CustomerId }) ?? "#"
                    })
                    .Take(2)
                    .ToListAsync();

                // Search in Medicines
                var medicines = await _context.Medicines
                    .Include(m => m.Category)
                    .Where(m => m.BrandName.Contains(searchTerm) ||
                               m.MedicineCode.Contains(searchTerm))
                    .Select(m => new
                    {
                        id = m.MedicineId.ToString(),
                        title = m.BrandName,
                        subtitle = m.MedicineCode + " - " + (m.Category != null ? m.Category.CategoryName : ""),
                        category = "داروها",
                        icon = "solar:leaf-bold-duotone",
                        url = Url.Action("EditMedicine", "Medicine", new { id = m.MedicineId }) ?? "#"
                    })
                    .Take(2)
                    .ToListAsync();

                // Combine results
                results.AddRange(users);
                results.AddRange(customers);
                results.AddRange(medicines);

                return Json(new { results = results.Take(15) });
            }
            catch (Exception ex)
            {
                // Log the exception if you have logging
                return Json(new { results = new List<object>(), error = "خطا در جستجو" });
            }
        }

        private async Task<Setting?> ReadSettingAsync(DarouAppContext context)
        {
            return await context.Settings.FirstOrDefaultAsync();
        }

        // Make this method static to avoid EF Core issues
        private static string GetStatusBadgeClass(string? status)
        {
            return status switch
            {
                "تایید شده" => "bg-success",
                "پیش‌نویس" => "bg-warning",
                "لغو شده" => "bg-danger",
                "تکمیل شده" => "bg-success",
                "در حال اجرا" => "bg-info",
                _ => "bg-secondary"
            };
        }
    }
}