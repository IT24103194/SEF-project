import React from 'react';
import { Users } from 'lucide-react';

export const MembershipsPage = () => {
  return (
    <div>
      <div style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800 }}>Membership & Goal Tracking</h1>
        <p style={{ color: 'var(--text-secondary)' }}>Component 4 — Subscriptions, goal setting & progress analytics.</p>
      </div>

      <div className="glass-card" style={{ padding: '2rem', textAlign: 'center' }}>
        <Users size={48} color="var(--accent-amber)" style={{ marginBottom: '1rem' }} />
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '0.5rem' }}>Membership Module Ready</h2>
        <p style={{ color: 'var(--text-secondary)', maxWidth: 600, margin: '0 auto' }}>
          This interface is structured to consume the ASP.NET Core membership endpoints: <code>/api/membership-plans</code>, <code>/api/memberships</code>, and <code>/api/goals</code>.
        </p>
      </div>
    </div>
  );
};

export default MembershipsPage;
