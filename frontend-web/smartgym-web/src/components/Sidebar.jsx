import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Package,
  Wrench,
  Calendar,
  Users,
  Dumbbell
} from 'lucide-react';

export const Sidebar = () => {
  const navItems = [
    { to: '/', label: 'Overview', icon: LayoutDashboard },
    { to: '/inventory', label: 'Supplements & Stock', icon: Package },
    { to: '/facility', label: 'Facility Resolution (AI)', icon: Wrench },
    { to: '/classes', label: 'Class Timetable', icon: Calendar },
    { to: '/memberships', label: 'Memberships & Plans', icon: Users },
  ];

  return (
    <aside className="sidebar">
      <div style={{ padding: '1.5rem', borderBottom: '1px solid var(--border-subtle)', display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
        <div style={{
          width: 36,
          height: 36,
          borderRadius: 'var(--radius-md)',
          background: 'linear-gradient(135deg, var(--primary), var(--accent-cyan))',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center'
        }}>
          <Dumbbell size={20} color="#ffffff" />
        </div>
        <div>
          <div style={{ fontWeight: 800, fontSize: '1.05rem', letterSpacing: '-0.02em' }}>SmartGym</div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Enterprise Portal</div>
        </div>
      </div>

      <nav style={{ padding: '1.25rem 0.75rem', display: 'flex', flexDirection: 'column', gap: '0.35rem', flex: 1 }}>
        <div style={{ fontSize: '0.7rem', fontWeight: 700, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em', padding: '0 0.75rem 0.5rem 0.75rem' }}>
          Core Modules
        </div>

        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              style={({ isActive }) => ({
                display: 'flex',
                alignItems: 'center',
                gap: '0.75rem',
                padding: '0.75rem',
                borderRadius: 'var(--radius-sm)',
                fontSize: '0.875rem',
                fontWeight: isActive ? 600 : 500,
                color: isActive ? '#ffffff' : 'var(--text-secondary)',
                backgroundColor: isActive ? 'var(--primary-light)' : 'transparent',
                border: isActive ? '1px solid var(--border-active)' : '1px solid transparent',
                transition: 'var(--transition)',
              })}
            >
              <Icon size={18} />
              <span>{item.label}</span>
            </NavLink>
          );
        })}
      </nav>

      <div style={{ padding: '1rem', borderTop: '1px solid var(--border-subtle)', fontSize: '0.75rem', color: 'var(--text-muted)' }}>
        <div>SE3090 Assignment 1</div>
        <div>Monorepo Architecture</div>
      </div>
    </aside>
  );
};

export default Sidebar;
