const API_URL = (import.meta.env.VITE_API_URL || "http://localhost:5000/api/employees").replace(/\/$/, "");
const EXCEL_URL = API_URL.replace(/\/employees$/, "/excel");

// Accept ASP.NET ValidationProblemDetails, JSON messages, and plain text errors.
async function checkResponse(response) {
  if (response.ok) return response;
  const fallback = response.status >= 500 ? "An unexpected server error occurred. Please try again."
    : response.status === 404 ? "The requested resource was not found."
    : response.status === 409 ? "An employee with this email already exists."
    : response.status === 400 ? "Please check the submitted information." : "The request failed.";
  const text = await response.text();
  let body;
  try { body = JSON.parse(text); } catch { body = text; }
  const fields = {};
  if (body?.errors && response.status < 500) {
    for (const [key, messages] of Object.entries(body.errors)) {
      const name = key.split(".").pop();
      fields[name.charAt(0).toLowerCase() + name.slice(1)] = [].concat(messages).join(" ");
    }
  }
  // Never display a server stack trace or HTML error page.
  const message = response.status >= 500 ? fallback
    : Object.values(fields)[0] || (typeof body === "string" && !body.includes("<") ? body : body?.message) || fallback;
  const error = new Error(message);
  error.status = response.status;
  error.fields = fields;
  throw error;
}

async function request(url, options) {
  try { return await checkResponse(await fetch(url, options)); }
  catch (error) {
    if (error instanceof TypeError) throw new Error("Could not connect to the server. Check that the backend is running.");
    throw error;
  }
}
export async function getEmployees() { return (await request(API_URL)).json(); }
export async function createEmployee(employee) {
  return (await request(API_URL, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(employee) })).json();
}
export async function updateEmployee(id, employee) {
  await request(`${API_URL}/${id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(employee) });
}
export async function deleteEmployee(id) { await request(`${API_URL}/${id}`, { method: "DELETE" }); }
export async function getExcel() { return (await request(EXCEL_URL)).json(); }
export async function uploadExcel(file) {
  const body = new FormData();
  body.append("file", file);
  return (await request(EXCEL_URL, { method: "POST", body })).json();
}
export async function downloadExcel(fileName) {
  const response = await request(`${EXCEL_URL}/download`);
  const disposition = response.headers.get("Content-Disposition") || "";
  const encoded = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  const plain = disposition.match(/filename="([^"]+)"|filename=([^;]+)/i);
  let name = fileName || "employee-data.xlsx";
  if (encoded) { try { name = decodeURIComponent(encoded[1]); } catch { /* Use known filename. */ } }
  else if (plain) name = plain[1] || plain[2].trim();
  const url = URL.createObjectURL(await response.blob());
  const link = document.createElement("a");
  link.href = url;
  link.download = name;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
