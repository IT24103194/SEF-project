import React, { useState, useEffect } from 'react';
import { X, Save, Award } from 'lucide-react';
import membershipApi from '../../services/membershipApi';

export const PlanModal = ({ plan, onClose, onSaved }) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    price: 49.99,
    durationDays: 30,
    maxClassesPerWeek: 5,
    hasTrainerAccess: false,
    isActive: true,
  });
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (plan) {
      setFormData({
        name: plan.name || '',
        description: plan.description || '',
        price: plan.price ?? 49.99,
        durationDays: plan.durationDays ?? 30,
        maxClassesPerWeek: plan.maxClassesPerWeek ?? 5,
        hasTrainerAccess: !!plan.hasTrainerAccess,
        isActive: plan.isActive ?? true,
      });
    }
  }, [plan]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      setErrorMsg('Plan name is required.');
      return;
    }
    if (formData.price < 0) {
      setErrorMsg('Price cannot be negative.');
      return;
    }
    if (formData.durationDays < 1) {
      setErrorMsg('Duration must be at least 1 day.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      const payload = {
        name: formData.name.trim(),
        description: formData.description.trim(),
        price: parseFloat(formData.price),
        durationDays: parseInt(formData.durationDays, 10),
        maxClassesPerWeek: parseInt(formData.maxClassesPerWeek, 10),
        hasTrainerAccess: formData.hasTrainerAccess,
        isActive: formData.isActive,
      };

      if (plan?.id) {
        await membershipApi.updatePlan(plan.id, payload);
      } else {
        await membershipApi.createPlan(payload);
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
        style={{ width: '100%', maxWidth: '520px', padding: '1.75rem' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <Award size={24} color="var(--primary-color, #6366f1)" />
            <h2 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>
              {plan?.id ? 'Edit Membership Plan' : 'Create Membership Plan'}
            </h2>
          </div>
          <button className="btn-icon" onClick={onClose} aria-label="Close modal">
            <X size={20} />
          </button>
        </div>

        {errorMsg && (
          <div style={{ padding: '0.75rem', background: 'rgba(239, 68, 68, 0.15)', color: '#ef4444', borderRadius: '8px', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {errorMsg}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Plan Name *</label>
            <input
              type="text"
              className="form-input"
              required
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g. Gold All-Access Monthly"
            />
          </div>

          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Description</label>
            <textarea
              className="form-input"
              rows={2}
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Features, amenities included, access hours..."
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Price ($) *</label>
              <input
                type="number"
                step="0.01"
                min="0"
                className="form-input"
                required
                value={formData.price}
                onChange={(e) => setFormData({ ...formData, price: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Duration (Days) *</label>
              <input
                type="number"
                min="1"
                className="form-input"
                required
                value={formData.durationDays}
                onChange={(e) => setFormData({ ...formData, durationDays: e.target.value })}
              />
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.25rem' }}>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Max Classes/Week</label>
              <input
                type="number"
                min="0"
                className="form-input"
                value={formData.maxClassesPerWeek}
                onChange={(e) => setFormData({ ...formData, maxClassesPerWeek: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: '0.5rem' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '0.875rem', fontWeight: 600 }}>
                <input
                  type="checkbox"
                  checked={formData.hasTrainerAccess}
                  onChange={(e) => setFormData({ ...formData, hasTrainerAccess: e.target.checked })}
                />
                Trainer Access Included
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '0.875rem', fontWeight: 600 }}>
                <input
                  type="checkbox"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                />
                Active (Available for purchase)
              </label>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
            <button type="button" className="btn-secondary" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={submitting} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Save size={16} />
              {submitting ? 'Saving...' : 'Save Plan'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default PlanModal;
