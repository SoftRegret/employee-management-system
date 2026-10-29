// Data Access / ORM: DbContext lets EF Core query and persist SQL Server data as C# objects.
using EmployeeManagement.Api.Models;

//gives us EF Core classes such as DbContext, DbContextOptions, and DbSet
//without this, we would have to write our own code to connect to the database and perform CRUD operations
using Microsoft.EntityFrameworkCore;

//avoid naming conflicts with other classes in the application
namespace EmployeeManagement.Api.Data;

public class AppDbContext : DbContext //: means that AppDbContext inherits from DbContext
{   //constructor when an object is created.
    public AppDbContext(DbContextOptions<AppDbContext> options) //options: configuration settings for this DbContext.
        : base(options) //pass the options to the parent class constructor (DbContext) so that it can use them to configure the database connection and other settings.
    {
    }
    // DbSet: these collections map employee and global workbook records to separate database tables.
    public DbSet<Employee> Employees { get; set; } //connects the Employee model to the Employees table in the database. EF Core will use this property to perform CRUD operations on the Employees table.
    public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
}