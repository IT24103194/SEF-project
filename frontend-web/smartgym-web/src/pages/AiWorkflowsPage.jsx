import React, { useState, useEffect } from 'react';
import aiWorkflowsApi from '../services/aiWorkflowsApi';
import PageHeader from '../components/common/PageHeader';
import SearchBar from '../components/common/SearchBar';
import Pagination from '../components/common/Pagination';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Bot, Cpu, CheckCircle2, AlertTriangle, Eye, Clock, X, Terminal, ShieldAlert } from 'lucide-react';

export const AiWorkflowsPage = () => {
  const [workflows, setWorkflows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Trace Detail Modal
  const [selectedWorkflow, setSelectedWorkflow] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const fetchWorkflows = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await aiWorkflowsApi.getWorkflows({
        status: statusFilter || undefined,
        search: search.trim() || undefined,
        page: pageNumber,
        pageSize,
      });
      setWorkflows(data.items || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load AI workflows:', err);
      setError('Unable to load AI workflows.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchWorkflows();
  }, [statusFilter, pageNumber, pageSize]);

  const handleOpenDetail = async (id) => {
    setDetailLoading(true);
    try {
      const data = await aiWorkflowsApi.getWorkflowById(id);
      setSelectedWorkflow(data);
    } catch (err) {
      console.error('Failed to load workflow trace:', err);
    } finally {
      setDetailLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Completed':
        return <span className="badge badge-success">Completed</span>;
      case 'Executing':
      case 'Analyzing':
        return <span className="badge badge-info">{status}</span>;
      case 'AwaitingApproval':
        return <span className="badge badge-warning">Awaiting Approval</span>;
      case 'Failed':
        return <span className="badge badge-danger">Failed</span>;
      default:
        return <span className="badge badge-info">{status}</span>;
    }
  };

  return (
    <div>
      <PageHeader
        title="AI Workflow Operations"
        subtitle="Autonomous agentic workflow execution traces, tool dispatches, and diagnostic telemetry"
        icon={Bot}
        badge={`${totalCount} Traces`}
        actions={
          <button onClick={fetchWorkflows} className="btn btn-primary">
            Refresh
          </button>
        }
      />

      {/* Filter and Search Bar */}
      <div className="glass-card" style={{ padding: '1rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <form onSubmit={(e) => { e.preventDefault(); setPageNumber(1); fetchWorkflows(); }} style={{ display: 'flex', gap: '0.75rem', flex: 1, maxWidth: 360 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search diagnosis or issue..."
            onClear={() => {
              setSearch('');
              setPageNumber(1);
              fetchWorkflows();
            }}
          />
        </form>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Status:</span>
          <select
            className="form-select"
            style={{ width: 'auto', padding: '0.35rem 0.75rem' }}
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPageNumber(1);
            }}
          >
            <option value="">All Statuses</option>
            <option value="Initiated">Initiated</option>
            <option value="Analyzing">Analyzing</option>
            <option value="AwaitingApproval">Awaiting Approval</option>
            <option value="Executing">Executing</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
          </select>
        </div>
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Querying AI workflow logs..." />
      ) : workflows.length === 0 ? (
        <EmptyState
          icon={Bot}
          title="No AI workflows found"
          description="There are no agentic AI workflows matching the filter criteria."
        />
      ) : (
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Issue / Target</th>
                <th>Workflow Type</th>
                <th>Confidence</th>
                <th>Model</th>
                <th>Tokens</th>
                <th>Steps</th>
                <th>Status</th>
                <th style={{ textAlign: 'right' }}>Execution Trace</th>
              </tr>
            </thead>
            <tbody>
              {workflows.map((w) => (
                <tr key={w.id}>
                  <td>
                    <div style={{ fontWeight: 600, color: '#ffffff' }}>
                      {w.issueTitle || 'Diagnostic Workflow'}
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                      Equipment: {w.equipmentName}
                    </div>
                  </td>
                  <td>
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                      {w.workflowType}
                    </span>
                  </td>
                  <td>
                    <strong style={{ color: w.estimatedConfidenceScore >= 0.8 ? 'var(--accent-emerald)' : 'var(--accent-amber)' }}>
                      {Math.round(w.estimatedConfidenceScore * 100)}%
                    </strong>
                  </td>
                  <td>
                    <span style={{ fontSize: '0.8rem', color: 'var(--accent-cyan)', fontFamily: 'monospace' }}>
                      {w.modelIdentifier}
                    </span>
                  </td>
                  <td>
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                      {w.totalTokensUsed?.toLocaleString()}
                    </span>
                  </td>
                  <td>
                    <span style={{ fontSize: '0.85rem', color: '#ffffff', fontWeight: 600 }}>
                      {w.stepsCount} steps
                    </span>
                  </td>
                  <td>
                    {getStatusBadge(w.status)}
                  </td>
                  <td style={{ textAlign: 'right' }}>
                    <button
                      onClick={() => handleOpenDetail(w.id)}
                      className="btn btn-secondary"
                      style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                    >
                      <Eye size={13} /> Inspect Trace
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

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

      {/* Execution Trace Modal */}
      {selectedWorkflow && (
        <div className="modal-backdrop">
          <div className="modal-content" style={{ maxWidth: 720, maxHeight: '85vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1.25rem', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '0.75rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
                <Cpu size={22} color="var(--primary)" />
                <div>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: '#ffffff' }}>
                    AI Workflow Trace: {selectedWorkflow.issueTitle}
                  </h3>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    ID: {selectedWorkflow.id} • Model: {selectedWorkflow.modelIdentifier}
                  </div>
                </div>
              </div>
              <button
                onClick={() => setSelectedWorkflow(null)}
                style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
              >
                <X size={20} />
              </button>
            </div>

            {/* Diagnosis & Recommendations */}
            <div className="glass-card" style={{ padding: '1rem', marginBottom: '1.25rem' }}>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600 }}>DIAGNOSTIC SUMMARY</div>
              <p style={{ color: '#ffffff', fontSize: '0.9rem', marginTop: '0.25rem', lineHeight: 1.5 }}>
                {selectedWorkflow.diagnosisSummary || 'No summary provided.'}
              </p>
              <div style={{ fontSize: '0.8rem', color: 'var(--accent-cyan)', fontWeight: 600, marginTop: '0.75rem' }}>RECOMMENDED ACTION</div>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginTop: '0.2rem' }}>
                {selectedWorkflow.recommendedAction || 'No recommendation.'}
              </p>
            </div>

            {/* Steps Timeline */}
            <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: '#ffffff', marginBottom: '0.75rem' }}>
              Execution Steps ({selectedWorkflow.steps?.length || 0})
            </h4>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginBottom: '1.5rem' }}>
              {(selectedWorkflow.steps || []).map((step) => (
                <div
                  key={step.id}
                  style={{
                    background: 'rgba(255,255,255,0.02)',
                    border: '1px solid var(--border-subtle)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '0.85rem 1rem',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.35rem' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <span className="badge badge-info" style={{ fontSize: '0.65rem' }}>Step {step.stepOrder}</span>
                      <strong style={{ color: '#ffffff', fontSize: '0.9rem' }}>{step.stepName}</strong>
                    </div>
                    <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                      {step.executionDurationMs}ms
                    </span>
                  </div>
                  <p style={{ color: 'var(--text-secondary)', fontSize: '0.825rem', marginBottom: '0.5rem' }}>
                    {step.summary}
                  </p>

                  {/* Tool Executions */}
                  {(step.toolExecutions || []).length > 0 && (
                    <div style={{ background: 'rgba(0,0,0,0.3)', padding: '0.5rem 0.75rem', borderRadius: '4px', fontSize: '0.75rem' }}>
                      <div style={{ color: 'var(--accent-amber)', fontWeight: 600, marginBottom: '0.25rem', display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                        <Terminal size={12} /> Tools Invoked:
                      </div>
                      {step.toolExecutions.map((t) => (
                        <div key={t.id} style={{ fontFamily: 'monospace', color: 'var(--text-secondary)', marginBottom: '0.2rem' }}>
                          • {t.toolName} ({t.executionTimeMs}ms) - {t.isSuccess ? 'Success' : 'Failed'}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>

            {/* Validation Rules */}
            {(selectedWorkflow.validationResults || []).length > 0 && (
              <div>
                <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: '#ffffff', marginBottom: '0.5rem' }}>
                  Safety & Business Rule Validations
                </h4>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem' }}>
                  {selectedWorkflow.validationResults.map((vr) => (
                    <div key={vr.id} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.8rem', color: vr.passed ? 'var(--accent-emerald)' : 'var(--accent-rose)' }}>
                      {vr.passed ? <CheckCircle2 size={14} /> : <AlertTriangle size={14} />}
                      <span>{vr.ruleName}: {vr.validationMessage || (vr.passed ? 'Verified' : 'Flagged')}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default AiWorkflowsPage;
