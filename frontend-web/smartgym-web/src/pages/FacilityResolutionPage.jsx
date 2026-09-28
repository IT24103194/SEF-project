import React, { useState, useEffect } from 'react';
import { 
  Wrench, MapPin, AlertCircle, FileText, CheckCircle, Clock, 
  Plus, Search, RefreshCw, Filter, Eye, ShieldAlert, Star, MessageSquare, Trash2, Edit 
} from 'lucide-react';
import facilityApi from '../services/facilityApi';
import IssueDetailsModal from '../components/facility/IssueDetailsModal';
import EquipmentModal from '../components/facility/EquipmentModal';
import LocationModal from '../components/facility/LocationModal';
import RepairOrderModal from '../components/facility/RepairOrderModal';
import EquipmentHistoryModal from '../components/facility/EquipmentHistoryModal';
import FeedbackResponseModal from '../components/facility/FeedbackResponseModal';

export const FacilityResolutionPage = () => {
  const [activeTab, setActiveTab] = useState('issues'); // issues, equipment, locations, repairs, feedback
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  // Data states
  const [issues, setIssues] = useState([]);
  const [equipmentList, setEquipmentList] = useState([]);
  const [locations, setLocations] = useState([]);
  const [repairOrders, setRepairOrders] = useState([]);
  const [feedbacks, setFeedbacks] = useState([]);

  // Modals
  const [selectedIssue, setSelectedIssue] = useState(null);
  const [editingEquipment, setEditingEquipment] = useState(null);
  const [showEquipmentModal, setShowEquipmentModal] = useState(false);
  const [inspectingEquipmentHistoryId, setInspectingEquipmentHistoryId] = useState(null);
  const [editingLocation, setEditingLocation] = useState(null);
  const [showLocationModal, setShowLocationModal] = useState(false);
  const [showRepairModal, setShowRepairModal] = useState(false);
  const [respondingFeedback, setRespondingFeedback] = useState(null);

  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [issueStatusFilter, setIssueStatusFilter] = useState('');
  const [issueSeverityFilter, setIssueSeverityFilter] = useState('');
  const [locationFilter, setLocationFilter] = useState('');

  useEffect(() => {
    loadAllData();
  }, []);

  const loadAllData = async () => {
    try {
      setLoading(true);
      setErrorMsg('');

      const [locsRes, eqRes, issRes, repRes, fdbRes] = await Promise.all([
        facilityApi.getLocations({ pageSize: 50 }),
        facilityApi.getEquipment({ pageSize: 50 }),
        facilityApi.getFacilityIssues({ pageSize: 50 }),
        facilityApi.getRepairOrders({ pageSize: 50 }),
        facilityApi.getFeedbacks({ pageSize: 50 }),
      ]);

      setLocations(locsRes.items || []);
      setEquipmentList(eqRes.items || []);
      setIssues(issRes.items || []);
      setRepairOrders(repRes.items || []);
      setFeedbacks(fdbRes.items || []);
    } catch (err) {
      console.error('Failed to load facility data', err);
      setErrorMsg('Failed to load facility data. Please check your connection.');
    } finally {
      setLoading(false);
    }
  };

  const handleApproveRepair = async (id, decision) => {
    try {
      await facilityApi.processRepairApproval(id, {
        decision: decision === 'Approved' ? 1 : 2,
        comments: `Manager ${decision.toLowerCase()} financial repair authorization.`,
      });
      loadAllData();
    } catch (err) {
      alert(err.response?.data?.detail || err.message || 'Failed to process repair approval.');
    }
  };

  const handleDeleteLocation = async (id) => {
    if (!window.confirm('Are you sure you want to delete this location?')) return;
    try {
      await facilityApi.deleteLocation(id);
      loadAllData();
    } catch (err) {
      alert(err.response?.data?.detail || err.message || 'Cannot delete location with existing items.');
    }
  };

  const handleDeleteEquipment = async (id) => {
    if (!window.confirm('Are you sure you want to delete this equipment?')) return;
    try {
      await facilityApi.deleteEquipment(id);
      loadAllData();
    } catch (err) {
      alert(err.response?.data?.detail || err.message || 'Cannot delete equipment with active issues.');
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 'SUBMITTED': return '#64748B';
      case 'AI_ANALYZING': return '#8B5CF6';
      case 'PENDING_APPROVAL': return '#F59E0B';
      case 'APPROVED': return '#3B82F6';
      case 'VENDOR_CONTACTED': return '#06B6D4';
      case 'REPAIR_SCHEDULED': return '#EC4899';
      case 'IN_PROGRESS': return '#38BDF8';
      case 'RESOLVED': return '#10B981';
      case 'REJECTED': return '#EF4444';
      case 'REVISION_REQUIRED': return '#F97316';
      default: return '#94A3B8';
    }
  };

  // Filtered issues
  const filteredIssues = issues.filter((i) => {
    if (issueStatusFilter && (i.statusName !== issueStatusFilter && i.status !== issueStatusFilter)) return false;
    if (issueSeverityFilter && (i.severityName !== issueSeverityFilter && i.severity !== issueSeverityFilter)) return false;
    if (locationFilter && i.locationId !== locationFilter) return false;
    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      return (
        i.title.toLowerCase().includes(term) ||
        i.description.toLowerCase().includes(term) ||
        (i.equipmentName && i.equipmentName.toLowerCase().includes(term)) ||
        i.locationName.toLowerCase().includes(term)
      );
    }
    return true;
  });

  return (
    <div style={{ maxWidth: '1400px', margin: '0 auto', paddingBottom: '3rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, color: 'var(--text-primary)' }}>
            Facility Resolution & Maintenance
          </h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
            Equipment reliability, member issue resolution, financial approvals, and feedback management.
          </p>
        </div>
        <button
          onClick={loadAllData}
          className="btn-secondary"
          style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
        >
          <RefreshCw size={16} className={loading ? 'animate-spin' : ''} /> Refresh
        </button>
      </div>

      {errorMsg && (
        <div style={{
          backgroundColor: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)',
          color: '#F87171', padding: '1rem', borderRadius: '10px', marginBottom: '1.5rem'
        }}>
          {errorMsg}
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: 'flex', gap: '0.5rem', borderBottom: '1px solid rgba(255,255,255,0.08)', marginBottom: '1.75rem', overflowX: 'auto' }}>
        {[
          { id: 'issues', label: 'Facility Issues', count: issues.length, icon: AlertCircle },
          { id: 'equipment', label: 'Equipment & Maintenance', count: equipmentList.length, icon: Wrench },
          { id: 'locations', label: 'Facility Locations', count: locations.length, icon: MapPin },
          { id: 'repairs', label: 'Repair Orders & HITL', count: repairOrders.length, icon: ShieldAlert },
          { id: 'feedback', label: 'Member Feedback', count: feedbacks.length, icon: MessageSquare },
        ].map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              style={{
                display: 'flex', alignItems: 'center', gap: '8px', padding: '0.85rem 1.25rem',
                border: 'none', background: 'none', cursor: 'pointer',
                color: isActive ? 'var(--primary)' : 'var(--text-secondary)',
                fontWeight: isActive ? 700 : 500,
                borderBottom: isActive ? '2px solid var(--primary)' : '2px solid transparent',
                transition: 'all 0.2s ease', whiteSpace: 'nowrap'
              }}
            >
              <Icon size={18} />
              {tab.label}
              <span style={{
                backgroundColor: isActive ? 'var(--primary)' : 'rgba(255,255,255,0.06)',
                color: isActive ? 'white' : 'var(--text-secondary)',
                padding: '2px 8px', borderRadius: '999px', fontSize: '0.75rem'
              }}>
                {tab.count}
              </span>
            </button>
          );
        })}
      </div>

      {/* TAB 1: FACILITY ISSUES */}
      {activeTab === 'issues' && (
        <div>
          {/* Controls & Filter Bar */}
          <div className="glass-card" style={{ padding: '1rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'center' }}>
            <div style={{ position: 'relative', flex: '1 1 240px' }}>
              <Search size={18} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input
                type="text"
                placeholder="Search issues, equipment, or descriptions..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                style={{
                  width: '100%', padding: '0.6rem 0.75rem 0.6rem 2.25rem', borderRadius: '8px',
                  backgroundColor: 'rgba(0,0,0,0.3)', border: '1px solid rgba(255,255,255,0.1)', color: 'white'
                }}
              />
            </div>

            <select
              value={issueStatusFilter}
              onChange={(e) => setIssueStatusFilter(e.target.value)}
              style={{
                padding: '0.6rem 0.85rem', borderRadius: '8px',
                backgroundColor: 'rgba(0,0,0,0.3)', border: '1px solid rgba(255,255,255,0.1)', color: 'white'
              }}
            >
              <option value="">All Statuses</option>
              <option value="SUBMITTED">SUBMITTED</option>
              <option value="AI_ANALYZING">AI_ANALYZING</option>
              <option value="PENDING_APPROVAL">PENDING_APPROVAL</option>
              <option value="APPROVED">APPROVED</option>
              <option value="VENDOR_CONTACTED">VENDOR_CONTACTED</option>
              <option value="REPAIR_SCHEDULED">REPAIR_SCHEDULED</option>
              <option value="IN_PROGRESS">IN_PROGRESS</option>
              <option value="RESOLVED">RESOLVED</option>
              <option value="REJECTED">REJECTED</option>
              <option value="REVISION_REQUIRED">REVISION_REQUIRED</option>
            </select>

            <select
              value={issueSeverityFilter}
              onChange={(e) => setIssueSeverityFilter(e.target.value)}
              style={{
                padding: '0.6rem 0.85rem', borderRadius: '8px',
                backgroundColor: 'rgba(0,0,0,0.3)', border: '1px solid rgba(255,255,255,0.1)', color: 'white'
              }}
            >
              <option value="">All Severities</option>
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Critical">Critical</option>
            </select>

            <select
              value={locationFilter}
              onChange={(e) => setLocationFilter(e.target.value)}
              style={{
                padding: '0.6rem 0.85rem', borderRadius: '8px',
                backgroundColor: 'rgba(0,0,0,0.3)', border: '1px solid rgba(255,255,255,0.1)', color: 'white'
              }}
            >
              <option value="">All Locations</option>
              {locations.map((l) => (
                <option key={l.id} value={l.id}>{l.name}</option>
              ))}
            </select>
          </div>

          {/* Issues Table */}
          <div className="glass-card" style={{ padding: '0', overflow: 'hidden' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.08)', backgroundColor: 'rgba(255,255,255,0.02)' }}>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>STATUS</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>SEVERITY</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>ISSUE TITLE</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>EQUIPMENT / ZONE</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>REPORTED BY</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>PHOTOS</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)', textAlign: 'right' }}>ACTIONS</th>
                </tr>
              </thead>
              <tbody>
                {filteredIssues.length > 0 ? (
                  filteredIssues.map((iss) => (
                    <tr
                      key={iss.id}
                      style={{ borderBottom: '1px solid rgba(255,255,255,0.04)', cursor: 'pointer' }}
                      onClick={() => setSelectedIssue(iss)}
                    >
                      <td style={{ padding: '1rem' }}>
                        <span style={{
                          backgroundColor: `${getStatusColor(iss.statusName || iss.status)}22`,
                          color: getStatusColor(iss.statusName || iss.status),
                          padding: '3px 10px', borderRadius: '999px', fontSize: '0.75rem', fontWeight: 700,
                          border: `1px solid ${getStatusColor(iss.statusName || iss.status)}55`
                        }}>
                          {iss.statusName || iss.status}
                        </span>
                      </td>
                      <td style={{ padding: '1rem' }}>
                        <span style={{
                          color: iss.severityName === 'Critical' ? '#EF4444' : iss.severityName === 'High' ? '#F97316' : '#94A3B8',
                          fontWeight: 600, fontSize: '0.85rem'
                        }}>
                          {iss.severityName || iss.severity}
                        </span>
                      </td>
                      <td style={{ padding: '1rem' }}>
                        <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{iss.title}</div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', maxWidth: '350px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                          {iss.sanitizedDescription || iss.description}
                        </div>
                      </td>
                      <td style={{ padding: '1rem' }}>
                        <div style={{ fontSize: '0.875rem', fontWeight: 500 }}>{iss.equipmentName || 'Facility Area'}</div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{iss.locationName}</div>
                      </td>
                      <td style={{ padding: '1rem', fontSize: '0.85rem' }}>
                        <div>{iss.reporterName}</div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                          {new Date(iss.reportedAt).toLocaleDateString()}
                        </div>
                      </td>
                      <td style={{ padding: '1rem' }}>
                        {iss.images && iss.images.length > 0 ? (
                          <span style={{ fontSize: '0.8rem', color: 'var(--primary)', fontWeight: 600 }}>
                            {iss.images.length} Photo{iss.images.length > 1 ? 's' : ''}
                          </span>
                        ) : (
                          <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>—</span>
                        )}
                      </td>
                      <td style={{ padding: '1rem', textAlign: 'right' }}>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            setSelectedIssue(iss);
                          }}
                          className="btn-secondary"
                          style={{ padding: '4px 10px', fontSize: '0.8rem', display: 'inline-flex', alignItems: 'center', gap: '4px' }}
                        >
                          <Eye size={14} /> Review
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={7} style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                      No facility issues matching the selected filters.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 2: EQUIPMENT & ASSETS */}
      {activeTab === 'equipment' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Gym Equipment Inventory</h3>
            <button
              onClick={() => {
                setEditingEquipment(null);
                setShowEquipmentModal(true);
              }}
              className="btn-primary"
              style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <Plus size={16} /> Add Equipment
            </button>
          </div>

          <div className="glass-card" style={{ padding: '0', overflow: 'hidden' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.08)', backgroundColor: 'rgba(255,255,255,0.02)' }}>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>EQUIPMENT NAME</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>SERIAL #</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>MODEL / BRAND</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>LOCATION</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>STATUS</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>ACTIVE ISSUES</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)', textAlign: 'right' }}>ACTIONS</th>
                </tr>
              </thead>
              <tbody>
                {equipmentList.map((eq) => (
                  <tr key={eq.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    <td style={{ padding: '1rem', fontWeight: 600 }}>{eq.name}</td>
                    <td style={{ padding: '1rem', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>{eq.serialNumber}</td>
                    <td style={{ padding: '1rem', fontSize: '0.85rem' }}>{eq.model || '—'} ({eq.manufacturer || '—'})</td>
                    <td style={{ padding: '1rem', fontSize: '0.85rem' }}>{eq.locationName} ({eq.locationFloor})</td>
                    <td style={{ padding: '1rem' }}>
                      <span style={{
                        padding: '3px 8px', borderRadius: '6px', fontSize: '0.75rem', fontWeight: 600,
                        backgroundColor: eq.statusName === 'Operational' ? '#10B98122' : '#F59E0B22',
                        color: eq.statusName === 'Operational' ? '#10B981' : '#F59E0B',
                      }}>
                        {eq.statusName}
                      </span>
                    </td>
                    <td style={{ padding: '1rem', fontSize: '0.85rem' }}>
                      {eq.activeIssuesCount > 0 ? (
                        <span style={{ color: '#F59E0B', fontWeight: 700 }}>{eq.activeIssuesCount} Open</span>
                      ) : (
                        <span style={{ color: '#10B981' }}>None</span>
                      )}
                    </td>
                    <td style={{ padding: '1rem', textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                        <button
                          onClick={() => setInspectingEquipmentHistoryId(eq.id)}
                          className="btn-secondary"
                          style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                          title="View Maintenance History"
                        >
                          <Clock size={14} /> History
                        </button>
                        <button
                          onClick={() => {
                            setEditingEquipment(eq);
                            setShowEquipmentModal(true);
                          }}
                          className="btn-secondary"
                          style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                        >
                          <Edit size={14} />
                        </button>
                        <button
                          onClick={() => handleDeleteEquipment(eq.id)}
                          className="btn-secondary"
                          style={{ padding: '4px 8px', fontSize: '0.75rem', color: '#EF4444' }}
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 3: LOCATIONS & AREAS */}
      {activeTab === 'locations' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Gym Facility Locations</h3>
            <button
              onClick={() => {
                setEditingLocation(null);
                setShowLocationModal(true);
              }}
              className="btn-primary"
              style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <Plus size={16} /> Add Location
            </button>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '1.25rem' }}>
            {locations.map((loc) => (
              <div key={loc.id} className="glass-card" style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
                    <span style={{
                      backgroundColor: 'rgba(99, 102, 241, 0.15)', color: 'var(--primary)',
                      padding: '2px 8px', borderRadius: '4px', fontSize: '0.75rem', fontWeight: 600
                    }}>
                      {loc.floor}
                    </span>
                    <div style={{ display: 'flex', gap: '4px' }}>
                      <button
                        onClick={() => {
                          setEditingLocation(loc);
                          setShowLocationModal(true);
                        }}
                        style={{ background: 'none', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer', padding: '4px' }}
                      >
                        <Edit size={16} />
                      </button>
                      <button
                        onClick={() => handleDeleteLocation(loc.id)}
                        style={{ background: 'none', border: 'none', color: '#EF4444', cursor: 'pointer', padding: '4px' }}
                      >
                        <Trash2 size={16} />
                      </button>
                    </div>
                  </div>
                  <h4 style={{ fontSize: '1.15rem', fontWeight: 700, marginBottom: '0.5rem' }}>{loc.name}</h4>
                  <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', lineHeight: 1.4, marginBottom: '1rem' }}>
                    {loc.description || 'No description provided.'}
                  </p>
                </div>

                <div style={{
                  display: 'flex', justifyContent: 'space-between', paddingTop: '1rem',
                  borderTop: '1px solid rgba(255,255,255,0.06)', fontSize: '0.85rem'
                }}>
                  <span>Equipment: <strong>{loc.equipmentCount}</strong></span>
                  <span>Active Issues: <strong style={{ color: loc.activeIssuesCount > 0 ? '#F59E0B' : '#10B981' }}>{loc.activeIssuesCount}</strong></span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* TAB 4: REPAIR ORDERS & HITL APPROVALS */}
      {activeTab === 'repairs' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
            <div>
              <h3 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Equipment Repair Orders</h3>
              <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                High-value repairs (≥ $500 / Rs. 25,000) require Human-in-the-Loop manager approval before execution.
              </p>
            </div>
            <button
              onClick={() => setShowRepairModal(true)}
              className="btn-primary"
              style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <Plus size={16} /> Create Repair Order
            </button>
          </div>

          <div className="glass-card" style={{ padding: '0', overflow: 'hidden' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.08)', backgroundColor: 'rgba(255,255,255,0.02)' }}>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>ORDER #</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>EQUIPMENT / ISSUE</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>ESTIMATED COST</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>STATUS</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>TECHNICIAN</th>
                  <th style={{ padding: '1rem', fontSize: '0.8rem', color: 'var(--text-secondary)', textAlign: 'right' }}>APPROVAL ACTION</th>
                </tr>
              </thead>
              <tbody>
                {repairOrders.map((ro) => (
                  <tr key={ro.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    <td style={{ padding: '1rem', fontWeight: 700 }}>{ro.orderNumber}</td>
                    <td style={{ padding: '1rem' }}>
                      <div style={{ fontWeight: 600 }}>{ro.equipmentName}</div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{ro.issueTitle}</div>
                    </td>
                    <td style={{ padding: '1rem' }}>
                      <div style={{ fontWeight: 700, color: 'var(--primary)' }}>${ro.estimatedCost.toFixed(2)}</div>
                      {ro.requiresApproval && (
                        <div style={{ fontSize: '0.7rem', color: '#F59E0B', fontWeight: 600 }}>★ High-Value Gate</div>
                      )}
                    </td>
                    <td style={{ padding: '1rem' }}>
                      <span style={{
                        padding: '3px 8px', borderRadius: '6px', fontSize: '0.75rem', fontWeight: 600,
                        backgroundColor: ro.statusName === 'Approved' ? '#10B98122' : ro.statusName === 'PendingApproval' ? '#F59E0B22' : 'rgba(255,255,255,0.06)',
                        color: ro.statusName === 'Approved' ? '#10B981' : ro.statusName === 'PendingApproval' ? '#F59E0B' : 'var(--text-secondary)'
                      }}>
                        {ro.statusName}
                      </span>
                    </td>
                    <td style={{ padding: '1rem', fontSize: '0.85rem' }}>{ro.technicianName || 'In-House'}</td>
                    <td style={{ padding: '1rem', textAlign: 'right' }}>
                      {ro.statusName === 'PendingApproval' ? (
                        <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                          <button
                            onClick={() => handleApproveRepair(ro.id, 'Approved')}
                            className="btn-primary"
                            style={{ padding: '4px 10px', fontSize: '0.75rem', backgroundColor: '#10B981' }}
                          >
                            Authorize
                          </button>
                          <button
                            onClick={() => handleApproveRepair(ro.id, 'Rejected')}
                            className="btn-secondary"
                            style={{ padding: '4px 10px', fontSize: '0.75rem', color: '#EF4444' }}
                          >
                            Reject
                          </button>
                        </div>
                      ) : (
                        <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                          {ro.approval?.decisionName || 'Authorized'}
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 5: MEMBER FEEDBACK */}
      {activeTab === 'feedback' && (
        <div>
          <div style={{ marginBottom: '1.5rem' }}>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Member Feedback & Suggestions</h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
              Review member ratings, suggestions, and send administrative responses.
            </p>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {feedbacks.map((f) => (
              <div key={f.id} className="glass-card" style={{ padding: '1.25rem' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <h4 style={{ fontSize: '1.05rem', fontWeight: 700 }}>{f.subject}</h4>
                      <div style={{ display: 'flex', color: '#F59E0B' }}>
                        {[...Array(5)].map((_, i) => (
                          <Star key={i} size={14} fill={i < f.rating ? '#F59E0B' : 'none'} color="#F59E0B" />
                        ))}
                      </div>
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                      By {f.memberName} ({f.memberEmail}) on {new Date(f.createdAt).toLocaleDateString()}
                    </div>
                  </div>
                  <span style={{
                    padding: '2px 8px', borderRadius: '4px', fontSize: '0.75rem', fontWeight: 600,
                    backgroundColor: f.statusName === 'Reviewed' ? '#10B98122' : 'rgba(255,255,255,0.06)',
                    color: f.statusName === 'Reviewed' ? '#10B981' : 'var(--text-secondary)'
                  }}>
                    {f.statusName}
                  </span>
                </div>

                <p style={{ fontSize: '0.875rem', color: 'var(--text-primary)', lineHeight: 1.5, margin: '0.5rem 0' }}>
                  "{f.content}"
                </p>

                {f.adminResponse ? (
                  <div style={{
                    marginTop: '0.75rem', padding: '0.75rem 1rem',
                    backgroundColor: 'rgba(16, 185, 129, 0.08)', borderRadius: '8px',
                    borderLeft: '3px solid #10B981', fontSize: '0.85rem'
                  }}>
                    <strong style={{ color: '#10B981' }}>Staff Response:</strong> {f.adminResponse}
                  </div>
                ) : (
                  <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '0.5rem' }}>
                    <button
                      onClick={() => setRespondingFeedback(f)}
                      className="btn-primary"
                      style={{ padding: '4px 10px', fontSize: '0.8rem', display: 'inline-flex', alignItems: 'center', gap: '4px' }}
                    >
                      <MessageSquare size={14} /> Respond to Member
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Modals */}
      {selectedIssue && (
        <IssueDetailsModal
          issue={selectedIssue}
          onClose={() => setSelectedIssue(null)}
          onRefresh={loadAllData}
        />
      )}

      {showEquipmentModal && (
        <EquipmentModal
          equipment={editingEquipment}
          locations={locations}
          onClose={() => setShowEquipmentModal(false)}
          onSaved={() => {
            setShowEquipmentModal(false);
            loadAllData();
          }}
        />
      )}

      {inspectingEquipmentHistoryId && (
        <EquipmentHistoryModal
          equipmentId={inspectingEquipmentHistoryId}
          onClose={() => setInspectingEquipmentHistoryId(null)}
        />
      )}

      {showLocationModal && (
        <LocationModal
          location={editingLocation}
          onClose={() => setShowLocationModal(false)}
          onSaved={() => {
            setShowLocationModal(false);
            loadAllData();
          }}
        />
      )}

      {showRepairModal && (
        <RepairOrderModal
          issues={issues}
          equipmentList={equipmentList}
          onClose={() => setShowRepairModal(false)}
          onSaved={() => {
            setShowRepairModal(false);
            loadAllData();
          }}
        />
      )}

      {respondingFeedback && (
        <FeedbackResponseModal
          feedback={respondingFeedback}
          onClose={() => setRespondingFeedback(null)}
          onSaved={() => {
            setRespondingFeedback(null);
            loadAllData();
          }}
        />
      )}
    </div>
  );
};

export default FacilityResolutionPage;
