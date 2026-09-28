import React, { useState, useEffect, useCallback } from 'react';
import {
  Award,
  Users,
  CreditCard,
  Target,
  BarChart3,
  Search,
  Filter,
  Plus,
  RefreshCw,
  Calendar,
  CheckCircle,
  XCircle,
  Clock,
  TrendingUp,
  AlertCircle,
  ChevronLeft,
  ChevronRight,
  Edit,
  Trash2,
  CheckCircle2,
  Ban,
  Activity,
  DollarSign
} from 'lucide-react';
import membershipApi from '../services/membershipApi';
import PlanModal from '../components/membership/PlanModal';
import RenewalModal from '../components/membership/RenewalModal';
import GoalModal from '../components/membership/GoalModal';
import ProgressModal from '../components/membership/ProgressModal';

export const MembershipsPage = () => {
  // Tabs: 'plans' | 'memberships' | 'members' | 'goals' | 'analytics'
  const [activeTab, setActiveTab] = useState('plans');

  // Search & Filter State
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);

  // Data States
  const [plans, setPlans] = useState([]);
  const [memberships, setMemberships] = useState({ items: [], totalCount: 0, totalPages: 1 });
  const [members, setMembers] = useState({ items: [], totalCount: 0, totalPages: 1 });
  const [goals, setGoals] = useState({ items: [], totalCount: 0, totalPages: 1 });
  const [analytics, setAnalytics] = useState(null);

  // Modals & Popups
  const [selectedPlan, setSelectedPlan] = useState(null);
  const [isPlanModalOpen, setIsPlanModalOpen] = useState(false);

  const [selectedRenewalMember, setSelectedRenewalMember] = useState(null);
  const [selectedRenewalMembership, setSelectedRenewalMembership] = useState(null);
  const [isRenewalModalOpen, setIsRenewalModalOpen] = useState(false);

  const [selectedGoal, setSelectedGoal] = useState(null);
  const [isGoalModalOpen, setIsGoalModalOpen] = useState(false);

  const [selectedProgressGoal, setSelectedProgressGoal] = useState(null);
  const [isProgressModalOpen, setIsProgressModalOpen] = useState(false);

  const [historyMember, setHistoryMember] = useState(null);
  const [memberHistory, setMemberHistory] = useState([]);
  const [isHistoryModalOpen, setIsHistoryModalOpen] = useState(false);

  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  // Fetch Plans
  const fetchPlans = useCallback(async () => {
    try {
      setLoading(true);
      const data = await membershipApi.getPlans(true);
      setPlans(data || []);
    } catch (err) {
      console.error(err);
      setErrorMsg('Failed to load membership plans.');
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch Memberships
  const fetchMemberships = useCallback(async () => {
    try {
      setLoading(true);
      const params = {
        page,
        pageSize,
        ...(statusFilter && { status: statusFilter }),
      };
      const data = await membershipApi.getMemberships(params);
      setMemberships(data || { items: [], totalCount: 0, totalPages: 1 });
    } catch (err) {
      console.error(err);
      setErrorMsg('Failed to load memberships.');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, statusFilter]);

  // Fetch Members
  const fetchMembers = useCallback(async () => {
    try {
      setLoading(true);
      const params = {
        page,
        pageSize,
        ...(searchTerm && { search: searchTerm }),
      };
      const data = await membershipApi.getMembers(params);
      setMembers(data || { items: [], totalCount: 0, totalPages: 1 });
    } catch (err) {
      console.error(err);
      setErrorMsg('Failed to load members.');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, searchTerm]);

  // Fetch Goals
  const fetchGoals = useCallback(async () => {
    try {
      setLoading(true);
      const params = {
        page,
        pageSize,
        ...(statusFilter && { status: statusFilter }),
      };
      const data = await membershipApi.getGoals(params);
      setGoals(data || { items: [], totalCount: 0, totalPages: 1 });
    } catch (err) {
      console.error(err);
      setErrorMsg('Failed to load fitness goals.');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, statusFilter]);

  // Fetch Analytics
  const fetchAnalytics = useCallback(async () => {
    try {
      setLoading(true);
      const data = await membershipApi.getAnalytics();
      setAnalytics(data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, []);

  // Load appropriate data when tab or filters change
  useEffect(() => {
    setErrorMsg('');
    setSuccessMsg('');
    if (activeTab === 'plans') {
      fetchPlans();
    } else if (activeTab === 'memberships') {
      fetchMemberships();
      fetchPlans();
    } else if (activeTab === 'members') {
      fetchMembers();
      fetchPlans();
    } else if (activeTab === 'goals') {
      fetchGoals();
      fetchMembers();
    } else if (activeTab === 'analytics') {
      fetchAnalytics();
    }
  }, [activeTab, fetchPlans, fetchMemberships, fetchMembers, fetchGoals, fetchAnalytics]);

  const handleDeletePlan = async (id, name) => {
    if (!window.confirm(`Are you sure you want to deactivate plan "${name}"?`)) return;
    try {
      await membershipApi.deletePlan(id);
      setSuccessMsg(`Plan "${name}" deactivated successfully.`);
      fetchPlans();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to delete plan.');
    }
  };

  const handleCancelMembership = async (id) => {
    const reason = window.prompt('Please enter the reason for membership cancellation:');
    if (reason === null) return;
    try {
      await membershipApi.cancelMembership(id, reason.trim() || 'Cancelled by staff');
      setSuccessMsg('Membership cancelled successfully.');
      fetchMemberships();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to cancel membership.');
    }
  };

  const handleCompleteGoal = async (id, title) => {
    if (!window.confirm(`Mark goal "${title}" as completed?`)) return;
    try {
      await membershipApi.completeGoal(id);
      setSuccessMsg(`Goal "${title}" marked as achieved!`);
      fetchGoals();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to complete goal.');
    }
  };

  const handleDeleteGoal = async (id, title) => {
    if (!window.confirm(`Delete goal "${title}"?`)) return;
    try {
      await membershipApi.deleteGoal(id);
      setSuccessMsg(`Goal "${title}" deleted.`);
      fetchGoals();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to delete goal.');
    }
  };

  const handleViewHistory = async (member) => {
    try {
      setLoading(true);
      const history = await membershipApi.getMembershipHistory(member.id || member.memberId);
      setMemberHistory(history || []);
      setHistoryMember(member);
      setIsHistoryModalOpen(true);
    } catch (err) {
      setErrorMsg('Failed to load membership history.');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    // 0 = Active, 1 = Expired, 2 = Cancelled, 3 = Pending
    const s = String(status).toLowerCase();
    if (s === 'active' || s === '0') {
      return (
        <span className="badge badge-success" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
          <CheckCircle size={12} /> Active
        </span>
      );
    }
    if (s === 'expired' || s === '1') {
      return (
        <span className="badge badge-warning" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
          <Clock size={12} /> Expired
        </span>
      );
    }
    if (s === 'cancelled' || s === '2') {
      return (
        <span className="badge badge-danger" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
          <Ban size={12} /> Cancelled
        </span>
      );
    }
    return <span className="badge badge-secondary">{status}</span>;
  };

  const getGoalStatusBadge = (status) => {
    // 0 = InProgress, 1 = Achieved, 2 = Abandoned
    const s = String(status).toLowerCase();
    if (s === 'achieved' || s === '1') {
      return (
        <span className="badge badge-success" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
          <CheckCircle2 size={12} /> Achieved
        </span>
      );
    }
    if (s === 'abandoned' || s === '2') {
      return (
        <span className="badge badge-danger" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
          <XCircle size={12} /> Abandoned
        </span>
      );
    }
    return (
      <span className="badge badge-info" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
        <Activity size={12} /> In Progress
      </span>
    );
  };

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, margin: 0 }}>Membership & Goal Tracking</h1>
          <p style={{ color: 'var(--text-secondary)', margin: '0.25rem 0 0 0' }}>
            Component 4 — Manage plans, member subscriptions, fitness milestones & measurements.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          {activeTab === 'plans' && (
            <button
              className="btn-primary"
              onClick={() => {
                setSelectedPlan(null);
                setIsPlanModalOpen(true);
              }}
              style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
            >
              <Plus size={18} /> Add Plan
            </button>
          )}

          {activeTab === 'memberships' && (
            <button
              className="btn-primary"
              onClick={() => {
                setSelectedRenewalMember(null);
                setSelectedRenewalMembership(null);
                setIsRenewalModalOpen(true);
              }}
              style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
            >
              <Plus size={18} /> Renew / Assign
            </button>
          )}

          {activeTab === 'goals' && (
            <button
              className="btn-primary"
              onClick={() => {
                setSelectedGoal(null);
                setIsGoalModalOpen(true);
              }}
              style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
            >
              <Plus size={18} /> New Goal
            </button>
          )}

          <button
            className="btn-secondary"
            onClick={() => {
              if (activeTab === 'plans') fetchPlans();
              if (activeTab === 'memberships') fetchMemberships();
              if (activeTab === 'members') fetchMembers();
              if (activeTab === 'goals') fetchGoals();
              if (activeTab === 'analytics') fetchAnalytics();
            }}
            aria-label="Refresh data"
            title="Refresh"
          >
            <RefreshCw size={18} />
          </button>
        </div>
      </div>

      {/* Notifications */}
      {errorMsg && (
        <div style={{ padding: '0.75rem 1rem', background: 'rgba(239, 68, 68, 0.15)', color: '#ef4444', borderRadius: '8px', marginBottom: '1.25rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <AlertCircle size={18} /> {errorMsg}
        </div>
      )}
      {successMsg && (
        <div style={{ padding: '0.75rem 1rem', background: 'rgba(16, 185, 129, 0.15)', color: '#10b981', borderRadius: '8px', marginBottom: '1.25rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <CheckCircle size={18} /> {successMsg}
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: 'flex', borderBottom: '1px solid var(--border-color)', gap: '1rem', marginBottom: '1.5rem', overflowX: 'auto' }}>
        <button
          className={`tab-btn ${activeTab === 'plans' ? 'active' : ''}`}
          onClick={() => { setActiveTab('plans'); setPage(1); }}
          style={{ padding: '0.75rem 1.25rem', background: 'none', border: 'none', borderBottom: activeTab === 'plans' ? '2px solid var(--primary-color, #6366f1)' : '2px solid transparent', color: activeTab === 'plans' ? 'var(--primary-color, #6366f1)' : 'var(--text-secondary)', fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '0.5rem' }}
        >
          <Award size={18} /> Membership Plans
        </button>

        <button
          className={`tab-btn ${activeTab === 'memberships' ? 'active' : ''}`}
          onClick={() => { setActiveTab('memberships'); setPage(1); }}
          style={{ padding: '0.75rem 1.25rem', background: 'none', border: 'none', borderBottom: activeTab === 'memberships' ? '2px solid var(--primary-color, #6366f1)' : '2px solid transparent', color: activeTab === 'memberships' ? 'var(--primary-color, #6366f1)' : 'var(--text-secondary)', fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '0.5rem' }}
        >
          <CreditCard size={18} /> Subscriptions & Renewals
        </button>

        <button
          className={`tab-btn ${activeTab === 'members' ? 'active' : ''}`}
          onClick={() => { setActiveTab('members'); setPage(1); }}
          style={{ padding: '0.75rem 1.25rem', background: 'none', border: 'none', borderBottom: activeTab === 'members' ? '2px solid var(--primary-color, #6366f1)' : '2px solid transparent', color: activeTab === 'members' ? 'var(--primary-color, #6366f1)' : 'var(--text-secondary)', fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '0.5rem' }}
        >
          <Users size={18} /> Member Directory
        </button>

        <button
          className={`tab-btn ${activeTab === 'goals' ? 'active' : ''}`}
          onClick={() => { setActiveTab('goals'); setPage(1); }}
          style={{ padding: '0.75rem 1.25rem', background: 'none', border: 'none', borderBottom: activeTab === 'goals' ? '2px solid var(--primary-color, #6366f1)' : '2px solid transparent', color: activeTab === 'goals' ? 'var(--primary-color, #6366f1)' : 'var(--text-secondary)', fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '0.5rem' }}
        >
          <Target size={18} /> Fitness Goals & Progress
        </button>

        <button
          className={`tab-btn ${activeTab === 'analytics' ? 'active' : ''}`}
          onClick={() => { setActiveTab('analytics'); }}
          style={{ padding: '0.75rem 1.25rem', background: 'none', border: 'none', borderBottom: activeTab === 'analytics' ? '2px solid var(--primary-color, #6366f1)' : '2px solid transparent', color: activeTab === 'analytics' ? 'var(--primary-color, #6366f1)' : 'var(--text-secondary)', fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '0.5rem' }}
        >
          <BarChart3 size={18} /> Analytics & Reports
        </button>
      </div>

      {/* TAB 1: MEMBERSHIP PLANS */}
      {activeTab === 'plans' && (
        <div>
          {loading ? (
            <div style={{ textAlign: 'center', padding: '3rem' }}>Loading membership plans...</div>
          ) : plans.length === 0 ? (
            <div className="glass-card" style={{ padding: '3rem', textAlign: 'center' }}>
              <Award size={48} style={{ opacity: 0.5, marginBottom: '1rem' }} />
              <h3>No Membership Plans Found</h3>
              <p style={{ color: 'var(--text-secondary)' }}>Get started by creating your first subscription plan.</p>
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1.5rem' }}>
              {plans.map((p) => (
                <div key={p.id} className="glass-card" style={{ padding: '1.75rem', position: 'relative', display: 'flex', flexDirection: 'column', justifyContent: 'space-between', border: p.isActive ? '1px solid var(--border-color)' : '1px dashed rgba(239, 68, 68, 0.4)' }}>
                  <div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
                      <h3 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>{p.name}</h3>
                      <span className={`badge ${p.isActive ? 'badge-success' : 'badge-danger'}`}>
                        {p.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.25rem', marginBottom: '1rem' }}>
                      <span style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--primary-color, #6366f1)' }}>${p.price.toFixed(2)}</span>
                      <span style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>/ {p.durationDays} days</span>
                    </div>

                    <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1.25rem', minHeight: '2.7rem' }}>
                      {p.description || 'Full gym facility access.'}
                    </p>

                    <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '1rem', display: 'flex', flexDirection: 'column', gap: '0.5rem', fontSize: '0.875rem' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>Classes Quota:</span>
                        <strong style={{ color: 'var(--text-primary)' }}>{p.maxClassesPerWeek} / week</strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>Trainer Access:</span>
                        <strong style={{ color: 'var(--text-primary)' }}>{p.hasTrainerAccess ? 'Included' : 'Not Included'}</strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>Active Members:</span>
                        <strong style={{ color: 'var(--text-primary)' }}>{p.activeSubscribersCount || 0} enrolled</strong>
                      </div>
                    </div>
                  </div>

                  <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginTop: '1.5rem', borderTop: '1px solid var(--border-color)', paddingTop: '1rem' }}>
                    <button
                      className="btn-secondary"
                      onClick={() => {
                        setSelectedPlan(p);
                        setIsPlanModalOpen(true);
                      }}
                      style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.85rem' }}
                    >
                      <Edit size={14} /> Edit
                    </button>
                    <button
                      className="btn-secondary"
                      onClick={() => handleDeletePlan(p.id, p.name)}
                      style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.85rem', color: '#ef4444' }}
                    >
                      <Trash2 size={14} /> Deactivate
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* TAB 2: MEMBERSHIPS / SUBSCRIPTIONS */}
      {activeTab === 'memberships' && (
        <div>
          {/* Filters */}
          <div className="glass-card" style={{ padding: '1rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'center' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Filter size={16} color="var(--text-secondary)" />
              <span style={{ fontSize: '0.875rem', fontWeight: 600 }}>Status:</span>
              <select
                className="form-input"
                style={{ width: 'auto' }}
                value={statusFilter}
                onChange={(e) => {
                  setStatusFilter(e.target.value);
                  setPage(1);
                }}
              >
                <option value="">All Statuses</option>
                <option value="Active">Active</option>
                <option value="Expired">Expired</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </div>
          </div>

          {/* Table */}
          <div className="glass-card" style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.875rem' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                  <th style={{ padding: '1rem' }}>Member</th>
                  <th style={{ padding: '1rem' }}>Plan</th>
                  <th style={{ padding: '1rem' }}>Validity Period</th>
                  <th style={{ padding: '1rem' }}>Status</th>
                  <th style={{ padding: '1rem' }}>Paid</th>
                  <th style={{ padding: '1rem' }}>Auto-Renew</th>
                  <th style={{ padding: '1rem', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan="7" style={{ textAlign: 'center', padding: '2rem' }}>Loading subscriptions...</td>
                  </tr>
                ) : memberships.items.length === 0 ? (
                  <tr>
                    <td colSpan="7" style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)' }}>
                      No membership subscriptions found matching criteria.
                    </td>
                  </tr>
                ) : (
                  memberships.items.map((m) => (
                    <tr key={m.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                      <td style={{ padding: '1rem' }}>
                        <div style={{ fontWeight: 600 }}>{m.memberName}</div>
                        <div style={{ color: 'var(--text-secondary)', fontSize: '0.8rem' }}>{m.memberEmail}</div>
                      </td>
                      <td style={{ padding: '1rem', fontWeight: 600 }}>{m.planName}</td>
                      <td style={{ padding: '1rem' }}>
                        <div>{new Date(m.startDate).toLocaleDateString()} – {new Date(m.endDate).toLocaleDateString()}</div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                          {new Date(m.endDate) >= new Date() ? 'Ends' : 'Expired'} {new Date(m.endDate).toLocaleDateString()}
                        </div>
                      </td>
                      <td style={{ padding: '1rem' }}>{getStatusBadge(m.status)}</td>
                      <td style={{ padding: '1rem', fontWeight: 600 }}>${m.pricePaid.toFixed(2)}</td>
                      <td style={{ padding: '1rem' }}>
                        {m.autoRenew ? (
                          <span style={{ color: '#10b981', display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                            <CheckCircle size={14} /> Yes
                          </span>
                        ) : (
                          <span style={{ color: 'var(--text-secondary)' }}>No</span>
                        )}
                      </td>
                      <td style={{ padding: '1rem', textAlign: 'right' }}>
                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
                          <button
                            className="btn-secondary"
                            onClick={() => {
                              setSelectedRenewalMember({ id: m.memberId, fullName: m.memberName, email: m.memberEmail });
                              setSelectedRenewalMembership(m);
                              setIsRenewalModalOpen(true);
                            }}
                            title="Renew Membership"
                            style={{ padding: '0.4rem 0.6rem', fontSize: '0.8rem' }}
                          >
                            <RefreshCw size={14} /> Renew
                          </button>

                          <button
                            className="btn-secondary"
                            onClick={() => handleViewHistory(m)}
                            title="Membership History"
                            style={{ padding: '0.4rem 0.6rem', fontSize: '0.8rem' }}
                          >
                            <Calendar size={14} /> History
                          </button>

                          {String(m.status).toLowerCase() === 'active' || String(m.status) === '0' ? (
                            <button
                              className="btn-secondary"
                              onClick={() => handleCancelMembership(m.id)}
                              title="Cancel Membership"
                              style={{ padding: '0.4rem 0.6rem', fontSize: '0.8rem', color: '#ef4444' }}
                            >
                              <Ban size={14} />
                            </button>
                          ) : null}
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {memberships.totalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.5rem' }}>
              <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Showing page {page} of {memberships.totalPages} ({memberships.totalCount} total)
              </span>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button
                  className="btn-secondary"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}
                >
                  <ChevronLeft size={16} /> Prev
                </button>
                <button
                  className="btn-secondary"
                  disabled={page >= memberships.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}
                >
                  Next <ChevronRight size={16} />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* TAB 3: MEMBER DIRECTORY */}
      {activeTab === 'members' && (
        <div>
          {/* Search bar */}
          <div className="glass-card" style={{ padding: '1rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center' }}>
            <Search size={18} color="var(--text-secondary)" />
            <input
              type="text"
              className="form-input"
              style={{ border: 'none', background: 'transparent', padding: 0 }}
              placeholder="Search members by name, email, or phone..."
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setPage(1);
              }}
            />
          </div>

          {/* Members Table */}
          <div className="glass-card" style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.875rem' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                  <th style={{ padding: '1rem' }}>Member</th>
                  <th style={{ padding: '1rem' }}>Joined</th>
                  <th style={{ padding: '1rem' }}>Current Plan</th>
                  <th style={{ padding: '1rem' }}>Status</th>
                  <th style={{ padding: '1rem' }}>Goals</th>
                  <th style={{ padding: '1rem' }}>Bookings</th>
                  <th style={{ padding: '1rem', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan="7" style={{ textAlign: 'center', padding: '2rem' }}>Loading member directory...</td>
                  </tr>
                ) : members.items.length === 0 ? (
                  <tr>
                    <td colSpan="7" style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)' }}>
                      No members found matching your search.
                    </td>
                  </tr>
                ) : (
                  members.items.map((m) => (
                    <tr key={m.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                      <td style={{ padding: '1rem' }}>
                        <div style={{ fontWeight: 600 }}>{m.fullName || m.userName}</div>
                        <div style={{ color: 'var(--text-secondary)', fontSize: '0.8rem' }}>{m.email}</div>
                      </td>
                      <td style={{ padding: '1rem' }}>
                        {m.joinDate ? new Date(m.joinDate).toLocaleDateString() : '—'}
                      </td>
                      <td style={{ padding: '1rem', fontWeight: 600 }}>
                        {m.currentPlanName || 'No Active Plan'}
                      </td>
                      <td style={{ padding: '1rem' }}>
                        {m.currentMembershipStatus ? getStatusBadge(m.currentMembershipStatus) : <span className="badge badge-secondary">None</span>}
                      </td>
                      <td style={{ padding: '1rem' }}>
                        <span style={{ fontWeight: 600, color: 'var(--primary-color, #6366f1)' }}>{m.activeGoalsCount || 0}</span> active
                      </td>
                      <td style={{ padding: '1rem' }}>
                        <span>{m.totalBookingsCount || 0} classes</span>
                      </td>
                      <td style={{ padding: '1rem', textAlign: 'right' }}>
                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
                          <button
                            className="btn-secondary"
                            onClick={() => {
                              setSelectedRenewalMember(m);
                              setSelectedRenewalMembership(null);
                              setIsRenewalModalOpen(true);
                            }}
                            title="Assign / Renew Plan"
                            style={{ padding: '0.4rem 0.6rem', fontSize: '0.8rem' }}
                          >
                            <RefreshCw size={14} /> Assign Plan
                          </button>
                          <button
                            className="btn-secondary"
                            onClick={() => handleViewHistory(m)}
                            title="Membership History"
                            style={{ padding: '0.4rem 0.6rem', fontSize: '0.8rem' }}
                          >
                            <Calendar size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {members.totalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.5rem' }}>
              <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Showing page {page} of {members.totalPages} ({members.totalCount} members)
              </span>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button
                  className="btn-secondary"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}
                >
                  <ChevronLeft size={16} /> Prev
                </button>
                <button
                  className="btn-secondary"
                  disabled={page >= members.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}
                >
                  Next <ChevronRight size={16} />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* TAB 4: GOALS & PROGRESS */}
      {activeTab === 'goals' && (
        <div>
          {/* Status filter */}
          <div className="glass-card" style={{ padding: '1rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center' }}>
            <Filter size={16} color="var(--text-secondary)" />
            <span style={{ fontSize: '0.875rem', fontWeight: 600 }}>Goal Status:</span>
            <select
              className="form-input"
              style={{ width: 'auto' }}
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value);
                setPage(1);
              }}
            >
              <option value="">All Goals</option>
              <option value="InProgress">In Progress</option>
              <option value="Achieved">Achieved</option>
              <option value="Abandoned">Abandoned</option>
            </select>
          </div>

          {/* Goals Grid */}
          {loading ? (
            <div style={{ textAlign: 'center', padding: '3rem' }}>Loading goals and measurements...</div>
          ) : goals.items.length === 0 ? (
            <div className="glass-card" style={{ padding: '3rem', textAlign: 'center' }}>
              <Target size={48} style={{ opacity: 0.5, marginBottom: '1rem' }} />
              <h3>No Fitness Goals Found</h3>
              <p style={{ color: 'var(--text-secondary)' }}>Members can set fitness milestones like weight goals, marathon prep, or lifting targets.</p>
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))', gap: '1.5rem' }}>
              {goals.items.map((g) => {
                // Calculate percentage towards target
                const percent = Math.min(100, Math.round((g.currentValue / g.targetValue) * 100)) || 0;
                return (
                  <div key={g.id} className="glass-card" style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
                    <div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
                        <div>
                          <h3 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>{g.title}</h3>
                          <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Member: {g.memberName}</div>
                        </div>
                        {getGoalStatusBadge(g.status)}
                      </div>

                      {/* Progress Bar */}
                      <div style={{ margin: '1rem 0' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem', marginBottom: '0.35rem' }}>
                          <span style={{ color: 'var(--text-secondary)' }}>
                            Current: <strong>{g.currentValue} {g.unit}</strong>
                          </span>
                          <span style={{ color: 'var(--text-secondary)' }}>
                            Target: <strong>{g.targetValue} {g.unit}</strong> ({percent}%)
                          </span>
                        </div>
                        <div style={{ height: '8px', background: 'rgba(255, 255, 255, 0.1)', borderRadius: '4px', overflow: 'hidden' }}>
                          <div
                            style={{
                              height: '100%',
                              width: `${percent}%`,
                              background: percent >= 100 ? '#10b981' : 'linear-gradient(90deg, #6366f1, #8b5cf6)',
                              borderRadius: '4px',
                              transition: 'width 0.3s ease',
                            }}
                          />
                        </div>
                      </div>

                      {/* Target Date */}
                      <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
                        <Calendar size={14} />
                        Target Date: <strong>{new Date(g.targetDate).toLocaleDateString()}</strong>
                      </div>

                      {/* Recent Logs Snippet */}
                      {g.recentLogs && g.recentLogs.length > 0 && (
                        <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '0.75rem', marginTop: '0.5rem' }}>
                          <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase', marginBottom: '0.5rem' }}>
                            Recent Logs ({g.totalLogsCount} total)
                          </div>
                          {g.recentLogs.slice(0, 3).map((log) => (
                            <div key={log.id} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.8rem', marginBottom: '0.25rem' }}>
                              <span>{new Date(log.recordedDate).toLocaleDateString()}: <strong>{log.value} {g.unit}</strong></span>
                              <span style={{ color: 'var(--text-secondary)', fontStyle: 'italic' }}>{log.notes || 'No notes'}</span>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>

                    {/* Goal Actions */}
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.25rem', borderTop: '1px solid var(--border-color)', paddingTop: '1rem' }}>
                      <button
                        className="btn-primary"
                        onClick={() => {
                          setSelectedProgressGoal(g);
                          setIsProgressModalOpen(true);
                        }}
                        style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.8rem' }}
                      >
                        <TrendingUp size={14} /> Log Progress
                      </button>

                      <div style={{ display: 'flex', gap: '0.5rem' }}>
                        {String(g.status).toLowerCase() !== 'achieved' && String(g.status) !== '1' && (
                          <button
                            className="btn-secondary"
                            onClick={() => handleCompleteGoal(g.id, g.title)}
                            title="Mark as Achieved"
                            style={{ padding: '0.4rem 0.6rem', fontSize: '0.8rem', color: '#10b981' }}
                          >
                            <CheckCircle2 size={14} />
                          </button>
                        )}
                        <button
                          className="btn-secondary"
                          onClick={() => {
                            setSelectedGoal(g);
                            setIsGoalModalOpen(true);
                          }}
                          title="Edit Goal"
                          style={{ padding: '0.4rem 0.6rem', fontSize: '0.8rem' }}
                        >
                          <Edit size={14} />
                        </button>
                        <button
                          className="btn-secondary"
                          onClick={() => handleDeleteGoal(g.id, g.title)}
                          title="Delete Goal"
                          style={{ padding: '0.4rem 0.6rem', fontSize: '0.8rem', color: '#ef4444' }}
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          {/* Pagination */}
          {goals.totalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.5rem' }}>
              <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Showing page {page} of {goals.totalPages} ({goals.totalCount} goals)
              </span>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button
                  className="btn-secondary"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}
                >
                  <ChevronLeft size={16} /> Prev
                </button>
                <button
                  className="btn-secondary"
                  disabled={page >= goals.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}
                >
                  Next <ChevronRight size={16} />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* TAB 5: ANALYTICS & REPORTS */}
      {activeTab === 'analytics' && (
        <div>
          {analytics ? (
            <div>
              {/* Top Metric Cards */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.25rem', marginBottom: '2rem' }}>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', fontWeight: 600 }}>Total Members</span>
                    <Users size={20} color="var(--primary-color, #6366f1)" />
                  </div>
                  <div style={{ fontSize: '1.85rem', fontWeight: 800 }}>{analytics.totalMembers}</div>
                  <div style={{ fontSize: '0.8rem', color: '#10b981', marginTop: '0.25rem' }}>Registered Profiles</div>
                </div>

                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', fontWeight: 600 }}>Active Subscriptions</span>
                    <CheckCircle size={20} color="#10b981" />
                  </div>
                  <div style={{ fontSize: '1.85rem', fontWeight: 800, color: '#10b981' }}>{analytics.activeMemberships}</div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>Currently Valid</div>
                </div>

                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', fontWeight: 600 }}>Expired / Cancelled</span>
                    <Clock size={20} color="#f59e0b" />
                  </div>
                  <div style={{ fontSize: '1.85rem', fontWeight: 800, color: '#f59e0b' }}>
                    {analytics.expiredMemberships + analytics.cancelledMemberships}
                  </div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
                    {analytics.expiredMemberships} Expired • {analytics.cancelledMemberships} Cancelled
                  </div>
                </div>

                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', fontWeight: 600 }}>Membership Revenue</span>
                    <DollarSign size={20} color="#3b82f6" />
                  </div>
                  <div style={{ fontSize: '1.85rem', fontWeight: 800, color: '#3b82f6' }}>
                    ${analytics.totalRevenue.toFixed(2)}
                  </div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>All-Time Subscriptions</div>
                </div>
              </div>

              {/* Goal Achievement & Breakdown Card */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '1.5rem' }}>
                <div className="glass-card" style={{ padding: '1.75rem' }}>
                  <h3 style={{ fontSize: '1.15rem', fontWeight: 700, marginBottom: '1.25rem' }}>Fitness Goals Performance</h3>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                    <span style={{ color: 'var(--text-secondary)' }}>Total Goals Tracked:</span>
                    <strong style={{ color: 'var(--text-primary)' }}>{analytics.totalGoals}</strong>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                    <span style={{ color: 'var(--text-secondary)' }}>Achieved Goals:</span>
                    <strong style={{ color: '#10b981' }}>{analytics.achievedGoals}</strong>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1.25rem' }}>
                    <span style={{ color: 'var(--text-secondary)' }}>Achievement Success Rate:</span>
                    <strong style={{ color: 'var(--primary-color, #6366f1)' }}>
                      {analytics.totalGoals > 0 ? Math.round((analytics.achievedGoals / analytics.totalGoals) * 100) : 0}%
                    </strong>
                  </div>

                  {/* Visual Bar */}
                  <div style={{ height: '12px', background: 'rgba(255, 255, 255, 0.1)', borderRadius: '6px', overflow: 'hidden' }}>
                    <div
                      style={{
                        height: '100%',
                        width: `${analytics.totalGoals > 0 ? (analytics.achievedGoals / analytics.totalGoals) * 100 : 0}%`,
                        background: 'linear-gradient(90deg, #10b981, #059669)',
                        borderRadius: '6px',
                      }}
                    />
                  </div>
                </div>

                <div className="glass-card" style={{ padding: '1.75rem' }}>
                  <h3 style={{ fontSize: '1.15rem', fontWeight: 700, marginBottom: '1.25rem' }}>Plan Popularity Distribution</h3>
                  {plans.length === 0 ? (
                    <div style={{ color: 'var(--text-secondary)' }}>No plan data available.</div>
                  ) : (
                    plans.map((p) => {
                      const share = analytics.activeMemberships > 0
                        ? Math.round(((p.activeSubscribersCount || 0) / analytics.activeMemberships) * 100)
                        : 0;
                      return (
                        <div key={p.id} style={{ marginBottom: '1rem' }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', marginBottom: '0.25rem' }}>
                            <span>{p.name}</span>
                            <span style={{ fontWeight: 600 }}>{p.activeSubscribersCount || 0} active ({share}%)</span>
                          </div>
                          <div style={{ height: '6px', background: 'rgba(255, 255, 255, 0.1)', borderRadius: '3px', overflow: 'hidden' }}>
                            <div style={{ height: '100%', width: `${share}%`, background: '#6366f1', borderRadius: '3px' }} />
                          </div>
                        </div>
                      );
                    })
                  )}
                </div>
              </div>
            </div>
          ) : (
            <div style={{ textAlign: 'center', padding: '3rem' }}>Loading analytics metrics...</div>
          )}
        </div>
      )}

      {/* History Modal */}
      {isHistoryModalOpen && historyMember && (
        <div className="modal-backdrop" onClick={() => setIsHistoryModalOpen(false)} style={{ zIndex: 1000 }}>
          <div
            className="glass-card modal-content"
            onClick={(e) => e.stopPropagation()}
            style={{ width: '100%', maxWidth: '640px', padding: '1.75rem' }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                <Calendar size={24} color="var(--primary-color, #6366f1)" />
                <div>
                  <h2 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>Membership History</h2>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                    {historyMember.fullName || historyMember.memberName || historyMember.userName || historyMember.email}
                  </div>
                </div>
              </div>
              <button className="btn-icon" onClick={() => setIsHistoryModalOpen(false)}>
                &times;
              </button>
            </div>

            {memberHistory.length === 0 ? (
              <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                No prior membership records preserved for this member.
              </div>
            ) : (
              <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                      <th style={{ padding: '0.75rem' }}>Plan</th>
                      <th style={{ padding: '0.75rem' }}>Dates</th>
                      <th style={{ padding: '0.75rem' }}>Status</th>
                      <th style={{ padding: '0.75rem' }}>Paid</th>
                    </tr>
                  </thead>
                  <tbody>
                    {memberHistory.map((h) => (
                      <tr key={h.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                        <td style={{ padding: '0.75rem', fontWeight: 600 }}>{h.planName}</td>
                        <td style={{ padding: '0.75rem' }}>
                          {new Date(h.startDate).toLocaleDateString()} – {new Date(h.endDate).toLocaleDateString()}
                        </td>
                        <td style={{ padding: '0.75rem' }}>{getStatusBadge(h.status)}</td>
                        <td style={{ padding: '0.75rem', fontWeight: 600 }}>${h.pricePaid.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1.5rem' }}>
              <button className="btn-secondary" onClick={() => setIsHistoryModalOpen(false)}>
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Plan Modal */}
      {isPlanModalOpen && (
        <PlanModal
          plan={selectedPlan}
          onClose={() => setIsPlanModalOpen(false)}
          onSaved={() => {
            setIsPlanModalOpen(false);
            setSuccessMsg('Membership plan saved successfully.');
            fetchPlans();
          }}
        />
      )}

      {/* Renewal Modal */}
      {isRenewalModalOpen && (
        <RenewalModal
          member={selectedRenewalMember}
          currentMembership={selectedRenewalMembership}
          plans={plans}
          onClose={() => setIsRenewalModalOpen(false)}
          onRenewed={() => {
            setIsRenewalModalOpen(false);
            setSuccessMsg('Membership renewed successfully! Validity extended.');
            if (activeTab === 'memberships') fetchMemberships();
            if (activeTab === 'members') fetchMembers();
          }}
          isStaff={true}
        />
      )}

      {/* Goal Modal */}
      {isGoalModalOpen && (
        <GoalModal
          goal={selectedGoal}
          members={members.items}
          onClose={() => setIsGoalModalOpen(false)}
          onSaved={() => {
            setIsGoalModalOpen(false);
            setSuccessMsg('Goal saved successfully.');
            fetchGoals();
          }}
          isStaff={true}
        />
      )}

      {/* Progress Modal */}
      {isProgressModalOpen && selectedProgressGoal && (
        <ProgressModal
          goal={selectedProgressGoal}
          onClose={() => setIsProgressModalOpen(false)}
          onLogged={() => {
            setIsProgressModalOpen(false);
            setSuccessMsg('Progress record logged successfully.');
            fetchGoals();
          }}
        />
      )}
    </div>
  );
};

export default MembershipsPage;
