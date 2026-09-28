import React, { useEffect, useState } from 'react';
import { X, History, TrendingUp, TrendingDown, ArrowUpDown, User, Calendar } from 'lucide-react';
import inventoryApi from '../../services/inventoryApi';

export const StockHistoryModal = ({ item, isOpen, onClose }) => {
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (isOpen && item) {
      loadHistory();
    }
  }, [isOpen, item]);

  const loadHistory = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await inventoryApi.getStockHistory(item.id);
      setHistory(data);
    } catch (err) {
      setError('Failed to load stock movement history.');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen || !item) return null;

  const getMovementBadge = (type, qty) => {
    switch (type?.toLowerCase()) {
      case 'restock':
        return <span className="badge badge-success"><TrendingUp size={12} /> Restock (+{qty})</span>;
      case 'sale':
        return <span className="badge badge-danger"><TrendingDown size={12} /> Sale ({qty})</span>;
      case 'waste':
        return <span className="badge badge-warning"><TrendingDown size={12} /> Waste ({qty})</span>;
      case 'return':
        return <span className="badge badge-info"><ArrowUpDown size={12} /> Return ({qty})</span>;
      default:
        return <span className="badge badge-info"><ArrowUpDown size={12} /> Adjust ({qty > 0 ? `+${qty}` : qty})</span>;
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" style={{ maxWidth: 650 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3 style={{ fontSize: '1.15rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <History size={18} color="var(--accent-cyan)" />
              Stock Movement History
            </h3>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
              {item.productName} ({item.productSKU}) — Current Stock: <strong>{item.quantityInStock}</strong>
            </p>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </div>

        <div className="modal-body">
          {loading && (
            <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)' }}>
              Loading movement timeline...
            </div>
          )}

          {error && (
            <div className="alert alert-error">{error}</div>
          )}

          {!loading && !error && history.length === 0 && (
            <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)' }}>
              No recorded stock movements for this item yet.
            </div>
          )}

          {!loading && !error && history.length > 0 && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
              {history.map((mov) => (
                <div
                  key={mov.id}
                  style={{
                    background: 'rgba(0, 0, 0, 0.2)',
                    border: '1px solid var(--border-subtle)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '0.85rem 1rem',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '0.4rem'
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      {getMovementBadge(mov.movementType, mov.quantityChange)}
                      <span style={{ fontSize: '0.85rem', fontWeight: 600 }}>{mov.reason}</span>
                    </div>
                    <div style={{
                      fontSize: '1rem',
                      fontWeight: 700,
                      color: mov.quantityChange > 0 ? 'var(--accent-emerald)' : 'var(--accent-rose)'
                    }}>
                      {mov.quantityChange > 0 ? `+${mov.quantityChange}` : mov.quantityChange}
                    </div>
                  </div>

                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                      <User size={12} /> {mov.performedByUserName || 'System / Batch'}
                    </span>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                      <Calendar size={12} /> {new Date(mov.createdAt).toLocaleString()}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button className="btn btn-secondary" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default StockHistoryModal;
