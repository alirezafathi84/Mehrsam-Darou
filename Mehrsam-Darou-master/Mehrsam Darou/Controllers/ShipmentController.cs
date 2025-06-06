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
    public class ShipmentController : BaseController
    {
        private readonly DarouAppContext _context;

        public ShipmentController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Shipment/ShipmentList
        public async Task<IActionResult> ShipmentList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Shipment> query = _context.Shipments
                .Include(s => s.Customer)
                .Include(s => s.SalesOrder);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(s => s.ShipmentNumber.Contains(searchKey) ||
                                     s.Customer.CustomerName.Contains(searchKey) ||
                                     s.Carrier.Contains(searchKey) ||
                                     s.TrackingNumber.Contains(searchKey))
                            .OrderByDescending(s => s.CreatedDate);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Shipment>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: Shipment/AddShipment
        public async Task<IActionResult> AddShipment()
        {
            await PopulateShipmentDropdowns();
            return View(new Shipment
            {
                ShipmentDate = DateOnly.FromDateTime(DateTime.Today),
                CreatedDate = DateTime.Now,
                Status = "در حال آماده‌سازی"
            });
        }

        // POST: Shipment/AddShipment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShipment(Shipment shipment)
        {
            // Remove Customer and SalesOrder from validation as we only need their IDs
            ModelState.Remove("Customer");
            ModelState.Remove("SalesOrder");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Shipments.AnyAsync(s => s.ShipmentNumber == shipment.ShipmentNumber))
                    {
                        TempData["ErrorMessage"] = "حمل و نقل با این شماره قبلاً ثبت شده است";
                        await PopulateShipmentDropdowns();
                        return View(shipment);
                    }

                    shipment.ShipmentId = Guid.NewGuid();
                    shipment.CreatedDate = DateTime.Now;

                    // Clear navigation properties to avoid EF issues
                    shipment.Customer = null;
                    shipment.SalesOrder = null;

                    _context.Add(shipment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "حمل و نقل جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(ShipmentList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد حمل و نقل: " + ex.Message;
                }
            }

            await PopulateShipmentDropdowns();
            return View(shipment);
        }

        // GET: Shipment/EditShipment/5
        public async Task<IActionResult> EditShipment(Guid id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound();
            }

            await PopulateShipmentDropdowns();
            return View(shipment);
        }

        // POST: Shipment/EditShipment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShipment(Guid id, Shipment shipment)
        {
            if (id != shipment.ShipmentId)
            {
                return NotFound();
            }

            // Remove Customer and SalesOrder from validation as we only need their IDs
            ModelState.Remove("Customer");
            ModelState.Remove("SalesOrder");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Shipments.AnyAsync(s =>
                        s.ShipmentId != id &&
                        s.ShipmentNumber == shipment.ShipmentNumber))
                    {
                        TempData["ErrorMessage"] = "حمل و نقل با این شماره قبلاً ثبت شده است";
                        await PopulateShipmentDropdowns();
                        return View(shipment);
                    }

                    var existingShipment = await _context.Shipments.FindAsync(id);
                    if (existingShipment == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    shipment.CreatedDate = existingShipment.CreatedDate;

                    // Clear navigation properties to avoid EF issues
                    shipment.Customer = null;
                    shipment.SalesOrder = null;

                    _context.Entry(existingShipment).CurrentValues.SetValues(shipment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات حمل و نقل با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(ShipmentList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShipmentExists(shipment.ShipmentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateShipmentDropdowns();
            return View(shipment);
        }

        // POST: Shipment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                TempData["ErrorMessage"] = "حمل و نقل مورد نظر یافت نشد";
                return RedirectToAction(nameof(ShipmentList));
            }

            // Check if shipment has items
            bool hasItems = await _context.ShipmentItems.AnyAsync(si => si.ShipmentId == id);
            if (hasItems)
            {
                TempData["ErrorMessage"] = "این حمل و نقل دارای آیتم‌های مرسوله است و قابل حذف نیست";
                return RedirectToAction(nameof(ShipmentList));
            }

            // Don't allow deletion if shipment is delivered
            if (shipment.Status == "تحویل داده شده")
            {
                TempData["ErrorMessage"] = "حمل و نقل تحویل داده شده قابل حذف نیست";
                return RedirectToAction(nameof(ShipmentList));
            }

            try
            {
                _context.Shipments.Remove(shipment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "حمل و نقل با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف حمل و نقل: " + ex.Message;
            }

            return RedirectToAction(nameof(ShipmentList));
        }

        private bool ShipmentExists(Guid id)
        {
            return _context.Shipments.Any(e => e.ShipmentId == id);
        }

        private async Task PopulateShipmentDropdowns()
        {
            // Active customers
            var customers = await _context.Customers
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");

            // Sales orders that are not fully shipped
            var salesOrders = await _context.SalesOrders
                .Where(so => so.Status != "لغو شده")
                .OrderByDescending(so => so.CreatedDate)
                .Take(100) // Limit for performance
                .ToListAsync();

            ViewBag.SalesOrders = new SelectList(salesOrders, "SalesOrderId", "SoNumber");

            // Common carriers
            var carriers = new List<SelectListItem>
            {
                new SelectListItem { Value = "پست پیشتاز", Text = "پست پیشتاز" },
                new SelectListItem { Value = "تیپاکس", Text = "تیپاکس" },
                new SelectListItem { Value = "پیک موتوری", Text = "پیک موتوری" },
                new SelectListItem { Value = "باربری", Text = "باربری" },
                new SelectListItem { Value = "حمل شخصی", Text = "حمل شخصی" },
                new SelectListItem { Value = "سایر", Text = "سایر" }
            };

            ViewBag.Carriers = carriers;
        }
    }
}