import React from 'react';
import { Wrench, ShieldAlert, Cpu } from 'lucide-react';

export const FacilityResolutionPage = () => {
  return (
    <div>
      <div style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800 }}>Feedback & Facility Resolution</h1>
        <p style={{ color: 'var(--text-secondary)' }}>Component 2 — AI Issue Triage & Human-in-the-Loop Financial Authorization.</p>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '1.5rem', marginBottom: '2rem' }}>
        <div className="glass-card" style={{ padding: '1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1rem' }}>
            <Cpu size={24} color="var(--primary)" />
            <h3 style={{ fontSize: '1.1rem', fontWeight: 700 }}>Agentic AI Workflow</h3>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
            4 distinct LangGraph agents triage member problem reports, assess equipment warranties, and estimate repair costs.
          </p>
        </div>

        <div className="glass-card" style={{ padding: '1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1rem' }}>
            <ShieldAlert size={24} color="var(--accent-amber)" />
            <h3 style={{ fontSize: '1.1rem', fontWeight: 700 }}>HITL Financial Approval Gate</h3>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
            Repairs exceeding <strong>Rs. 25,000</strong> are automatically held for manager approval before supplier purchase orders are executed.
          </p>
        </div>
      </div>
    </div>
  );
};

export default FacilityResolutionPage;
