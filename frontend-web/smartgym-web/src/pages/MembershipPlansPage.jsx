import React, { useState, useEffect } from 'react';
import { membershipApi } from '../services/membershipApi';
import PageHeader from '../components/common/PageHeader';
import PlanModal from '../components/membership/PlanModal';
import ConfirmationModal from '../components/common/ConfirmationModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { ShieldCheck, Plus, Edit2, Trash2, CheckCircle, XCircle } from 'lucide-react';

export const MembershipPlansPage = () => {
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedPlan, setSelectedPlan] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deleteTargetId, setDeleteTargetId] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const fetchPlans = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await membershipApi.getPlans(true);
      setPlans(data || []);
    } catch (err) {
      console.error('Failed to load membership plans:', err);
      setError('Unable to load membership plans.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPlans();
  }, []);

  const handleSavePlan = async (planData) => {
    if (selectedPlan) {
      await membershipApi.updatePlan(selectedPlan.id, planData);
    } else {
      await membershipApi.createPlan(planData);
    }
    setIsModalOpen(false);
    setSelectedPlan(null);
    fetchPlans();
  };

  const handleDeleteConfirm = async () => {
    if (!deleteTargetId) return;
    setIsDeleting(true);
    try {
      await membershipApi.deletePlan(deleteTargetId);
      setDeleteTargetId(null);
      fetchPlans();
    } catch (err) {
      console.error('Failed to delete plan:', err);
      setError(err.response?.data?.detail || 'Failed to delete plan. It may have active memberships attached.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Membership Plans"
        subtitle="Configure tier tiers, durations, pricing, and trainer privileges"
        icon={ShieldCheck}
        badge={`${plans.length} Tiers`}
        actions={
          <button
            onClick={() => {
              setSelectedPlan(null);
              setIsModalOpen(true);
            }}
            className="btn btn-primary"
          >
            <Plus size={16} /> New Plan
          </button>
        }
      />

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading membership plans..." />
      ) : plans.length === 0 ? (
        <EmptyState
          icon={ShieldCheck}
          title="No membership plans found"
          description="Create your first membership tier to start registering gym members."
          action={
            <button
              onClick={() => {
                setSelectedPlan(null);
                setIsModalOpen(true);
              }}
              className="btn btn-primary"
            >
              <Plus size={16} /> Create Plan
            </button>
          }
        />
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1.5rem' }}>
          {plans.map((p) => (
            <div
              key={p.id}
              className="glass-card"
              style={{
                padding: '1.75rem',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'space-between',
                border: p.isActive ? '1px solid var(--border-subtle)' : '1px solid rgba(244,63,94,0.3)',
                position: 'relative',
              }}
            >
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
                  <h3 style={{ fontSize: '1.25rem', fontWeight: 800, color: '#ffffff' }}>
                    {p.name}
                  </h3>
                  <span className={`badge ${p.isActive ? 'badge-success' : 'badge-danger'}`}>
                    {p.isActive ? 'Active' : 'Archived'}
                  </span>
                </div>

                <div style={{ fontSize: '1.75rem', fontWeight: 800, color: 'var(--primary)', marginBottom: '0.5rem' }}>
                  ${p.price.toFixed(2)}
                  <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 500 }}>
                    {' '}/ {p.durationDays} days
                  </span>
                </div>

                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: '1.25rem', minHeight: 40 }}>
                  {p.description || 'Full gym facility access according to plan conditions.'}
                </p>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', fontSize: '0.825rem', borderTop: '1px solid var(--border-subtle)', paddingTop: '1rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--text-secondary)' }}>
                    <CheckCircle size={15} color="var(--accent-emerald)" />
                    Max classes: <strong style={{ color: '#ffffff' }}>{p.maxClassesPerWeek > 0 ? `${p.maxClassesPerWeek}/week` : 'Unlimited'}</strong>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--text-secondary)' }}>
                    {p.hasTrainerAccess ? (
                      <>
                        <CheckCircle size={15} color="var(--accent-emerald)" />
                        <span>Dedicated Trainer Consultation included</span>
                      </>
                    ) : (
                      <>
                        <XCircle size={15} color="var(--text-muted)" />
                        <span style={{ color: 'var(--text-muted)' }}>No Trainer Access</span>
                      </>
                    )}
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginTop: '1.5rem', borderTop: '1px solid var(--border-subtle)', paddingTop: '1rem' }}>
                <button
                  onClick={() => {
                    setSelectedPlan(p);
                    setIsModalOpen(true);
                  }}
                  className="btn btn-secondary"
                  style={{ padding: '0.4rem 0.75rem', fontSize: '0.8rem' }}
                >
                  <Edit2 size={14} /> Edit
                </button>
                <button
                  onClick={() => setDeleteTargetId(p.id)}
                  className="btn btn-danger"
                  style={{ padding: '0.4rem 0.75rem', fontSize: '0.8rem' }}
                >
                  <Trash2 size={14} /> Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {isModalOpen && (
        <PlanModal
          plan={selectedPlan}
          onClose={() => {
            setIsModalOpen(false);
            setSelectedPlan(null);
          }}
          onSave={handleSavePlan}
        />
      )}

      <ConfirmationModal
        isOpen={!!deleteTargetId}
        title="Delete Membership Plan"
        message="Are you sure you want to permanently delete this plan? This action cannot be undone if members are enrolled."
        confirmText="Delete Plan"
        isDestructive={true}
        isLoading={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTargetId(null)}
      />
    </div>
  );
};

export default MembershipPlansPage;
