using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class QCTestController : BaseController
    {
        private readonly DarouAppContext _context;

        public QCTestController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: QualityControl/QCTestList
        public async Task<IActionResult> QCTestList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<QcTest> query = _context.QcTests
                .Include(q => q.CreatedByNavigation);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(q => q.TestCode.Contains(searchKey) ||
                                     q.TestName.Contains(searchKey) ||
                                     q.TestMethod.Contains(searchKey) ||
                                     q.ResponsibleDepartment.Contains(searchKey))
                            .OrderBy(q => q.TestName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(q => q.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<QcTest>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: QualityControl/AddQCTest
        public IActionResult AddQCTest()
        {
            return View(new QcTest
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                PriorityLevel = 3,
                CalibrationRequired = false,
                EnvironmentalControl = false,
                ApprovalRequired = false,
                TrendAnalysisRequired = false,
                StatisticalControl = false,
                StabilityImpact = false,
                RegulatoryRequirement = false,
                ChangeControlRequired = false,
                Currency = "IRR"
            });
        }

        // POST: QualityControl/AddQCTest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQCTest(QcTest test)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QcTests.AnyAsync(q => q.TestCode == test.TestCode))
                    {
                        TempData["ErrorMessage"] = "آزمایش با این کد قبلاً ثبت شده است";
                        return View(test);
                    }

                    test.TestId = Guid.NewGuid();
                    test.CreatedDate = DateTime.Now;

                    _context.Add(test);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "آزمایش جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(QCTestList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد آزمایش: " + ex.Message;
                }
            }

            return View(test);
        }

        // GET: QualityControl/EditQCTest/5
        public async Task<IActionResult> EditQCTest(Guid id)
        {
            var test = await _context.QcTests.FindAsync(id);
            if (test == null)
            {
                return NotFound();
            }

            return View(test);
        }

        // POST: QualityControl/EditQCTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQCTest(Guid id, QcTest test)
        {
            if (id != test.TestId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QcTests.AnyAsync(q =>
                        q.TestId != id &&
                        q.TestCode == test.TestCode))
                    {
                        TempData["ErrorMessage"] = "آزمایش با این کد قبلاً ثبت شده است";
                        return View(test);
                    }

                    var existingTest = await _context.QcTests.FindAsync(id);
                    if (existingTest == null)
                    {
                        return NotFound();
                    }

                    // Keep original creation data
                    test.CreatedDate = existingTest.CreatedDate;
                    test.CreatedBy = existingTest.CreatedBy;
                    test.LastModifiedDate = DateTime.Now;

                    _context.Entry(existingTest).CurrentValues.SetValues(test);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات آزمایش با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(QCTestList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QCTestExists(test.TestId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(test);
        }

        // POST: QualityControl/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var test = await _context.QcTests.FindAsync(id);
            if (test == null)
            {
                TempData["ErrorMessage"] = "آزمایش مورد نظر یافت نشد";
                return RedirectToAction(nameof(QCTestList));
            }

            // Check if test has any batch tests
            bool hasBatchTests = await _context.BatchTests.AnyAsync(b => b.TestId == id);

            if (hasBatchTests)
            {
                TempData["ErrorMessage"] = "این آزمایش دارای تست‌های بچ است و قابل حذف نیست";
                return RedirectToAction(nameof(QCTestList));
            }

            try
            {
                _context.QcTests.Remove(test);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "آزمایش با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف آزمایش: " + ex.Message;
            }

            return RedirectToAction(nameof(QCTestList));
        }

        // GET: QualityControl/CopyQCTest/5
        public async Task<IActionResult> CopyQCTest(Guid id)
        {
            var originalTest = await _context.QcTests.FindAsync(id);
            if (originalTest == null)
            {
                return NotFound();
            }

            // Create a copy with modified properties
            var copiedTest = new QcTest
            {
                // Reset primary key and audit fields
                TestId = Guid.NewGuid(),
                TestCode = "", // User will need to enter new code
                TestName = $"کپی از {originalTest.TestName}",
                CreatedDate = DateTime.Now,
                CreatedBy = null,
                LastModifiedBy = null,
                LastModifiedDate = null,

                // Copy all other properties
                TestType = originalTest.TestType,
                TestCategory = originalTest.TestCategory,
                TestMethod = originalTest.TestMethod,
                StandardReference = originalTest.StandardReference,
                Description = originalTest.Description,
                TestProcedure = originalTest.TestProcedure,
                EquipmentRequired = originalTest.EquipmentRequired,
                ReagentsRequired = originalTest.ReagentsRequired,
                SamplePreparation = originalTest.SamplePreparation,
                AcceptanceCriteria = originalTest.AcceptanceCriteria,
                SpecificationMin = originalTest.SpecificationMin,
                SpecificationMax = originalTest.SpecificationMax,
                UnitOfMeasure = originalTest.UnitOfMeasure,
                TestDurationMinutes = originalTest.TestDurationMinutes,
                TemperatureCondition = originalTest.TemperatureCondition,
                HumidityCondition = originalTest.HumidityCondition,
                StorageCondition = originalTest.StorageCondition,
                Frequency = originalTest.Frequency,
                CalibrationRequired = originalTest.CalibrationRequired,
                EnvironmentalControl = originalTest.EnvironmentalControl,
                SafetyRequirements = originalTest.SafetyRequirements,
                OperatorQualification = originalTest.OperatorQualification,
                DataIntegrityLevel = originalTest.DataIntegrityLevel,
                ApprovalRequired = originalTest.ApprovalRequired,
                CostPerTest = originalTest.CostPerTest,
                Currency = originalTest.Currency,
                ApplicableProducts = originalTest.ApplicableProducts,
                ApplicableStages = originalTest.ApplicableStages,
                RelatedTests = originalTest.RelatedTests,
                TrendAnalysisRequired = originalTest.TrendAnalysisRequired,
                StatisticalControl = originalTest.StatisticalControl,
                DeviationThreshold = originalTest.DeviationThreshold,
                AlertLimit = originalTest.AlertLimit,
                ActionLimit = originalTest.ActionLimit,
                RetestCriteria = originalTest.RetestCriteria,
                StabilityImpact = originalTest.StabilityImpact,
                RegulatoryRequirement = originalTest.RegulatoryRequirement,

                // Reset validation status for new test
                ValidationStatus = "نیاز به اعتبارسنجی",
                LastValidationDate = null,
                NextValidationDate = null,
                ChangeControlRequired = originalTest.ChangeControlRequired,

                ResponsibleDepartment = originalTest.ResponsibleDepartment,
                ResponsiblePerson = originalTest.ResponsiblePerson,
                BackupPerson = originalTest.BackupPerson,
                TrainingRequired = originalTest.TrainingRequired,
                DocumentReferences = originalTest.DocumentReferences,

                // Reset dates for new test
                EffectiveDate = null,
                ReviewDate = null,
                RetirementDate = null,

                PriorityLevel = originalTest.PriorityLevel,
                RiskLevel = originalTest.RiskLevel,
                ImpactLevel = originalTest.ImpactLevel,
                Notes = $"کپی شده از آزمایش {originalTest.TestCode} در تاریخ {DateTime.Now.ToString("yyyy/MM/dd")}",
                Tags = originalTest.Tags,
                IsActive = true,

                // Clear revision history for new test
                RevisionHistory = $"ایجاد شده از کپی آزمایش {originalTest.TestCode}"
            };

            ViewBag.OriginalTestCode = originalTest.TestCode;
            ViewBag.OriginalTestName = originalTest.TestName;
            ViewBag.IsCopy = true;

            return View("AddQCTest", copiedTest);
        }

        // POST: QualityControl/CopyQCTest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyQCTest(QcTest test)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QcTests.AnyAsync(q => q.TestCode == test.TestCode))
                    {
                        TempData["ErrorMessage"] = "آزمایش با این کد قبلاً ثبت شده است";
                        ViewBag.IsCopy = true;
                        return View("AddQCTest", test);
                    }

                    test.TestId = Guid.NewGuid();
                    test.CreatedDate = DateTime.Now;

                    _context.Add(test);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "آزمایش کپی شده با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(QCTestList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در کپی آزمایش: " + ex.Message;
                }
            }

            ViewBag.IsCopy = true;
            return View("AddQCTest", test);
        }

        private bool QCTestExists(Guid id)
        {
            return _context.QcTests.Any(e => e.TestId == id);
        }
    }
}