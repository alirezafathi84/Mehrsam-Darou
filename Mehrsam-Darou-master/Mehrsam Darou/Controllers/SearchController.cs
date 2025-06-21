using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System.Text.Json;

namespace Mehrsam_Darou.Controllers
{
    public class SearchController : BaseController
    {
        public SearchController(DarouAppContext context) : base(context)
        {
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

            // Search Customers
            var customers = await _context.Customers
                .Where(c => c.CustomerName.Contains(searchTerm) ||
                           c.CustomerCode.Contains(searchTerm) ||
                           (c.ContactPerson != null && c.ContactPerson.Contains(searchTerm)) ||
                           (c.Email != null && c.Email.Contains(searchTerm)))
                .Select(c => new
                {
                    c.CustomerId,
                    c.CustomerName,
                    c.CustomerCode,
                    c.ContactPerson,
                    c.Email,
                    c.IsActive
                })
                .Take(5)
                .ToListAsync();

            var customerResults = customers.Select(c => new SearchResultItem
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
            }).ToList();

            // Search Suppliers
            var suppliers = await _context.Suppliers
                .Where(s => s.SupplierName.Contains(searchTerm) ||
                           s.SupplierCode.Contains(searchTerm) ||
                           (s.ContactPerson != null && s.ContactPerson.Contains(searchTerm)) ||
                           (s.Email != null && s.Email.Contains(searchTerm)))
                .Select(s => new
                {
                    s.SupplierId,
                    s.SupplierName,
                    s.SupplierCode,
                    s.ContactPerson,
                    s.Email,
                    s.IsActive
                })
                .Take(5)
                .ToListAsync();

            var supplierResults = suppliers.Select(s => new SearchResultItem
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
            }).ToList();

