import { useState } from "react";

function validateEmployee(data) {
  const errors = {};
  if (!data.fullName) errors.fullName = "Full name is required.";
  else if (/\p{Nd}/u.test(data.fullName)) errors.fullName = "Full name cannot contain numbers.";
  else if (!/^[\p{L}\p{M} '-]+$/u.test(data.fullName) || !/\p{L}/u.test(data.fullName))
    errors.fullName = "Full name must contain letters and may only include spaces, hyphens and apostrophes.";
  if (!data.email) errors.email = "Email is required.";
  else if (data.email !== data.email.toLowerCase()) errors.email = "Email must be in lowercase.";
  else if (!/^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/.test(data.email)) errors.email = "Enter a valid email address.";
  if (!data.department) errors.department = "Department is required.";
  if (!data.jobTitle) errors.jobTitle = "Job title is required.";
  if (![0, 1].includes(data.status)) errors.status = "Select a valid employee status.";
  return errors;
}

export default function EmployeeForm({ employee, onSave, onClose }) {
  const [data, setData] = useState(employee ? {
    fullName: employee.fullName, email: employee.email, department: employee.department,
    jobTitle: employee.jobTitle, status: employee.status,
  } : { fullName: "", email: "", department: "", jobTitle: "", status: 0 });
  const [errors, setErrors] = useState({});
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  function change(event) {
    const { name, value } = event.target;
    setData(previous => ({ ...previous, [name]: name === "status" ? Number(value) : value }));
    setErrors(previous => ({ ...previous, [name]: "" }));
  }
  async function submit(event) {
    event.preventDefault();
    const trimmed = { ...data };
    for (const field of ["fullName", "email", "department", "jobTitle"]) trimmed[field] = trimmed[field].trim();
    const validation = validateEmployee(trimmed);
    setErrors(validation); setError("");
    if (Object.keys(validation).length) return;
    setSaving(true);
    try { await onSave(trimmed); }
    catch (error) { setErrors(error.fields || {}); setError(error.message); }
    finally { setSaving(false); }
  }
  return <div className="modal-background">
    <div className="modal" role="dialog" aria-modal="true" aria-labelledby="form-title">
      <div className="modal-header"><h2 id="form-title">{employee ? "Edit Employee" : "Add Employee"}</h2><button className="close-button" aria-label="Close form" disabled={saving} onClick={onClose}>?</button></div>
      <form noValidate onSubmit={submit}>
        {error && <div className="error-message" role="alert">{error}</div>}
        <div className="form-grid">
          {[["fullName", "Full Name"], ["email", "Email"], ["department", "Department"], ["jobTitle", "Job Title"]].map(([name, label]) =>
            <div className="form-group" key={name}>
              <label htmlFor={name}>{label}</label>
              <input id={name} name={name} type={name === "email" ? "email" : "text"} value={data[name]} onChange={change} aria-invalid={!!errors[name]} aria-describedby={errors[name] ? `${name}-error` : undefined} disabled={saving} />
              {errors[name] && <span className="field-error" id={`${name}-error`} role="alert">{errors[name]}</span>}
            </div>)}
          <div className="form-group"><label htmlFor="status">Status</label><select id="status" name="status" value={data.status} onChange={change} disabled={saving} aria-invalid={!!errors.status} aria-describedby={errors.status ? "status-error" : undefined}><option value={0}>Active</option><option value={1}>Inactive</option></select>{errors.status && <span id="status-error" className="field-error">{errors.status}</span>}</div>
        </div>
        <div className="form-actions"><button type="button" className="cancel-button" disabled={saving} onClick={onClose}>Cancel</button><button className="primary-button" disabled={saving}>{saving ? "Saving..." : employee ? "Save Changes" : "Add Employee"}</button></div>
      </form>
    </div>
  </div>;
}
