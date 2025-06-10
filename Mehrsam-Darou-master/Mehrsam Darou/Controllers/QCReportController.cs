using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class QCReportController : BaseController
    {
        private readonly DarouAppContext _context;

        public QCReportController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: QCReport/QCReportList
        public async Task<IActionResult> QCReportList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<QcReport> query = _context.QcReports;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(qr => qr.ReportNumber.Contains(searchKey) ||
                                     qr.ReportTitle.Contains(searchKey) ||
                                     qr.ReportType.Contains(searchKey) ||
                                     qr.PreparedBy.Contains(searchKey))
                            .OrderByDescending(qr => qr.CreatedDate);
            }
            else
            {
                query = query.OrderByDescending(qr => qr.CreatedDate);
            }

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<QcReport>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: QCReport/AddQCReport
        public IActionResult AddQCReport()
        {
            return View(new QcReport
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                ReportStatus = "پیش‌نویس",
                PriorityLevel = 3,
                ConfidentialityLevel = "داخلی"
            });
        }

        // POST: QCReport/AddQCReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQCReport(QcReport qcReport)
        {
            // Remove navigation property validation errors
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("LastModifiedByNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QcReports.AnyAsync(qr => qr.ReportNumber == qcReport.ReportNumber))
                    {
                        TempData["ErrorMessage"] = "گزارش با این شماره قبلاً ثبت شده است";
                        return View(qcReport);
                    }

                    qcReport.ReportId = Guid.NewGuid();
                    qcReport.CreatedDate = DateTime.Now;

                    _context.Add(qcReport);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "گزارش جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(QCReportList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد گزارش: " + ex.Message;
                }
            }

            return View(qcReport);
        }

        // GET: QCReport/EditQCReport/5
        public async Task<IActionResult> EditQCReport(Guid id)
        {
            var qcReport = await _context.QcReports.FindAsync(id);
            if (qcReport == null)
            {
                return NotFound();
            }

            return View(qcReport);
        }

        // POST: QCReport/EditQCReport/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQCReport(Guid id, QcReport qcReport)
        {
            if (id != qcReport.ReportId)
            {
                return NotFound();
            }

            // Remove navigation property validation errors
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("LastModifiedByNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QcReports.AnyAsync(qr =>
                        qr.ReportId != id &&
                        qr.ReportNumber == qcReport.ReportNumber))
                    {
                        TempData["ErrorMessage"] = "گزارش با این شماره قبلاً ثبت شده است";
                        return View(qcReport);
                    }

                    var existingQcReport = await _context.QcReports.FindAsync(id);
                    if (existingQcReport == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    qcReport.CreatedDate = existingQcReport.CreatedDate;
                    qcReport.LastModifiedDate = DateTime.Now;

                    _context.Entry(existingQcReport).CurrentValues.SetValues(qcReport);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات گزارش با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(QCReportList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QCReportExists(qcReport.ReportId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(qcReport);
        }

        // POST: QCReport/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var qcReport = await _context.QcReports.FindAsync(id);
            if (qcReport == null)
            {
                TempData["ErrorMessage"] = "گزارش مورد نظر یافت نشد";
                return RedirectToAction(nameof(QCReportList));
            }

            try
            {
                _context.QcReports.Remove(qcReport);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "گزارش با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف گزارش: " + ex.Message;
            }

            return RedirectToAction(nameof(QCReportList));
        }

        // GET: QCReport/DownloadReport/5
        public async Task<IActionResult> DownloadReport(Guid id, string format = "pdf")
        {
            var qcReport = await _context.QcReports.FindAsync(id);
            if (qcReport == null)
            {
                return NotFound();
            }

            try
            {
                byte[] fileBytes;
                string fileName;
                string contentType;

                switch (format.ToLower())
                {
                    case "pdf":
                        fileBytes = GeneratePdfReport(qcReport);
                        fileName = $"QC_Report_{qcReport.ReportNumber}_{DateTime.Now:yyyyMMdd}.html";
                        contentType = "text/html; charset=utf-8";
                        break;
                    case "excel":
                        fileBytes = GenerateExcelReport(qcReport);
                        fileName = $"QC_Report_{qcReport.ReportNumber}_{DateTime.Now:yyyyMMdd}.csv";
                        contentType = "text/csv; charset=utf-8";
                        break;
                    case "word":
                        fileBytes = GenerateWordReport(qcReport);
                        fileName = $"QC_Report_{qcReport.ReportNumber}_{DateTime.Now:yyyyMMdd}.rtf";
                        contentType = "application/rtf";
                        break;
                    default:
                        return BadRequest("فرمت پشتیبانی نشده");
                }

                // Add UTF-8 BOM for proper Persian text display
                if (format.ToLower() == "excel")
                {
                    var bom = new byte[] { 0xEF, 0xBB, 0xBF };
                    var withBom = new byte[bom.Length + fileBytes.Length];
                    bom.CopyTo(withBom, 0);
                    fileBytes.CopyTo(withBom, bom.Length);
                    fileBytes = withBom;
                }

                Response.Headers.Add("Content-Disposition", $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileName)}");
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در ایجاد فایل گزارش: " + ex.Message;
                return RedirectToAction(nameof(QCReportList));
            }
        }

        // POST: QCReport/BulkDownload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDownload([FromForm] string reportIds, [FromForm] string format = "pdf")
        {
            if (string.IsNullOrEmpty(reportIds))
            {
                TempData["ErrorMessage"] = "هیچ گزارشی انتخاب نشده است";
                return RedirectToAction(nameof(QCReportList));
            }

            try
            {
                var idList = reportIds.Split(',')
                    .Where(x => Guid.TryParse(x, out _))
                    .Select(Guid.Parse)
                    .ToList();

                if (!idList.Any())
                {
                    TempData["ErrorMessage"] = "شناسه گزارش‌های انتخاب شده معتبر نیست";
                    return RedirectToAction(nameof(QCReportList));
                }

                var reports = await _context.QcReports
                    .Where(r => idList.Contains(r.ReportId))
                    .ToListAsync();

                if (!reports.Any())
                {
                    TempData["ErrorMessage"] = "گزارش‌های انتخاب شده یافت نشدند";
                    return RedirectToAction(nameof(QCReportList));
                }

                // For simplicity, return the first report if only one is selected
                if (reports.Count == 1)
                {
                    return await DownloadReport(reports.First().ReportId, format);
                }

                // Create a ZIP file containing all reports
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        foreach (var report in reports)
                        {
                            byte[] fileBytes;
                            string fileName;

                            switch (format.ToLower())
                            {
                                case "pdf":
                                    fileBytes = GeneratePdfReport(report);
                                    fileName = $"QC_Report_{report.ReportNumber}.html";
                                    break;
                                case "excel":
                                    fileBytes = GenerateExcelReport(report);
                                    fileName = $"QC_Report_{report.ReportNumber}.csv";
                                    break;
                                case "word":
                                    fileBytes = GenerateWordReport(report);
                                    fileName = $"QC_Report_{report.ReportNumber}.rtf";
                                    break;
                                default:
                                    continue;
                            }

                            // Add UTF-8 BOM for CSV files
                            if (format.ToLower() == "excel")
                            {
                                var bom = new byte[] { 0xEF, 0xBB, 0xBF };
                                var withBom = new byte[bom.Length + fileBytes.Length];
                                bom.CopyTo(withBom, 0);
                                fileBytes.CopyTo(withBom, bom.Length);
                                fileBytes = withBom;
                            }

                            var zipEntry = archive.CreateEntry(fileName);
                            using (var zipStream = zipEntry.Open())
                            {
                                zipStream.Write(fileBytes, 0, fileBytes.Length);
                            }
                        }
                    }

                    var zipFileName = $"QC_Reports_Bulk_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                    var zipBytes = memoryStream.ToArray();

                    Response.Headers.Add("Content-Disposition", $"attachment; filename*=UTF-8''{Uri.EscapeDataString(zipFileName)}");
                    return File(zipBytes, "application/zip", zipFileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در ایجاد فایل‌های گزارش: " + ex.Message;
                return RedirectToAction(nameof(QCReportList));
            }
        }

        public class BulkDownloadRequest
        {
            public List<Guid> ReportIds { get; set; } = new List<Guid>();
            public string Format { get; set; } = "pdf";
        }

        private byte[] GeneratePdfReport(QcReport report)
        {
            // Using System.Text for simple HTML to PDF conversion
            // In production, you would use libraries like iTextSharp, SelectPdf, or DinkToPdf

            var html = GenerateReportHtml(report);

            // For now, return HTML as bytes (you should replace this with actual PDF generation)
            // This is a placeholder - implement actual PDF generation based on your requirements
            var htmlBytes = System.Text.Encoding.UTF8.GetBytes(html);

            // TODO: Implement actual PDF generation
            // Example with iTextSharp:
            // using (var stream = new MemoryStream())
            // {
            //     var document = new Document();
            //     var writer = PdfWriter.GetInstance(document, stream);
            //     document.Open();
            //     // Add content to PDF
            //     document.Close();
            //     return stream.ToArray();
            // }

            return htmlBytes;
        }

        private byte[] GenerateExcelReport(QcReport report)
        {
            // Using System.IO for Excel generation
            // In production, you would use libraries like EPPlus, ClosedXML, or NPOI

            using (var stream = new MemoryStream())
            {
                // TODO: Implement actual Excel generation
                // Example with EPPlus:
                // using (var package = new ExcelPackage(stream))
                // {
                //     var worksheet = package.Workbook.Worksheets.Add("QC Report");
                //     worksheet.Cells[1, 1].Value = "شماره گزارش";
                //     worksheet.Cells[1, 2].Value = report.ReportNumber;
                //     // Add more cells...
                //     package.Save();
                // }

                // Placeholder CSV content
                var csv = GenerateReportCsv(report);
                var csvBytes = System.Text.Encoding.UTF8.GetBytes(csv);
                stream.Write(csvBytes, 0, csvBytes.Length);

                return stream.ToArray();
            }
        }

        private byte[] GenerateWordReport(QcReport report)
        {
            // Using System.IO for Word generation
            // In production, you would use libraries like DocumentFormat.OpenXml or DocX

            using (var stream = new MemoryStream())
            {
                // TODO: Implement actual Word document generation
                // For now, return RTF format as placeholder
                var rtf = GenerateReportRtf(report);
                var rtfBytes = System.Text.Encoding.UTF8.GetBytes(rtf);
                stream.Write(rtfBytes, 0, rtfBytes.Length);

                return stream.ToArray();
            }
        }

        private string GenerateReportHtml(QcReport report)
        {
            var html = $@"
<!DOCTYPE html>
<html dir='rtl' lang='fa'>
<head>
    <meta charset='utf-8'>
    <title>گزارش کنترل کیفیت - {report.ReportNumber}</title>
    <style>
        body {{ font-family: 'B Nazanin', 'Tahoma', sans-serif; margin: 20px; }}
        .header {{ text-align: center; border-bottom: 2px solid #333; padding-bottom: 10px; }}
        .section {{ margin: 15px 0; }}
        .label {{ font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: right; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>گزارش کنترل کیفیت</h1>
        <h2>{report.ReportTitle}</h2>
        <p>شماره گزارش: {report.ReportNumber}</p>
    </div>
    
    <div class='section'>
        <h3>اطلاعات پایه</h3>
        <table>
            <tr><th>نوع گزارش</th><td>{report.ReportType ?? "-"}</td></tr>
            <tr><th>دسته‌بندی</th><td>{report.ReportCategory ?? "-"}</td></tr>
            <tr><th>وضعیت</th><td>{report.ReportStatus ?? "-"}</td></tr>
            <tr><th>دوره گزارش</th><td>{report.ReportPeriod ?? "-"}</td></tr>
        </table>
    </div>
    
    <div class='section'>
        <h3>اطلاعات پرسنل</h3>
        <table>
            <tr><th>تهیه‌کننده</th><td>{report.PreparedBy ?? "-"}</td></tr>
            <tr><th>بازبین</th><td>{report.ReviewedBy ?? "-"}</td></tr>
            <tr><th>تایید کننده</th><td>{report.ApprovedBy ?? "-"}</td></tr>
        </table>
    </div>
    
    <div class='section'>
        <h3>خلاصه اجرایی</h3>
        <p>{report.ExecutiveSummary ?? "-"}</p>
    </div>
    
    <div class='section'>
        <h3>یافته‌های کلیدی</h3>
        <p>{report.KeyFindings ?? "-"}</p>
    </div>
    
    <div class='section'>
        <h3>توصیه‌ها</h3>
        <p>{report.Recommendations ?? "-"}</p>
    </div>
    
    <div class='section'>
        <h3>نتیجه‌گیری</h3>
        <p>{report.Conclusions ?? "-"}</p>
    </div>
    
    <div class='section'>
        <p><strong>تاریخ تولید گزارش:</strong> {DateTime.Now.ToString("yyyy/MM/dd HH:mm")}</p>
    </div>
</body>
</html>";

            return html;
        }

        private string GenerateReportCsv(QcReport report)
        {
            var csv = $@"فیلد,مقدار
شماره گزارش,{report.ReportNumber}
عنوان گزارش,{report.ReportTitle}
نوع گزارش,{report.ReportType ?? ""}
دسته‌بندی,{report.ReportCategory ?? ""}
وضعیت,{report.ReportStatus ?? ""}
دوره گزارش,{report.ReportPeriod ?? ""}
تهیه‌کننده,{report.PreparedBy ?? ""}
بازبین,{report.ReviewedBy ?? ""}
تایید کننده,{report.ApprovedBy ?? ""}
خلاصه اجرایی,""{report.ExecutiveSummary ?? ""}""
یافته‌های کلیدی,""{report.KeyFindings ?? ""}""
توصیه‌ها,""{report.Recommendations ?? ""}""
نتیجه‌گیری,""{report.Conclusions ?? ""}""
تاریخ ایجاد,{report.CreatedDate.ToString("yyyy/MM/dd")}";

            return csv;
        }

        private string GenerateReportRtf(QcReport report)
        {
            var rtf = $@"{{\rtf1\ansi\deff0 {{\fonttbl {{\f0 Times New Roman;}}}}
\f0\fs24 
\qc\b گزارش کنترل کیفیت\b0\par
\qc\b {report.ReportTitle}\b0\par
\qc شماره گزارش: {report.ReportNumber}\par
\par
\ql\b اطلاعات پایه:\b0\par
نوع گزارش: {report.ReportType ?? "-"}\par
دسته‌بندی: {report.ReportCategory ?? "-"}\par
وضعیت: {report.ReportStatus ?? "-"}\par
\par
\ql\b اطلاعات پرسنل:\b0\par
تهیه‌کننده: {report.PreparedBy ?? "-"}\par
بازبین: {report.ReviewedBy ?? "-"}\par
تایید کننده: {report.ApprovedBy ?? "-"}\par
\par
\ql\b خلاصه اجرایی:\b0\par
{report.ExecutiveSummary ?? "-"}\par
\par
\ql\b یافته‌های کلیدی:\b0\par
{report.KeyFindings ?? "-"}\par
\par
\ql\b توصیه‌ها:\b0\par
{report.Recommendations ?? "-"}\par
\par
\ql\b نتیجه‌گیری:\b0\par
{report.Conclusions ?? "-"}\par
\par
تاریخ تولید گزارش: {DateTime.Now.ToString("yyyy/MM/dd HH:mm")}\par
}}";

            return rtf;
        }

        private bool QCReportExists(Guid id)
        {
            return _context.QcReports.Any(e => e.ReportId == id);
        }
    }
}