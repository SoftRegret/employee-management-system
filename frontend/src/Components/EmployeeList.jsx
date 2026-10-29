// SRP: displays employees and delegates edit/delete actions to its parent through callbacks.

function EmployeeList({
  employees,
  onEdit,
  onDelete,
}) {
  // If there are no employees, show a message.
  if (employees.length === 0) {
    return <div className="empty-state">No employees found.</div>;
  }

  return (
    <div className="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>#</th>
            <th>Name</th>
            <th>Email</th>
            <th>Department</th>
            <th>Position</th>
            <th>Date of Joining</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>

        <tbody>
          {employees.map((employee, index) => (
            <tr key={employee.id}>
              <td>{index + 1}</td>

              <td>{employee.fullName}</td>

              <td>{employee.email}</td>

              <td>{employee.department}</td>

              <td>{employee.jobTitle}</td>

              <td>
                {new Date(
                  employee.dateOfJoining
                ).toLocaleDateString()}
              </td>

              <td>
                <span
                  className={
                    employee.status === 0
                      ? "status active"
                      : "status inactive"
                  }
                >
                  {employee.status === 0
                    ? "Active"
                    : "Inactive"}
                </span>
              </td>

              <td>
                <div className="action-buttons">

                  {/* Edit employee */}
                  <button
                    className="action-button"
                    onClick={() => onEdit(employee)}
                  >
                    Edit
                  </button>

                  {/* Delete employee */}
                  <button
                    className="action-button"
                    onClick={() => onDelete(employee.id)}
                  >
                    Delete
                  </button>


                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default EmployeeList;