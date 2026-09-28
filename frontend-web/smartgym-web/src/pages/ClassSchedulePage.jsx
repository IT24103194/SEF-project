import React from 'react';
import { Calendar } from 'lucide-react';

export const ClassSchedulePage = () => {
  return (
    <div>
      <div style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800 }}>Class Scheduling & Booking</h1>
        <p style={{ color: 'var(--text-secondary)' }}>Component 3 — Timetables, trainer allocations & capacity management.</p>
      </div>

      <div className="glass-card" style={{ padding: '2rem', textAlign: 'center' }}>
        <Calendar size={48} color="var(--accent-emerald)" style={{ marginBottom: '1rem' }} />
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '0.5rem' }}>Class Scheduling Module Ready</h2>
        <p style={{ color: 'var(--text-secondary)', maxWidth: 600, margin: '0 auto' }}>
          This interface is structured to consume the ASP.NET Core scheduling endpoints: <code>/api/classes</code>, <code>/api/schedules</code>, and <code>/api/bookings</code>.
        </p>
      </div>
    </div>
  );
};

export default ClassSchedulePage;
