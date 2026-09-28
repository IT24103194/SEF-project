import React, { useState } from 'react';
import { X, Send, MessageSquare, Star } from 'lucide-react';
import facilityApi from '../../services/facilityApi';

export const FeedbackResponseModal = ({ feedback, onClose, onSaved }) => {
  const [response, setResponse] = useState(feedback?.adminResponse || '');
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!response.trim()) {
      setErrorMsg('Please enter a response.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');
      await facilityApi.respondFeedback(feedback.id, {
        adminResponse: response.trim(),
        status: 2, // Reviewed
      });
      onSaved();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to submit response.');
    } finally {
      setSubmitting(false);
    }
  };

  if (!feedback) return null;

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(5px)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '1.5rem'
    }}>
      <div className="glass-card" style={{
        maxWidth: '600px', width: '100%', borderRadius: '16px', padding: '2rem', border: '1px solid rgba(255,255,255,0.1)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h3 style={{ fontSize: '1.35rem', fontWeight: 800, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <MessageSquare size={22} color="var(--primary)" /> Respond to Member Feedback
          </h3>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
            <X size={22} />
          </button>
        </div>

        {errorMsg && (
          <div style={{
            backgroundColor: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)',
            color: '#F87171', padding: '0.75rem 1rem', borderRadius: '8px', marginBottom: '1.25rem', fontSize: '0.875rem'
          }}>
            {errorMsg}
          </div>
        )}

        <div style={{
          padding: '1rem', backgroundColor: 'rgba(255,255,255,0.03)',
          borderRadius: '10px', marginBottom: '1.25rem'
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '6px' }}>
            <span style={{ fontWeight: 700 }}>{feedback.subject}</span>
            <div style={{ display: 'flex', alignItems: 'center', gap: '2px', color: '#F59E0B' }}>
              {[...Array(5)].map((_, i) => (
                <Star key={i} size={14} fill={i < feedback.rating ? '#F59E0B' : 'none'} color="#F59E0B" />
              ))}
            </div>
          </div>
          <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', lineHeight: 1.5, margin: 0 }}>
            "{feedback.content}"
          </p>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '8px' }}>
            Submitted by <strong>{feedback.memberName}</strong> ({feedback.memberEmail}) on {new Date(feedback.createdAt).toLocaleDateString()}
          </div>
        </div>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <div>
            <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
              Administrative Response / Resolution Action *
            </label>
            <textarea
              required
              rows={4}
              value={response}
              onChange={(e) => setResponse(e.target.value)}
              placeholder="State what actions have been taken or provide clarification..."
              style={{
                width: '100%', padding: '0.75rem', borderRadius: '8px',
                backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.15)', color: 'white'
              }}
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
            <button type="button" onClick={onClose} className="btn-secondary">
              Cancel
            </button>
            <button type="submit" disabled={submitting} className="btn-primary" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Send size={16} /> {submitting ? 'Submitting...' : 'Send Response'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default FeedbackResponseModal;
