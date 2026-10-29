import { useEffect, useState } from "react";
import { getExcel, downloadExcel } from "../Services/employeeServices";

function columnLabel(index) {
  let label = "";
  for (let number = index + 1; number > 0; number = Math.floor((number - 1) / 26))
    label = String.fromCharCode(65 + (number - 1) % 26) + label;
  return label;
}

export default function ExcelViewer() {
  const [workbook, setWorkbook] = useState(null);
  const [active, setActive] = useState(0);
  const [error, setError] = useState("");
  const [downloadError, setDownloadError] = useState("");
  const [downloading, setDownloading] = useState(false);
  useEffect(() => { getExcel().then(setWorkbook).catch(error => setError(error.message)); }, []);
  async function download() {
    setDownloadError(""); setDownloading(true);
    try { await downloadExcel(workbook.fileName); }
    catch (error) { setDownloadError(error.message); }
    finally { setDownloading(false); }
  }
  if (error) return <main className="container viewer-state"><h1>Excel Document</h1><p className="error-message" role="alert">{error}</p><a href="/">Back to employees</a></main>;
  if (!workbook) return <main className="container viewer-state" role="status">Loading Excel workbook...</main>;
  const sheet = workbook.sheets[active];
  const columns = sheet?.rows[0]?.length || 0;
  return <main className="excel-viewer">
    <header className="viewer-header"><div><h1>{workbook.fileName}</h1><p>Uploaded {new Date(workbook.uploadedAt).toLocaleString()} ? Read-only workbook</p></div><button className="primary-button" disabled={downloading} onClick={download}>{downloading ? "Downloading..." : "Download Excel"}</button></header>
    {downloadError && <div className="error-message" role="alert">{downloadError}</div>}
    <div className="spreadsheet-scroll" role="region" aria-label={sheet?.name || "Worksheet"} tabIndex={0}>
      {columns ? <table className="spreadsheet"><thead><tr><th aria-label="Row number" />{Array.from({ length: columns }, (_, index) => <th key={index} scope="col">{columnLabel(index)}</th>)}</tr></thead><tbody>{sheet.rows.map((row, index) => <tr key={index}><th scope="row">{index + 1}</th>{row.map((cell, column) => <td key={column}>{cell}</td>)}</tr>)}</tbody></table> : <p className="empty-state">This worksheet is empty.</p>}
    </div>
    <nav className="sheet-tabs" aria-label="Worksheets">{workbook.sheets.map((sheet, index) => <button key={sheet.name} className={active === index ? "selected" : ""} aria-current={active === index ? "page" : undefined} onClick={() => setActive(index)}>{sheet.name}</button>)}</nav>
  </main>;
}
