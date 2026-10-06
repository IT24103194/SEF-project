import React from 'react';
import { NavLink } from 'react-router-dom';
import { useSelector } from 'react-redux';
import {
  LayoutDashboard,
  Users,
  Dumbbell,
  ShieldCheck,
  Award,
  Target,
  Calendar,
  Clock,
  CalendarCheck,
  CheckSquare,
  Truck,
  Package,
  Boxes,
  MessageSquare,
  Wrench,
  FileSpreadsheet,
  Bell,
  BarChart3,
  Bot,
  ShieldAlert,
} from 'lucide-react';

export const Sidebar = () => {
  const { role, user } = useSelector((state) => state.auth);

  const rolesList = [
    ...(role ? [role] : []),
    ...(Array.isArray(user?.roles) ? user.roles : []),
    ...(user?.role ? [user.role] : []),
  ].map((r) => String(r).toUpperCase());

  const hasRoleAccess = (allowedRoles) => {
    if (!allowedRoles || allowedRoles.length === 0) return true;
    return allowedRoles.some((allowed) => {
      const upperAllowed = allowed.toUpperCase();
      if (upperAllowed === 'ADMIN') {
        return rolesList.some((r) => r === 'ADMIN' || r === 'FACILITYMANAGER' || r === 'FACILITY_MANAGER');
      }
      return rolesList.includes(upperAllowed);
    });
  };

  const primaryRole = rolesList.includes('ADMIN') || rolesList.includes('FACILITYMANAGER') || rolesList.includes('FACILITY_MANAGER')
    ? 'ADMIN'
    : rolesList.includes('TRAINER')
    ? 'TRAINER'
    : 'MEMBER';

  const allSections = [
    {
      title: 'Main',
      roles: ['ADMIN', 'TRAINER', 'MEMBER'],
      items: [
        { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, roles: ['ADMIN', 'TRAINER', 'MEMBER'] },
      ],
    },
    {
      title: 'Memberships & Training',
      roles: ['ADMIN', 'TRAINER', 'MEMBER'],
      items: [
        { to: '/members', label: 'Members', icon: Users, roles: ['ADMIN', 'TRAINER'] },
        { to: '/trainers', label: 'Trainers', icon: Dumbbell, roles: ['ADMIN'] },
        { to: '/membership-plans', label: 'Plans & Tiers', icon: ShieldCheck, roles: ['ADMIN'] },
        { to: '/memberships', label: 'Subscriptions', icon: Award, roles: ['ADMIN'] },
        { to: '/goals', label: 'Fitness Goals', icon: Target, roles: ['ADMIN', 'TRAINER', 'MEMBER'] },
      ],
    },
    {
      title: 'Classes & Timetable',
      roles: ['ADMIN', 'TRAINER', 'MEMBER'],
      items: [
        { to: '/classes', label: 'Class Catalog', icon: Calendar, roles: ['ADMIN', 'TRAINER', 'MEMBER'] },
        { to: '/schedules', label: 'Schedules', icon: Clock, roles: ['ADMIN', 'TRAINER'] },
        { to: '/bookings', label: 'Reservations', icon: CalendarCheck, roles: ['ADMIN', 'TRAINER', 'MEMBER'] },
        { to: '/attendance', label: 'Attendance', icon: CheckSquare, roles: ['ADMIN', 'TRAINER'] },
      ],
    },
    {
      title: 'Supplements & Inventory',
      roles: ['ADMIN'],
      items: [
        { to: '/inventory', label: 'Stock Inventory', icon: Boxes, roles: ['ADMIN'] },
        { to: '/products', label: 'Products', icon: Package, roles: ['ADMIN'] },
        { to: '/suppliers', label: 'Suppliers', icon: Truck, roles: ['ADMIN'] },
      ],
    },
    {
      title: 'Facility & Equipment',
      roles: ['ADMIN', 'TRAINER', 'MEMBER'],
      items: [
        { to: '/facility-issues', label: 'Facility Issues', icon: Wrench, roles: ['ADMIN', 'TRAINER', 'MEMBER'] },
        { to: '/repair-orders', label: 'Repair Orders', icon: FileSpreadsheet, roles: ['ADMIN'] },
        { to: '/feedback', label: 'Member Feedback', icon: MessageSquare, roles: ['ADMIN', 'TRAINER', 'MEMBER'] },
      ],
    },
    {
      title: 'AI & Intelligence',
      roles: ['ADMIN'],
      items: [
        { to: '/ai-workflows', label: 'AI Workflows', icon: Bot, roles: ['ADMIN'] },
        { to: '/approvals', label: 'Approvals Queue', icon: ShieldAlert, roles: ['ADMIN'] },
        { to: '/reports', label: 'Analytics Reports', icon: BarChart3, roles: ['ADMIN'] },
      ],
    },
    {
      title: 'Communication',
      roles: ['ADMIN', 'TRAINER', 'MEMBER'],
      items: [
        { to: '/notifications', label: 'Notifications', icon: Bell, roles: ['ADMIN', 'TRAINER', 'MEMBER'] },
      ],
    },
  ];

  // Filter sections and items based on role
  const visibleSections = allSections
    .filter((sec) => hasRoleAccess(sec.roles))
    .map((sec) => ({
      ...sec,
      items: sec.items.filter((item) => hasRoleAccess(item.roles)),
    }))
    .filter((sec) => sec.items.length > 0);

  return (
    <aside className="sidebar" style={{ overflowY: 'auto' }}>
      <div style={{ padding: '1.25rem 1.5rem', borderBottom: '1px solid var(--border-subtle)', display: 'flex', alignItems: 'center', gap: '0.75rem', flexShrink: 0 }}>
        <div style={{
          width: 38,
          height: 38,
          borderRadius: 'var(--radius-md)',
          background: 'linear-gradient(135deg, var(--primary), var(--accent-cyan))',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          boxShadow: '0 4px 12px rgba(99,102,241,0.3)',
        }}>
          <Dumbbell size={22} color="#ffffff" />
        </div>
        <div>
          <div style={{ fontWeight: 800, fontSize: '1.05rem', letterSpacing: '-0.02em', color: '#ffffff' }}>SmartGym</div>
          <div style={{ fontSize: '0.725rem', color: 'var(--accent-cyan)', fontWeight: 600 }}>{primaryRole} CONSOLE</div>
        </div>
      </div>

      <nav style={{ padding: '1rem 0.75rem', display: 'flex', flexDirection: 'column', gap: '1.25rem', flex: 1 }}>
        {visibleSections.map((sec, idx) => (
          <div key={idx}>
            <div style={{
              fontSize: '0.675rem',
              fontWeight: 800,
              color: 'var(--text-muted)',
              textTransform: 'uppercase',
              letterSpacing: '0.06em',
              padding: '0 0.75rem 0.4rem 0.75rem',
            }}>
              {sec.title}
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.2rem' }}>
              {sec.items.map((item) => {
                const Icon = item.icon;
                return (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    style={({ isActive }) => ({
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.75rem',
                      padding: '0.55rem 0.75rem',
                      borderRadius: 'var(--radius-sm)',
                      fontSize: '0.85rem',
                      fontWeight: isActive ? 700 : 500,
                      color: isActive ? '#ffffff' : 'var(--text-secondary)',
                      backgroundColor: isActive ? 'var(--primary-light)' : 'transparent',
                      border: isActive ? '1px solid var(--border-active)' : '1px solid transparent',
                      transition: 'var(--transition)',
                    })}
                  >
                    <Icon size={16} />
                    <span>{item.label}</span>
                  </NavLink>
                );
              })}
            </div>
          </div>
        ))}
      </nav>

      <div style={{ padding: '1rem 1.25rem', borderTop: '1px solid var(--border-subtle)', fontSize: '0.725rem', color: 'var(--text-muted)', flexShrink: 0 }}>
        <div>SmartGym Enterprise v1.0</div>
        <div>Role: <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{primaryRole}</span></div>
      </div>
    </aside>
  );
};

export default Sidebar;
