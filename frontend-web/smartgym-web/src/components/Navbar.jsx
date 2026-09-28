import React from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { logout } from '../store/authSlice';
import { Activity, ShieldCheck, User, LogOut } from 'lucide-react';
import NotificationCenter from './notifications/NotificationCenter';

export const Navbar = () => {
  const { user, role, isAuthenticated } = useSelector((state) => state.auth);
  const dispatch = useDispatch();

  return (
    <header className="top-navbar">
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
        <Activity size={22} color="var(--primary)" />
        <span style={{ fontWeight: 700, letterSpacing: '-0.02em', fontSize: '1.1rem' }}>
          SmartGym <span style={{ color: 'var(--primary)' }}>Console</span>
        </span>
        <span className="badge badge-info" style={{ marginLeft: '0.5rem' }}>
          v1.0.0
        </span>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.875rem' }}>
          <ShieldCheck size={16} color="var(--accent-emerald)" />
          <span style={{ color: 'var(--text-secondary)' }}>System:</span>
          <span className="badge badge-success">Healthy</span>
        </div>

        {isAuthenticated && <NotificationCenter />}

        {isAuthenticated && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <User size={18} color="var(--text-secondary)" />
              <span style={{ fontSize: '0.875rem', fontWeight: 600 }}>{user?.username || 'Admin'}</span>
              <span className="badge badge-warning">{role || 'ADMIN'}</span>
            </div>
            <button
              onClick={() => dispatch(logout())}
              className="btn btn-secondary"
              style={{ padding: '0.35rem 0.75rem', fontSize: '0.8rem' }}
            >
              <LogOut size={14} /> Logout
            </button>
          </div>
        )}
      </div>
    </header>
  );
};

export default Navbar;
