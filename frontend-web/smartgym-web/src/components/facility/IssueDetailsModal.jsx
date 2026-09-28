import React, { useState, useEffect } from 'react';
import { 
  X, CheckCircle, Clock, AlertTriangle, ShieldCheck, 
  Upload, Image as ImageIcon, Send, ArrowRight, User, MapPin, Wrench 
} from 'lucide-react';
import facilityApi from '../../services/facilityApi';

export const IssueDetailsModal = ({ issue, onClose, onRefresh }) => {
  const [history, setHistory] = useState([]);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [selectedStatus, setSelectedStatus] = useState('');
  const [resolutionNotes, setResolutionNotes] = useState('');
  const [transitionComments, setTransitionComments] = useState('');
  const [submittingStatus, setSubmittingStatus] = useState(false);
  const [uploadingImage, setUploadingImage] = useState(false);
  const [selectedImageZoom, setSelectedImageZoom] = useState(null);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (issue?.id) {
      loadHistory();
    }
  }, [issue?.id]);

  const loadHistory = async () => {
    try {
      setLoadingHistory(true);
      const data = await facilityApi.getIssueHistory(issue.id);
      setHistory(data || []);
    } catch (err) {
      console.error('Failed to load issue history', err);
    } finally {
      setLoadingHistory(false);
    }
  };

  const handleStatusChange = async (e) => {
    e.preventDefault();
    if (!selectedStatus) return;

    if (selectedStatus === 'RESOLVED' && !resolutionNotes.trim()) {
      setErrorMsg('Resolution notes are required when marking an issue as RESOLVED.');
      return;
    }

    try {
      setSubmittingStatus(true);
      setErrorMsg('');
      await facilityApi.transitionStatus(issue.id, {
        newStatus: selectedStatus,
        resolutionNotes: selectedStatus === 'RESOLVED' ? resolutionNotes.trim() : null,
        comments: transitionComments.trim() || null,
      });
      setSelectedStatus('');
      setResolutionNotes('');
      setTransitionComments('');
      await loadHistory();
      if (onRefresh) onRefresh();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to update status.');
    } finally {
      setSubmittingStatus(false);
    }
  };

  const handleImageUpload = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      setUploadingImage(true);
      setErrorMsg('');
      await facilityApi.uploadIssueImage(issue.id, file);
      await loadHistory();
      if (onRefresh) onRefresh();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to upload photo.');
    } finally {
      setUploadingImage(false);
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 'SUBMITTED': return '#64748B';
      case 'AI_ANALYZING': return '#8B5CF6';
      case 'PENDING_APPROVAL': return '#F59E0B';
      case 'APPROVED': return '#3B82F6';
      case 'VENDOR_CONTACTED': return '#06B6D4';
      case 'REPAIR_SCHEDULED': return '#EC4899';
      case 'IN_PROGRESS': return '#38BDF8';
      case 'RESOLVED': return '#10B981';
      case 'REJECTED': return '#EF4444';
      case 'REVISION_REQUIRED': return '#F97316';
      default: return '#94A3B8';
    }
  };

  if (!issue) return null;

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0, 0, 0, 0.75)', backdropFilter: 'blur(6px)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '1.5rem'
    }}>
      <div className="glass-card" style={{
        maxWidth: '900px', width: '100%', maxHeight: '90vh', overflowY: 'auto',
        borderRadius: '16px', padding: '2rem', border: '1px solid rgba(255,255,255,0.1)'
      }}>
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem' }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.5rem' }}>
              <span style={{
                backgroundColor: `${getStatusColor(issue.statusName || issue.status)}22`,
                color: getStatusColor(issue.statusName || issue.status),
                padding: '4px 12px', borderRadius: '999px', fontSize: '0.75rem', fontWeight: 700,
                border: `1px solid ${getStatusColor(issue.statusName || issue.status)}55`
              }}>
                {issue.statusName || issue.status}
              </span>
              <span style={{
                backgroundColor: issue.severityName === 'Critical' ? '#EF444422' : '#F59E0B22',
                color: issue.severityName === 'Critical' ? '#EF4444' : '#F59E0B',
                padding: '4px 10px', borderRadius: '6px', fontSize: '0.75rem', fontWeight: 600
              }}>
                {issue.severityName || issue.severity} Severity
              </span>
              {issue.moderationStatus === 'Flagged' ? (
                <span style={{
                  backgroundColor: '#F59E0B22', color: '#F59E0B', padding: '4px 10px',
                  borderRadius: '6px', fontSize: '0.75rem', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '4px'
                }}>
                  <AlertTriangle size={12} /> Content Moderated
                </span>
              ) : (
                <span style={{
                  backgroundColor: '#10B98122', color: '#10B981', padding: '4px 10px',
                  borderRadius: '6px', fontSize: '0.75rem', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '4px'
                }}>
                  <ShieldCheck size={12} /> Clean Content
                </span>
              )}
            </div>
            <h2 style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--text-primary)' }}>{issue.title}</h2>
          </div>
          <button onClick={onClose} style={{
            background: 'none', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer', padding: '4px'
          }}>
            <X size={24} />
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

        {/* Issue Details Grid */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
          <div style={{ padding: '0.75rem 1rem', backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: '8px' }}>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <User size={14} /> Reported By
            </div>
            <div style={{ fontWeight: 600, marginTop: '4px' }}>{issue.reporterName || 'Member'}</div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{issue.reporterEmail}</div>
          </div>
          <div style={{ padding: '0.75rem 1rem', backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: '8px' }}>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <MapPin size={14} /> Location / Zone
            </div>
            <div style={{ fontWeight: 600, marginTop: '4px' }}>{issue.locationName}</div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{issue.locationFloor}</div>
          </div>
          <div style={{ padding: '0.75rem 1rem', backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: '8px' }}>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Wrench size={14} /> Equipment Asset
            </div>
            <div style={{ fontWeight: 600, marginTop: '4px' }}>{issue.equipmentName || 'General Facility Area'}</div>
            {issue.equipmentSerialNumber && (
              <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>SN: {issue.equipmentSerialNumber}</div>
            )}
          </div>
        </div>

        {/* Description & Moderation Notes */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '0.5rem' }}>
            Problem Description
          </label>
          <div style={{
            padding: '1rem', backgroundColor: 'rgba(0,0,0,0.3)', borderRadius: '8px', border: '1px solid rgba(255,255,255,0.05)',
            fontSize: '0.925rem', lineHeight: 1.5, whiteSpace: 'pre-wrap'
          }}>
            {issue.sanitizedDescription || issue.description}
          </div>
          {issue.moderationReason && (
            <p style={{ fontSize: '0.75rem', color: '#F59E0B', marginTop: '0.35rem' }}>
              ⚠️ Moderation Note: {issue.moderationReason}
            </p>
          )}
        </div>

        {/* Resolution Notes (if resolved) */}
        {issue.resolutionNotes && (
          <div style={{
            marginBottom: '1.5rem', padding: '1rem', backgroundColor: 'rgba(16, 185, 129, 0.08)',
            border: '1px solid rgba(16, 185, 129, 0.25)', borderRadius: '8px'
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#10B981', fontWeight: 700, marginBottom: '0.5rem' }}>
              <CheckCircle size={18} /> Resolution Data (Resolved at {new Date(issue.resolvedAt).toLocaleString()})
            </div>
            <p style={{ fontSize: '0.9rem', color: 'var(--text-primary)' }}>{issue.resolutionNotes}</p>
          </div>
        )}

        {/* Attached Photos */}
        <div style={{ marginBottom: '1.5rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
            <label style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <ImageIcon size={16} /> Attached Photos ({issue.images?.length || 0})
            </label>
            <label style={{
              display: 'inline-flex', alignItems: 'center', gap: '6px', padding: '6px 12px',
              backgroundColor: 'rgba(255,255,255,0.08)', borderRadius: '6px', fontSize: '0.8rem',
              cursor: 'pointer', color: 'var(--text-primary)'
            }}>
              <Upload size={14} /> {uploadingImage ? 'Uploading...' : 'Attach Photo'}
              <input type="file" accept="image/png,image/jpeg,image/webp" onChange={handleImageUpload} style={{ display: 'none' }} disabled={uploadingImage} />
            </label>
          </div>
          
          <div style={{ display: 'flex', gap: '1rem', overflowX: 'auto', paddingBottom: '0.5rem' }}>
            {issue.images && issue.images.length > 0 ? (
              issue.images.map((img) => (
                <div 
                  key={img.id} 
                  onClick={() => setSelectedImageZoom(img.imageUrl)}
                  style={{
                    width: '120px', height: '90px', borderRadius: '8px', overflow: 'hidden',
                    border: '1px solid rgba(255,255,255,0.1)', cursor: 'pointer', flexShrink: 0
                  }}
                >
                  <img src={img.imageUrl} alt="Facility Issue" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                </div>
              ))
            ) : (
              <p style={{ fontSize: '0.825rem', color: 'var(--text-secondary)' }}>No photos attached to this report.</p>
            )}
          </div>
        </div>

        {/* State Transition Controls */}
        <div style={{
          padding: '1.25rem', backgroundColor: 'rgba(255,255,255,0.02)',
          border: '1px solid rgba(255,255,255,0.08)', borderRadius: '12px', marginBottom: '1.5rem'
        }}>
          <h4 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '0.75rem', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <ArrowRight size={18} color="var(--primary)" /> Advance Issue State Machine
          </h4>

          <form onSubmit={handleStatusChange} style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
              <select
                value={selectedStatus}
                onChange={(e) => setSelectedStatus(e.target.value)}
                style={{
                  flex: '1 1 250px', padding: '0.65rem 1rem', borderRadius: '8px',
                  backgroundColor: 'rgba(0,0,0,0.5)', border: '1px solid rgba(255,255,255,0.15)', color: 'white'
                }}
              >
                <option value="">-- Select Target Status --</option>
                <option value="AI_ANALYZING">AI_ANALYZING (Trigger AI Analysis)</option>
                <option value="PENDING_APPROVAL">PENDING_APPROVAL (Hold for Financial Review)</option>
                <option value="APPROVED">APPROVED (Authorize Repair)</option>
                <option value="VENDOR_CONTACTED">VENDOR_CONTACTED (Dispatch Supplier)</option>
                <option value="REPAIR_SCHEDULED">REPAIR_SCHEDULED (Service Date Booked)</option>
                <option value="IN_PROGRESS">IN_PROGRESS (Technician On Site)</option>
                <option value="RESOLVED">RESOLVED (Complete Repair)</option>
                <option value="REVISION_REQUIRED">REVISION_REQUIRED (Request More Details)</option>
                <option value="REJECTED">REJECTED (Decline / Duplicate)</option>
              </select>

              <button
                type="submit"
                disabled={!selectedStatus || submittingStatus}
                className="btn-primary"
                style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
              >
                <Send size={16} /> {submittingStatus ? 'Updating...' : 'Update Status'}
              </button>
            </div>

            {selectedStatus === 'RESOLVED' && (
              <div>
                <label style={{ fontSize: '0.8rem', fontWeight: 600, color: '#10B981', display: 'block', marginBottom: '4px' }}>
                  Resolution Notes (MANDATORY for RESOLVED status)
                </label>
                <textarea
                  required
                  rows={3}
                  value={resolutionNotes}
                  onChange={(e) => setResolutionNotes(e.target.value)}
                  placeholder="Detail parts replaced, tests conducted, or work done..."
                  style={{
                    width: '100%', padding: '0.75rem', borderRadius: '8px',
                    backgroundColor: 'rgba(0,0,0,0.5)', border: '1px solid #10B98166', color: 'white'
                  }}
                />
              </div>
            )}

            <input
              type="text"
              value={transitionComments}
              onChange={(e) => setTransitionComments(e.target.value)}
              placeholder="Internal transition log note or technician update (optional)..."
              style={{
                width: '100%', padding: '0.65rem 1rem', borderRadius: '8px',
                backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.1)', color: 'white', fontSize: '0.85rem'
              }}
            />
          </form>
        </div>

        {/* Audit & Status History Timeline */}
        <div>
          <h4 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '0.75rem', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Clock size={18} color="var(--primary)" /> Issue History & Audit Trail
          </h4>

          {loadingHistory ? (
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>Loading timeline...</p>
          ) : history.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
              {history.map((h) => (
                <div key={h.id} style={{
                  padding: '0.75rem 1rem', backgroundColor: 'rgba(255,255,255,0.02)',
                  borderRadius: '8px', borderLeft: '3px solid var(--primary)', fontSize: '0.85rem'
                }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                    <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{h.action}</span>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '0.75rem' }}>
                      {new Date(h.timestamp).toLocaleString()}
                    </span>
                  </div>
                  <div style={{ color: 'var(--text-secondary)' }}>
                    By: <strong>{h.performedByUserName || 'System'}</strong>
                    {h.fromStatus && h.toStatus && ` • ${h.fromStatus} → ${h.toStatus}`}
                  </div>
                  {h.notes && (
                    <div style={{ marginTop: '4px', color: 'var(--text-primary)', fontStyle: 'italic' }}>
                      "{h.notes}"
                    </div>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>No history entries logged yet.</p>
          )}
        </div>

        {/* Image Zoom Modal */}
        {selectedImageZoom && (
          <div 
            onClick={() => setSelectedImageZoom(null)}
            style={{
              position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
              backgroundColor: 'rgba(0,0,0,0.9)', display: 'flex', alignItems: 'center', justifyContent: 'center',
              zIndex: 1100, cursor: 'zoom-out'
            }}
          >
            <img src={selectedImageZoom} alt="Enlarged" style={{ maxWidth: '90%', maxHeight: '90%', borderRadius: '12px' }} />
          </div>
        )}
      </div>
    </div>
  );
};

export default IssueDetailsModal;
