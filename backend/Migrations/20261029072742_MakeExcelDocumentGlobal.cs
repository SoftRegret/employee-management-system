using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeExcelDocumentGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the latest legacy upload as the single current global workbook.
            // This touches documents only; Employees and their data are unchanged.
            migrationBuilder.Sql("""
                DELETE FROM [EmployeeDocuments]
                WHERE [Id] NOT IN (
                    SELECT TOP (1) [Id] FROM [EmployeeDocuments]
                    ORDER BY [UploadedAt] DESC, [Id] DESC
                );
                """);

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "EmployeeDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "EmployeeDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