            // Search Medicines
            var medicines = await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.BrandName.Contains(searchTerm) ||
                           m.MedicineCode.Contains(searchTerm))
                .Select(m => new
                {
                    m.MedicineId,
                    m.BrandName,
                    m.MedicineCode,
                    CategoryName = m.Category != null ? m.Category.CategoryName : null,
                    m.Strength,
                    m.IsActive
                })
                .Take(5)
                .ToListAsync();

            var medicineResults = medicines.Select(m => new SearchResultItem
            {
                Id = m.MedicineId.ToString(),
                Title = m.BrandName,
                Subtitle = m.MedicineCode + (m.CategoryName != null ? " - " + m.CategoryName : ""),
                Description = m.Strength != null ? $"قدرت: {m.Strength}" : "",
                Category = "داروها",
                Icon = "solar:leaf-bold-duotone",
                Url = Url.Action("EditMedicine", "Medicine", new { id = m.MedicineId }) ?? "#",
                Badge = m.IsActive == true ? "فعال" : "غیرفعال",
                BadgeClass = m.IsActive == true ? "bg-success" : "bg-secondary"
            }).ToList();

            // Search Raw Materials
            var materials = await _context.RawMaterials
                .Include(m => m.Category)
                .Where(m => m.MaterialName.Contains(searchTerm) ||
                           m.MaterialCode.Contains(searchTerm))
                .Select(m => new
                {
                    m.MaterialId,
                    m.MaterialName,
                    m.MaterialCode,
                    CategoryName = m.Category != null ? m.Category.CategoryName : null,
                    m.MinStockLevel,
                    m.IsActive
                })
                .Take(5)
                .ToListAsync();

            var materialResults = materials.Select(m => new SearchResultItem
            {
                Id = m.MaterialId.ToString(),
                Title = m.MaterialName,
                Subtitle = m.MaterialCode + (m.CategoryName != null ? " - " + m.CategoryName : ""),
                Description = m.MinStockLevel != null ? $"حداقل موجودی: {m.MinStockLevel}" : "",
                Category = "مواد اولیه",
                Icon = "solar:box-bold-duotone",
                Url = Url.Action("EditRawMaterial", "RawMaterial", new { id = m.MaterialId }) ?? "#",
                Badge = m.IsActive == true ? "فعال" : "غیرفعال",
                BadgeClass = m.IsActive == true ? "bg-success" : "bg-secondary"
            }).ToList();

            // Search Users (only if user has permission)
            var userResults = new List<SearchResultItem>();
            var hasSystemUsersPermission = ViewData["SystemUsersMenu"] as bool? ?? false;

            if (hasSystemUsersPermission)
            {
                var users = await _context.Users
                    .Include(u => u.Team)
                    .Where(u => (u.FirstName + " " + u.LastName).Contains(searchTerm) ||
                               (u.Username != null && u.Username.Contains(searchTerm)))
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Username,
                        TeamName = u.Team != null ? u.Team.Name : null
                    })
                    .Take(5)
                    .ToListAsync();

                userResults = users.Select(u => new SearchResultItem
                {
                    Id = u.Id.ToString(),
                    Title = (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                    Subtitle = (u.Username ?? "") + (u.TeamName != null ? " - " + u.TeamName : ""),
                    Description = "",
                    Category = "کاربران",
                    Icon = "solar:user-id-bold-duotone",
                    Url = Url.Action("EditUser", "User", new { id = u.Id }) ?? "#",
                    Badge = "کاربر",
                    BadgeClass = "bg-primary"
                }).ToList();
            }

            // Search Sales Orders
            var salesOrders = await _context.SalesOrders
                .Include(so => so.Customer)
                .Where(so => so.SoNumber.Contains(searchTerm) ||
                            (so.Customer != null && so.Customer.CustomerName.Contains(searchTerm)))
                .Select(so => new
                {
                    so.SalesOrderId,
                    so.SoNumber,
                    CustomerName = so.Customer != null ? so.Customer.CustomerName : null,
                    so.TotalAmount,
                    so.Status
                })
                .Take(5)
                .ToListAsync();

            var salesOrderResults = salesOrders.Select(so => new SearchResultItem
            {
                Id = so.SalesOrderId.ToString(),
                Title = "سفارش فروش " + so.SoNumber,
                Subtitle = so.CustomerName ?? "",
                Description = so.TotalAmount != null ? $"مبلغ: {so.TotalAmount:N0} ریال" : "",
                Category = "سفارشات فروش",
                Icon = "solar:gift-bold-duotone",
                Url = Url.Action("EditSalesOrder", "SalesOrder", new { id = so.SalesOrderId }) ?? "#",
                Badge = so.Status ?? "نامشخص",
                BadgeClass = GetStatusBadgeClass(so.Status)
            }).ToList();

            // Search Purchase Orders
            var purchaseOrders = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Where(po => po.PoNumber.Contains(searchTerm) ||
                            (po.Supplier != null && po.Supplier.SupplierName.Contains(searchTerm)))
                .Select(po => new
                {
                    po.PurchaseOrderId,
                    po.PoNumber,
                    SupplierName = po.Supplier != null ? po.Supplier.SupplierName : null,
                    po.TotalAmount,
                    po.Status
                })
                .Take(5)
                .ToListAsync();

            var purchaseOrderResults = purchaseOrders.Select(po => new SearchResultItem
            {
                Id = po.PurchaseOrderId.ToString(),
                Title = "سفارش خرید " + po.PoNumber,
                Subtitle = po.SupplierName ?? "",
                Description = po.TotalAmount != null ? $"مبلغ: {po.TotalAmount:N0} ریال" : "",
                Category = "سفارشات خرید",
                Icon = "solar:case-bold-duotone",
                Url = Url.Action("EditPurchaseOrder", "PurchaseOrder", new { id = po.PurchaseOrderId }) ?? "#",
                Badge = po.Status ?? "نامشخص",
                BadgeClass = GetStatusBadgeClass(po.Status)
            }).ToList();

            // Combine all results
            searchResults.Results.AddRange(customerResults);
            searchResults.Results.AddRange(supplierResults);
            searchResults.Results.AddRange(medicineResults);
            searchResults.Results.AddRange(materialResults);
            searchResults.Results.AddRange(userResults);
            searchResults.Results.AddRange(salesOrderResults);
            searchResults.Results.AddRange(purchaseOrderResults);

            // Group results by category
            searchResults.GroupedResults = searchResults.Results
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            searchResults.TotalResults = searchResults.Results.Count;

            return View(searchResults);
        }

        // AJAX Search for autocomplete
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
                // Quick search in customers
                var customers = await _context.Customers
                    .Where(c => c.CustomerName.Contains(searchTerm) ||
                               c.CustomerCode.Contains(searchTerm))
                    .Select(c => new
                    {
                        id = c.CustomerId.ToString(),
                        title = c.CustomerName,
                        subtitle = c.CustomerCode,
                        category = "مشتریان",
                        icon = "solar:user-rounded-bold-duotone",
                        url = Url.Action("EditCustomer", "Customer", new { id = c.CustomerId }) ?? "#"
                    })
                    .Take(3)
                    .ToListAsync();

                // Quick search in medicines
                var medicines = await _context.Medicines
                    .Where(m => m.BrandName.Contains(searchTerm) ||
                               m.MedicineCode.Contains(searchTerm))
                    .Select(m => new
                    {
                        id = m.MedicineId.ToString(),
                        title = m.BrandName,
                        subtitle = m.MedicineCode,
                        category = "داروها",
                        icon = "solar:leaf-bold-duotone",
                        url = Url.Action("EditMedicine", "Medicine", new { id = m.MedicineId }) ?? "#"
                    })
                    .Take(3)
                    .ToListAsync();

                // Quick search in suppliers
                var suppliers = await _context.Suppliers
                    .Where(s => s.SupplierName.Contains(searchTerm) ||
                               s.SupplierCode.Contains(searchTerm))
                    .Select(s => new
                    {
                        id = s.SupplierId.ToString(),
                        title = s.SupplierName,
                        subtitle = s.SupplierCode,
                        category = "تأمین‌کنندگان",
                        icon = "solar:case-bold-duotone",
                        url = Url.Action("EditSupplier", "Supplier", new { id = s.SupplierId }) ?? "#"
                    })
                    .Take(3)
                    .ToListAsync();

                results.AddRange(customers);
                results.AddRange(medicines);
                results.AddRange(suppliers);

                return Json(new { results = results.Take(9) });
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