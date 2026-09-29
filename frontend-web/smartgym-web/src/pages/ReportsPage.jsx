import React, { useState, useEffect } from 'react';
import reportsApi from '../services/reportsApi';
import PageHeader from '../components/common/PageHeader';
import LoadingSpinner from '../components/common/LoadingSpinner';
import {
  BarChart3,
  Users,
  Calendar,
  Package,
  Wrench,
  Bot,
  TrendingUp,
  AlertTriangle,
  CheckCircle2,
  Clock,
  RefreshCw,
  DollarSign
} from 'lucide-react';

export const ReportsPage = () => {
  const [activeTab, setActiveTab] = useState('membership'); // membership, classes, inventory, facility, ai
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [membershipData, setMembershipData] = useState(null);
  const [classesData, setClassesData] = useState(null);
  const [inventoryData, setInventoryData] = useState(null);
  const [facilityData, setFacilityData] = useState(null);
  const [aiData, setAiData] = useState(null);

  const fetchReports = async () => {
    setLoading(true);
    setError(null);
    try {
      const [mem, cls, inv, fac, ai] = await Promise.all([
        reportsApi.getMembershipReport().catch(() => null),
        reportsApi.getClassesReport().catch(() => null),
        reportsApi.getInventoryReport().catch(() => null),
        reportsApi.getFacilityReport().catch(() => null),
        reportsApi.getAiReport().catch(() => null),
      ]);
      setMembershipData(mem);
      setClassesData(cls);
      setInventoryData(inv);
      setFacilityData(fac);
      setAiData(ai);
    } catch (err) {
      console.error('Failed to load reports:', err);
      setError('Unable to load analytics reports.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReports();
  }, []);

  const tabs = [
    { id: 'membership', label: 'Memberships', icon: Users },
    { id: 'classes', label: 'Classes & Bookings', icon: Calendar },
    { id: 'inventory', label: 'Inventory & Stock', icon: Package },
    { id: 'facility', label: 'Facility & Maintenance', icon: Wrench },
    { id: 'ai', label: 'AI Subsystem', icon: Bot },
  ];

  return (
    <div>
      <PageHeader
        title="Enterprise Analytics & Reporting"
        subtitle="Cross-cutting business intelligence, utilization metrics and subsystem reports"
        icon={BarChart3}
        badge="Real-Time Data"
        actions={
          <button onClick={fetchReports} className="btn btn-primary">
            <RefreshCw size={16} /> Refresh Reports
          </button>
        }
      />

      {/* Navigation Tabs */}
      <div className="glass-card" style={{ padding: '0.5rem', marginBottom: '1.5rem', display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
        {tabs.map((t) => {
          const Icon = t.icon;
          const isActive = activeTab === t.id;
          return (
            <button
              key={t.id}
              onClick={() => setActiveTab(t.id)}
              className={`btn ${isActive ? 'btn-primary' : 'btn-secondary'}`}
              style={{ padding: '0.5rem 1rem', fontSize: '0.85rem' }}
            >
              <Icon size={16} /> {t.label}
            </button>
          );
        })}
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Calculating cross-module analytics..." />
      ) : (
        <div>
          {/* TAB 1: MEMBERSHIP */}
          {activeTab === 'membership' && membershipData && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.25rem' }}>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Active Members</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-emerald)', marginTop: '0.25rem' }}>
                    {membershipData.activeMembers}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Expired Memberships</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-rose)', marginTop: '0.25rem' }}>
                    {membershipData.expiredMemberships}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Expiring Soon (30d)</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-amber)', marginTop: '0.25rem' }}>
                    {membershipData.expiringMemberships}
                  </div>
                </div>
              </div>

              {/* Distribution */}
              <div className="glass-card" style={{ padding: '1.5rem' }}>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: '#ffffff', marginBottom: '1rem' }}>
                  Membership Plan Distribution
                </h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
                  {(membershipData.planDistribution || []).map((dist, idx) => (
                    <div key={idx}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', marginBottom: '0.25rem' }}>
                        <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{dist.planName}</span>
                        <span style={{ color: 'var(--text-muted)' }}>{dist.activeCount} members ({dist.percentage?.toFixed(1)}%)</span>
                      </div>
                      <div style={{ width: '100%', height: 8, background: 'rgba(255,255,255,0.08)', borderRadius: 'var(--radius-full)', overflow: 'hidden' }}>
                        <div style={{ width: `${dist.percentage || 0}%`, height: '100%', background: 'linear-gradient(90deg, var(--primary), var(--accent-cyan))', borderRadius: 'var(--radius-full)' }} />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* TAB 2: CLASSES */}
          {activeTab === 'classes' && classesData && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.25rem' }}>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Total Bookings</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--primary)', marginTop: '0.25rem' }}>
                    {classesData.totalBookings}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Attended Sessions</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-emerald)', marginTop: '0.25rem' }}>
                    {classesData.totalAttended}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Overall Utilization</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-cyan)', marginTop: '0.25rem' }}>
                    {classesData.overallUtilizationPercentage?.toFixed(1)}%
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Cancellations</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-rose)', marginTop: '0.25rem' }}>
                    {classesData.totalCancelled}
                  </div>
                </div>
              </div>

              {/* Class Utilization Breakdown */}
              <div className="glass-card" style={{ padding: '1.5rem' }}>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: '#ffffff', marginBottom: '1rem' }}>
                  Class Utilization Breakdown
                </h3>
                <div className="table-wrapper">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Class Name</th>
                        <th>Sessions</th>
                        <th>Booked / Capacity</th>
                        <th>Utilization</th>
                        <th>Attendance Rate</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(classesData.classUtilizations || []).map((u, i) => (
                        <tr key={i}>
                          <td style={{ fontWeight: 600, color: '#ffffff' }}>{u.className}</td>
                          <td>{u.scheduledSessions}</td>
                          <td>{u.totalBooked} / {u.totalCapacity}</td>
                          <td>
                            <strong style={{ color: u.utilizationPercentage >= 80 ? 'var(--accent-emerald)' : 'var(--accent-cyan)' }}>
                              {u.utilizationPercentage?.toFixed(1)}%
                            </strong>
                          </td>
                          <td>{u.attendanceRate?.toFixed(1)}%</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}

          {/* TAB 3: INVENTORY */}
          {activeTab === 'inventory' && inventoryData && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.25rem' }}>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Low-Stock Alerts</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-rose)', marginTop: '0.25rem' }}>
                    {inventoryData.lowStockCount}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Stock Restocks (Inbound)</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-emerald)', marginTop: '0.25rem' }}>
                    {inventoryData.totalRestocks}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Total Units Consumed</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--primary)', marginTop: '0.25rem' }}>
                    {inventoryData.totalConsumed}
                  </div>
                </div>
              </div>

              {/* Popular Products */}
              <div className="glass-card" style={{ padding: '1.5rem' }}>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: '#ffffff', marginBottom: '1rem' }}>
                  Most Active Products & Supplements
                </h3>
                <div className="table-wrapper">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Product</th>
                        <th>Category</th>
                        <th>Current Stock</th>
                        <th>Units Sold/Adjusted</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(inventoryData.popularProducts || []).map((p, i) => (
                        <tr key={i}>
                          <td style={{ fontWeight: 600, color: '#ffffff' }}>{p.productName}</td>
                          <td><span className="badge badge-info">{p.categoryName || 'Supplement'}</span></td>
                          <td><strong style={{ color: p.currentStock <= 10 ? 'var(--accent-rose)' : '#ffffff' }}>{p.currentStock}</strong></td>
                          <td>{p.totalUnitsMoved} units</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}

          {/* TAB 4: FACILITY */}
          {activeTab === 'facility' && facilityData && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.25rem' }}>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Open Facility Issues</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-rose)', marginTop: '0.25rem' }}>
                    {facilityData.openIssues}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Resolved Issues</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-emerald)', marginTop: '0.25rem' }}>
                    {facilityData.resolvedIssues}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Avg Resolution Time</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-cyan)', marginTop: '0.25rem' }}>
                    {facilityData.averageResolutionTimeHours?.toFixed(1)}h
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Total Repair Costs</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-amber)', marginTop: '0.25rem' }}>
                    ${facilityData.totalRepairCost?.toFixed(2)}
                  </div>
                </div>
              </div>

              {/* Issues by Priority */}
              <div className="glass-card" style={{ padding: '1.5rem' }}>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: '#ffffff', marginBottom: '1rem' }}>
                  Issues by Severity / Priority
                </h3>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: '1rem' }}>
                  {(facilityData.issuesByPriority || []).map((p, idx) => (
                    <div key={idx} style={{ background: 'rgba(255,255,255,0.03)', padding: '1rem', borderRadius: 'var(--radius-sm)' }}>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{p.priority}</div>
                      <div style={{ fontSize: '1.5rem', fontWeight: 800, color: '#ffffff', marginTop: '0.25rem' }}>
                        {p.count}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* TAB 5: AI SUBSYSTEM */}
          {activeTab === 'ai' && aiData && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.25rem' }}>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>AI Workflow Triggers</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--primary)', marginTop: '0.25rem' }}>
                    {aiData.totalWorkflows}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Pending Approvals</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-amber)', marginTop: '0.25rem' }}>
                    {aiData.pendingApprovals}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Approved Workflows</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-emerald)', marginTop: '0.25rem' }}>
                    {aiData.approvals}
                  </div>
                </div>
                <div className="glass-card" style={{ padding: '1.5rem' }}>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Safe Failures & Fallbacks</div>
                  <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-rose)', marginTop: '0.25rem' }}>
                    {aiData.safeFailures}
                  </div>
                </div>
              </div>

              <div className="glass-card" style={{ padding: '1.5rem' }}>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: '#ffffff', marginBottom: '1rem' }}>
                  Agentic AI System Health & Governance
                </h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '0.75rem' }}>
                    <span style={{ color: 'var(--text-secondary)' }}>Average Workflow Duration:</span>
                    <strong style={{ color: '#ffffff' }}>{aiData.averageWorkflowDurationSeconds?.toFixed(2)} seconds</strong>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '0.75rem' }}>
                    <span style={{ color: 'var(--text-secondary)' }}>Human Decisions (Rejections / Revisions):</span>
                    <strong style={{ color: '#ffffff' }}>{aiData.rejections} rejected / {aiData.revisions} revised</strong>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span style={{ color: 'var(--text-secondary)' }}>Model Identifier:</span>
                    <strong style={{ color: 'var(--accent-cyan)' }}>gemini-1.5-pro / multi-tool agent</strong>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default ReportsPage;
