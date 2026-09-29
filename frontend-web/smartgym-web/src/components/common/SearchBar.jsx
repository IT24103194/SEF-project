import React from 'react';
import { Search, X } from 'lucide-react';

export const SearchBar = ({
  value,
  onChange,
  placeholder = 'Search records...',
  onClear,
}) => {
  return (
    <div style={{ position: 'relative', width: '100%', maxWidth: 360 }}>
      <Search
        size={16}
        color="var(--text-muted)"
        style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)' }}
      />
      <input
        type="text"
        className="form-input"
        style={{ paddingLeft: '2.25rem', paddingRight: value ? '2rem' : '0.85rem' }}
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
      {value && (
        <button
          onClick={onClear || (() => onChange(''))}
          type="button"
          style={{
            position: 'absolute',
            right: 8,
            top: '50%',
            transform: 'translateY(-50%)',
            background: 'transparent',
            border: 'none',
            color: 'var(--text-muted)',
            cursor: 'pointer',
            padding: 4,
          }}
        >
          <X size={14} />
        </button>
      )}
    </div>
  );
};

export default SearchBar;
