//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Mehrsam_Darou.Models;
//using Mehrsam_Darou.ViewModels;
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using static Mehrsam_Darou.Helper.Helper;

//namespace Mehrsam_Darou.Controllers
//{
//    public class InventoryReportController : BaseController
//    {
//        private readonly DarouAppContext _context;

//        public InventoryReportController(DarouAppContext context) : base(context)
//        {
//            _context = context;
//        }

//        // GET: InventoryReport/InventoryReport
//        public async Task<IActionResult> InventoryReport(int? page, string searchKey, Guid? locationFilter, string statusFilter)
//        {
//            var setting = await ReadSettingAsync(_context);
//            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
//            int pageNumber = page ?? 1;

//            // Get all storage locations with their finished goods batches
//            IQueryable<StorageLocation> locationsQuery = _context.StorageLocations
//                .Include(l => l.FinishedGoodsBatches)
//                    .ThenInclude(b => b.Medicine)
//                .Include(l => l.FinishedGoodsBatches)
//                    .ThenInclude(b => b.Unit)
//                .Where(l => l.IsActive == true);

//            // Apply location filter
//            if (locationFilter.HasValue)
//            {
//                locationsQuery = locationsQuery.Where(l => l.LocationId == locationFilter.Value);
//            }

//            // Apply search filter
//            if (!string.IsNullOrWhiteSpace(searchKey))
//            {
//                locationsQuery = locationsQuery.Where(l =>
//                    l.LocationName.Contains(searchKey) ||
//                    l.LocationCode.Contains(searchKey) ||
//                    l.FinishedGoodsBatches.Any(b =>
//                        b.BatchNumber.Contains(searchKey) ||
//                        b.Medicine.BrandName.Contains(searchKey) ||
//                        b.Medicine.MedicineCode.Contains(searchKey)
//                    )
//                );
//            }

//            // Apply status filter to batches
//            if (!string.IsNullOrWhiteSpace(statusFilter))
//            {
//                locationsQuery = locationsQuery.Where(l =>
//                    l.FinishedGoodsBatches.Any(b => b.Status == statusFilter)
//                );
//            }

//            var locations = await locationsQuery
//                .OrderBy(l => l.LocationName)
//                .ToListAsync();

//            // Filter batches by status if specified
//            if (!string.IsNullOrWhiteSpace(statusFilter))
//            {
//                foreach (var location in locations)
//                {
//                    location.FinishedGoodsBatches = location.FinishedGoodsBatches
//                        .Where(b => b.Status == statusFilter)
//                        .ToList();
//                }
//            }

//            // Create inventory report data
//            var inventoryData = locations.Select(location => new InventoryReportViewModel
//            {
//                Location = location,
//                TotalBatches = location.FinishedGoodsBatches.Count,
//                TotalQuantity = location.FinishedGoodsBatches.Sum(b => b.Quantity),
//                ExpiredBatches = location.FinishedGoodsBatches.Count(b => b.ExpiryDate < DateOnly.FromDateTime(DateTime.Now)),
//                NearExpiryBatches = location.FinishedGoodsBatches.Count(b =>
//                    b.ExpiryDate < DateOnly.FromDateTime(DateTime.Now.AddMonths(3)) &&
//                    b.ExpiryDate >= DateOnly.FromDateTime(DateTime.Now)),
//                ReleasedBatches = location.FinishedGoodsBatches.Count(b => b.Status == "آزاد شده"),
//                QuarantineBatches = location.FinishedGoodsBatches.Count(b => b.Status == "قرنطینه"),
//                RejectedBatches = location.FinishedGoodsBatches.Count(b => b.Status == "رد شده")
//            }).ToList();

//            // Pagination
//            int total = inventoryData.Count;
//            var paginatedData = inventoryData
//                .Skip((pageNumber - 1) * pageSize)
//                .Take(pageSize)
//                .ToList();

//            var paginatedList = new PaginatedList<InventoryReportViewModel>(paginatedData, total, pageNumber, pageSize);

//            // Load filter data
//            ViewBag.Locations = await _context.StorageLocations
//                .Where(l => l.IsActive == true)
//                .OrderBy(l => l.LocationName)
//                .ToListAsync();

