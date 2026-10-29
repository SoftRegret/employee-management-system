namespace EmployeeManagement.Api.Models;

public class EmployeeDocument
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] FileData { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAt { get; set; }
}