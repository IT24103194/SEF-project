import React, { useState } from 'react';
import { X, AlertTriangle, ArrowRight, CheckCircle2 } from 'lucide-react';
import inventoryApi from '../../services/inventoryApi';

export const StockAdjustmentModal = ({ item, isOpen, onClose, onSuccess }) => {
  const [quantityChange, setQuantityChange] = useState('');
  const [movementType, setMovementType] = useState('Adjustment');
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  if (!isOpen || !item) return null;

  const currentStock = item.quantityInStock;
  const numChange = parseInt(quantityChange, 10) || 0;
  const projectedStock = currentStock + numChange;
  const isNegative = projectedStock < 0;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (numChange === 0) {
      setError('Quantity change must be non-zero.');
      return;
    }
    if (isNegative) {
      setError('Business rule violation: Stock balance cannot become negative.');
      return;
    }
    if (!reason.trim()) {
      setError('Reason is mandatory for auditing stock movements.');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      await inventoryApi.adjustStock(item.id, {
        quantityChange: numChange,
        movementType,
        reason: reason.trim(),
      });
      onSuccess?.();
      onClose();
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Failed to adjust stock.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3 style={{ fontSize: '1.15rem', fontWeight: 700 }}>Adjust Stock Level</h3>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
              {item.productName} ({item.productSKU})
            </p>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div className="alert alert-error">
                <AlertTriangle size={18} />
                <span>{error}</span>
              </div>
            )}

            {/* Current vs Projected preview card */}
            <div style={{
              background: 'rgba(0,0,0,0.25)',
              padding: '1rem',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--border-subtle)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-around',
              marginBottom: '1.25rem'
            }}>
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Current Stock</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 800 }}>{currentStock}</div>
              </div>
              <ArrowRight size={20} color="var(--text-muted)" />
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Adjustment</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 800, color: numChange >= 0 ? 'var(--accent-emerald)' : 'var(--accent-rose)' }}>
                  {numChange > 0 ? `+${numChange}` : numChange}
                </div>
              </div>
              <ArrowRight size={20} color="var(--text-muted)" />
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>New Stock</div>
                <div style={{
                  fontSize: '1.5rem',
                  fontWeight: 800,
                  color: isNegative ? 'var(--accent-rose)' : projectedStock <= item.reorderThreshold ? 'var(--accent-amber)' : 'var(--text-primary)'
                }}>
                  {projectedStock}
                </div>
              </div>
            </div>

            {isNegative && (
              <div style={{ color: 'var(--accent-rose)', fontSize: '0.8rem', marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <AlertTriangle size={14} /> Stock cannot drop below zero.
              </div>
            )}

            <div className="form-group">
              <label className="form-label">Movement Type</label>
              <select
                className="form-select"
                value={movementType}
                onChange={(e) => setMovementType(e.target.value)}
              >
                <option value="Adjustment">Inventory Adjustment (Cycle Count)</option>
                <option value="Restock">Restock / Direct Delivery (+)</option>
                <option value="Sale">Direct Sale / Consumption (-)</option>
                <option value="Waste">Damaged / Expired / Waste (-)</option>
                <option value="Return">Supplier Return (-)</option>
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">Quantity Change (+ for addition, - for deduction)</label>
              <input
                type="number"
                className="form-input"
                placeholder="e.g. 10 or -5"
                value={quantityChange}
                onChange={(e) => setQuantityChange(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">Audit Reason</label>
              <textarea
                className="form-textarea"
                rows={3}
                placeholder="State the operational reason for this adjustment..."
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading || isNegative || numChange === 0}>
              {loading ? 'Processing...' : 'Apply Stock Adjustment'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default StockAdjustmentModal;
