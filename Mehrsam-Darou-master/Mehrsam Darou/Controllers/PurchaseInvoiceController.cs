using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class PurchaseInvoiceController : BaseController
    {
        private readonly DarouAppContext _context;

        public PurchaseInvoiceController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: PurchaseInvoice/PurchaseInvoiceList
        public async Task<IActionResult> PurchaseInvoiceList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<PurchaseInvoice> query = _context.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrder);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(p => p.InvoiceNumber.Contains(searchKey) ||
                                     p.SupplierInvoiceNumber.Contains(searchKey) ||
                                     p.Supplier.SupplierName.Contains(searchKey) ||
                                     p.Status.Contains(searchKey))
                            .OrderByDescending(p => p.CreatedDate);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<PurchaseInvoice>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: PurchaseInvoice/AddPurchaseInvoice
        public async Task<IActionResult> AddPurchaseInvoice()
        {
            await LoadDropdownData();

            return View(new PurchaseInvoice
            {
                InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "پیش‌نویس",
                Currency = "IRR",
                CreatedDate = DateTime.Now,
                TaxAmount = 0,
                DiscountAmount = 0,
                PaidAmount = 0
            });
        }

        // POST: PurchaseInvoice/AddPurchaseInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPurchaseInvoice(PurchaseInvoice purchaseInvoice)
        {
            // Remove navigation properties from ModelState validation
            ModelState.Remove("Supplier");
            ModelState.Remove("PurchaseOrder");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PurchaseInvoices.AnyAsync(p => p.InvoiceNumber == purchaseInvoice.InvoiceNumber))
                    {
                        TempData["ErrorMessage"] = "فاکتور خرید با این شماره قبلاً ثبت شده است";
                        await LoadDropdownData();
                        return View(purchaseInvoice);
                    }

                    purchaseInvoice.PurchaseInvoiceId = Guid.NewGuid();
                    purchaseInvoice.CreatedDate = DateTime.Now;

                    _context.Add(purchaseInvoice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "فاکتور خرید جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(PurchaseInvoiceList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد فاکتور خرید: " + ex.Message;
                }
            }

            await LoadDropdownData();
            return View(purchaseInvoice);
        }

        // GET: PurchaseInvoice/EditPurchaseInvoice/5
        public async Task<IActionResult> EditPurchaseInvoice(Guid id)
        {
            var purchaseInvoice = await _context.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrder)
                .FirstOrDefaultAsync(p => p.PurchaseInvoiceId == id);

            if (purchaseInvoice == null)
            {
                return NotFound();
            }

            await LoadDropdownData();

            // Load purchase invoice items for display - THIS IS CRUCIAL
            var items = await _context.PurchaseInvoiceItems
                .Include(pii => pii.Material)
                .Include(pii => pii.Unit)
                .Include(pii => pii.Batch)
                .Where(pii => pii.PurchaseInvoiceId == id)
                .OrderBy(pii => pii.Material.MaterialName)
                .ToListAsync();

            ViewBag.PurchaseInvoiceItems = items;

            return View(purchaseInvoice);
        }

        // POST: PurchaseInvoice/EditPurchaseInvoice/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPurchaseInvoice(Guid id, PurchaseInvoice purchaseInvoice)
        {
            // Debug: Check if IDs match
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "شناسه فاکتور خرید نامعتبر است";
                return RedirectToAction(nameof(PurchaseInvoiceList));
            }

            if (id != purchaseInvoice.PurchaseInvoiceId)
            {
                // If the model's ID is empty, set it from the route parameter
                if (purchaseInvoice.PurchaseInvoiceId == Guid.Empty)
                {
                    purchaseInvoice.PurchaseInvoiceId = id;
                }
                else
                {
                    return NotFound();
                }
            }

            // Remove navigation properties from ModelState validation
            ModelState.Remove("Supplier");
            ModelState.Remove("PurchaseOrder");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PurchaseInvoices.AnyAsync(p =>
                        p.PurchaseInvoiceId != id &&
                        p.InvoiceNumber == purchaseInvoice.InvoiceNumber))
                    {
                        TempData["ErrorMessage"] = "فاکتور خرید با این شماره قبلاً ثبت شده است";
                        await LoadDropdownData();

                        // Load items for redisplay
                        var items = await _context.PurchaseInvoiceItems
                            .Include(pii => pii.Material)
                            .Include(pii => pii.Unit)
                            .Include(pii => pii.Batch)
                            .Where(pii => pii.PurchaseInvoiceId == id)
                            .OrderBy(pii => pii.Material.MaterialName)
                            .ToListAsync();
                        ViewBag.PurchaseInvoiceItems = items;

                        return View(purchaseInvoice);
                    }

                    var existingPurchaseInvoice = await _context.PurchaseInvoices.FindAsync(id);
                    if (existingPurchaseInvoice == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    purchaseInvoice.CreatedDate = existingPurchaseInvoice.CreatedDate;

                    _context.Entry(existingPurchaseInvoice).CurrentValues.SetValues(purchaseInvoice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات فاکتور خرید با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(PurchaseInvoiceList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PurchaseInvoiceExists(purchaseInvoice.PurchaseInvoiceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی فاکتور خرید: " + ex.Message;
                }
            }

            await LoadDropdownData();

            // Load items for redisplay
            var itemsForRedisplay = await _context.PurchaseInvoiceItems
                .Include(pii => pii.Material)
                .Include(pii => pii.Unit)
                .Include(pii => pii.Batch)
                .Where(pii => pii.PurchaseInvoiceId == id)
                .OrderBy(pii => pii.Material.MaterialName)
                .ToListAsync();
            ViewBag.PurchaseInvoiceItems = itemsForRedisplay;

            return View(purchaseInvoice);
        }

        // POST: PurchaseInvoice/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var purchaseInvoice = await _context.PurchaseInvoices.FindAsync(id);
            if (purchaseInvoice == null)
            {
                TempData["ErrorMessage"] = "فاکتور خرید مورد نظر یافت نشد";
                return RedirectToAction(nameof(PurchaseInvoiceList));
            }

            // Check if purchase invoice has any items
            bool hasItems = await _context.PurchaseInvoiceItems.AnyAsync(pii => pii.PurchaseInvoiceId == id);

            if (hasItems)
            {
                TempData["ErrorMessage"] = "این فاکتور خرید دارای آیتم است و قابل حذف نیست";
                return RedirectToAction(nameof(PurchaseInvoiceList));
            }

            try
            {
                _context.PurchaseInvoices.Remove(purchaseInvoice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "فاکتور خرید با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف فاکتور خرید: " + ex.Message;
            }

            return RedirectToAction(nameof(PurchaseInvoiceList));
        }

        // POST: PurchaseInvoice/CreateFromPurchaseOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromPurchaseOrder(PurchaseInvoice purchaseInvoice)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get purchase order ID from form data
                var purchaseOrderId = Guid.Parse(Request.Form["purchaseOrderId"]);

                // Debug logging
                Console.WriteLine($"Creating invoice from purchase order: {purchaseOrderId}");
                Console.WriteLine($"Invoice number: {purchaseInvoice.InvoiceNumber}");

                // Check if invoice number already exists
                if (await _context.PurchaseInvoices.AnyAsync(p => p.InvoiceNumber == purchaseInvoice.InvoiceNumber))
                {
                    return Json(new { success = false, message = "فاکتور خرید با این شماره قبلاً ثبت شده است" });
                }

                // Get purchase order with items
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Material)
                    .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Unit)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId);

                if (purchaseOrder == null)
                {
                    return Json(new { success = false, message = "سفارش خرید یافت نشد" });
                }

                Console.WriteLine($"Purchase order found with {purchaseOrder.PurchaseOrderItems?.Count ?? 0} items");

                // Validate that purchase order has items
                if (purchaseOrder.PurchaseOrderItems == null || !purchaseOrder.PurchaseOrderItems.Any())
                {
                    return Json(new { success = false, message = "سفارش خرید انتخابی هیچ آیتمی ندارد" });
                }

                // Create new purchase invoice
                purchaseInvoice.PurchaseInvoiceId = Guid.NewGuid();
                purchaseInvoice.PurchaseOrderId = purchaseOrderId;
                purchaseInvoice.SupplierId = purchaseOrder.SupplierId;
                purchaseInvoice.Currency = purchaseOrder.Currency ?? "IRR";
                purchaseInvoice.CreatedDate = DateTime.Now;

                // Calculate subtotal from purchase order items
                var subtotal = purchaseOrder.PurchaseOrderItems.Sum(poi => poi.TotalPrice);
                purchaseInvoice.Subtotal = subtotal;

                // Calculate total amount
                var taxAmount = purchaseInvoice.TaxAmount ?? 0;
                var discountAmount = purchaseInvoice.DiscountAmount ?? 0;
                purchaseInvoice.TotalAmount = subtotal + taxAmount - discountAmount;

                Console.WriteLine($"Invoice created with ID: {purchaseInvoice.PurchaseInvoiceId}");

                // Add purchase invoice to context
                _context.PurchaseInvoices.Add(purchaseInvoice);
                await _context.SaveChangesAsync(); // Save invoice first to get the ID

                Console.WriteLine("Invoice saved to database");

                // Create invoice items from purchase order items
                var invoiceItems = new List<PurchaseInvoiceItem>();
                foreach (var orderItem in purchaseOrder.PurchaseOrderItems)
                {
                    var invoiceItem = new PurchaseInvoiceItem
                    {
                        PiItemId = Guid.NewGuid(),
                        PurchaseInvoiceId = purchaseInvoice.PurchaseInvoiceId,
                        MaterialId = orderItem.MaterialId,
                        Quantity = orderItem.Quantity,
                        UnitId = orderItem.UnitId,
                        UnitPrice = orderItem.UnitPrice,
                        TotalPrice = orderItem.TotalPrice,
                        Notes = orderItem.Notes,
                        BatchId = null // Will be set later if needed
                    };

                    invoiceItems.Add(invoiceItem);
                    _context.PurchaseInvoiceItems.Add(invoiceItem);

                    Console.WriteLine($"Added invoice item: {orderItem.Material?.MaterialName} - Qty: {orderItem.Quantity}");
                }

                // Save invoice items
                await _context.SaveChangesAsync();
                Console.WriteLine($"Saved {invoiceItems.Count} invoice items to database");

                // Verify items were actually saved
                var savedItemsCount = await _context.PurchaseInvoiceItems
                    .CountAsync(pii => pii.PurchaseInvoiceId == purchaseInvoice.PurchaseInvoiceId);
                Console.WriteLine($"Verification: {savedItemsCount} items found in database for invoice {purchaseInvoice.PurchaseInvoiceId}");

                // Commit transaction
                await transaction.CommitAsync();
                Console.WriteLine("Transaction committed successfully");

                return Json(new
                {
                    success = true,
                    message = $"فاکتور خرید با موفقیت ایجاد شد. {invoiceItems.Count} آیتم کپی شد.",
                    invoiceId = purchaseInvoice.PurchaseInvoiceId,
                    itemsCount = invoiceItems.Count,
                    savedItemsCount = savedItemsCount
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error creating invoice: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "خطا در ایجاد فاکتور خرید: " + ex.Message });
            }
        }

        // GET: PurchaseInvoice/TestItemCopy/{purchaseOrderId} - For testing item copy
        [HttpGet]
        public async Task<IActionResult> TestItemCopy(Guid purchaseOrderId)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Material)
                    .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Unit)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId);

                if (purchaseOrder == null)
                {
                    return Json(new { success = false, message = "Purchase order not found" });
                }

                var orderItems = purchaseOrder.PurchaseOrderItems?.Select(poi => new
                {
                    MaterialId = poi.MaterialId,
                    MaterialName = poi.Material?.MaterialName,
                    Quantity = poi.Quantity,
                    UnitId = poi.UnitId,
                    UnitName = poi.Unit?.UnitName,
                    UnitPrice = poi.UnitPrice,
                    TotalPrice = poi.TotalPrice,
                    Notes = poi.Notes
                }).ToList();

                return Json(new
                {
                    success = true,
                    purchaseOrderId = purchaseOrderId,
                    supplierId = purchaseOrder.SupplierId,
                    itemsCount = orderItems?.Count ?? 0,
                    items = orderItems
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: PurchaseInvoice/DebugInvoiceItems/{id} - For debugging
        [HttpGet]
        public async Task<IActionResult> DebugInvoiceItems(Guid id)
        {
            var items = await _context.PurchaseInvoiceItems
                .Include(pii => pii.Material)
                .Include(pii => pii.Unit)
                .Where(pii => pii.PurchaseInvoiceId == id)
                .ToListAsync();

            var result = items.Select(item => new
            {
                ItemId = item.PiItemId,
                InvoiceId = item.PurchaseInvoiceId,
                MaterialName = item.Material?.MaterialName,
                Quantity = item.Quantity,
                UnitName = item.Unit?.UnitName,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            });

            return Json(new
            {
                success = true,
                invoiceId = id,
                itemsCount = items.Count,
                items = result
            });
        }

        #region Purchase Invoice Items Methods

        // GET: PurchaseInvoice/GetItem/{id} - For Edit Modal
        [HttpGet]
        public async Task<IActionResult> GetItem(Guid id)
        {
            var item = await _context.PurchaseInvoiceItems
                .Include(pii => pii.Material)
                .Include(pii => pii.Unit)
                .Include(pii => pii.Batch)
                .FirstOrDefaultAsync(pii => pii.PiItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            return Json(new
            {
                piItemId = item.PiItemId,
                materialId = item.MaterialId,
                materialName = item.Material.MaterialName,
                quantity = item.Quantity,
                unitId = item.UnitId,
                unitName = item.Unit.UnitName,
                unitPrice = item.UnitPrice,
                totalPrice = item.TotalPrice,
                batchId = item.BatchId,
                batchNumber = item.Batch?.BatchNumber,
                notes = item.Notes
            });
        }

        // GET: PurchaseInvoice/GetMaterials - For Add/Edit Modal
        [HttpGet]
        public async Task<IActionResult> GetMaterials()
        {
            var materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .Select(m => new { value = m.MaterialId, text = m.MaterialName })
                .ToListAsync();

            return Json(materials);
        }

        // GET: PurchaseInvoice/GetUnits - For Add/Edit Modal
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

        // GET: PurchaseInvoice/GetBatches - For Add/Edit Modal
        [HttpGet]
        public async Task<IActionResult> GetBatches()
        {
            var batches = await _context.MaterialBatches
                .Include(b => b.Material)
             //   .Where(b => b.IsActive == true)
                .OrderBy(b => b.BatchNumber)
                .Select(b => new { value = b.BatchId, text = $"{b.BatchNumber} - {b.Material.MaterialName}" })
                .ToListAsync();

            return Json(batches);
        }

        // GET: PurchaseInvoice/GetPurchaseOrderDetails - For loading PO details
        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderDetails(Guid purchaseOrderId)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Material)
                .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Unit)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId);

            if (purchaseOrder == null)
            {
                return Json(new { success = false, message = "سفارش خرید یافت نشد" });
            }

            var items = new List<object>();
            if (purchaseOrder.PurchaseOrderItems != null)
            {
                items = purchaseOrder.PurchaseOrderItems.Select(poi => new
                {
                    materialId = poi.MaterialId,
                    materialName = poi.Material?.MaterialName,
                    quantity = poi.Quantity,
                    unitId = poi.UnitId,
                    unitName = poi.Unit?.UnitName,
                    unitPrice = poi.UnitPrice,
                    totalPrice = poi.TotalPrice,
                    notes = poi.Notes
                }).Cast<object>().ToList();
            }

            return Json(new
            {
                success = true,
                supplierId = purchaseOrder.SupplierId,
                supplierName = purchaseOrder.Supplier?.SupplierName,
                totalAmount = purchaseOrder.TotalAmount,
                currency = purchaseOrder.Currency,
                items = items
            });
        }

        // POST: PurchaseInvoice/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(PurchaseInvoiceItem item)
        {
            try
            {
                // Check if purchase invoice exists
                var purchaseInvoiceExists = await _context.PurchaseInvoices
                    .AnyAsync(pi => pi.PurchaseInvoiceId == item.PurchaseInvoiceId);

                if (!purchaseInvoiceExists)
                {
                    return Json(new { success = false, message = "فاکتور خرید مورد نظر یافت نشد" });
                }

                // Check if material already exists in this purchase invoice
                var existingItem = await _context.PurchaseInvoiceItems
                    .AnyAsync(pii => pii.PurchaseInvoiceId == item.PurchaseInvoiceId &&
                                   pii.MaterialId == item.MaterialId);

                if (existingItem)
                {
                    return Json(new { success = false, message = "این ماده اولیه قبلاً به فاکتور خرید اضافه شده است" });
                }

                item.PiItemId = Guid.NewGuid();
                item.TotalPrice = item.Quantity * item.UnitPrice;

                _context.PurchaseInvoiceItems.Add(item);
                await _context.SaveChangesAsync();

                // Update purchase invoice subtotal
                await UpdatePurchaseInvoiceSubtotalAsync(item.PurchaseInvoiceId);

                return Json(new { success = true, message = "آیتم جدید با موفقیت اضافه شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در افزودن آیتم: " + ex.Message });
            }
        }

        // POST: PurchaseInvoice/EditItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(PurchaseInvoiceItem item)
        {
            try
            {
                var existingItem = await _context.PurchaseInvoiceItems
                    .FirstOrDefaultAsync(pii => pii.PiItemId == item.PiItemId);

                if (existingItem == null)
                {
                    return Json(new { success = false, message = "آیتم مورد نظر یافت نشد" });
                }

                // Check if material already exists in this purchase invoice (excluding current item)
                var duplicateItem = await _context.PurchaseInvoiceItems
                    .AnyAsync(pii => pii.PurchaseInvoiceId == existingItem.PurchaseInvoiceId &&
                                   pii.MaterialId == item.MaterialId &&
                                   pii.PiItemId != item.PiItemId);

                if (duplicateItem)
                {
                    return Json(new { success = false, message = "این ماده اولیه قبلاً به فاکتور خرید اضافه شده است" });
                }

                existingItem.MaterialId = item.MaterialId;
                existingItem.Quantity = item.Quantity;
                existingItem.UnitId = item.UnitId;
                existingItem.UnitPrice = item.UnitPrice;
                existingItem.TotalPrice = item.Quantity * item.UnitPrice;
                existingItem.BatchId = item.BatchId;
                existingItem.Notes = item.Notes;

                await _context.SaveChangesAsync();

                // Update purchase invoice subtotal
                await UpdatePurchaseInvoiceSubtotalAsync(existingItem.PurchaseInvoiceId);

                return Json(new { success = true, message = "آیتم با موفقیت ویرایش شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در ویرایش آیتم: " + ex.Message });
            }
        }

        // POST: PurchaseInvoice/DeleteItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            try
            {
                var item = await _context.PurchaseInvoiceItems.FindAsync(id);
                if (item == null)
                {
                    return Json(new { success = false, message = "آیتم مورد نظر یافت نشد" });
                }

                var purchaseInvoiceId = item.PurchaseInvoiceId;
                _context.PurchaseInvoiceItems.Remove(item);
                await _context.SaveChangesAsync();

                // Update purchase invoice subtotal
                await UpdatePurchaseInvoiceSubtotalAsync(purchaseInvoiceId);

                return Json(new { success = true, message = "آیتم با موفقیت حذف شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در حذف آیتم: " + ex.Message });
            }
        }

        #endregion

        #region Private Methods

        private bool PurchaseInvoiceExists(Guid id)
        {
            return _context.PurchaseInvoices.Any(e => e.PurchaseInvoiceId == id);
        }

        private async Task LoadDropdownData()
        {
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            ViewBag.Suppliers = suppliers.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = s.SupplierId.ToString(),
                Text = s.SupplierName
            }).ToList();

            var purchaseOrders = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Where(po => po.Status != "لغو شده")
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();

            ViewBag.PurchaseOrders = purchaseOrders.Select(po => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = po.PurchaseOrderId.ToString(),
                Text = $"{po.PoNumber} - {po.Supplier?.SupplierName}"
            }).ToList();
        }

        private async Task UpdatePurchaseInvoiceSubtotalAsync(Guid purchaseInvoiceId)
        {
            var subtotal = await _context.PurchaseInvoiceItems
                .Where(pii => pii.PurchaseInvoiceId == purchaseInvoiceId)
                .SumAsync(pii => pii.TotalPrice);

            var purchaseInvoice = await _context.PurchaseInvoices.FindAsync(purchaseInvoiceId);
            if (purchaseInvoice != null)
            {
                purchaseInvoice.Subtotal = subtotal;
                purchaseInvoice.TotalAmount = subtotal + (purchaseInvoice.TaxAmount ?? 0) - (purchaseInvoice.DiscountAmount ?? 0);
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}