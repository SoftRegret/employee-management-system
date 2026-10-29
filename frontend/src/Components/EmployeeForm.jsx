//displays and manages the form used to add or edit an employee
import { useEffect, useState } from "react";

const emptyForm = {
  fullName: "",
  email: "",
  department: "",
  jobTitle: "",
  status: 0,
};

function EmployeeForm({ employee, onSave, onClose }) {
  const [formData, setFormData] = useState(emptyForm);

  useEffect(() => {
    if (employee) {
      setFormData({
        fullName: employee.fullName,
        email: employee.email,
        department: employee.department,
        jobTitle: employee.jobTitle,
        status: employee.status,
      });
    } else {
      setFormData(emptyForm);
    }
  }, [employee]);

  function handleChange(event) {
    const { name, value } = event.target;

    setFormData({
      ...formData,
      [name]: name === "status" ? Number(value) : value,
    });
  }

  function handleSubmit(event) {
    event.preventDefault();
    onSave(formData);
  }

  return (
    <div className="modal-background">
      <div className="modal">
        <div className="modal-header">
          <h2>{employee ? "Edit Employee" : "Add Employee"}</h2>

          <button className="close-button" onClick={onClose}>
            ×
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-grid">
            <div className="form-group">
              <label>Name</label>
              <input
                type="text"
                name="fullName"
                placeholder="Enter full name"
                value={formData.fullName}
                onChange={handleChange}
                required
              />
            </div>

            <div className="form-group">
              <label>Email</label>
              <input
                type="email"
                name="email"
                placeholder="Enter email address"
                value={formData.email}
                onChange={handleChange}
                required
              />
            </div>

            <div className="form-group">
              <label>Department</label>
              <input
                type="text"
                name="department"
                placeholder="Enter department"
                value={formData.department}
                onChange={handleChange}
                required
              />
            </div>

            <div className="form-group">
              <label>Position</label>
              <input
                type="text"
                name="jobTitle"
                placeholder="Enter position"
                value={formData.jobTitle}
                onChange={handleChange}
                required
              />
            </div>

            <div className="form-group">
              <label>Status</label>

              <select
                name="status"
                value={formData.status}
                onChange={handleChange}
              >
                <option value={0}>Active</option>
                <option value={1}>Inactive</option>
              </select>
            </div>
          </div>

          <div className="form-actions">
            <button
              type="button"
              className="cancel-button"
              onClick={onClose}
            >
              Cancel
            </button>

            <button type="submit" className="primary-button">
              {employee ? "Save Changes" : "Add Employee"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default EmployeeForm;