//            ViewBag.CurrentLocationFilter = locationFilter;
//            ViewBag.CurrentStatusFilter = statusFilter;

//            return View(paginatedList);
//        }

//        // GET: InventoryReport/LocationDetails/5
//        public async Task<IActionResult> LocationDetails(Guid id)
//        {
//            var location = await _context.StorageLocations
//                .Include(l => l.FinishedGoodsBatches)
//                    .ThenInclude(b => b.Medicine)
//                .Include(l => l.FinishedGoodsBatches)
//                    .ThenInclude(b => b.Unit)
//                .Include(l => l.FinishedGoodsBatches)
//                    .ThenInclude(b => b.Order)
//                .FirstOrDefaultAsync(l => l.LocationId == id);

//            if (location == null)
//            {
//                return NotFound();
//            }

//            var viewModel = new LocationDetailsViewModel
//            {
//                Location = location,
//                Batches = location.FinishedGoodsBatches.OrderBy(b => b.ExpiryDate).ToList()
//            };

//            return View(viewModel);
//        }

//        // GET: InventoryReport/ExpiryReport
//        public async Task<IActionResult> ExpiryReport()
//        {
//            var today = DateOnly.FromDateTime(DateTime.Now);
//            var threeMonthsFromNow = DateOnly.FromDateTime(DateTime.Now.AddMonths(3));

//            var expiryData = await _context.FinishedGoodsBatches
//                .Include(b => b.Medicine)
//                .Include(b => b.Unit)
//                .Include(b => b.Location)
//                .Where(b => b.ExpiryDate <= threeMonthsFromNow)
//                .OrderBy(b => b.ExpiryDate)
//                .ToListAsync();

//            var viewModel = new ExpiryReportViewModel
//            {
//                ExpiredBatches = expiryData.Where(b => b.ExpiryDate < today).ToList(),
//                ExpiringBatches = expiryData.Where(b => b.ExpiryDate >= today && b.ExpiryDate <= threeMonthsFromNow).ToList()
//            };

//            return View(viewModel);
//        }

//        // GET: InventoryReport/Summary
//        public async Task<IActionResult> Summary()
//        {
//            var totalLocations = await _context.StorageLocations.CountAsync();
//            var activeLocations = await _context.StorageLocations.CountAsync(l => l.IsActive == true);

//            var allBatches = await _context.FinishedGoodsBatches
//                .Include(b => b.Location)
//                .Include(b => b.Medicine)
//                .ToListAsync();

//            var today = DateOnly.FromDateTime(DateTime.Now);
//            var threeMonthsFromNow = DateOnly.FromDateTime(DateTime.Now.AddMonths(3));

//            var viewModel = new InventorySummaryViewModel
//            {
//                TotalLocations = totalLocations,
//                ActiveLocations = activeLocations,
//                TotalBatches = allBatches.Count,
//                ExpiredBatches = allBatches.Count(b => b.ExpiryDate < today),
//                NearExpiryBatches = allBatches.Count(b => b.ExpiryDate >= today && b.ExpiryDate <= threeMonthsFromNow),
//                ReleasedBatches = allBatches.Count(b => b.Status == "آزاد شده"),
//                QuarantineBatches = allBatches.Count(b => b.Status == "قرنطینه"),
//                RejectedBatches = allBatches.Count(b => b.Status == "رد شده"),
//                TopLocationsByVolume = allBatches
//                    .GroupBy(b => new { b.Location?.LocationId, b.Location?.LocationName, b.Location?.LocationCode })
//                    .Select(g => new LocationSummary
//                    {
//                        LocationId = g.Key.LocationId ?? Guid.Empty,
//                        LocationName = g.Key.LocationName ?? "نامشخص",
//                        LocationCode = g.Key.LocationCode ?? "",
//                        BatchCount = g.Count(),
//                        TotalQuantity = g.Sum(b => b.Quantity)
//                    })
//                    .OrderByDescending(l => l.TotalQuantity)
//                    .Take(5)
//                    .ToList()
//            };

//            return View(viewModel);
//        }
//    }
//}