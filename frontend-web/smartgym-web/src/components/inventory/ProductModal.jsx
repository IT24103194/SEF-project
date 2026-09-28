import React, { useState, useEffect } from 'react';
import { X, PackagePlus, AlertTriangle } from 'lucide-react';
import inventoryApi from '../../services/inventoryApi';

export const ProductModal = ({ product, isOpen, onClose, onSuccess }) => {
  const [categories, setCategories] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [formData, setFormData] = useState({
    sku: '',
    name: '',
    description: '',
    categoryId: '',
    supplierId: '',
    unitPrice: '',
    costPrice: '',
    initialStock: 20,
    reorderThreshold: 10,
    maxStockLevel: 100,
    locationBin: 'Warehouse-A',
    isActive: true,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const isEdit = !!product;

  useEffect(() => {
    if (isOpen) {
      loadDropdowns();
      if (product) {
        setFormData({
          sku: product.sku || '',
          name: product.name || '',
          description: product.description || '',
          categoryId: product.categoryId || '',
          supplierId: product.supplierId || '',
          unitPrice: product.unitPrice ?? '',
          costPrice: product.costPrice ?? '',
          initialStock: product.quantityInStock ?? 0,
          reorderThreshold: product.reorderThreshold ?? 10,
          maxStockLevel: product.maxStockLevel ?? 100,
          locationBin: product.locationBin || 'Warehouse-A',
          isActive: product.isActive ?? true,
        });
      } else {
        setFormData({
          sku: '',
          name: '',
          description: '',
          categoryId: '',
          supplierId: '',
          unitPrice: '',
          costPrice: '',
          initialStock: 20,
          reorderThreshold: 10,
          maxStockLevel: 100,
          locationBin: 'Warehouse-A',
          isActive: true,
        });
      }
      setError(null);
    }
  }, [isOpen, product]);

  const loadDropdowns = async () => {
    try {
      const [catData, supData] = await Promise.all([
        inventoryApi.getCategories(),
        inventoryApi.getSuppliers({ pageSize: 100 }),
      ]);
      setCategories(catData || []);
      setSuppliers(supData.items || []);

      if (!product && catData?.length > 0 && !formData.categoryId) {
        setFormData((prev) => ({ ...prev, categoryId: catData[0].id }));
      }
      if (!product && supData?.items?.length > 0 && !formData.supplierId) {
        setFormData((prev) => ({ ...prev, supplierId: supData.items[0].id }));
      }
    } catch (err) {
      console.error('Failed to load categories or suppliers dropdown', err);
    }
  };

  if (!isOpen) return null;

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.sku.trim()) {
      setError('Product Name and SKU are required.');
      return;
    }
    if (!formData.categoryId) {
      setError('Please select a Product Category.');
      return;
    }
    if (!formData.supplierId) {
      setError('Please select an authorized Supplier.');
      return;
    }

    try {
      setLoading(true);
      setError(null);

      if (isEdit) {
        await inventoryApi.updateProduct(product.id, {
          sku: formData.sku.trim(),
          name: formData.name.trim(),
          description: formData.description.trim(),
          categoryId: formData.categoryId,
          supplierId: formData.supplierId,
          unitPrice: parseFloat(formData.unitPrice) || 0,
          costPrice: parseFloat(formData.costPrice) || 0,
          isActive: formData.isActive,
        });
      } else {
        await inventoryApi.createProduct({
          sku: formData.sku.trim(),
          name: formData.name.trim(),
          description: formData.description.trim(),
          categoryId: formData.categoryId,
          supplierId: formData.supplierId,
          unitPrice: parseFloat(formData.unitPrice) || 0,
          costPrice: parseFloat(formData.costPrice) || 0,
          initialStock: parseInt(formData.initialStock, 10) || 0,
          reorderThreshold: parseInt(formData.reorderThreshold, 10) || 10,
          maxStockLevel: parseInt(formData.maxStockLevel, 10) || 100,
          locationBin: formData.locationBin.trim(),
          isActive: formData.isActive,
        });
      }

      onSuccess?.();
      onClose();
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Failed to save product.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" style={{ maxWidth: 640 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3 style={{ fontSize: '1.15rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <PackagePlus size={18} color="var(--accent-cyan)" />
              {isEdit ? 'Edit Supplement Product' : 'Add New Supplement'}
            </h3>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
              {isEdit ? `SKU: ${product.sku}` : 'Register a product and provision its inventory item'}
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

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: '1rem' }}>
              <div className="form-group">
                <label className="form-label">SKU (Unique Code) *</label>
                <input
                  type="text"
                  name="sku"
                  className="form-input"
                  placeholder="e.g. WHEY-ISO-1KG"
                  value={formData.sku}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="form-group">
                <label className="form-label">Product Name *</label>
                <input
                  type="text"
                  name="name"
                  className="form-input"
                  placeholder="e.g. Gold Standard 100% Whey Vanilla"
                  value={formData.name}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div className="form-group">
                <label className="form-label">Category *</label>
                <select
                  name="categoryId"
                  className="form-select"
                  value={formData.categoryId}
                  onChange={handleChange}
                  required
                >
                  <option value="">Select Category</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Supplier *</label>
                <select
                  name="supplierId"
                  className="form-select"
                  value={formData.supplierId}
                  onChange={handleChange}
                  required
                >
                  <option value="">Select Supplier</option>
                  {suppliers.map((s) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div className="form-group">
                <label className="form-label">Retail Selling Price ($) *</label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  name="unitPrice"
                  className="form-input"
                  placeholder="79.99"
                  value={formData.unitPrice}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="form-group">
                <label className="form-label">Supplier Cost Price ($) *</label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  name="costPrice"
                  className="form-input"
                  placeholder="45.00"
                  value={formData.costPrice}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            {!isEdit && (
              <div style={{
                background: 'rgba(0,0,0,0.2)',
                padding: '0.85rem',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--border-subtle)',
                marginBottom: '1rem'
              }}>
                <div style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--accent-cyan)', marginBottom: '0.5rem' }}>
                  Initial Stock Configuration
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '0.75rem' }}>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Initial Quantity</label>
                    <input
                      type="number"
                      min="0"
                      name="initialStock"
                      className="form-input"
                      value={formData.initialStock}
                      onChange={handleChange}
                    />
                  </div>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Reorder Alert</label>
                    <input
                      type="number"
                      min="1"
                      name="reorderThreshold"
                      className="form-input"
                      value={formData.reorderThreshold}
                      onChange={handleChange}
                    />
                  </div>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Location Bin</label>
                    <input
                      type="text"
                      name="locationBin"
                      className="form-input"
                      value={formData.locationBin}
                      onChange={handleChange}
                    />
                  </div>
                </div>
              </div>
            )}

            <div className="form-group">
              <label className="form-label">Description</label>
              <textarea
                name="description"
                className="form-textarea"
                rows={2}
                placeholder="Nutritional profile, dosage instructions, flavour profile..."
                value={formData.description}
                onChange={handleChange}
              />
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Saving...' : isEdit ? 'Update Product' : 'Create Product'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ProductModal;
