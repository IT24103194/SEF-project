import React from 'react';
import { Package, UploadCloud, DownloadCloud } from 'lucide-react';

export const InventoryPage = () => {
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800 }}>Supplier & Supplement Inventory</h1>
          <p style={{ color: 'var(--text-secondary)' }}>Component 1 — Stock levels, threshold alarms & CSV data exchange.</p>
        </div>
        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button className="btn btn-secondary"><UploadCloud size={16} /> Import CSV</button>
          <button className="btn btn-secondary"><DownloadCloud size={16} /> Export CSV</button>
        </div>
      </div>

      <div className="glass-card" style={{ padding: '2rem', textAlign: 'center' }}>
        <Package size={48} color="var(--accent-cyan)" style={{ marginBottom: '1rem' }} />
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '0.5rem' }}>Module Ready for Component 1</h2>
        <p style={{ color: 'var(--text-secondary)', maxWidth: 600, margin: '0 auto' }}>
          This interface is structured to consume the ASP.NET Core inventory endpoints: <code>/api/products</code>, <code>/api/inventory</code>, and <code>/api/suppliers</code>.
        </p>
      </div>
    </div>
  );
};

export default InventoryPage;
