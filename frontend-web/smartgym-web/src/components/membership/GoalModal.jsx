import React, { useState, useEffect } from 'react';
import { X, Target, Save } from 'lucide-react';
import membershipApi from '../../services/membershipApi';

export const GoalModal = ({ goal, memberId, members = [], onClose, onSaved, isStaff = false }) => {
  const [formData, setFormData] = useState({
    memberId: memberId || '',
    title: '',
    targetValue: '',
    currentValue: '',
    unit: 'kg',
    targetDate: new Date(Date.now() + 60 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
  });
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (goal) {
      setFormData({
        memberId: goal.memberId || '',
        title: goal.title || '',
        targetValue: goal.targetValue || '',
        currentValue: goal.currentValue || '',
        unit: goal.unit || 'kg',
        targetDate: goal.targetDate ? goal.targetDate.split('T')[0] : '',
      });
    }
  }, [goal]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.title.trim()) {
      setErrorMsg('Goal title is required.');
      return;
    }
    if (!formData.targetValue || Number(formData.targetValue) <= 0) {
      setErrorMsg('Target value must be greater than zero.');
      return;
    }
    if (!formData.targetDate) {
      setErrorMsg('Target date is required.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      const payload = {
        title: formData.title.trim(),
        targetValue: parseFloat(formData.targetValue),
        currentValue: formData.currentValue ? parseFloat(formData.currentValue) : 0,
        unit: formData.unit.trim(),
        targetDate: new Date(formData.targetDate).toISOString(),
      };

      if (isStaff && formData.memberId) {
        payload.memberId = formData.memberId;
      }

      if (goal?.id) {
        await membershipApi.updateGoal(goal.id, {
          title: payload.title,
          targetValue: payload.targetValue,
          unit: payload.unit,
          targetDate: payload.targetDate,
        });
      } else {
        await membershipApi.createGoal(payload);
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
        style={{ width: '100%', maxWidth: '500px', padding: '1.75rem' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <Target size={24} color="var(--primary-color, #6366f1)" />
            <h2 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>
              {goal?.id ? 'Edit Fitness Goal' : 'Create Fitness Goal'}
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
          {isStaff && !goal?.id && (
            <div className="form-group" style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Member *</label>
              <select
                className="form-input"
                value={formData.memberId}
                onChange={(e) => setFormData({ ...formData, memberId: e.target.value })}
                required
              >
                <option value="" disabled>Select member...</option>
                {members.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.fullName || m.userName} ({m.email})
                  </option>
                ))}
              </select>
            </div>
          )}

          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Goal Title *</label>
            <input
              type="text"
              className="form-input"
              required
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              placeholder="e.g. Bench Press 100kg, 10km Marathon, Body Fat 15%"
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Target Value *</label>
              <input
                type="number"
                step="0.1"
                min="0.1"
                className="form-input"
                required
                value={formData.targetValue}
                onChange={(e) => setFormData({ ...formData, targetValue: e.target.value })}
                placeholder="100"
              />
            </div>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Unit of Measure *</label>
              <input
                type="text"
                className="form-input"
                required
                value={formData.unit}
                onChange={(e) => setFormData({ ...formData, unit: e.target.value })}
                placeholder="kg, lbs, km, reps, %"
              />
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.25rem' }}>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Baseline / Current</label>
              <input
                type="number"
                step="0.1"
                min="0"
                className="form-input"
                value={formData.currentValue}
                onChange={(e) => setFormData({ ...formData, currentValue: e.target.value })}
                placeholder="60"
              />
            </div>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Target Date *</label>
              <input
                type="date"
                className="form-input"
                required
                value={formData.targetDate}
                onChange={(e) => setFormData({ ...formData, targetDate: e.target.value })}
              />
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
            <button type="button" className="btn-secondary" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={submitting} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Save size={16} />
              {submitting ? 'Saving...' : 'Save Goal'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default GoalModal;
