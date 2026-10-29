using System.Data;
using ClosedXML.Excel;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

// SRP: this controller focuses on global Excel upload, validation, viewing, and download.
// Dependency Injection: ASP.NET Core supplies AppDbContext through this primary constructor.
[ApiController]
[Route("api/excel")]
public class ExcelController(AppDbContext context) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpPost]
    public async Task<IActionResult> Upload()
    {
        // Fail Fast: reject missing files and unsupported extensions before workbook processing.
        if (!Request.HasFormContentType)
            return BadRequest("Please select an Excel file.");
        IFormFile? file;
        try { file = (await Request.ReadFormAsync()).Files.GetFile("file"); }
        catch (InvalidDataException) { return BadRequest("Please select an Excel file."); }
        if (file == null || file.Length == 0)
            return BadRequest("Please select an Excel file.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx Excel files are allowed.");

        using var stream = new MemoryStream();
        // Async I/O: await the uploaded file copy rather than blocking while it is read.
        await file.CopyToAsync(stream);
        stream.Position = 0;
        XLWorkbook workbook;
        // Input Validation: parse the workbook because an .xlsx extension alone does not prove validity.
        try { workbook = new XLWorkbook(stream); }
        catch { return BadRequest("The uploaded file is not a valid Excel workbook."); }

        using (workbook)
        {
            // Data Integrity: compare workbook emails to existing employees, ignoring case.
            var emails = new HashSet<string>(await context.Employees.AsNoTracking()
                .Select(e => e.Email).ToListAsync(), StringComparer.OrdinalIgnoreCase);
            string? validationError;
            try { validationError = ValidateWorkbook(workbook, emails); }
            catch { return BadRequest("The uploaded file is not a valid Excel workbook."); }
            // Graceful Failure: validation errors leave the previously stored workbook untouched.
            if (validationError != null) return BadRequest(validationError);
        }

        // Transaction: commit only after full validation; serializable isolation protects concurrent uploads.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var document = await context.EmployeeDocuments.SingleOrDefaultAsync();
        if (document == null)
        {
            document = new EmployeeDocument();
            context.EmployeeDocuments.Add(document);
        }
        document.FileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        document.ContentType = ExcelContentType;
        document.FileData = stream.ToArray(); // Store the original bytes, never re-save with ClosedXML.
        document.UploadedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { fileName = document.FileName, uploadedAt = document.UploadedAt });
    }

    // Encapsulation: workbook validation stays private to the Excel controller.
    private static string? ValidateWorkbook(XLWorkbook workbook, HashSet<string> emails)
    {
        bool hasEmployeeRows = false;
        foreach (var sheet in workbook.Worksheets)
        {
            // Formatting-only cells and entirely blank rows do not count as data.
            var rows = sheet.RowsUsed(XLCellsUsedOptions.Contents)
                .Where(row => row.CellsUsed(XLCellsUsedOptions.Contents)
                    .Any(cell => !string.IsNullOrWhiteSpace(cell.GetFormattedString()))).ToList();
            if (rows.Count == 0) continue;
            // Business Rule: each non-empty sheet uses its first non-empty row as headers and requires Email.
            var emailHeaders = rows[0].CellsUsed(XLCellsUsedOptions.Contents)
                .Where(cell => string.Equals(cell.GetString().Trim(), "Email", StringComparison.OrdinalIgnoreCase)).ToList();
            if (emailHeaders.Count == 0)
                return "The Excel file must contain an Email column to identify employees.";
            if (emailHeaders.Count > 1)
                return $"Worksheet '{sheet.Name}' must contain only one Email column.";
            int emailColumn = emailHeaders[0].Address.ColumnNumber;
            foreach (var row in rows.Skip(1))
            {
                hasEmployeeRows = true;
                string email = row.Cell(emailColumn).GetFormattedString().Trim();
                if (email.Length == 0) return $"Row {row.RowNumber()} is missing an employee email.";
                if (!emails.Contains(email))
                    return $"Row {row.RowNumber()}: Employee with email '{email}' does not exist.";
            }
        }
        return hasEmployeeRows ? null : "The Excel file does not contain any data.";
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var document = await context.EmployeeDocuments.AsNoTracking().SingleOrDefaultAsync();
        if (document == null) return NotFound("No Excel file uploaded.");
        try
        {
            using var stream = new MemoryStream(document.FileData);
            using var workbook = new XLWorkbook(stream);
            var sheets = workbook.Worksheets.Select(sheet =>
            {
                int lastRow = sheet.LastRowUsed(XLCellsUsedOptions.Contents)?.RowNumber() ?? 0;
                int lastColumn = sheet.LastColumnUsed(XLCellsUsedOptions.Contents)?.ColumnNumber() ?? 0;
                // Preserve blank cells and row positions for spreadsheet coordinates.
                var rows = Enumerable.Range(1, lastRow).Select(row =>
                    Enumerable.Range(1, lastColumn).Select(column =>
                        sheet.Cell(row, column).GetFormattedString()).ToList()).ToList();
                return new { name = sheet.Name, rows };
            }).ToList();
            return Ok(new { fileName = document.FileName, uploadedAt = document.UploadedAt, sheets });
        }
        catch { return Problem("The stored Excel workbook could not be read.", statusCode: 500); }
    }

    // HTTP Download: return the original bytes and filename, without regenerating the workbook.
    [HttpGet("download")]
    public async Task<IActionResult> Download()
    {
        var document = await context.EmployeeDocuments.AsNoTracking().SingleOrDefaultAsync();
        if (document == null) return NotFound("No Excel file uploaded.");
        return File(document.FileData, ExcelContentType, document.FileName);
    }
}
