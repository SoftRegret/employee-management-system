# Employee Management System

A full-stack Employee Management System built as part of the BDO IT Consulting Internship 2026.

The application allows users to create, view, update, search, and delete employee records through a React frontend connected to an ASP.NET Core Web API and SQL Server database.

## Technologies

### Frontend
- React
- Vite
- JavaScript
- CSS

### Backend
- ASP.NET Core Web API
- C#
- Entity Framework Core
- SQL Server

### Testing
- xUnit
- ASP.NET Core Integration Testing

### DevOps
- Docker
- Docker Compose
- Git
- GitHub

## Employee Fields

Each employee contains:

- Id
- Full Name
- Email
- Department
- Job Title
- Date of Joining
- Status (Active / Inactive)

## Features

- View all employees
- Add a new employee
- Edit an existing employee
- Delete an employee
- Search employees by name or email
- Email validation
- Duplicate email prevention
- Active / Inactive employee status
- Persistent SQL Server storage
- REST API
- API integration tests
- Dockerized frontend, backend, and database

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/employees` | Get all employees |
| GET | `/api/employees/{id}` | Get employee by ID |
| POST | `/api/employees` | Create an employee |
| PUT | `/api/employees/{id}` | Update an employee |
| DELETE | `/api/employees/{id}` | Delete an employee |

## Project Structure

```text
employee-management-system/
├── backend/
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Migrations/
│   ├── Models/
│   ├── Dockerfile
│   └── Program.cs
├── frontend/
│   ├── src/
│   │   ├── Components/
│   │   └── Services/
│   └── Dockerfile
├── EmployeeManagement.Api.Tests/
├── docker-compose.yml
└── README.md