using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Api.Tests;

// Integration Testing: HTTP requests exercise routing and DTO validation without needing database access.
public class EmployeeValidationTests
{
    // Parameterized Testing: run the same create/update behavior checks with several invalid inputs.
    [Theory]
    [InlineData("Sara123", "sara@gmail.com", "IT", "Developer", 0, "Full name cannot contain numbers.")]
    [InlineData("Sara James", "Sara@gmail.com", "IT", "Developer", 0, "Email must be in lowercase.")]
    [InlineData("Sara James", "sara@gmail", "IT", "Developer", 0, "Enter a valid email address.")]
    [InlineData("Sara James", "sara@gmail.com", "  ", "Developer", 0, "Department is required.")]
    [InlineData("Sara James", "sara@gmail.com", "IT", "  ", 0, "Job title is required.")]
    [InlineData("Sara James", "sara@gmail.com", "IT", "Developer", 99, "Select a valid employee status.")]
    [InlineData("   ", "sara@gmail.com", "IT", "Developer", 0, "Full name is required.")]
    [InlineData("Sara!", "sara@gmail.com", "IT", "Developer", 0, "Full name must contain letters")]
    [InlineData("Sara James", "  ", "IT", "Developer", 0, "Email is required.")]
    public async Task CreateAndUpdateRejectInvalidData(string fullName, string email, string department, string jobTitle, int status, string expected)
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();
        var data = new { fullName, email, department, jobTitle, status };
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put })
        {
            using var request = new HttpRequestMessage(method, method == HttpMethod.Post ? "/api/employees" : "/api/employees/1") { Content = JsonContent.Create(data) };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expected, await response.Content.ReadAsStringAsync());
        }
    }
}

public class SqlIntegrationFactAttribute : FactAttribute
{
    public SqlIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION")))
            Skip = "Set TEST_SQL_CONNECTION to an isolated EmployeeManagementTests_ database.";
    }
}

