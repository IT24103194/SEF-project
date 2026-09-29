import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

export const Pagination = ({
  pageNumber = 1,
  pageSize = 20,
  totalCount = 0,
  totalPages = 1,
  onPageChange,
  onPageSizeChange,
}) => {
  const fromIndex = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const toIndex = Math.min(pageNumber * pageSize, totalCount);

  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      flexWrap: 'wrap',
      gap: '1rem',
      padding: '1rem 0.5rem',
      fontSize: '0.85rem',
      color: 'var(--text-secondary)',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
        <span>
          Showing <strong style={{ color: '#ffffff' }}>{fromIndex}</strong> to{' '}
          <strong style={{ color: '#ffffff' }}>{toIndex}</strong> of{' '}
          <strong style={{ color: '#ffffff' }}>{totalCount}</strong> entries
        </span>
        {onPageSizeChange && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
            <span>Per page:</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="form-select"
              style={{ width: 'auto', padding: '0.25rem 0.6rem', fontSize: '0.8rem' }}
            >
              <option value={10}>10</option>
              <option value={20}>20</option>
              <option value={50}>50</option>
            </select>
          </div>
        )}
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
        <button
          onClick={() => onPageChange(pageNumber - 1)}
          disabled={pageNumber <= 1}
          className="btn btn-secondary"
          style={{ padding: '0.4rem 0.75rem', opacity: pageNumber <= 1 ? 0.4 : 1 }}
        >
          <ChevronLeft size={16} /> Prev
        </button>

        <span style={{ padding: '0 0.5rem', fontWeight: 600, color: 'var(--text-primary)' }}>
          Page {pageNumber} of {Math.max(1, totalPages)}
        </span>

        <button
          onClick={() => onPageChange(pageNumber + 1)}
          disabled={pageNumber >= totalPages}
          className="btn btn-secondary"
          style={{ padding: '0.4rem 0.75rem', opacity: pageNumber >= totalPages ? 0.4 : 1 }}
        >
          Next <ChevronRight size={16} />
        </button>
      </div>
    </div>
  );
};

export default Pagination;
