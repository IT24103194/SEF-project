import React, { useState, useEffect } from 'react';
import { X, Building2, AlertTriangle } from 'lucide-react';
import inventoryApi from '../../services/inventoryApi';

export const SupplierModal = ({ supplier, isOpen, onClose, onSuccess }) => {
  const [formData, setFormData] = useState({
    name: '',
    contactPerson: '',
    email: '',
    phone: '',
    address: '',
    isActive: true,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (supplier) {
      setFormData({
        name: supplier.name || '',
        contactPerson: supplier.contactPerson || '',
        email: supplier.email || '',
        phone: supplier.phone || '',
        address: supplier.address || '',
        isActive: supplier.isActive ?? true,
      });
    } else {
      setFormData({
        name: '',
        contactPerson: '',
        email: '',
        phone: '',
        address: '',
        isActive: true,
      });
    }
    setError(null);
  }, [supplier, isOpen]);

  if (!isOpen) return null;

  const isEdit = !!supplier;

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      setError('Supplier name is required.');
      return;
    }

    try {
      setLoading(true);
      setError(null);

      if (isEdit) {
        await inventoryApi.updateSupplier(supplier.id, formData);
      } else {
        await inventoryApi.createSupplier(formData);
      }

      onSuccess?.();
      onClose();
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Failed to save supplier.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3 style={{ fontSize: '1.15rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Building2 size={18} color="var(--primary)" />
              {isEdit ? 'Edit Supplier Details' : 'Add New Supplier'}
            </h3>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
              {isEdit ? `Updating ${supplier.name}` : 'Register an authorized vendor or nutrition distributor'}
            </p>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div className="alert alert-error">
                <AlertTriangle size={18} />
                <span>{error}</span>
              </div>
            )}

            <div className="form-group">
              <label className="form-label">Company / Supplier Name *</label>
              <input
                type="text"
                name="name"
                className="form-input"
                placeholder="e.g. Optimum Nutrition Lanka Ltd"
                value={formData.name}
                onChange={handleChange}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">Contact Person</label>
              <input
                type="text"
                name="contactPerson"
                className="form-input"
                placeholder="e.g. John Doe (Account Manager)"
                value={formData.contactPerson}
                onChange={handleChange}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div className="form-group">
                <label className="form-label">Email Address</label>
                <input
                  type="email"
                  name="email"
                  className="form-input"
                  placeholder="orders@supplier.com"
                  value={formData.email}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Phone Number</label>
                <input
                  type="text"
                  name="phone"
                  className="form-input"
                  placeholder="+94 11 234 5678"
                  value={formData.phone}
                  onChange={handleChange}
                />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Physical / Warehouse Address</label>
              <textarea
                name="address"
                className="form-textarea"
                rows={2}
                placeholder="Distribution center address, street, city..."
                value={formData.address}
                onChange={handleChange}
              />
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.5rem' }}>
              <input
                type="checkbox"
                id="supplierActive"
                name="isActive"
                checked={formData.isActive}
                onChange={handleChange}
              />
              <label htmlFor="supplierActive" style={{ fontSize: '0.85rem', cursor: 'pointer' }}>
                Active Supplier (Available for orders and stock associations)
              </label>
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Saving...' : isEdit ? 'Update Supplier' : 'Save Supplier'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default SupplierModal;
