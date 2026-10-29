using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace EmployeeManagement.Api.Tests;

// Integration Testing: WebApplicationFactory exercises HTTP routing, controllers, and configured dependencies.
public class EmployeesApiTests
{
    [Fact] //tells xUnit that this is a test method and run it when dotnet is executed.
    public async Task GetEmployees_ReturnsSuccessStatusCode()
    {
        await using var application =
            new WebApplicationFactory<Program>();

        var client = application.CreateClient();

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployee_WithInvalidId_ReturnsNotFound()
    {
        await using var application =
            new WebApplicationFactory<Program>(); //lets the test project create a test version of the ASP.NET Core application.

        var client = application.CreateClient();

        var response = await client.GetAsync("/api/employees/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_WithInvalidEmail_ReturnsBadRequest()
    {
        await using var application =
            new WebApplicationFactory<Program>();

        var client = application.CreateClient();

        var employee = new
        {
            fullName = "Test Employee",
            email = "not-an-email",
            department = "IT",
            jobTitle = "Developer",
            status = 0
        };

        var response =
            await client.PostAsJsonAsync("/api/employees", employee);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_WithInvalidId_ReturnsNotFound()
    {
        await using var application =
            new WebApplicationFactory<Program>();

        var client = application.CreateClient();

        var response =
            await client.DeleteAsync("/api/employees/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}