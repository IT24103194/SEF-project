import React, { useState, useEffect } from 'react';
import { classesApi } from '../services/classesApi';
import PageHeader from '../components/common/PageHeader';
import Pagination from '../components/common/Pagination';
import ConfirmationModal from '../components/common/ConfirmationModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { CalendarCheck, Clock, XCircle, CheckCircle, MapPin } from 'lucide-react';

export const BookingsPage = () => {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Cancellation
  const [cancelTargetId, setCancelTargetId] = useState(null);
  const [isCancelling, setIsCancelling] = useState(false);

  const fetchBookings = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await classesApi.getBookings({
        status: statusFilter || undefined,
        page: pageNumber,
        pageSize,
      });
      setBookings(data.items || data || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load bookings:', err);
      setError('Unable to load bookings.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchBookings();
  }, [statusFilter, pageNumber, pageSize]);

  const handleCancelConfirm = async () => {
    if (!cancelTargetId) return;
    setIsCancelling(true);
    try {
      await classesApi.cancelBooking(cancelTargetId, { reason: 'Member or administrative cancellation' });
      setCancelTargetId(null);
      fetchBookings();
    } catch (err) {
      console.error('Failed to cancel booking:', err);
      setError('Failed to cancel booking. It may have already expired or been cancelled.');
    } finally {
      setIsCancelling(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Class Bookings"
        subtitle="Review member reservations, attendance receipts, and cancellation requests"
        icon={CalendarCheck}
        badge={`${totalCount} Reservations`}
        actions={
          <button onClick={fetchBookings} className="btn btn-secondary">
            Refresh
          </button>
        }
      />

      {/* Filter Tabs */}
      <div className="glass-card" style={{ padding: '0.75rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600, marginRight: '0.5rem' }}>Status:</span>
        {['', 'Confirmed', 'Attended', 'Cancelled'].map((status) => (
          <button
            key={status}
            onClick={() => {
              setStatusFilter(status);
              setPageNumber(1);
            }}
            className={`btn ${statusFilter === status ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.4rem 0.85rem', fontSize: '0.8rem' }}
          >
            {status || 'All Bookings'}
          </button>
        ))}
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading reservations..." />
      ) : bookings.length === 0 ? (
        <EmptyState
          icon={CalendarCheck}
          title="No bookings recorded"
          description="There are no class reservations matching the selected filter criteria."
        />
      ) : (
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Booking ID</th>
                <th>Member</th>
                <th>Class Session</th>
                <th>Reserved Time</th>
                <th>Booked On</th>
                <th>Status</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {bookings.map((b) => {
                const isCancelled = b.status === 'Cancelled';
                return (
                  <tr key={b.id}>
                    <td>
                      <span style={{ fontFamily: 'monospace', color: 'var(--text-muted)', fontSize: '0.8rem' }}>
                        #{b.id.substring(0, 8)}
                      </span>
                    </td>
                    <td>
                      <div style={{ fontWeight: 600, color: '#ffffff' }}>
                        {b.memberName || 'Member'}
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                        {b.memberEmail || b.memberId?.substring(0, 8)}
                      </div>
                    </td>
                    <td>
                      <div style={{ fontWeight: 600, color: 'var(--accent-cyan)' }}>
                        {b.className || 'Fitness Session'}
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                        Trainer: {b.trainerName || 'Staff'}
                      </div>
                    </td>
                    <td>
                      <div style={{ fontSize: '0.825rem', color: 'var(--text-secondary)' }}>
                        {b.classStartTime ? new Date(b.classStartTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' }) : 'N/A'}
                      </div>
                    </td>
                    <td>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                        {b.bookedAt ? new Date(b.bookedAt).toLocaleDateString() : 'N/A'}
                      </div>
                    </td>
                    <td>
                      <span className={`badge ${
                        b.status === 'Confirmed'
                          ? 'badge-success'
                          : b.status === 'Attended'
                          ? 'badge-info'
                          : 'badge-danger'
                      }`}>
                        {b.status}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      {!isCancelled && (
                        <button
                          onClick={() => setCancelTargetId(b.id)}
                          className="btn btn-danger"
                          style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                        >
                          <XCircle size={13} /> Cancel
                        </button>
                      )}
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

      <ConfirmationModal
        isOpen={!!cancelTargetId}
        title="Cancel Class Booking"
        message="Are you sure you want to cancel this reservation? The spot will be released back to the class capacity."
        confirmText="Cancel Reservation"
        isDestructive={true}
        isLoading={isCancelling}
        onConfirm={handleCancelConfirm}
        onCancel={() => setCancelTargetId(null)}
      />
    </div>
  );
};

export default BookingsPage;
