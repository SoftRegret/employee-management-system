namespace EmployeeManagement.Api.Models;

// Separation of Concerns: the global workbook is stored independently of Employee records.
public class EmployeeDocument
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    // Persistence: original file bytes are stored so downloads preserve the uploaded workbook.
    public byte[] FileData { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAt { get; set; }
}