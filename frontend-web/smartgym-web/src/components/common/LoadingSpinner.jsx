import React from 'react';
import { Loader2 } from 'lucide-react';

export const LoadingSpinner = ({ message = 'Loading data...', size = 32 }) => {
  return (
    <div style={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '3rem 1.5rem',
      gap: '0.75rem',
      color: 'var(--text-secondary)',
    }}>
      <Loader2
        size={size}
        color="var(--primary)"
        style={{ animation: 'spin 1s linear infinite' }}
      />
      {message && <span style={{ fontSize: '0.875rem' }}>{message}</span>}
      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
};

export default LoadingSpinner;
