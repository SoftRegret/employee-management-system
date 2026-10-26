const API_URL =
  import.meta.env.VITE_API_URL ||
  "http://localhost:5000/api/employees";

export async function getEmployees() {
  const response = await fetch(API_URL);

  if (!response.ok) {
    throw new Error(
      `Failed to load employees. ${response.status} ${response.statusText}` //to identigy the actual HTTP status code and status text returned by the API in case of an error.
    );
  }

  return response.json();
}

export async function createEmployee(employee) {
  const response = await fetch(API_URL, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(employee),
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "Failed to create employee.");
  }

  return response.json();
}

export async function updateEmployee(id, employee) {
  const response = await fetch(`${API_URL}/${id}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(employee),
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "Failed to update employee.");
  }
}

export async function deleteEmployee(id) {
  const response = await fetch(`${API_URL}/${id}`, {
    method: "DELETE",
  });

  if (!response.ok) {
    throw new Error("Failed to delete employee.");
  }
}