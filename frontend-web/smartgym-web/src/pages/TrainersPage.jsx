import React, { useState, useEffect } from 'react';
import { trainersApi } from '../services/trainersApi';
import PageHeader from '../components/common/PageHeader';
import SearchBar from '../components/common/SearchBar';
import Pagination from '../components/common/Pagination';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Dumbbell, Mail, Phone, Calendar, Award, ShieldCheck } from 'lucide-react';

export const TrainersPage = () => {
  const [trainers, setTrainers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  const fetchTrainers = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await trainersApi.getTrainers({
        search: search.trim() || undefined,
        page: pageNumber,
        pageSize,
      });
      setTrainers(data.items || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load trainers:', err);
      setError('Unable to load trainers list.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTrainers();
  }, [pageNumber, pageSize]);

  const handleSearch = (e) => {
    e?.preventDefault();
    setPageNumber(1);
    fetchTrainers();
  };

  return (
    <div>
      <PageHeader
        title="Trainer Roster"
        subtitle="Manage SmartGym certified personal instructors, specialties and schedules"
        icon={Dumbbell}
        badge={`${totalCount} Certified`}
        actions={
          <button onClick={fetchTrainers} className="btn btn-secondary">
            Refresh
          </button>
        }
      />

      <div className="glass-card" style={{ padding: '1rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center' }}>
        <form onSubmit={handleSearch} style={{ display: 'flex', gap: '0.75rem', flex: 1, maxWidth: 400 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search by instructor name..."
            onClear={() => {
              setSearch('');
              setPageNumber(1);
              fetchTrainers();
            }}
          />
          <button type="submit" className="btn btn-primary" style={{ padding: '0.5rem 1rem' }}>
            Search
          </button>
        </form>
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading trainer roster..." />
      ) : trainers.length === 0 ? (
        <EmptyState
          icon={Dumbbell}
          title="No trainers found"
          description={search ? `No trainers matched "${search}"` : 'No trainers currently active.'}
        />
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1.25rem' }}>
          {trainers.map((t) => (
            <div key={t.id} className="glass-card" style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '0.75rem' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.85rem' }}>
                  <div style={{
                    width: 48,
                    height: 48,
                    borderRadius: 'var(--radius-md)',
                    background: 'linear-gradient(135deg, var(--primary), var(--accent-emerald))',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontWeight: 800,
                    fontSize: '1.1rem',
                    color: '#ffffff',
                  }}>
                    {(t.firstName?.[0] || 'T').toUpperCase()}
                  </div>
                  <div>
                    <h3 style={{ fontSize: '1.05rem', fontWeight: 700, color: '#ffffff' }}>
                      {t.fullName || t.name || `${t.firstName || ''} ${t.lastName || ''}`.trim()}
                    </h3>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', marginTop: '0.2rem' }}>
                      <span className="badge badge-info" style={{ fontSize: '0.7rem' }}>
                        {t.role || 'Trainer'}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', background: 'rgba(255,255,255,0.02)', padding: '0.75rem', borderRadius: 'var(--radius-sm)' }}>
                <div style={{ fontWeight: 600, color: 'var(--accent-cyan)', marginBottom: '0.25rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <Award size={14} /> {t.specialization}
                </div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                  {t.bio}
                </div>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem', fontSize: '0.825rem', color: 'var(--text-secondary)' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <Mail size={14} color="var(--primary)" /> {t.email}
                </div>
                {t.phoneNumber && (
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Phone size={14} color="var(--accent-cyan)" /> {t.phoneNumber}
                  </div>
                )}
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <Calendar size={14} color="var(--accent-amber)" /> Active Classes: <strong style={{ color: '#ffffff' }}>{t.activeClassesCount}</strong>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <Pagination
          pageNumber={pageNumber}
          pageSize={pageSize}
          totalCount={totalCount}
          totalPages={totalPages}
          onPageChange={setPageNumber}
          onPageSizeChange={setPageSize}
        />
      )}
    </div>
  );
};

export default TrainersPage;
