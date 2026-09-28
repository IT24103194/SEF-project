import React, { useEffect, useState, useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { fetchSystemInfo } from '../store/systemSlice';
import {
  Package,
  Wrench,
  Calendar,
  Users,
  Server,
  Cpu,
  ArrowUpRight,
  TrendingUp,
  AlertTriangle,
  CheckCircle,
  Clock,
  DollarSign,
  Activity,
  BarChart3,
  RefreshCw,
  ShieldCheck
} from 'lucide-react';
import { Link } from 'react-router-dom';
import reportsApi from '../services/reportsApi';

export const DashboardPage = () => {
  const dispatch = useDispatch();
  const { info, status } = useSelector((state) => state.system);

  const [dashboardData, setDashboardData] = useState(null);
  const [loadingReports, setLoadingReports] = useState(false);
  const [reportError, setReportError] = useState('');

  const loadReports = useCallback(async () => {
    try {
      setLoadingReports(true);
      setReportError('');
      const data = await reportsApi.getExecutiveDashboard();
      setDashboardData(data);
    } catch (err) {
      console.error('Failed to load executive dashboard reports', err);
      setReportError('Unable to load live analytics report.');
    } finally {
      setLoadingReports(false);
    }
  }, []);

  useEffect(() => {
    dispatch(fetchSystemInfo());
    loadReports();
  }, [dispatch, loadReports]);

  const cards = [
    {
      title: 'Supplier & Supplement Inventory',
      desc: 'Stock tracking, threshold alarms, CSV import/export',
      icon: Package,
      path: '/inventory',
      color: 'var(--accent-cyan)'
    },
    {
      title: 'Facility Resolution & AI',
      desc: 'LangGraph multi-agent triage, HITL financial gate (Rs. 25,000)',
      icon: Wrench,
      path: '/facility',
      color: 'var(--primary)'
    },
    {
      title: 'Class Scheduling & Booking',
      desc: 'Timetable management, capacity limits, trainer assignment',
      icon: Calendar,
      path: '/classes',
      color: 'var(--accent-emerald)'
    },
    {
      title: 'Membership & Goal Tracking',
      desc: 'Tiered subscriptions, progress metrics, member analytics',
      icon: Users,
      path: '/memberships',
      color: 'var(--accent-amber)'
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, letterSpacing: '-0.025em', margin: 0 }}>
            System Dashboard & Executive Cockpit
          </h1>
          <p style={{ color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
            Real-time analytics and PostgreSQL reporting data for the SmartGym ecosystem.
          </p>
        </div>

        <button
          className="btn-secondary"
          onClick={loadReports}
          disabled={loadingReports}
          style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem' }}
        >
          <RefreshCw size={16} className={loadingReports ? 'animate-spin' : ''} />
          {loadingReports ? 'Refreshing...' : 'Refresh Reports'}
        </button>
      </div>

      {/* Live PostgreSQL KPI Cards */}
      {dashboardData && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.25rem', marginBottom: '2rem' }}>
          {/* Active Members */}
          <div className="glass-card" style={{ padding: '1.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
              <span style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', fontWeight: 600 }}>Active Members</span>
              <Users size={20} color="var(--primary-color, #6366f1)" />
            </div>
            <div style={{ fontSize: '1.85rem', fontWeight: 800 }}>{dashboardData.membership.activeMembers}</div>
            <div style={{ fontSize: '0.8rem', color: '#10b981', marginTop: '0.25rem' }}>
              {dashboardData.membership.totalMembers} Total Registered
            </div>
          </div>

          {/* Membership Revenue */}
          <div className="glass-card" style={{ padding: '1.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
              <span style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', fontWeight: 600 }}>Total Revenue</span>
              <DollarSign size={20} color="#10b981" />
            </div>
            <div style={{ fontSize: '1.85rem', fontWeight: 800, color: '#10b981' }}>
              ${dashboardData.membership.totalRevenue.toFixed(2)}
            </div>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
              {dashboardData.membership.expiringSoonCount} Expiring in 14d
            </div>
          </div>

          {/* Classes Utilization */}
          <div className="glass-card" style={{ padding: '1.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
              <span style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', fontWeight: 600 }}>Class Attendance</span>
              <Calendar size={20} color="#06b6d4" />
            </div>
            <div style={{ fontSize: '1.85rem', fontWeight: 800, color: '#06b6d4' }}>
              {dashboardData.classes.attendanceRatePercentage}%
            </div>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
              {dashboardData.classes.totalBookings} Total Bookings
            </div>
          </div>

          {/* Low Stock Items Alert */}
          <div className="glass-card" style={{ padding: '1.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
              <span style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', fontWeight: 600 }}>Inventory Health</span>
              <Package size={20} color={dashboardData.inventory.lowStockItemsCount > 0 ? '#f59e0b' : '#10b981'} />
            </div>
            <div style={{ fontSize: '1.85rem', fontWeight: 800, color: dashboardData.inventory.lowStockItemsCount > 0 ? '#f59e0b' : '#10b981' }}>
              {dashboardData.inventory.lowStockItemsCount}
            </div>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
              Low Stock Alerts • {dashboardData.inventory.totalProducts} Products
            </div>
          </div>
        </div>
      )}

      {/* Cross-Cutting Module Reports Breakdown */}
      {dashboardData && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(360px, 1fr))', gap: '1.5rem', marginBottom: '2rem' }}>
          {/* Facility Issues Summary */}
          <div className="glass-card" style={{ padding: '1.75rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>Facility & Repairs Status</h3>
              <Wrench size={18} color="var(--primary-color, #6366f1)" />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '0.75rem', marginBottom: '1.25rem', textAlign: 'center' }}>
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.75rem', borderRadius: '8px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>OPEN</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#f59e0b' }}>{dashboardData.facility.openIssues}</div>
              </div>
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.75rem', borderRadius: '8px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>IN REPAIR</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#06b6d4' }}>{dashboardData.facility.inProgressIssues}</div>
              </div>
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.75rem', borderRadius: '8px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>RESOLVED</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#10b981' }}>{dashboardData.facility.resolvedIssues}</div>
              </div>
            </div>

            <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '0.75rem', fontSize: '0.85rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.35rem' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Avg Resolution Time:</span>
                <strong>{dashboardData.facility.averageResolutionHours} hours</strong>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Total Repair Invoices:</span>
                <strong>${dashboardData.facility.totalRepairCosts.toFixed(2)}</strong>
              </div>
            </div>
          </div>

          {/* AI Workflow & Automation Summary */}
          <div className="glass-card" style={{ padding: '1.75rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>AI Agentic Workflows</h3>
              <Cpu size={18} color="var(--accent-cyan)" />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '0.75rem', marginBottom: '1.25rem', textAlign: 'center' }}>
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.75rem', borderRadius: '8px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>TOTAL RUNS</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800 }}>{dashboardData.ai.totalWorkflows}</div>
              </div>
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.75rem', borderRadius: '8px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>APPROVALS</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#10b981' }}>{dashboardData.ai.approvedCount}</div>
              </div>
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.75rem', borderRadius: '8px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>PENDING</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#f59e0b' }}>{dashboardData.ai.pendingApprovalsCount}</div>
              </div>
            </div>

            <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '0.75rem', fontSize: '0.85rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.35rem' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Failures Guarded:</span>
                <span className="badge badge-success">{dashboardData.ai.safeFailuresCount} Safe</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Avg Execution Speed:</span>
                <strong>{dashboardData.ai.averageWorkflowDurationSeconds}s</strong>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Backend & Diagnostics */}
      <div className="glass-card" style={{ padding: '1.5rem', marginBottom: '2rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <Server size={20} color="var(--primary)" />
            <h2 style={{ fontSize: '1.1rem', fontWeight: 700, margin: 0 }}>Authoritative Backend Status</h2>
          </div>
          <div>
            {status === 'loading' && <span className="badge badge-warning">Connecting...</span>}
            {status === 'succeeded' && <span className="badge badge-success">Online & Healthy</span>}
            {status === 'failed' && <span className="badge badge-danger">Connection Offline</span>}
          </div>
        </div>

        {status === 'succeeded' && info && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginTop: '1rem' }}>
            <div style={{ background: 'rgba(255,255,255,0.03)', padding: '1rem', borderRadius: 'var(--radius-sm)' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>SERVICE</div>
              <div style={{ fontWeight: 600 }}>{info.application}</div>
            </div>
            <div style={{ background: 'rgba(255,255,255,0.03)', padding: '1rem', borderRadius: 'var(--radius-sm)' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>ENVIRONMENT</div>
              <div style={{ fontWeight: 600 }}>{info.environment}</div>
            </div>
            <div style={{ background: 'rgba(255,255,255,0.03)', padding: '1rem', borderRadius: 'var(--radius-sm)' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>ACTIVE ROLES</div>
              <div style={{ fontWeight: 600 }}>{info.roles?.join(', ')}</div>
            </div>
            <div style={{ background: 'rgba(255,255,255,0.03)', padding: '1rem', borderRadius: 'var(--radius-sm)' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>AI SERVICE GATEWAY</div>
              <div style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                <Cpu size={14} color="var(--accent-cyan)" /> LangGraph Active
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Module Navigation Grid */}
      <h2 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '1rem' }}>
        Business Modules & Workspaces
      </h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))', gap: '1.25rem' }}>
        {cards.map((c) => {
          const Icon = c.icon;
          return (
            <Link
              key={c.title}
              to={c.path}
              className="glass-card"
              style={{
                padding: '1.5rem',
                textDecoration: 'none',
                color: 'inherit',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'space-between',
                transition: 'all 0.2s ease',
              }}
            >
              <div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem' }}>
                  <div
                    style={{
                      width: 42,
                      height: 42,
                      borderRadius: 'var(--radius-sm)',
                      background: 'rgba(255, 255, 255, 0.05)',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <Icon size={22} color={c.color} />
                  </div>
                  <ArrowUpRight size={18} color="var(--text-muted)" />
                </div>
                <h3 style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: '0.35rem' }}>{c.title}</h3>
                <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', lineHeight: 1.4 }}>{c.desc}</p>
              </div>
            </Link>
          );
        })}
      </div>
    </div>
  );
};

export default DashboardPage;
