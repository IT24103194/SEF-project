import React from 'react';
import { useSelector } from 'react-redux';
import { Navigate, useLocation, Link } from 'react-router-dom';
import { ShieldAlert, ArrowLeft } from 'lucide-react';

export const ProtectedRoute = ({ allowedRoles = [], children }) => {
  const { isAuthenticated, role, user, loading } = useSelector((state) => state.auth);
  const location = useLocation();

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <div style={{ color: 'var(--text-secondary)' }}>Authenticating session...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // Check role authorization if roles specified
  if (allowedRoles.length > 0) {
    const rolesList = [
      ...(role ? [role] : []),
      ...(Array.isArray(user?.roles) ? user.roles : []),
      ...(user?.role ? [user.role] : []),
    ].map((r) => String(r).toUpperCase());

    const hasRole = allowedRoles.some((allowed) => {
      const upperAllowed = allowed.toUpperCase();
      if (upperAllowed === 'ADMIN') {
        return rolesList.some((r) => r === 'ADMIN' || r === 'FACILITYMANAGER' || r === 'FACILITY_MANAGER');
      }
      return rolesList.includes(upperAllowed);
    });

    if (!hasRole) {
      const displayRole = (role || (Array.isArray(user?.roles) ? user.roles[0] : null) || 'MEMBER').toUpperCase();
      return (
        <div style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '60vh',
          textAlign: 'center',
          padding: '2rem',
        }}>
          <div style={{
            width: 64,
            height: 64,
            borderRadius: '50%',
            background: 'rgba(244, 63, 94, 0.15)',
            border: '1px solid rgba(244, 63, 94, 0.3)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: '1.25rem',
          }}>
            <ShieldAlert size={32} color="var(--accent-rose)" />
          </div>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 800, color: '#ffffff', marginBottom: '0.5rem' }}>
            Access Restricted
          </h2>
          <p style={{ color: 'var(--text-secondary)', maxWidth: 460, fontSize: '0.9rem', marginBottom: '1.5rem', lineHeight: 1.6 }}>
            Your account role (<strong style={{ color: 'var(--accent-amber)' }}>{displayRole}</strong>) does not have authorization to view this administrative resource. Required roles: {allowedRoles.join(', ')}.
          </p>
          <Link to="/dashboard" className="btn btn-primary" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem' }}>
            <ArrowLeft size={16} /> Return to Dashboard
          </Link>
        </div>
      );
    }
  }

  return children;
};

export default ProtectedRoute;
