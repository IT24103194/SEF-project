import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { fetchSystemInfo } from '../store/systemSlice';
import {
  Package,
  Wrench,
  Calendar,
  Users,
  Server,
  Cpu,
  ArrowUpRight
} from 'lucide-react';
import { Link } from 'react-router-dom';

export const DashboardPage = () => {
  const dispatch = useDispatch();
  const { info, status, error } = useSelector((state) => state.system);

  useEffect(() => {
    dispatch(fetchSystemInfo());
  }, [dispatch]);

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
      <div style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800, letterSpacing: '-0.025em' }}>
          System Dashboard & Architecture Cockpit
        </h1>
        <p style={{ color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
          Real-time diagnostics and module status for the SmartGym integrated ecosystem.
        </p>
      </div>

      {/* Backend & AI Connection Diagnostics */}
      <div className="glass-card" style={{ padding: '1.5rem', marginBottom: '2rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <Server size={20} color="var(--primary)" />
            <h2 style={{ fontSize: '1.1rem', fontWeight: 700 }}>Authoritative Backend Status</h2>
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

        {status === 'failed' && (
          <div style={{ color: 'var(--accent-rose)', fontSize: '0.875rem' }}>
            Could not reach ASP.NET Core API at <code>http://localhost:5000</code>. Ensure the backend is running.
          </div>
        )}
      </div>

      {/* Domain Modules Grid */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '1.5rem' }}>
        {cards.map((c) => {
          const Icon = c.icon;
          return (
            <div key={c.path} className="glass-card" style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
              <div>
                <div style={{
                  width: 44,
                  height: 44,
                  borderRadius: 'var(--radius-sm)',
                  backgroundColor: 'rgba(255, 255, 255, 0.05)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  marginBottom: '1rem',
                  color: c.color
                }}>
                  <Icon size={24} />
                </div>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 700, marginBottom: '0.5rem' }}>{c.title}</h3>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: '1.5rem' }}>
                  {c.desc}
                </p>
              </div>

              <Link to={c.path} className="btn btn-secondary" style={{ width: '100%', justifyContent: 'space-between' }}>
                <span>Access Module</span>
                <ArrowUpRight size={16} />
              </Link>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default DashboardPage;
