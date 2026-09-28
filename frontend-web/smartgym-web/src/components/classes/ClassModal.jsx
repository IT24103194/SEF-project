import React, { useState, useEffect } from 'react';
import { X, Save, Dumbbell } from 'lucide-react';
import classesApi from '../../services/classesApi';

export const ClassModal = ({ fitnessClass, categories, onClose, onSaved }) => {
  const [formData, setFormData] = useState({
    categoryId: '',
    name: '',
    description: '',
    durationMinutes: 60,
    defaultCapacity: 20,
    intensityLevel: 'Medium',
  });
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (fitnessClass) {
      setFormData({
        categoryId: fitnessClass.categoryId || (categories[0]?.id ?? ''),
        name: fitnessClass.name || '',
        description: fitnessClass.description || '',
        durationMinutes: fitnessClass.durationMinutes || 60,
        defaultCapacity: fitnessClass.defaultCapacity || 20,
        intensityLevel: fitnessClass.intensityLevel || 'Medium',
      });
    } else if (categories.length > 0) {
      setFormData((prev) => ({ ...prev, categoryId: categories[0].id }));
    }
  }, [fitnessClass, categories]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name || !formData.categoryId) {
      setErrorMsg('Please fill in all required fields.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      const payload = {
        categoryId: formData.categoryId,
        name: formData.name.trim(),
        description: formData.description.trim(),
        durationMinutes: parseInt(formData.durationMinutes, 10),
        defaultCapacity: parseInt(formData.defaultCapacity, 10),
        intensityLevel: formData.intensityLevel,
      };

      if (fitnessClass?.id) {
        await classesApi.updateClass(fitnessClass.id, payload);
      } else {
        await classesApi.createClass(payload);
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
        style={{ width: '100%', maxWidth: '560px', padding: '1.5rem', maxHeight: '90vh', overflowY: 'auto' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: '8px',
                background: 'rgba(99, 102, 241, 0.15)',
                color: '#6366f1',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Dumbbell size={20} />
            </div>
            <div>
              <h2 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>
                {fitnessClass ? 'Edit Fitness Class' : 'Add New Fitness Class'}
              </h2>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                Define class syllabus, duration, and capacity defaults
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
              Class Name *
            </label>
            <input
              type="text"
              className="input-field"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g. Metabolic Blast HIIT"
              required
            />
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
              Category *
            </label>
            <select
              className="input-field"
              value={formData.categoryId}
              onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}
              required
            >
              <option value="" disabled>
                Select category...
              </option>
              {categories.map((cat) => (
                <option key={cat.id} value={cat.id}>
                  {cat.name}
                </option>
              ))}
            </select>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '0.75rem', marginBottom: '1rem' }}>
            <div>
              <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
                Duration (min) *
              </label>
              <input
                type="number"
                min="15"
                max="300"
                className="input-field"
                value={formData.durationMinutes}
                onChange={(e) => setFormData({ ...formData, durationMinutes: e.target.value })}
                required
              />
            </div>
            <div>
              <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
                Default Capacity *
              </label>
              <input
                type="number"
                min="1"
                max="100"
                className="input-field"
                value={formData.defaultCapacity}
                onChange={(e) => setFormData({ ...formData, defaultCapacity: e.target.value })}
                required
              />
            </div>
            <div>
              <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
                Intensity *
              </label>
              <select
                className="input-field"
                value={formData.intensityLevel}
                onChange={(e) => setFormData({ ...formData, intensityLevel: e.target.value })}
                required
              >
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Extreme">Extreme</option>
              </select>
            </div>
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
              placeholder="Outline workout target, equipment used, and prerequisites..."
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              <Save size={16} />
              {submitting ? 'Saving...' : fitnessClass ? 'Update Class' : 'Create Class'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ClassModal;
