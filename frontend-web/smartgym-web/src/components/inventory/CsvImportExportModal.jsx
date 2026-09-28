import React, { useState } from 'react';
import { X, UploadCloud, DownloadCloud, FileText, CheckCircle2, AlertTriangle, AlertCircle } from 'lucide-react';
import inventoryApi from '../../services/inventoryApi';

export const CsvImportExportModal = ({ isOpen, onClose, onSuccess }) => {
  const [file, setFile] = useState(null);
  const [importing, setImporting] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  if (!isOpen) return null;

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
      setError(null);
      setResult(null);
    }
  };

  const handleImport = async () => {
    if (!file) {
      setError('Please select a valid CSV file first.');
      return;
    }

    try {
      setImporting(true);
      setError(null);
      const res = await inventoryApi.importInventoryCsv(file);
      setResult(res);
      if (res.successCount > 0) {
        onSuccess?.();
      }
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Failed to upload and import CSV file.';
      setError(msg);
    } finally {
      setImporting(false);
    }
  };

  const handleExport = async () => {
    try {
      setExporting(true);
      setError(null);
      const blob = await inventoryApi.exportInventoryCsv();
      const url = window.URL.createObjectURL(new Blob([blob], { type: 'text/csv' }));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `smartgym_inventory_${new Date().toISOString().slice(0, 10)}.csv`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      setError('Failed to export inventory CSV.');
    } finally {
      setExporting(false);
    }
  };

  const handleClose = () => {
    setFile(null);
    setResult(null);
    setError(null);
    onClose();
  };

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal-content" style={{ maxWidth: 700 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3 style={{ fontSize: '1.15rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <FileText size={18} color="var(--accent-cyan)" />
              CSV Data Exchange — Inventory Catalog
            </h3>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
              Bulk upload inventory rows with validation or export live catalog
            </p>
          </div>
          <button onClick={handleClose} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </div>

        <div className="modal-body">
          {error && (
            <div className="alert alert-error">
              <AlertTriangle size={18} />
              <span>{error}</span>
            </div>
          )}

          {/* Export Section */}
          <div style={{
            background: 'rgba(255, 255, 255, 0.02)',
            border: '1px solid var(--border-subtle)',
            borderRadius: 'var(--radius-md)',
            padding: '1.25rem',
            marginBottom: '1.5rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between'
          }}>
            <div>
              <div style={{ fontWeight: 600, fontSize: '0.95rem', marginBottom: '0.2rem' }}>
                Export Active Inventory to CSV
              </div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                Generates RFC 4180 CSV containing SKU, Name, Category, Supplier, Stock, Prices, and Thresholds.
              </div>
            </div>
            <button className="btn btn-secondary" onClick={handleExport} disabled={exporting}>
              <DownloadCloud size={16} />
              {exporting ? 'Exporting...' : 'Export CSV'}
            </button>
          </div>

          {/* Import Upload Section */}
          <div style={{
            border: '2px dashed var(--border-subtle)',
            borderRadius: 'var(--radius-md)',
            padding: '2rem',
            textAlign: 'center',
            background: 'rgba(0, 0, 0, 0.2)',
            marginBottom: '1.5rem'
          }}>
            <UploadCloud size={40} color="var(--primary)" style={{ marginBottom: '0.75rem' }} />
            <h4 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '0.25rem' }}>
              Select Inventory CSV File
            </h4>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
              Supported columns: <code>SKU, ProductName, CategoryName, SupplierName, UnitPrice, CostPrice, InitialStock...</code>
            </p>

            <input
              type="file"
              accept=".csv"
              id="csvFileInput"
              style={{ display: 'none' }}
              onChange={handleFileChange}
            />
            <label htmlFor="csvFileInput" className="btn btn-secondary" style={{ cursor: 'pointer' }}>
              {file ? file.name : 'Browse Files'}
            </label>

            {file && (
              <div style={{ marginTop: '1rem' }}>
                <button
                  className="btn btn-primary"
                  onClick={handleImport}
                  disabled={importing}
                >
                  <UploadCloud size={16} />
                  {importing ? 'Processing File...' : 'Upload & Validate CSV'}
                </button>
              </div>
            )}
          </div>

          {/* Results Panel */}
          {result && (
            <div style={{
              background: 'rgba(0, 0, 0, 0.25)',
              border: '1px solid var(--border-subtle)',
              borderRadius: 'var(--radius-md)',
              padding: '1.25rem'
            }}>
              <h4 style={{ fontSize: '0.95rem', fontWeight: 700, marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <CheckCircle2 size={16} color="var(--accent-emerald)" />
                Import Execution Summary
              </h4>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '1rem', marginBottom: '1.25rem' }}>
                <div style={{ background: 'rgba(255,255,255,0.03)', padding: '0.75rem', borderRadius: 'var(--radius-sm)', textAlign: 'center' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>TOTAL ROWS</div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800 }}>{result.totalRows}</div>
                </div>
                <div style={{ background: 'rgba(16, 185, 129, 0.1)', padding: '0.75rem', borderRadius: 'var(--radius-sm)', textAlign: 'center' }}>
                  <div style={{ fontSize: '0.75rem', color: '#34d399' }}>SUCCESSFUL</div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#34d399' }}>{result.successCount}</div>
                </div>
                <div style={{ background: 'rgba(244, 63, 94, 0.1)', padding: '0.75rem', borderRadius: 'var(--radius-sm)', textAlign: 'center' }}>
                  <div style={{ fontSize: '0.75rem', color: '#fb7185' }}>FAILED ROWS</div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#fb7185' }}>{result.failureCount}</div>
                </div>
              </div>

              {result.errors?.length > 0 && (
                <div>
                  <div style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--accent-rose)', marginBottom: '0.5rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                    <AlertCircle size={15} /> Row-Level Validation Errors:
                  </div>
                  <div style={{ maxHeight: 180, overflowY: 'auto' }}>
                    <table className="data-table" style={{ fontSize: '0.8rem' }}>
                      <thead>
                        <tr>
                          <th>Row #</th>
                          <th>SKU</th>
                          <th>Validation Error</th>
                        </tr>
                      </thead>
                      <tbody>
                        {result.errors.map((err, idx) => (
                          <tr key={idx}>
                            <td>{err.rowNumber}</td>
                            <td><code>{err.sku || 'N/A'}</code></td>
                            <td style={{ color: 'var(--accent-rose)' }}>{err.error}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button className="btn btn-secondary" onClick={handleClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default CsvImportExportModal;
