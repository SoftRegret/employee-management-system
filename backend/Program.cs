//tells ASP.NET Core what services the application needs and how the application should behave when it starts.
using EmployeeManagement.Api.Data;
//allows us too use EF Core functionaliity such as UseSqlServer() to configure the database connection.
using Microsoft.EntityFrameworkCore;

//the builder is used to configure things the application will need before the application runs.
var builder = WebApplication.CreateBuilder(args);

//register AppDbContext and tell it to use SQL Server with DefaultConnection.
builder.Services.AddDbContext<AppDbContext>(options => //AddDbContext registers AppDbContext with ASP.NET Core so it can be injected where needed.
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

//enable controller support in the application.
builder.Services.AddControllers();

//OpenAPI describes/document API endpoints in a standardized format.
// Register services with ASP.NET Core
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

// Enable OpenAPI while developing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Return a safe, useful response instead of exposing server exception details.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    context.Response.StatusCode = 500;
    await context.Response.WriteAsJsonAsync(new { message = "An unexpected server error occurred. Please try again." });
}));

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");
//makes controller routes available.
app.MapControllers();

// Start the application
app.Run();

public partial class Program { }