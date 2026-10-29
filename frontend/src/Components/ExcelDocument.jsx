import { useEffect, useRef, useState } from "react";
import { getExcel, uploadExcel, downloadExcel } from "../Services/employeeServices";

// Modularity: the global workbook controls remain separate from employee rows and forms.
export default function ExcelDocument() {
  const [document, setDocument] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const input = useRef(null);
  useEffect(() => {
    getExcel().then(setDocument).catch(error => { if (error.status !== 404) setError(error.message); }).finally(() => setLoading(false));
  }, []);
  async function upload(event) {
    const file = event.target.files[0];
    event.target.value = "";
    if (!file) return;
    setError(""); setMessage("");
    if (!file.name.toLowerCase().endsWith(".xlsx")) { setError("Only .xlsx Excel files are allowed."); return; }
    setBusy(true);
    // Graceful Failure: update the displayed document only after the API accepts the upload.
    try { setDocument(await uploadExcel(file)); setMessage("Excel file uploaded successfully."); }
    catch (error) { setError(error.message); }
    finally { setBusy(false); }
  }
  async function download() {
    setError(""); setBusy(true);
    try { await downloadExcel(document.fileName); }
    catch (error) { setError(error.message); }
    finally { setBusy(false); }
  }
  return <section className="excel-document" aria-labelledby="excel-title">
    <div className="excel-toolbar">
      <div><h2 id="excel-title">Excel Document</h2><p>{loading ? "Loading Excel document..." : document ? document.fileName : "No Excel file uploaded"}</p></div>
      <div className="excel-actions">
        <input ref={input} type="file" accept=".xlsx" hidden onChange={upload} />
        <button className="primary-button" disabled={busy} onClick={() => input.current.click()}>{busy ? "Please wait..." : "Upload Excel"}</button>
        {document && <><a className="cancel-button" href="/excel" target="_blank" rel="noopener noreferrer">View Excel</a><button className="cancel-button" disabled={busy} onClick={download}>Download Excel</button></>}
      </div>
    </div>
    {error && <div className="error-message" role="alert">{error}</div>}
    {message && <p className="success-message" role="status">{message}</p>}
  </section>;
}
