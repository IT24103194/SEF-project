import React, { useState, useEffect } from 'react';
import approvalsApi from '../services/approvalsApi';
import aiWorkflowsApi from '../services/aiWorkflowsApi';
import PageHeader from '../components/common/PageHeader';
import Pagination from '../components/common/Pagination';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { CheckCircle2, XCircle, AlertCircle, ShieldAlert, DollarSign, Wrench, MessageSquare, X } from 'lucide-react';

export const ApprovalsPage = () => {
  const [approvals, setApprovals] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('Pending');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Decision Modal
  const [selectedApproval, setSelectedApproval] = useState(null);
  const [decisionType, setDecisionType] = useState('Approved');
  const [comments, setComments] = useState('');
  const [managerJustification, setManagerJustification] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const fetchApprovals = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await approvalsApi.getApprovals({
        status: statusFilter || undefined,
        page: pageNumber,
        pageSize,
      });
      setApprovals(data.items || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load approvals:', err);
      setError('Unable to load approval queue.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchApprovals();
  }, [statusFilter, pageNumber, pageSize]);

  const handleOpenDecision = (item, type) => {
    setSelectedApproval(item);
    setDecisionType(type);
    setComments('');
    setManagerJustification('');
  };

  const handleSubmitDecision = async (e) => {
    e.preventDefault();
    if (!selectedApproval) return;

    if (decisionType === 'Approved' && (selectedApproval.estimatedCost >= 25000 || selectedApproval.estimatedCost >= 250)) {
      if (!managerJustification.trim() || managerJustification.trim().length < 10) {
        setError('High-value expenditures require an explicit manager audit justification note (at least 10 characters).');
        return;
      }
    }

    setIsSubmitting(true);
    setError(null);
    try {
      if (selectedApproval.aiWorkflowId) {
        const payloadComment = managerJustification.trim() 
          ? `[Justification: ${managerJustification.trim()}] ${comments.trim()}`.trim()
          : comments.trim();

        if (decisionType === 'Approved') {
          await aiWorkflowsApi.approveWorkflow(selectedApproval.aiWorkflowId, payloadComment);
        } else if (decisionType === 'Rejected') {
          await aiWorkflowsApi.rejectWorkflow(selectedApproval.aiWorkflowId, payloadComment);
        } else {
          await aiWorkflowsApi.reviseWorkflow(selectedApproval.aiWorkflowId, payloadComment);
        }
      } else {
        await approvalsApi.submitDecision(selectedApproval.id, {
          decision: decisionType === 'RevisionRequired' ? 'RevisionRequired' : decisionType,
          comments: comments.trim() || undefined,
          managerJustification: managerJustification.trim() || undefined,
          estimatedBudgetImpact: selectedApproval.estimatedCost,
        });
      }
      setSelectedApproval(null);
      fetchApprovals();
    } catch (err) {
      console.error('Failed to submit decision:', err);
      setError(err?.response?.data?.message || 'Failed to record approval decision.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Human-in-the-Loop Approvals"
        subtitle="Authorize high-value facility repair orders, agentic AI recommendations, and vendor quotes"
        icon={ShieldAlert}
        badge={`${totalCount} in Queue`}
        actions={
          <button onClick={fetchApprovals} className="btn btn-secondary">
            Refresh
          </button>
        }
      />

      {/* Filter Tabs */}
      <div className="glass-card" style={{ padding: '0.75rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600, marginRight: '0.5rem' }}>Decision Status:</span>
        {['Pending', 'Approved', 'Rejected', 'Revised', ''].map((s) => (
          <button
            key={s}
            onClick={() => {
              setStatusFilter(s);
              setPageNumber(1);
            }}
            className={`btn ${statusFilter === s ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.4rem 0.85rem', fontSize: '0.8rem' }}
          >
            {s || 'All Records'}
          </button>
        ))}
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading approval queue..." />
      ) : approvals.length === 0 ? (
        <EmptyState
          icon={CheckCircle2}
          title="No pending approvals"
          description={statusFilter ? `There are no items currently marked as "${statusFilter}".` : 'All repair orders and AI workflows have been reviewed.'}
        />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {approvals.map((app) => {
            const isPending = app.decision === 'Pending' || app.decision === 'PendingApproval';
            const isHighValue = app.estimatedCost >= app.approvalThreshold;

            return (
              <div
                key={app.id}
                className="glass-card"
                style={{
                  padding: '1.5rem',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  flexWrap: 'wrap',
                  gap: '1.25rem',
                  border: isPending ? '1px solid var(--border-active)' : '1px solid var(--border-subtle)',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: '1rem' }}>
                  <div style={{
                    width: 44,
                    height: 44,
                    borderRadius: 'var(--radius-md)',
                    background: isPending ? 'rgba(245,158,11,0.15)' : 'rgba(16,185,129,0.15)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flexShrink: 0,
                  }}>
                    <Wrench size={22} color={isPending ? 'var(--accent-amber)' : 'var(--accent-emerald)'} />
                  </div>
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem', marginBottom: '0.25rem' }}>
                      <h4 style={{ fontSize: '1.1rem', fontWeight: 800, color: '#ffffff' }}>
                        Order {app.orderNumber}
                      </h4>
                      <span className="badge badge-info" style={{ fontSize: '0.7rem' }}>
                        {app.equipmentName}
                      </span>
                      {isHighValue && (
                        <span className="badge badge-warning" style={{ fontSize: '0.65rem' }}>
                          High Value (&gt;${app.approvalThreshold})
                        </span>
                      )}
                      {app.aiWorkflowId && (
                        <span className="badge badge-primary" style={{ fontSize: '0.65rem', background: 'rgba(59,130,246,0.2)', color: '#60a5fa' }}>
                          AI Workflow
                        </span>
                      )}
                    </div>
                    <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '0.4rem' }}>
                      Issue: <strong style={{ color: '#ffffff' }}>{app.issueTitle || 'Reported breakdown'}</strong>
                    </div>
                    <div style={{ display: 'flex', gap: '1.5rem', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                      <span>Estimated: <strong style={{ color: 'var(--accent-emerald)' }}>${app.estimatedCost?.toFixed(2)}</strong></span>
                      <span>Requested: {new Date(app.requestedAt).toLocaleDateString()}</span>
                      {app.approverName && <span>Reviewed by: {app.approverName}</span>}
                    </div>
                    {app.comments && (
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', background: 'rgba(255,255,255,0.02)', padding: '0.4rem 0.6rem', borderRadius: '4px', marginTop: '0.5rem' }}>
                        Notes: {app.comments}
                      </div>
                    )}
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  {isPending ? (
                    <>
                      <button
                        onClick={() => handleOpenDecision(app, 'Approved')}
                        className="btn btn-primary"
                        style={{ padding: '0.5rem 0.85rem', background: 'var(--accent-emerald)' }}
                        title="Authorize repair order and workflow execution"
                      >
                        <CheckCircle2 size={16} /> Authorize
                      </button>
                      <button
                        onClick={() => handleOpenDecision(app, 'RevisionRequired')}
                        className="btn btn-secondary"
                        style={{ padding: '0.5rem 0.85rem', borderColor: 'var(--accent-amber)', color: 'var(--accent-amber)' }}
                        title="Request revision from planner agent"
                      >
                        <AlertCircle size={16} /> Request Revision
                      </button>
                      <button
                        onClick={() => handleOpenDecision(app, 'Rejected')}
                        className="btn btn-danger"
                        style={{ padding: '0.5rem 0.85rem' }}
                        title="Reject repair order and stop execution"
                      >
                        <XCircle size={16} /> Reject
                      </button>
                    </>
                  ) : (
                    <span className={`badge ${app.decision === 'Approved' ? 'badge-success' : (app.decision === 'Rejected' ? 'badge-danger' : 'badge-warning')}`} style={{ fontSize: '0.8rem' }}>
                      {app.decision}
                    </span>
                  )}
                </div>
              </div>
            );
          })}

          <Pagination
            pageNumber={pageNumber}
            pageSize={pageSize}
            totalCount={totalCount}
            totalPages={totalPages}
            onPageChange={setPageNumber}
            onPageSizeChange={(newSize) => {
              setPageSize(newSize);
              setPageNumber(1);
            }}
          />
        </div>
      )}

      {/* Decision Modal */}
      {selectedApproval && (
        <div className="modal-backdrop">
          <div className="modal-content" style={{ maxWidth: 480 }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1.25rem' }}>
              <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: '#ffffff' }}>
                Confirm {decisionType === 'Approved' ? 'Approval' : (decisionType === 'Rejected' ? 'Rejection' : 'Revision Request')}: {selectedApproval.orderNumber}
              </h3>
              <button
                onClick={() => setSelectedApproval(null)}
                style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
              >
                <X size={20} />
              </button>
            </div>

            <form onSubmit={handleSubmitDecision}>
              <div style={{ marginBottom: '1rem', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                You are about to {decisionType === 'RevisionRequired' ? 'request a revision for' : decisionType.toLowerCase()} the repair order for <strong style={{ color: '#ffffff' }}>{selectedApproval.equipmentName}</strong> estimated at <strong style={{ color: 'var(--accent-emerald)' }}>${selectedApproval.estimatedCost?.toFixed(2)}</strong>.
              </div>

              {(selectedApproval.estimatedCost >= 25000 || selectedApproval.estimatedCost >= 250) && (
                <div style={{
                  backgroundColor: 'rgba(245, 158, 11, 0.15)',
                  border: '1px solid rgba(245, 158, 11, 0.3)',
                  padding: '0.75rem',
                  borderRadius: '6px',
                  marginBottom: '1rem',
                  fontSize: '0.8rem',
                  color: 'var(--accent-amber)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px'
                }}>
                  <AlertCircle size={16} />
                  <span>
                    <strong>High-Value Authorization:</strong> Expenditures of this scale require mandatory manager audit justification.
                  </span>
                </div>
              )}

              {decisionType === 'Approved' && (selectedApproval.estimatedCost >= 25000 || selectedApproval.estimatedCost >= 250) && (
                <div className="form-group" style={{ marginBottom: '1rem' }}>
                  <label className="form-label" style={{ color: 'var(--accent-amber)', fontWeight: 600 }}>
                    Mandatory Manager Audit Justification *
                  </label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="e.g. Approved replacement of OEM motor bearings under maintenance SLA..."
                    value={managerJustification}
                    onChange={(e) => setManagerJustification(e.target.value)}
                    required
                  />
                </div>
              )}

              <div className="form-group">
                <label className="form-label">Review / Additional Comments</label>
                <textarea
                  className="form-textarea"
                  rows={3}
                  placeholder={decisionType === 'RevisionRequired' ? 'Explain what changes or additional data are required...' : 'Enter notes or review comments...'}
                  value={comments}
                  onChange={(e) => setComments(e.target.value)}
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button
                  type="button"
                  onClick={() => setSelectedApproval(null)}
                  className="btn btn-secondary"
                  disabled={isSubmitting}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className={`btn ${decisionType === 'Approved' ? 'btn-primary' : (decisionType === 'Rejected' ? 'btn-danger' : 'btn-secondary')}`}
                  style={decisionType === 'RevisionRequired' ? { background: 'var(--accent-amber)', color: '#000', fontWeight: 700 } : {}}
                >
                  {isSubmitting ? 'Recording...' : (decisionType === 'Approved' ? 'Confirm Approval' : (decisionType === 'Rejected' ? 'Confirm Rejection' : 'Submit Revision Request'))}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default ApprovalsPage;
