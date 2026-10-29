import { useEffect, useState } from "react";
import "./App.css";
import EmployeeList from "./Components/EmployeeList";
import EmployeeForm from "./Components/EmployeeForm";
import ExcelDocument from "./Components/ExcelDocument";
import ExcelViewer from "./Components/ExcelViewer";
import { getEmployees, createEmployee, updateEmployee, deleteEmployee } from "./Services/employeeServices";

// Composition: this page combines focused employee and Excel components.
function EmployeePage() {
  const [employees, setEmployees] = useState([]);
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [employeeToEdit, setEmployeeToEdit] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => { loadEmployees(); }, []);
  async function loadEmployees() {
    try { setError(""); setEmployees(await getEmployees()); }
    catch (error) { setError(error.message); }
    finally { setLoading(false); }
  }
  function closeForm() { setShowForm(false); setEmployeeToEdit(null); }
  async function handleSave(data) {
    // Error Propagation: let API errors reach the form so it stays open and displays field messages.
    if (employeeToEdit) await updateEmployee(employeeToEdit.id, data);
    else await createEmployee(data);
    closeForm();
    await loadEmployees();
  }
  async function handleDelete(id) {
    if (!window.confirm("Are you sure you want to delete this employee?")) return;
    try { await deleteEmployee(id); await loadEmployees(); }
    catch (error) { setError(error.message); }
  }
  const filtered = employees.filter(employee =>
    employee.fullName.toLowerCase().includes(search.toLowerCase()) || employee.email.toLowerCase().includes(search.toLowerCase()));

  return <>
    <header className="header"><div className="container"><h1>Employee Management System</h1></div></header>
    <main className="container">
      <ExcelDocument />
      <section className="employee-card">
        <div className="card-header">
          <div><h2>Employees</h2><p>Manage your team members</p></div>
          <button className="primary-button" onClick={() => { setEmployeeToEdit(null); setShowForm(true); }}>+ Add Employee</button>
        </div>
        <div className="search-container"><input aria-label="Search employees" placeholder="Search by name or email..." value={search} onChange={event => setSearch(event.target.value)} /></div>
        {error && <div className="error-message" role="alert">{error}</div>}
        {loading ? <p role="status">Loading employees...</p> : <EmployeeList employees={filtered} onEdit={employee => { setEmployeeToEdit(employee); setShowForm(true); }} onDelete={handleDelete} />}
        <div className="employee-count">Showing {filtered.length} {filtered.length === 1 ? "employee" : "employees"}</div>
      </section>
    </main>
    {showForm && <EmployeeForm employee={employeeToEdit} onSave={handleSave} onClose={closeForm} />}
  </>;
}

// KISS: two pages use a pathname check without adding a routing library.
export default function App() {
  return window.location.pathname.replace(/\/$/, "") === "/excel" ? <ExcelViewer /> : <EmployeePage />;
}
