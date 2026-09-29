import React, { useState, useEffect } from 'react';
import { membershipApi } from '../services/membershipApi';
import PageHeader from '../components/common/PageHeader';
import SearchBar from '../components/common/SearchBar';
import Pagination from '../components/common/Pagination';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Users, Mail, Phone, Calendar, UserCheck, Shield } from 'lucide-react';

export const MembersPage = () => {
  const [members, setMembers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  const fetchMembers = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await membershipApi.getMembers({
        search: search.trim() || undefined,
        page: pageNumber,
        pageSize,
      });
      setMembers(data.items || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load members:', err);
      setError('Unable to load members list. Please verify your permissions.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMembers();
  }, [pageNumber, pageSize]);

  const handleSearchSubmit = (e) => {
    e?.preventDefault();
    setPageNumber(1);
    fetchMembers();
  };

  return (
    <div>
      <PageHeader
        title="Member Directory"
        subtitle="Manage SmartGym members, profiles, contacts, and active memberships"
        icon={Users}
        badge={`${totalCount} Registered`}
        actions={
          <button onClick={fetchMembers} className="btn btn-secondary">
            Refresh
          </button>
        }
      />

      {/* Filter / Search Bar */}
      <div className="glass-card" style={{ padding: '1rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.75rem', flex: 1, maxWidth: 400 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search by name, email, or phone..."
            onClear={() => {
              setSearch('');
              setPageNumber(1);
              fetchMembers();
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
        <LoadingSpinner message="Loading member directory..." />
      ) : members.length === 0 ? (
        <EmptyState
          icon={Users}
          title="No members found"
          description={search ? `No members matched "${search}"` : 'No members currently registered.'}
          action={
            search && (
              <button
                onClick={() => {
                  setSearch('');
                  fetchMembers();
                }}
                className="btn btn-secondary"
              >
                Clear Search
              </button>
            )
          }
        />
      ) : (
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Member</th>
                <th>Contact Details</th>
                <th>Emergency Contact</th>
                <th>Active Plan</th>
                <th>Status</th>
                <th>Joined</th>
              </tr>
            </thead>
            <tbody>
              {members.map((m) => (
                <tr key={m.id}>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                      <div style={{
                        width: 36,
                        height: 36,
                        borderRadius: '50%',
                        background: 'linear-gradient(135deg, var(--primary), var(--accent-cyan))',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontWeight: 700,
                        fontSize: '0.9rem',
                        color: '#ffffff',
                      }}>
                        {(m.firstName?.[0] || 'M').toUpperCase()}
                      </div>
                      <div>
                        <div style={{ fontWeight: 600, color: '#ffffff' }}>
                          {m.firstName} {m.lastName}
                        </div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                          ID: {m.id.substring(0, 8)}...
                        </div>
                      </div>
                    </div>
                  </td>
                  <td>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.2rem', fontSize: '0.8rem' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', color: 'var(--text-secondary)' }}>
                        <Mail size={13} color="var(--primary)" /> {m.email}
                      </div>
                      {m.phoneNumber && (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', color: 'var(--text-muted)' }}>
                          <Phone size={13} color="var(--accent-cyan)" /> {m.phoneNumber}
                        </div>
                      )}
                    </div>
                  </td>
                  <td>
                    <div style={{ fontSize: '0.85rem' }}>
                      {m.emergencyContactName ? (
                        <>
                          <div style={{ color: 'var(--text-primary)' }}>{m.emergencyContactName}</div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{m.emergencyContactPhone}</div>
                        </>
                      ) : (
                        <span style={{ color: 'var(--text-muted)' }}>Not specified</span>
                      )}
                    </div>
                  </td>
                  <td>
                    {m.activePlanName ? (
                      <span className="badge badge-info" style={{ fontSize: '0.75rem' }}>
                        {m.activePlanName}
                      </span>
                    ) : (
                      <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>No Active Plan</span>
                    )}
                  </td>
                  <td>
                    <span className={`badge ${m.membershipStatus === 'Active' ? 'badge-success' : 'badge-warning'}`}>
                      {m.membershipStatus || 'Inactive'}
                    </span>
                  </td>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                      <Calendar size={13} />
                      {m.createdAt ? new Date(m.createdAt).toLocaleDateString() : 'N/A'}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <Pagination
            pageNumber={pageNumber}
            pageSize={pageSize}
            totalCount={totalCount}
            totalPages={totalPages}
            onPageChange={setPageNumber}
            onPageSizeChange={(newSize) => {
              setPageSize(newSize);
              setPageNumber(1);
            }}
          />
        </div>
      )}
    </div>
  );
};

export default MembersPage;
