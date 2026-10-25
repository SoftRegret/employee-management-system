import { useEffect, useState } from "react";
import "./App.css";

import EmployeeList from "./Components/EmployeeList";
import EmployeeForm from "./Components/EmployeeForm";

import {
  getEmployees,
  createEmployee,
  updateEmployee,
  deleteEmployee,
} from "./Services/employeeServices";

function App() {
  const [employees, setEmployees] = useState([]);
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [employeeToEdit, setEmployeeToEdit] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    loadEmployees();
  }, []);

  async function loadEmployees() {
    try {
      setError("");

      const data = await getEmployees();
      setEmployees(data);
    } catch {
      setError("Could not load employees. Make sure the backend is running.");
    }
  }

  function openAddForm() {
    setEmployeeToEdit(null);
    setShowForm(true);
  }

  function openEditForm(employee) {
    setEmployeeToEdit(employee);
    setShowForm(true);
  }

  function closeForm() {
    setShowForm(false);
    setEmployeeToEdit(null);
  }

  async function handleSave(formData) {
    try {
      setError("");

      if (employeeToEdit) {
        await updateEmployee(employeeToEdit.id, formData);
      } else {
        await createEmployee(formData);
      }

      closeForm();
      await loadEmployees();
    } catch (error) {
      setError(error.message);
    }
  }

  async function handleDelete(id) {
    const confirmed = window.confirm(
      "Are you sure you want to delete this employee?"
    );

    if (!confirmed) {
      return;
    }

    try {
      setError("");

      await deleteEmployee(id);
      await loadEmployees();
    } catch (error) {
      setError(error.message);
    }
  }

  const filteredEmployees = employees.filter((employee) => {
    const searchText = search.toLowerCase();

    return (
      employee.fullName.toLowerCase().includes(searchText) ||
      employee.email.toLowerCase().includes(searchText)
    );
  });

  return (
    <>
      <header className="header">
        <div className="container">
          <h1>Employee Management System</h1>
        </div>
      </header>

      <main className="container">
        <section className="employee-card">
          <div className="card-header">
            <div>
              <h2>Employees</h2>
              <p>Manage your team members</p>
            </div>

            <button
              className="primary-button add-button"
              onClick={openAddForm}
            >
              <span>＋</span>
              Add Employee
            </button>
          </div>

          <div className="search-container">
            <span className="search-icon">⌕</span>

            <input
              type="text"
              placeholder="Search by name or email..."
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>

          {error && <div className="error-message">{error}</div>}

          <EmployeeList
            employees={filteredEmployees}
            onEdit={openEditForm}
            onDelete={handleDelete}
          />

          <div className="employee-count">
            Showing {filteredEmployees.length}{" "}
            {filteredEmployees.length === 1 ? "employee" : "employees"}
          </div>
        </section>
      </main>

      {showForm && (
        <EmployeeForm
          employee={employeeToEdit}
          onSave={handleSave}
          onClose={closeForm}
        />
      )}
    </>
  );
}

export default App;