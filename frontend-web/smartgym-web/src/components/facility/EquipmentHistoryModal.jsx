import React, { useState, useEffect } from 'react';
import { X, Wrench, Clock, DollarSign, AlertCircle, CheckCircle } from 'lucide-react';
import facilityApi from '../../services/facilityApi';

export const EquipmentHistoryModal = ({ equipmentId, onClose }) => {
  const [history, setHistory] = useState(null);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (equipmentId) {
      loadHistory();
    }
  }, [equipmentId]);

  const loadHistory = async () => {
    try {
      setLoading(true);
      const data = await facilityApi.getEquipmentHistory(equipmentId);
      setHistory(data);
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to load maintenance history.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(5px)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '1.5rem'
    }}>
      <div className="glass-card" style={{
        maxWidth: '800px', width: '100%', maxHeight: '90vh', overflowY: 'auto',
        borderRadius: '16px', padding: '2rem', border: '1px solid rgba(255,255,255,0.1)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h3 style={{ fontSize: '1.35rem', fontWeight: 800, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Wrench size={22} color="var(--primary)" />
            Equipment Maintenance History
          </h3>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
            <X size={22} />
          </button>
        </div>

        {errorMsg && (
          <div style={{
            backgroundColor: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)',
            color: '#F87171', padding: '0.75rem 1rem', borderRadius: '8px', marginBottom: '1.25rem', fontSize: '0.875rem'
          }}>
            {errorMsg}
          </div>
        )}

        {loading ? (
          <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)' }}>
            Loading equipment maintenance logs...
          </div>
        ) : history ? (
          <div>
            {/* Summary Cards */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
              <div style={{ padding: '1rem', backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: '10px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Equipment Asset</div>
                <div style={{ fontSize: '1.1rem', fontWeight: 700, marginTop: '4px' }}>{history.equipment.name}</div>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>SN: {history.equipment.serialNumber}</div>
              </div>

              <div style={{ padding: '1rem', backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: '10px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <AlertCircle size={14} color="#F59E0B" /> Lifetime Issues
                </div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, marginTop: '4px', color: '#F59E0B' }}>
                  {history.totalIssuesCount}
                </div>
              </div>

              <div style={{ padding: '1rem', backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: '10px' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <DollarSign size={14} color="#10B981" /> Total Repair Spend
                </div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, marginTop: '4px', color: '#10B981' }}>
                  ${history.totalRepairCost.toFixed(2)}
                </div>
              </div>
            </div>

            {/* Repair Orders */}
            <div style={{ marginBottom: '1.5rem' }}>
              <h4 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '0.75rem' }}>Repair Orders</h4>
              {history.repairOrders && history.repairOrders.length > 0 ? (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                  {history.repairOrders.map((ro) => (
                    <div key={ro.id} style={{
                      padding: '1rem', backgroundColor: 'rgba(255,255,255,0.02)',
                      borderRadius: '8px', border: '1px solid rgba(255,255,255,0.06)'
                    }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                        <span style={{ fontWeight: 700 }}>{ro.orderNumber}</span>
                        <span style={{ color: 'var(--primary)', fontWeight: 700 }}>
                          ${(ro.actualCost ?? ro.estimatedCost).toFixed(2)}
                        </span>
                      </div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', display: 'flex', gap: '1rem' }}>
                        <span>Status: <strong>{ro.statusName}</strong></span>
                        {ro.technicianName && <span>Tech: {ro.technicianName}</span>}
                        <span>Date: {new Date(ro.createdAt).toLocaleDateString()}</span>
                      </div>
                      {ro.items && ro.items.length > 0 && (
                        <div style={{ marginTop: '0.5rem', fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                          Parts: {ro.items.map((it) => `${it.partName} (x${it.quantity})`).join(', ')}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>No repair orders recorded for this asset.</p>
              )}
            </div>

            {/* Facility Issues Log */}
            <div>
              <h4 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '0.75rem' }}>Reported Problem Logs</h4>
              {history.issues && history.issues.length > 0 ? (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                  {history.issues.map((iss) => (
                    <div key={iss.id} style={{
                      padding: '0.75rem 1rem', backgroundColor: 'rgba(255,255,255,0.02)',
                      borderRadius: '8px', fontSize: '0.85rem', borderLeft: '3px solid var(--primary)'
                    }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2px' }}>
                        <span style={{ fontWeight: 600 }}>{iss.title}</span>
                        <span style={{ color: 'var(--text-secondary)', fontSize: '0.75rem' }}>
                          {new Date(iss.reportedAt).toLocaleDateString()}
                        </span>
                      </div>
                      {iss.resolutionNotes && (
                        <div style={{ color: '#10B981', fontSize: '0.8rem', marginTop: '4px' }}>
                          ✓ Resolved: {iss.resolutionNotes}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>No issues reported for this equipment.</p>
              )}
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
};

export default EquipmentHistoryModal;
