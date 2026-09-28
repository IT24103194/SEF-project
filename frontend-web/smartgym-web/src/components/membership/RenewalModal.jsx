import React, { useState } from 'react';
import { X, RefreshCw, Calendar, CheckCircle } from 'lucide-react';
import membershipApi from '../../services/membershipApi';

export const RenewalModal = ({ member, currentMembership, plans = [], onClose, onRenewed, isStaff = false }) => {
  const [selectedPlanId, setSelectedPlanId] = useState(
    currentMembership?.planId || (plans.length > 0 ? plans[0].id : '')
  );
  const [autoRenew, setAutoRenew] = useState(currentMembership?.autoRenew ?? false);
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const selectedPlan = plans.find((p) => p.id === selectedPlanId);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedPlanId) {
      setErrorMsg('Please select a membership plan.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      const payload = {
        memberId: member?.id || currentMembership?.memberId,
        planId: selectedPlanId,
        autoRenew: autoRenew,
      };

      await membershipApi.renewMembership(payload);
      onRenewed();
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || err.message || 'Renewal failed';
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
        style={{ width: '100%', maxWidth: '480px', padding: '1.75rem' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <RefreshCw size={24} color="var(--primary-color, #6366f1)" />
            <h2 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>
              Renew Membership
            </h2>
          </div>
          <button className="btn-icon" onClick={onClose} aria-label="Close modal">
            <X size={20} />
          </button>
        </div>

        {member && (
          <div style={{ padding: '0.75rem 1rem', background: 'rgba(255, 255, 255, 0.05)', borderRadius: '8px', marginBottom: '1rem' }}>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>Renewing for Member</div>
            <div style={{ fontWeight: 600 }}>{member.fullName || member.userName || member.email}</div>
          </div>
        )}

        {errorMsg && (
          <div style={{ padding: '0.75rem', background: 'rgba(239, 68, 68, 0.15)', color: '#ef4444', borderRadius: '8px', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {errorMsg}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group" style={{ marginBottom: '1.25rem' }}>
            <label style={{ display: 'block', marginBottom: '0.35rem', fontSize: '0.875rem', fontWeight: 600 }}>
              Select Membership Plan *
            </label>
            <select
              className="form-input"
              value={selectedPlanId}
              onChange={(e) => setSelectedPlanId(e.target.value)}
              required
            >
              <option value="" disabled>Choose a plan...</option>
              {plans.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} — ${p.price} ({p.durationDays} days)
                </option>
              ))}
            </select>
          </div>

          {selectedPlan && (
            <div style={{ padding: '1rem', background: 'rgba(99, 102, 241, 0.08)', borderRadius: '8px', border: '1px solid rgba(99, 102, 241, 0.2)', marginBottom: '1.25rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Duration:</span>
                <span style={{ fontWeight: 600 }}>{selectedPlan.durationDays} days</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Classes per week:</span>
                <span style={{ fontWeight: 600 }}>{selectedPlan.maxClassesPerWeek}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Trainer Access:</span>
                <span style={{ fontWeight: 600 }}>{selectedPlan.hasTrainerAccess ? 'Yes' : 'No'}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', borderTop: '1px solid rgba(255, 255, 255, 0.1)', paddingTop: '0.5rem' }}>
                <span style={{ fontWeight: 700 }}>Total Fee:</span>
                <span style={{ fontWeight: 700, color: 'var(--primary-color, #6366f1)', fontSize: '1.1rem' }}>${selectedPlan.price.toFixed(2)}</span>
              </div>
            </div>
          )}

          <div className="form-group" style={{ marginBottom: '1.5rem' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '0.875rem', fontWeight: 600 }}>
              <input
                type="checkbox"
                checked={autoRenew}
                onChange={(e) => setAutoRenew(e.target.checked)}
              />
              Enable Auto-Renewal upon expiry
            </label>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
            <button type="button" className="btn-secondary" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={submitting} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <RefreshCw size={16} />
              {submitting ? 'Processing...' : 'Confirm Renewal'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default RenewalModal;
