import React, { useState } from 'react';
import { X, TrendingUp, Save } from 'lucide-react';
import membershipApi from '../../services/membershipApi';

export const ProgressModal = ({ goal, onClose, onLogged }) => {
  const [value, setValue] = useState('');
  const [notes, setNotes] = useState('');
  const [recordedDate, setRecordedDate] = useState(
    new Date().toISOString().split('T')[0]
  );
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!value || isNaN(value)) {
      setErrorMsg('A valid numeric measurement value is required.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      await membershipApi.recordProgress({
        goalId: goal.id,
        value: parseFloat(value),
        notes: notes.trim() || undefined,
        recordedDate: new Date(recordedDate).toISOString(),
      });

      onLogged();
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || err.message || 'Failed to log progress';
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
        style={{ width: '100%', maxWidth: '460px', padding: '1.75rem' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <TrendingUp size={24} color="var(--primary-color, #6366f1)" />
            <h2 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>
              Log Progress
            </h2>
          </div>
          <button className="btn-icon" onClick={onClose} aria-label="Close modal">
            <X size={20} />
          </button>
        </div>

        {goal && (
          <div style={{ padding: '0.85rem 1rem', background: 'rgba(255, 255, 255, 0.05)', borderRadius: '8px', marginBottom: '1.25rem' }}>
            <div style={{ fontWeight: 700, fontSize: '1rem', marginBottom: '0.25rem' }}>{goal.title}</div>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
              Current: <strong style={{ color: 'var(--text-primary)' }}>{goal.currentValue} {goal.unit}</strong> | Target: <strong style={{ color: 'var(--text-primary)' }}>{goal.targetValue} {goal.unit}</strong>
            </div>
          </div>
        )}

        {errorMsg && (
          <div style={{ padding: '0.75rem', background: 'rgba(239, 68, 68, 0.15)', color: '#ef4444', borderRadius: '8px', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {errorMsg}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '0.75rem', marginBottom: '1rem' }}>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>New Measurement *</label>
              <input
                type="number"
                step="0.1"
                min="0"
                className="form-input"
                required
                value={value}
                onChange={(e) => setValue(e.target.value)}
                placeholder="e.g. 85.5"
                autoFocus
              />
            </div>
            <div className="form-group">
              <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Unit</label>
              <input
                type="text"
                className="form-input"
                disabled
                value={goal?.unit || 'kg'}
                style={{ opacity: 0.7, cursor: 'not-allowed' }}
              />
            </div>
          </div>

          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Date Recorded *</label>
            <input
              type="date"
              className="form-input"
              required
              value={recordedDate}
              onChange={(e) => setRecordedDate(e.target.value)}
            />
          </div>

          <div className="form-group" style={{ marginBottom: '1.5rem' }}>
            <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>Notes / Observations</label>
            <textarea
              className="form-input"
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="e.g. Set new PR during afternoon workout, felt great"
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
            <button type="button" className="btn-secondary" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={submitting} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Save size={16} />
              {submitting ? 'Saving...' : 'Record Measurement'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ProgressModal;
