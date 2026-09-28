import React, { useState, useEffect } from 'react';
import { X, Save, Tags } from 'lucide-react';
import classesApi from '../../services/classesApi';

export const CategoryModal = ({ category, onClose, onSaved }) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
  });
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (category) {
      setFormData({
        name: category.name || '',
        description: category.description || '',
      });
    }
  }, [category]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      setErrorMsg('Category name is required.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      const payload = {
        name: formData.name.trim(),
        description: formData.description.trim(),
      };

      if (category?.id) {
        await classesApi.updateCategory(category.id, payload);
      } else {
        await classesApi.createCategory(payload);
      }

      onSaved();
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || err.message || 'Operation failed';
      setErrorMsg(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="modal-backdrop" onClick={onClose} style={{ zIndex: 1000 }}>
      <div
        className="glass-card modal-content"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: '480px', padding: '1.5rem' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: '8px',
                background: 'rgba(168, 85, 247, 0.15)',
                color: '#a855f7',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Tags size={20} />
            </div>
            <div>
              <h2 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>
                {category ? 'Edit Class Category' : 'Create Class Category'}
              </h2>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                Classification tag for grouping workouts
              </span>
            </div>
          </div>
          <button className="btn-icon" onClick={onClose} title="Close">
            <X size={18} />
          </button>
        </div>

        {errorMsg && (
          <div
            style={{
              padding: '0.75rem 1rem',
              borderRadius: '8px',
              background: 'rgba(239, 68, 68, 0.12)',
              border: '1px solid rgba(239, 68, 68, 0.25)',
              color: '#ef4444',
              fontSize: '0.85rem',
              marginBottom: '1rem',
            }}
          >
            {errorMsg}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
              Category Name *
            </label>
            <input
              type="text"
              className="input-field"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g. High-Intensity Interval Training (HIIT)"
              required
            />
          </div>

          <div style={{ marginBottom: '1.25rem' }}>
            <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
              Description
            </label>
            <textarea
              className="input-field"
              rows={3}
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Brief description of category target and focus..."
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              <Save size={16} />
              {submitting ? 'Saving...' : category ? 'Update Category' : 'Create Category'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CategoryModal;
