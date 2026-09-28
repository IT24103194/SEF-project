import React, { useState } from 'react';
import { X, ShoppingCart, CheckCircle2, AlertTriangle, Calendar } from 'lucide-react';
import inventoryApi from '../../services/inventoryApi';

export const ReorderModal = ({ item, isOpen, onClose, onSuccess }) => {
  const [quantity, setQuantity] = useState(25);
  const [expectedDate, setExpectedDate] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [orderResult, setOrderResult] = useState(null);

  if (!isOpen || !item) return null;

  const unitCost = item.costPrice || 0;
  const numQty = parseInt(quantity, 10) || 0;
  const totalCost = (unitCost * numQty).toFixed(2);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (numQty <= 0) {
      setError('Reorder quantity must be at least 1.');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const res = await inventoryApi.reorderStock(item.id, {
        quantity: numQty,
        expectedDeliveryDate: expectedDate ? new Date(expectedDate).toISOString() : null,
        notes: notes.trim() || undefined,
      });
      setOrderResult(res);
      onSuccess?.();
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Failed to place reorder.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  const handleDone = () => {
    setOrderResult(null);
    onClose();
  };

  return (
    <div className="modal-overlay" onClick={handleDone}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3 style={{ fontSize: '1.15rem', fontWeight: 700 }}>Supplier Reorder Request</h3>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
              Purchase Order for {item.productName}
            </p>
          </div>
          <button onClick={handleDone} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </div>

        {orderResult ? (
          <div className="modal-body" style={{ textAlign: 'center', padding: '2rem 1.5rem' }}>
            <div style={{
              width: 56,
              height: 56,
              borderRadius: '50%',
              background: 'rgba(16, 185, 129, 0.15)',
              color: 'var(--accent-emerald)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 1.25rem'
            }}>
              <CheckCircle2 size={32} />
            </div>
            <h4 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '0.5rem' }}>
              Purchase Order Submitted!
            </h4>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: '1.5rem' }}>
              PO <strong>#{orderResult.orderNumber}</strong> has been created for supplier <strong>{orderResult.supplierName}</strong>.
            </p>
            <div style={{
              background: 'rgba(0,0,0,0.25)',
              padding: '1rem',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--border-subtle)',
              textAlign: 'left',
              fontSize: '0.85rem',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.5rem'
            }}>
              <div><strong>Quantity:</strong> {orderResult.quantity} units</div>
              <div><strong>Unit Cost:</strong> ${orderResult.unitCost?.toFixed(2)}</div>
              <div><strong>Total Order Cost:</strong> ${orderResult.totalCost?.toFixed(2)}</div>
              <div><strong>Status:</strong> <span className="badge badge-warning">{orderResult.status}</span></div>
            </div>
            <button className="btn btn-primary" style={{ marginTop: '1.5rem', width: '100%' }} onClick={handleDone}>
              Done
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="modal-body">
              {error && (
                <div className="alert alert-error">
                  <AlertTriangle size={18} />
                  <span>{error}</span>
                </div>
              )}

              <div style={{
                background: 'rgba(99, 102, 241, 0.08)',
                border: '1px solid rgba(99, 102, 241, 0.25)',
                padding: '1rem',
                borderRadius: 'var(--radius-md)',
                marginBottom: '1.25rem'
              }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem', fontSize: '0.85rem' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Designated Supplier:</span>
                  <strong style={{ color: 'var(--accent-cyan)' }}>{item.supplierName}</strong>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem', fontSize: '0.85rem' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Current Stock Level:</span>
                  <strong>{item.quantityInStock} units</strong>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Unit Cost Price:</span>
                  <strong>${unitCost.toFixed(2)}</strong>
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Reorder Quantity</label>
                <input
                  type="number"
                  min="1"
                  className="form-input"
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                  required
                />
              </div>

              <div className="form-group">
                <label className="form-label">Expected Delivery Date (Optional)</label>
                <input
                  type="date"
                  className="form-input"
                  value={expectedDate}
                  onChange={(e) => setExpectedDate(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Order Notes / Instructions</label>
                <textarea
                  className="form-textarea"
                  rows={2}
                  placeholder="e.g. Rush replenishment for gym front desk..."
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                />
              </div>

              <div style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                paddingTop: '0.75rem',
                borderTop: '1px solid var(--border-subtle)',
                marginTop: '1rem'
              }}>
                <span style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>Estimated Order Total:</span>
                <span style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--accent-emerald)' }}>
                  ${totalCost}
                </span>
              </div>
            </div>

            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" disabled={loading || numQty <= 0}>
                <ShoppingCart size={16} />
                {loading ? 'Submitting PO...' : 'Create Purchase Order'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};

export default ReorderModal;
