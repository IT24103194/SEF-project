import React, { useState, useEffect } from 'react';
import { membershipApi } from '../services/membershipApi';
import PageHeader from '../components/common/PageHeader';
import Pagination from '../components/common/Pagination';
import RenewalModal from '../components/membership/RenewalModal';
import ConfirmationModal from '../components/common/ConfirmationModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Award, Calendar, RotateCcw, XCircle, CheckCircle } from 'lucide-react';

export const MembershipsListPage = () => {
  const [memberships, setMemberships] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Renewal & Cancellation states
  const [renewalMembership, setRenewalMembership] = useState(null);
  const [cancelTargetId, setCancelTargetId] = useState(null);
  const [isCancelling, setIsCancelling] = useState(false);
  const [plans, setPlans] = useState([]);

  const fetchMemberships = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await membershipApi.getMemberships({
        status: statusFilter || undefined,
        page: pageNumber,
        pageSize,
      });
      setMemberships(data.items || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load memberships:', err);
      setError('Unable to load memberships list.');
    } finally {
      setLoading(false);
    }
  };

  const loadPlans = async () => {
    try {
      const data = await membershipApi.getPlans(false);
      setPlans(data || []);
    } catch (err) {
      console.error('Failed to load plans for renewal:', err);
    }
  };

  useEffect(() => {
    fetchMemberships();
  }, [statusFilter, pageNumber, pageSize]);

  useEffect(() => {
    loadPlans();
  }, []);

  const handleRenew = async (renewData) => {
    await membershipApi.renewMembership(renewData);
    setRenewalMembership(null);
    fetchMemberships();
  };

  const handleCancelConfirm = async () => {
    if (!cancelTargetId) return;
    setIsCancelling(true);
    try {
      await membershipApi.cancelMembership(cancelTargetId, 'Administrative cancellation requested');
      setCancelTargetId(null);
      fetchMemberships();
    } catch (err) {
      console.error('Failed to cancel membership:', err);
      setError(err.response?.data?.detail || 'Failed to cancel membership.');
    } finally {
      setIsCancelling(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Member Subscriptions"
        subtitle="Manage active enrollments, validity periods, renewals, and cancellations"
        icon={Award}
        badge={`${totalCount} Subscriptions`}
        actions={
          <button onClick={fetchMemberships} className="btn btn-secondary">
            Refresh
          </button>
        }
      />

      {/* Filter Tabs */}
      <div className="glass-card" style={{ padding: '0.75rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600, marginRight: '0.5rem' }}>Status:</span>
        {['', 'Active', 'Expired', 'Cancelled'].map((status) => (
          <button
            key={status}
            onClick={() => {
              setStatusFilter(status);
              setPageNumber(1);
            }}
            className={`btn ${statusFilter === status ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.4rem 0.85rem', fontSize: '0.8rem' }}
          >
            {status || 'All Statuses'}
          </button>
        ))}
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading subscriptions..." />
      ) : memberships.length === 0 ? (
        <EmptyState
          icon={Award}
          title="No memberships found"
          description="There are no memberships recorded under the selected status."
        />
      ) : (
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Member</th>
                <th>Plan Name</th>
                <th>Validity Window</th>
                <th>Price Paid</th>
                <th>Auto-Renew</th>
                <th>Status</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {memberships.map((m) => {
                const isExpired = new Date(m.endDate) < new Date();
                return (
                  <tr key={m.id}>
                    <td>
                      <div style={{ fontWeight: 600, color: '#ffffff' }}>
                        {m.memberName || 'Member'}
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                        {m.memberEmail || m.memberId?.substring(0, 8)}
                      </div>
                    </td>
                    <td>
                      <span className="badge badge-info" style={{ fontSize: '0.75rem' }}>
                        {m.planName}
                      </span>
                    </td>
                    <td>
                      <div style={{ fontSize: '0.825rem', display: 'flex', flexDirection: 'column', gap: '0.15rem' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>
                          From: {new Date(m.startDate).toLocaleDateString()}
                        </span>
                        <span style={{ color: isExpired ? 'var(--accent-rose)' : 'var(--accent-emerald)', fontWeight: 600 }}>
                          To: {new Date(m.endDate).toLocaleDateString()}
                        </span>
                      </div>
                    </td>
                    <td>
                      <strong style={{ color: '#ffffff' }}>${m.pricePaid?.toFixed(2)}</strong>
                    </td>
                    <td>
                      {m.autoRenew ? (
                        <span style={{ color: 'var(--accent-emerald)', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                          <CheckCircle size={14} /> Enabled
                        </span>
                      ) : (
                        <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>Manual</span>
                      )}
                    </td>
                    <td>
                      <span className={`badge ${
                        m.status === 'Active' && !isExpired
                          ? 'badge-success'
                          : m.status === 'Cancelled'
                          ? 'badge-danger'
                          : 'badge-warning'
                      }`}>
                        {m.status}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                        <button
                          onClick={() => setRenewalMembership(m)}
                          className="btn btn-secondary"
                          style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                          title="Renew Membership"
                        >
                          <RotateCcw size={13} /> Renew
                        </button>
                        {m.status === 'Active' && (
                          <button
                            onClick={() => setCancelTargetId(m.id)}
                            className="btn btn-danger"
                            style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                            title="Cancel Membership"
                          >
                            <XCircle size={13} /> Cancel
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
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

      {renewalMembership && (
        <RenewalModal
          membership={renewalMembership}
          plans={plans}
          onClose={() => setRenewalMembership(null)}
          onRenew={handleRenew}
        />
      )}

      <ConfirmationModal
        isOpen={!!cancelTargetId}
        title="Cancel Member Subscription"
        message="Are you sure you want to cancel this membership? The member will immediately lose access to booking classes."
        confirmText="Cancel Membership"
        isDestructive={true}
        isLoading={isCancelling}
        onConfirm={handleCancelConfirm}
        onCancel={() => setCancelTargetId(null)}
      />
    </div>
  );
};

export default MembershipsListPage;
