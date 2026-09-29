import React, { useState, useEffect } from 'react';
import { membershipApi } from '../services/membershipApi';
import PageHeader from '../components/common/PageHeader';
import GoalModal from '../components/membership/GoalModal';
import ProgressModal from '../components/membership/ProgressModal';
import ConfirmationModal from '../components/common/ConfirmationModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Target, Plus, CheckCircle, TrendingUp, Calendar } from 'lucide-react';

export const GoalsPage = () => {
  const [goals, setGoals] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('');

  // Modals
  const [isGoalModalOpen, setIsGoalModalOpen] = useState(false);
  const [selectedGoalForProgress, setSelectedGoalForProgress] = useState(null);
  const [completeTargetId, setCompleteTargetId] = useState(null);
  const [isCompleting, setIsCompleting] = useState(false);

  const fetchGoals = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await membershipApi.getGoals({
        status: statusFilter || undefined,
      });
      setGoals(data.items || data || []);
    } catch (err) {
      console.error('Failed to load goals:', err);
      setError('Unable to load fitness goals.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGoals();
  }, [statusFilter]);

  const handleSaveGoal = async (goalData) => {
    await membershipApi.createGoal(goalData);
    setIsGoalModalOpen(false);
    fetchGoals();
  };

  const handleSaveProgress = async (recordData) => {
    await membershipApi.recordProgress(recordData);
    setSelectedGoalForProgress(null);
    fetchGoals();
  };

  const handleCompleteConfirm = async () => {
    if (!completeTargetId) return;
    setIsCompleting(true);
    try {
      await membershipApi.completeGoal(completeTargetId);
      setCompleteTargetId(null);
      fetchGoals();
    } catch (err) {
      console.error('Failed to complete goal:', err);
      setError('Failed to mark goal as completed.');
    } finally {
      setIsCompleting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Fitness Goals & Progress"
        subtitle="Track personal athletic milestones, metric targets, and workout records"
        icon={Target}
        badge={`${goals.length} Goals`}
        actions={
          <button
            onClick={() => setIsGoalModalOpen(true)}
            className="btn btn-primary"
          >
            <Plus size={16} /> New Goal
          </button>
        }
      />

      {/* Filter Tabs */}
      <div className="glass-card" style={{ padding: '0.75rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600, marginRight: '0.5rem' }}>Status:</span>
        {['', 'InProgress', 'Completed', 'Cancelled'].map((status) => (
          <button
            key={status}
            onClick={() => setStatusFilter(status)}
            className={`btn ${statusFilter === status ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.4rem 0.85rem', fontSize: '0.8rem' }}
          >
            {status || 'All Goals'}
          </button>
        ))}
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading fitness goals..." />
      ) : goals.length === 0 ? (
        <EmptyState
          icon={Target}
          title="No fitness goals found"
          description="Create your first fitness milestone to begin logging progress."
          action={
            <button onClick={() => setIsGoalModalOpen(true)} className="btn btn-primary">
              <Plus size={16} /> Set Goal
            </button>
          }
        />
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1.5rem' }}>
          {goals.map((g) => {
            const current = g.currentValue || 0;
            const target = g.targetValue || 1;
            const pct = Math.min(100, Math.max(0, Math.round((current / target) * 100)));
            const isDone = g.status === 'Completed';

            return (
              <div
                key={g.id}
                className="glass-card"
                style={{
                  padding: '1.5rem',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'space-between',
                  border: isDone ? '1px solid rgba(16,185,129,0.3)' : '1px solid var(--border-subtle)',
                }}
              >
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
                    <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: '#ffffff' }}>
                      {g.title}
                    </h3>
                    <span className={`badge ${isDone ? 'badge-success' : 'badge-info'}`}>
                      {g.status}
                    </span>
                  </div>

                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '1.25rem' }}>
                    Member ID: {g.memberId?.substring(0, 8)}...
                  </div>

                  {/* Progress Metric */}
                  <div style={{ marginBottom: '1.25rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: '0.4rem' }}>
                      <span style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>Current Progress:</span>
                      <div style={{ fontSize: '1.1rem', fontWeight: 800, color: isDone ? 'var(--accent-emerald)' : 'var(--primary)' }}>
                        {current} <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 500 }}>/ {target} {g.unit}</span>
                      </div>
                    </div>

                    <div style={{ width: '100%', height: 8, background: 'rgba(255,255,255,0.08)', borderRadius: 'var(--radius-full)', overflow: 'hidden' }}>
                      <div
                        style={{
                          width: `${pct}%`,
                          height: '100%',
                          background: isDone
                            ? 'linear-gradient(90deg, var(--accent-emerald), #34d399)'
                            : 'linear-gradient(90deg, var(--primary), var(--accent-cyan))',
                          borderRadius: 'var(--radius-full)',
                          transition: 'width 0.4s ease',
                        }}
                      />
                    </div>
                    <div style={{ textAlign: 'right', fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
                      {pct}% Achieved
                    </div>
                  </div>

                  {g.targetDate && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                      <Calendar size={13} /> Target Date: {new Date(g.targetDate).toLocaleDateString()}
                    </div>
                  )}
                </div>

                <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginTop: '1.5rem', borderTop: '1px solid var(--border-subtle)', paddingTop: '1rem' }}>
                  {!isDone && (
                    <>
                      <button
                        onClick={() => setSelectedGoalForProgress(g)}
                        className="btn btn-secondary"
                        style={{ padding: '0.35rem 0.65rem', fontSize: '0.8rem' }}
                      >
                        <TrendingUp size={14} /> Log Progress
                      </button>
                      <button
                        onClick={() => setCompleteTargetId(g.id)}
                        className="btn btn-primary"
                        style={{ padding: '0.35rem 0.65rem', fontSize: '0.8rem', background: 'var(--accent-emerald)' }}
                      >
                        <CheckCircle size={14} /> Complete
                      </button>
                    </>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {isGoalModalOpen && (
        <GoalModal
          onClose={() => setIsGoalModalOpen(false)}
          onSave={handleSaveGoal}
        />
      )}

      {selectedGoalForProgress && (
        <ProgressModal
          goal={selectedGoalForProgress}
          onClose={() => setSelectedGoalForProgress(null)}
          onSave={handleSaveProgress}
        />
      )}

      <ConfirmationModal
        isOpen={!!completeTargetId}
        title="Complete Goal"
        message="Congratulations! Mark this athletic fitness goal as 100% completed?"
        confirmText="Mark Completed"
        isDestructive={false}
        isLoading={isCompleting}
        onConfirm={handleCompleteConfirm}
        onCancel={() => setCompleteTargetId(null)}
      />
    </div>
  );
};

export default GoalsPage;
