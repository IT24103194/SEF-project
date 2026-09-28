import React, { useState } from 'react';
import { X, Plus, Trash2, ShieldAlert, Save } from 'lucide-react';
import facilityApi from '../../services/facilityApi';

export const RepairOrderModal = ({ issues, equipmentList, onClose, onSaved }) => {
  const [issueId, setIssueId] = useState(issues[0]?.id || '');
  const [equipmentId, setEquipmentId] = useState(equipmentList[0]?.id || '');
  const [technicianName, setTechnicianName] = useState('');
  const [items, setItems] = useState([
    { partName: '', partNumber: '', quantity: 1, unitCost: 0 }
  ]);
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const calculateTotal = () => {
    return items.reduce((sum, item) => sum + (Number(item.quantity) * Number(item.unitCost) || 0), 0);
  };

  const totalCost = calculateTotal();
  const isHighValue = totalCost >= 500; // $500 / Rs. 25,000 threshold

  const addItem = () => {
    setItems([...items, { partName: '', partNumber: '', quantity: 1, unitCost: 0 }]);
  };

  const removeItem = (index) => {
    if (items.length === 1) return;
    setItems(items.filter((_, i) => i !== index));
  };

  const updateItem = (index, field, value) => {
    const next = [...items];
    next[index][field] = value;
    setItems(next);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!issueId || !equipmentId) {
      setErrorMsg('Please select an issue and equipment.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      const payload = {
        issueId,
        equipmentId,
        estimatedCost: totalCost,
        technicianName: technicianName.trim() || null,
        items: items
          .filter((it) => it.partName.trim())
          .map((it) => ({
            partName: it.partName.trim(),
            partNumber: it.partNumber?.trim() || null,
            quantity: parseInt(it.quantity, 10) || 1,
            unitCost: parseFloat(it.unitCost) || 0,
          })),
      };

      await facilityApi.createRepairOrder(payload);
      onSaved();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to create repair order.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(5px)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '1.5rem'
    }}>
      <div className="glass-card" style={{
        maxWidth: '750px', width: '100%', maxHeight: '90vh', overflowY: 'auto',
        borderRadius: '16px', padding: '2rem', border: '1px solid rgba(255,255,255,0.1)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h3 style={{ fontSize: '1.35rem', fontWeight: 800 }}>Create Equipment Repair Order</h3>
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

        {isHighValue && (
          <div style={{
            padding: '1rem', backgroundColor: 'rgba(245, 158, 11, 0.12)',
            border: '1px solid rgba(245, 158, 11, 0.3)', borderRadius: '10px',
            marginBottom: '1.25rem', display: 'flex', alignItems: 'center', gap: '12px'
          }}>
            <ShieldAlert size={26} color="#F59E0B" />
            <div>
              <div style={{ fontWeight: 700, color: '#F59E0B', fontSize: '0.9rem' }}>
                High-Value Financial Authorization Triggered
              </div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                Estimated total exceeds the $500 (Rs. 25,000) threshold. This order will automatically be routed to manager review before parts purchase.
              </div>
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div>
              <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
                Associated Facility Issue *
              </label>
              <select
                required
                value={issueId}
                onChange={(e) => setIssueId(e.target.value)}
                style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: '8px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.15)', color: 'white' }}
              >
                {issues.map((iss) => (
                  <option key={iss.id} value={iss.id}>
                    {iss.title} ({iss.statusName})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
                Equipment Under Repair *
              </label>
              <select
                required
                value={equipmentId}
                onChange={(e) => setEquipmentId(e.target.value)}
                style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: '8px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.15)', color: 'white' }}
              >
                {equipmentList.map((eq) => (
                  <option key={eq.id} value={eq.id}>
                    {eq.name} ({eq.serialNumber})
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
              Assigned Technician / Service Agency
            </label>
            <input
              type="text"
              value={technicianName}
              onChange={(e) => setTechnicianName(e.target.value)}
              placeholder="e.g. Apex Gym Spares & Service (Kamal Gunaratne)"
              style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: '8px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.15)', color: 'white' }}
            />
          </div>

          {/* Line Items */}
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
              <label style={{ fontSize: '0.85rem', fontWeight: 700, color: 'var(--text-primary)' }}>
                Replacement Parts & Labor Items
              </label>
              <button
                type="button"
                onClick={addItem}
                style={{
                  display: 'inline-flex', alignItems: 'center', gap: '4px', background: 'none',
                  border: 'none', color: 'var(--primary)', cursor: 'pointer', fontSize: '0.8rem', fontWeight: 600
                }}
              >
                <Plus size={14} /> Add Line Item
              </button>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              {items.map((item, idx) => (
                <div key={idx} style={{ display: 'grid', gridTemplateColumns: '3fr 2fr 1fr 1.5fr auto', gap: '0.5rem', alignItems: 'center' }}>
                  <input
                    type="text"
                    required
                    placeholder="Part or Service Name"
                    value={item.partName}
                    onChange={(e) => updateItem(idx, 'partName', e.target.value)}
                    style={{ padding: '0.55rem', borderRadius: '6px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.1)', color: 'white', fontSize: '0.85rem' }}
                  />
                  <input
                    type="text"
                    placeholder="Part # (Optional)"
                    value={item.partNumber}
                    onChange={(e) => updateItem(idx, 'partNumber', e.target.value)}
                    style={{ padding: '0.55rem', borderRadius: '6px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.1)', color: 'white', fontSize: '0.85rem' }}
                  />
                  <input
                    type="number"
                    min="1"
                    placeholder="Qty"
                    value={item.quantity}
                    onChange={(e) => updateItem(idx, 'quantity', e.target.value)}
                    style={{ padding: '0.55rem', borderRadius: '6px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.1)', color: 'white', fontSize: '0.85rem' }}
                  />
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    placeholder="Unit Cost ($)"
                    value={item.unitCost}
                    onChange={(e) => updateItem(idx, 'unitCost', e.target.value)}
                    style={{ padding: '0.55rem', borderRadius: '6px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.1)', color: 'white', fontSize: '0.85rem' }}
                  />
                  <button
                    type="button"
                    onClick={() => removeItem(idx)}
                    disabled={items.length === 1}
                    style={{ background: 'none', border: 'none', color: '#EF4444', cursor: 'pointer', padding: '4px' }}
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              ))}
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '0.75rem', fontSize: '1rem', fontWeight: 700 }}>
              Total Estimated Cost: <span style={{ color: 'var(--primary)', marginLeft: '8px' }}>${totalCost.toFixed(2)}</span>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1rem' }}>
            <button type="button" onClick={onClose} className="btn-secondary">
              Cancel
            </button>
            <button type="submit" disabled={submitting} className="btn-primary" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Save size={16} /> {submitting ? 'Creating Order...' : 'Generate Repair Order'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default RepairOrderModal;
