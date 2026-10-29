# Global Excel and validation guide

The application keeps Employee CRUD and one global Excel workbook separate. No npm or NuGet packages were added, no files were removed, and the application database has not been migrated or reset during this implementation.

## Files

Modified existing files (including files that already had uncommitted Excel changes):

- `backend/Controllers/EmployeesController.cs`: removes employee Excel endpoints and filename projection; trims input; returns field-specific duplicate-email errors.
- `backend/DTOs/CreateEmployeeDto.cs`: shared Data Annotation and custom validation.
- `backend/DTOs/UpdateEmployeeDto.cs`: inherits the same input rules.
- `backend/DTOs/EmployeeResponseDto.cs`: removes ExcelFileName. This file was already untracked when work began; it was edited rather than newly introduced by this work.
- `backend/Models/EmployeeDocument.cs`: removes EmployeeId.
- `backend/Migrations/AppDbContextModelSnapshot.cs`: records the global document model.
- `backend/Program.cs`: exposes Content-Disposition to React through CORS and returns safe server errors.
- `frontend/src/App.jsx`: employee management page and simple pathname selection for /excel.
- `frontend/src/App.css`: wider layout, spacing, inline errors, and spreadsheet styling.
- `frontend/src/Components/EmployeeList.jsx`: employee rows now have Edit and Delete only.
- `frontend/src/Components/EmployeeForm.jsx`: explicit validation, field errors, pending state, and API errors.
- `frontend/src/Services/employeeServices.js`: global Excel API functions, useful API errors, and original-filename download.
- `README.md`: links to this guide.

Created files:

- `backend/Controllers/ExcelController.cs`
- `backend/Migrations/20261029072742_MakeExcelDocumentGlobal.cs`
- `backend/Migrations/20261029072742_MakeExcelDocumentGlobal.Designer.cs`
- `frontend/src/Components/ExcelDocument.jsx`
- `frontend/src/Components/ExcelViewer.jsx`
- `EmployeeManagement.Api.Tests/GlobalExcelTests.cs`
- `GLOBAL_EXCEL_GUIDE.md`

No source files were removed. Employee.cs, AppDbContext.cs, the existing migrations, Docker configuration, and package manifests did not need changes. AppDbContext still uses EmployeeDocuments as the table/DbSet name; the model no longer has an EmployeeId. The old model had no configured foreign key or navigation relationship to remove.

## Backend and frontend behavior

| Method | Endpoint | Behavior |
| --- | --- | --- |
| POST | /api/excel | Validate every sheet, then store or replace the single workbook |
| GET | /api/excel | Return fileName, uploadedAt, and dynamic sheets containing name and rows |
| GET | /api/excel/download | Return original stored bytes and original filename |

The old /api/employees/{id}/excel endpoints have been removed. Employee endpoints retain their existing CRUD responsibilities.

The main page has a separate Excel Document section. Upload is always present and is temporarily disabled during an operation. View and Download appear when a workbook exists. Upload errors stay next to this section. Invalid uploads keep the displayed filename and stored workbook unchanged.

View Excel is an ordinary link with target="_blank" and opens /excel in a new browser tab. A simple pathname check selects the viewer; React Router is not installed or needed. The viewer fetches /api/excel, displays the original filename, A/B/C column labels, row numbers, cell borders, scrolling in both directions, sheet buttons, empty-sheet/loading/error states, and a Download button. It is read-only and displays formatted cell values; it does not reproduce Excel charts, merged-cell styling, or full desktop editing.

Download uses the stored bytes directly, never a newly generated workbook. The API sets Content-Disposition, CORS exposes that header, and React uses it to preserve the filename, with the loaded filename as fallback.

## Validation

For both adding and editing employees:

- Full Name is required, cannot contain digits, and must contain a letter. Unicode letters and combining marks, ordinary spaces, hyphens, and straight apostrophes are accepted. Unsupported characters produce a specific error.
- Email is required, must have an address format with a dotted domain, and must already be lowercase. Uppercase is rejected rather than silently converted.
- Department and Job Title are required and cannot be whitespace.
- Leading/trailing whitespace is trimmed before submission and storage.
- Status must be Active (0) or Inactive (1).
- The API checks duplicate email case-insensitively, excluding the current employee when editing, and returns 409 with an Email field error.

React validates before sending a request. ASP.NET validates the DTO independently, so bypassing React with Postman cannot bypass these rules. UpdateEmployeeDto inherits CreateEmployeeDto to share the rules without a validation library. Required and EnumDataType are built-in Data Annotations; IValidatableObject implements the small custom name/email checks. With ApiController, invalid DTOs automatically produce 400 ValidationProblemDetails.

The React service reads ValidationProblemDetails.errors, JSON messages, or plain text errors. It attaches field errors to the thrown Error. The form catches that Error, stays open, and shows the messages beside the inputs. Duplicate email is decided by the backend and displayed in the same way. 404 and 409 have useful fallbacks; 500 responses and network errors use safe messages. Normal validation does not use alert().