public class GlobalExcelTests
{
    private static byte[] Workbook(params string[][][] sheets)
    {
        using var workbook = new XLWorkbook();
        for (int index = 0; index < sheets.Length; index++)
        {
            var sheet = workbook.AddWorksheet($"Sheet{index + 1}");
            for (int row = 0; row < sheets[index].Length; row++)
                for (int column = 0; column < sheets[index][row].Length; column++)
                    sheet.Cell(row + 1, column + 1).Value = sheets[index][row][column];
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static Task<HttpResponseMessage> Upload(HttpClient client, byte[] bytes, string name = "Additional Data.xlsx")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", name);
        return client.PostAsync("/api/excel", form);
    }

    [SqlIntegrationFact]
    // Regression Testing: verify schema migration keeps the latest workbook and preserves employee records.
    public async Task MigrationKeepsLatestWorkbookAndAllEmployees()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION")!);
        Assert.StartsWith("EmployeeManagementTests_", connection.InitialCatalog);
        connection.InitialCatalog += "_Migration";
        await using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await database.GetService<IMigrator>().MigrateAsync("20261029052544_AddEmployeeDocuments");
            var employee = new Employee { FullName = "Sara James", Email = "sara@gmail.com", Department = "IT", JobTitle = "Developer", DateOfJoining = DateTime.UtcNow };
            database.Employees.Add(employee);
            await database.SaveChangesAsync();
            var original = Workbook(new[] { new[] { "Email" }, new[] { "sara@gmail.com" } });
            foreach (var name in new[] { "Older.xlsx", "Latest.xlsx" })
                await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO EmployeeDocuments (EmployeeId, FileName, ContentType, FileData, UploadedAt) VALUES ({employee.Id}, {name}, {"application/octet-stream"}, {original}, {DateTime.UtcNow})");
            await database.Database.MigrateAsync();
            var document = await database.EmployeeDocuments.SingleAsync();
            Assert.Equal("Latest.xlsx", document.FileName);
            Assert.Equal(original, document.FileData);
            Assert.Equal(1, await database.Employees.CountAsync());
            Assert.Equal("sara@gmail.com", (await database.Employees.SingleAsync()).Email);
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    [SqlIntegrationFact]
    public async Task GlobalWorkbookAndEmployeeCrud_WorkWithoutChangingEachOther()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;
        await using var database = new AppDbContext(options);
        // Test Isolation: use a dedicated test database and refuse normal application database names.
        Assert.StartsWith("EmployeeManagementTests_", database.Database.GetDbConnection().Database);
        await database.Database.MigrateAsync();
        try
        {
            await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.UseSetting("ConnectionStrings:DefaultConnection", connection));
            using var client = app.CreateClient();
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/excel")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/excel/download")).StatusCode);
            var create = await client.PostAsJsonAsync("/api/employees", new { fullName = " Anne-Marie O'Connor ", email = " sara@gmail.com ", department = " IT ", jobTitle = " Developer ", status = 0 });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var employee = await create.Content.ReadFromJsonAsync<Employee>();
            Assert.Equal("Anne-Marie O'Connor", employee!.FullName);
            Assert.Equal("sara@gmail.com", employee.Email);
            Assert.Equal("IT", employee.Department);
            var duplicate = await client.PostAsJsonAsync("/api/employees", new { fullName = "Other Person", email = "sara@gmail.com", department = "IT", jobTitle = "Developer", status = 0 });
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            var original = Workbook(new[] { new[] { " EMAIL ", "Phone", "Skills" }, new[] { "SARA@GMAIL.COM", "59123456", "React, C#" } }, Array.Empty<string[]>());
            Assert.Equal(HttpStatusCode.OK, (await Upload(client, original)).StatusCode);
            var preview = JsonDocument.Parse(await client.GetStringAsync("/api/excel"));
            Assert.Equal(2, preview.RootElement.GetProperty("sheets").GetArrayLength());
            Assert.Equal("59123456", preview.RootElement.GetProperty("sheets")[0].GetProperty("rows")[1][1].GetString());
            var download = await client.GetAsync("/api/excel/download");
            Assert.Equal(original, await download.Content.ReadAsByteArrayAsync());
            Assert.Contains("Additional Data.xlsx", download.Content.Headers.ContentDisposition!.ToString());

            var invalid = new (byte[] bytes, string name, string message)[] {
                (new byte[] { 1 }, "file.txt", "Only .xlsx Excel files are allowed."),
                (new byte[] { 1 }, "file.xlsx", "The uploaded file is not a valid Excel workbook."),
                (Array.Empty<byte>(), "file.xlsx", "Please select an Excel file."),
                (Workbook(Array.Empty<string[]>()), "file.xlsx", "The Excel file does not contain any data."),
                (Workbook(new[] { new[] { "Email" } }), "file.xlsx", "The Excel file does not contain any data."),
                (Workbook(new[] { new[] { "Product", "Price" }, new[] { "Laptop", "50000" } }), "file.xlsx", "The Excel file must contain an Email column to identify employees."),
                (Workbook(new[] { new[] { "Email", "Notes" }, new[] { "", "Missing email" } }), "file.xlsx", "Row 2 is missing an employee email."),
                (Workbook(new[] { new[] { "Email" }, new[] { "unknown@gmail.com" } }), "file.xlsx", "Row 2: Employee with email 'unknown@gmail.com' does not exist."),
                (Workbook(new[] { new[] { "Email" }, new[] { "sara@gmail.com" } }, new[] { new[] { "Email" }, new[] { "unknown@gmail.com" } }), "file.xlsx", "Row 2: Employee with email 'unknown@gmail.com' does not exist."),
                (Workbook(new[] { new[] { "Email", " email " }, new[] { "sara@gmail.com", "sara@gmail.com" } }), "file.xlsx", "must contain only one Email column.")
            };
            // Behavior Testing: failed uploads must return the reason and preserve the exact previous file.
            foreach (var test in invalid)
            {
                var response = await Upload(client, test.bytes, test.name);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains(test.message, await response.Content.ReadAsStringAsync());
                Assert.Equal(original, await client.GetByteArrayAsync("/api/excel/download"));
            }
            using var emptyForm = new MultipartFormDataContent();
            var missing = await client.PostAsync("/api/excel", emptyForm);
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
            Assert.Contains("Please select an Excel file.", await missing.Content.ReadAsStringAsync());
            database.ChangeTracker.Clear();
            var unchanged = await database.Employees.SingleAsync();
            Assert.Equal(employee.Email, unchanged.Email);
            Assert.Equal(employee.Department, unchanged.Department);
            Assert.Equal(employee.JobTitle, unchanged.JobTitle);
            Assert.Equal(employee.Status, unchanged.Status);

            var replacement = Workbook(new[] { new[] { "Email", "Notes" }, new[] { "sara@gmail.com", "Replacement" } });
            Assert.Equal(HttpStatusCode.OK, (await Upload(client, replacement, "Replacement.xlsx")).StatusCode);
            Assert.Equal(1, await database.EmployeeDocuments.CountAsync());
            Assert.Equal(replacement, await client.GetByteArrayAsync("/api/excel/download"));
            var update = await client.PutAsJsonAsync($"/api/employees/{employee.Id}", new { fullName = "Sara James", email = "sara@gmail.com", department = "HR", jobTitle = "Manager", status = 1 });
            Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/employees/{employee.Id}")).StatusCode);
            Assert.Equal(replacement, await client.GetByteArrayAsync("/api/excel/download"));
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }
}