Excel upload checks:

1. Missing or zero-length file: `Please select an Excel file.`
2. Non-xlsx extension: `Only .xlsx Excel files are allowed.`
3. Unreadable workbook: `The uploaded file is not a valid Excel workbook.`
4. No meaningful employee data, including a workbook with only Email headers: `The Excel file does not contain any data.`
5. Missing Email header: `The Excel file must contain an Email column to identify employees.`
6. Duplicate Email headers: a clear worksheet-specific error.
7. Non-empty data row with blank Email: `Row 4 is missing an employee email.`
8. Unknown email: `Row 5: Employee with email 'unknown@gmail.com' does not exist.`

Every non-empty worksheet is treated as employee data. The first non-empty row is its header. Exactly one header must equal Email after trimming and case-insensitive comparison. Later non-empty rows must each contain an existing Employee email. Completely empty or whitespace-only rows/sheets are ignored during validation; formatting alone is not meaningful data. At least one employee row must exist in the workbook. A title row above the headers, notes-only sheets, or unrelated product sheets are rejected by this deliberately simple rule. Row numbers in errors are actual worksheet row numbers.

Employee emails are loaded into a case-insensitive HashSet for matching. Extra columns remain dynamic and are not added to Employee or copied into CRUD data. A workbook may include only a subset of employees; duplicate employee rows are allowed. The API validates all worksheets before touching the saved document. It then uses a serializable transaction to update the one row, or insert it when none exists. A failed validation never deletes or overwrites the current workbook.

The workbook is checked against employees at upload time. Editing/deleting employees later does not rewrite it or block ordinary CRUD. Upload a refreshed workbook if its references become outdated.

## Migration and Docker deployment

Migration: `20261029072742_MakeExcelDocumentGlobal`.

It keeps the most recent existing document by UploadedAt (highest Id breaks ties), removes older document rows, then removes EmployeeId. **Older per-employee workbook records are discarded when this migration is applied.** Export any older workbooks you want to keep before applying it. The retained legacy workbook is not retroactively validated; uploading a new workbook applies the new validation rules. The migration does not alter, reset, or delete Employee records. Rolling the migration back cannot restore discarded documents or their original employee associations.

**You still need to run dotnet ef database update against the Docker SQL Server EmployeeManagementDb database.** Running it with the default appsettings connection would target local SQLEXPRESS01 instead. The runtime backend image does not include the EF CLI, so use the installed host SDK/EF CLI.

Run these commands in PowerShell from the repository root while employee-database is running. They read the existing container password into variables without printing it. If you use a different container name, adjust employee-database.

```powershell
$containerInfo = docker inspect employee-database | ConvertFrom-Json
$passwordSetting = $containerInfo[0].Config.Env | Where-Object { $_ -match '^MSSQL_SA_PASSWORD=' } | Select-Object -First 1
if (-not $passwordSetting) { throw 'SQL password setting was not found.' }
$sqlPassword = ($passwordSetting -split '=', 2)[1]
$previousConnection = $env:ConnectionStrings__DefaultConnection
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=EmployeeManagementDb;User Id=sa;Password=$sqlPassword;TrustServerCertificate=True;"
try {
    dotnet ef database update --project backend
} finally {
    $env:ConnectionStrings__DefaultConnection = $previousConnection
}
```

Stop the backend during migration if anyone else is using it. Rebuild backend and frontend after the migration; rebuilding alone does not apply migrations. Compose expects MSSQL_SA_PASSWORD in the shell or .env; the following sets it from the same running database to avoid changing the SQL password:

```powershell
$env:MSSQL_SA_PASSWORD = $sqlPassword
docker compose up -d --build backend frontend
```

Keep the existing employee-data volume. Do not run docker compose down -v. The database image does not need rebuilding. No npm or NuGet package installation is required for this change. Normal builds restore the dependencies already in the project.

## Verification and exact manual tests

Automated verification performed:

- Backend compiled successfully as part of EF generation and dotnet test.
- Frontend npm.cmd run build succeeded.
- Frontend npm.cmd run lint succeeded without warnings.
- All 15 tests passed, including the original four tests, nine create/update validation cases, a global-workbook integration scenario, and a legacy migration preservation scenario.
- EF has-pending-model-changes reports no pending model changes.
- SQL integration tests created and removed only databases whose names start with EmployeeManagementTests_. The application EmployeeManagementDb was not migrated or reset. Original tests only read it or submit an invalid request.

The build reports existing NU1902/NU1903 warnings about the transitive System.Security.Cryptography.Pkcs 9.0.11 dependency. No dependency versions were changed in this feature.

After applying the migration and rebuilding, open http://localhost:5173. Create a test employee using Full Name Sara James, Email sara@gmail.com, Department IT, Job Title Developer, Status Active. If that email is already in use, use an existing employee and substitute its email in the workbook examples below.

Repeat each employee-validation test in both Add Employee and Edit Employee:

| Test | Input/action | Expected |
| --- | --- | --- |
| 1 | Full Name Sara123 | Full name cannot contain numbers. |
| 2 | Email Sara@gmail.com | Email must be in lowercase. |
| 3 | Email sara@gmail | Enter a valid email address. |
| 4 | Add a second employee with sara@gmail.com, or edit another employee to that email | An employee with this email already exists. |
| 5 | Department containing spaces only | Department is required. |
| 6 | Job Title containing spaces only | Job title is required. |

For each, the form stays open and shows the error close to the field. Cancel or correct the error before continuing. Editing Sara without changing her email must succeed. Also test Anne-Marie Smith and John O'Connor as valid names, blank name/email as invalid, and verify that surrounding spaces are trimmed after saving.

Use Microsoft Excel to prepare and save these .xlsx files. Save real workbooks rather than changing text-file extensions, except for the intentional corrupt-file test.

| Test | Action | Expected |
| --- | --- | --- |
| 7 | Upload a .txt file. In the chooser select All files if needed. | Only .xlsx Excel files are allowed. |
| 8 | Upload a workbook with an empty Sheet1 | The Excel file does not contain any data. |
| 9 | A1 Product, B1 Price; A2 Laptop, B2 50000 | The Excel file must contain an Email column to identify employees. |
| 10 | A1 Email, B1 Phone; A2 sara@gmail.com, B2 59123456; A3 unknown@gmail.com, B3 58123456 | Row 3: Employee with email 'unknown@gmail.com' does not exist. |
| 11 | A1 Email, B1 Phone, C1 Address; A2 sara@gmail.com, B2 59123456, C2 Port Louis | Upload succeeds; original filename appears in the global section. Employee data stays unchanged. |
| 12 | Click View Excel | A new tab opens at /excel showing the filename, cell grid, row/column labels, and workbook values. The original tab still shows Employees. |
| 13 | Click Download Excel in either page | Original .xlsx downloads with its original filename; open it in Microsoft Excel. |
| 14 | After test 11, upload the invalid workbook from test 10 | Error appears; previous filename, viewer data, and original download remain available. |
| 15 | Upload another valid workbook with Email and Notes columns and a different filename | It replaces the one global workbook; it does not create/update employees. |

Additional checks: add a second sheet with Email and Skills to a valid workbook and switch sheets in the viewer; add a blank sheet and view its empty state; put an unknown email on the second sheet and confirm the entire upload is rejected. Try ` EMAIL ` as a header and uppercase SARA@GMAIL.COM in Excel: these should match the existing lowercase email. Put data in B4 while A4 under Email is empty: expect Row 4 is missing an employee email. Rename a text file to .xlsx: expect the invalid workbook error. After uploading, delete a disposable employee and verify that deleting it does not delete the global workbook.

In Postman, POST http://localhost:5000/api/employees with Content-Type application/json and body:

```json
{"fullName":"Sara123","email":"newperson@gmail.com","department":"IT","jobTitle":"Developer","status":0}
```

Expect 400 and Full name cannot contain numbers. Then use Sara James with Sara@gmail.com, sara@gmail, whitespace department/jobTitle, and status 99 in separate requests to verify server validation. Repeat with PUT /api/employees/{an-existing-id}. For Excel, POST /api/excel with form-data key file of type File; unknown emails must return 400 and leave the previous workbook unchanged. GET /api/excel or /api/excel/download with no stored file returns 404.

To rerun the new SQL tests without touching application data, set TEST_SQL_CONNECTION to a new isolated database name beginning EmployeeManagementTests_ and run:

```powershell
$env:TEST_SQL_CONNECTION = "Server=localhost,1433;Database=EmployeeManagementTests_Demo;User Id=sa;Password=$sqlPassword;TrustServerCertificate=True;"
dotnet test EmployeeManagement.Api.Tests --filter 'FullyQualifiedName~EmployeeValidationTests|FullyQualifiedName~GlobalExcelTests'
```

Without TEST_SQL_CONNECTION, the two new SQL integration tests are skipped; the nine validation cases do not require a database. The four original tests still need the configured application SQL connection.

## Concepts for the internship demo

- A DTO defines the API input contract. Sharing validation between create and update keeps the rules consistent without changing the database entity.
- ApiController plus Data Annotations rejects invalid requests before controller CRUD logic runs. Client validation helps the user immediately; server validation enforces the API rules independently.
- HTTP 400 means invalid input, 404 means missing data, 409 means conflicting email, and 500 means an unexpected server problem.
- A HashSet provides quick email membership checks. OrdinalIgnoreCase means email casing does not change the match.
- ClosedXML reads workbook contents for validation and display. The original byte array is stored separately, which preserves the downloadable workbook.
- A transaction groups database writes into one commit. Serializable isolation prevents simultaneous first uploads from creating two global document rows.
- An EF migration changes only the necessary schema/data in an existing database. It is separate from building application code.
- The Employee table holds core employee records. The global document holds flexible additional information, using email only as a validation link at upload time.
- React state controls the selected worksheet and loading/errors. An ordinary browser link opens the dedicated viewer without a routing library.
