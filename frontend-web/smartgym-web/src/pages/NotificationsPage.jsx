import React, { useState, useEffect } from 'react';
import notificationsApi from '../services/notificationsApi';
import PageHeader from '../components/common/PageHeader';
import Pagination from '../components/common/Pagination';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Bell, CheckCheck, Filter, AlertCircle, Calendar, ShieldCheck, Wrench, RefreshCw, Send } from 'lucide-react';

export const NotificationsPage = () => {
  const [notifications, setNotifications] = useState([]);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Event trigger simulator
  const [simEvent, setSimEvent] = useState('BookingConfirmation');
  const [simTitle, setSimTitle] = useState('New Class Reserved');
  const [simMsg, setSimMsg] = useState('Your session has been successfully booked.');
  const [isSimulating, setIsSimulating] = useState(false);

  const fetchNotifications = async () => {
    setLoading(true);
    setError(null);
    try {
      const [pagedData, sumData] = await Promise.all([
        notificationsApi.getNotifications({
          unreadOnly: unreadOnly || undefined,
          page: pageNumber,
          pageSize,
        }),
        notificationsApi.getSummary(),
      ]);
      setNotifications(pagedData.items || []);
      setTotalCount(pagedData.totalCount || 0);
      setTotalPages(pagedData.totalPages || 1);
      setSummary(sumData);
    } catch (err) {
      console.error('Failed to load notifications:', err);
      setError('Unable to load notifications.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchNotifications();
  }, [unreadOnly, pageNumber, pageSize]);

  const handleMarkAsRead = async (id) => {
    try {
      await notificationsApi.markAsRead(id);
      fetchNotifications();
    } catch (err) {
      console.error('Failed to mark read:', err);
    }
  };

  const handleMarkAllRead = async () => {
    try {
      await notificationsApi.markAllAsRead();
      fetchNotifications();
    } catch (err) {
      console.error('Failed to mark all read:', err);
    }
  };

  const handleTriggerSim = async (e) => {
    e.preventDefault();
    setIsSimulating(true);
    try {
      await notificationsApi.triggerEvent({
        eventType: simEvent,
        title: simTitle,
        message: simMsg,
        category: simEvent.includes('Booking') ? 'Booking' : simEvent.includes('Membership') ? 'Membership' : 'Facility',
        priority: 'Normal',
      });
      fetchNotifications();
    } catch (err) {
      console.error('Failed to trigger event:', err);
    } finally {
      setIsSimulating(false);
    }
  };

  const filteredItems = selectedCategory
    ? notifications.filter((n) => n.category?.toLowerCase() === selectedCategory.toLowerCase())
    : notifications;

  return (
    <div>
      <PageHeader
        title="Notification Center"
        subtitle="Real-time multi-channel system notifications, receipts, and alert dispatches"
        icon={Bell}
        badge={summary?.unreadCount ? `${summary.unreadCount} Unread` : 'All Caught Up'}
        actions={
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            {summary?.unreadCount > 0 && (
              <button onClick={handleMarkAllRead} className="btn btn-secondary">
                <CheckCheck size={16} /> Mark All as Read
              </button>
            )}
            <button onClick={fetchNotifications} className="btn btn-primary">
              <RefreshCw size={16} /> Refresh
            </button>
          </div>
        }
      />

      {/* Summary KPI Tiles */}
      {summary && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
          <div className="glass-card" style={{ padding: '1rem 1.25rem' }}>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Total Received</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: '#ffffff', marginTop: '0.2rem' }}>
              {summary.totalCount}
            </div>
          </div>
          <div className="glass-card" style={{ padding: '1rem 1.25rem' }}>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Unread Alerts</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: summary.unreadCount > 0 ? 'var(--accent-amber)' : 'var(--accent-emerald)', marginTop: '0.2rem' }}>
              {summary.unreadCount}
            </div>
          </div>
          <div className="glass-card" style={{ padding: '1rem 1.25rem' }}>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Booking Updates</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--accent-cyan)', marginTop: '0.2rem' }}>
              {summary.unreadBookingEvents || 0}
            </div>
          </div>
          <div className="glass-card" style={{ padding: '1rem 1.25rem' }}>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Facility Alerts</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--primary)', marginTop: '0.2rem' }}>
              {summary.unreadFacilityEvents || 0}
            </div>
          </div>
        </div>
      )}

      {/* Filter Tabs & Event Simulator */}
      <div className="glass-card" style={{ padding: '1rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <button
            onClick={() => setUnreadOnly(false)}
            className={`btn ${!unreadOnly ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.4rem 0.85rem', fontSize: '0.8rem' }}
          >
            All Updates
          </button>
          <button
            onClick={() => setUnreadOnly(true)}
            className={`btn ${unreadOnly ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.4rem 0.85rem', fontSize: '0.8rem' }}
          >
            Unread Only ({summary?.unreadCount || 0})
          </button>

          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginLeft: '0.5rem' }}>Category:</span>
          <select
            className="form-select"
            style={{ width: 'auto', padding: '0.35rem 0.75rem' }}
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
          >
            <option value="">All Categories</option>
            <option value="Booking">Booking</option>
            <option value="Membership">Membership</option>
            <option value="Facility">Facility</option>
            <option value="System">System</option>
          </select>
        </div>

        {/* Quick Event Simulation Form for testing */}
        <form onSubmit={handleTriggerSim} style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <select
            className="form-select"
            style={{ width: 'auto', padding: '0.35rem 0.6rem', fontSize: '0.75rem' }}
            value={simEvent}
            onChange={(e) => setSimEvent(e.target.value)}
          >
            <option value="BookingConfirmation">Simulate: Booking Confirmation</option>
            <option value="BookingCancellation">Simulate: Booking Cancellation</option>
            <option value="MembershipExpiry">Simulate: Membership Expiry</option>
            <option value="IssueUpdate">Simulate: Issue Update</option>
            <option value="RepairApproval">Simulate: Repair Approval</option>
          </select>
          <button
            type="submit"
            disabled={isSimulating}
            className="btn btn-secondary"
            style={{ padding: '0.35rem 0.75rem', fontSize: '0.75rem' }}
          >
            <Send size={12} /> Test Trigger
          </button>
        </form>
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading notifications..." />
      ) : filteredItems.length === 0 ? (
        <EmptyState
          icon={Bell}
          title="No notifications to display"
          description={unreadOnly ? 'You have read all your notifications!' : 'There are no notifications matching your filter criteria.'}
        />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          {filteredItems.map((n) => {
            const isUnread = !n.isRead;
            return (
              <div
                key={n.id}
                className="glass-card"
                style={{
                  padding: '1.25rem 1.5rem',
                  display: 'flex',
                  alignItems: 'flex-start',
                  justifyContent: 'space-between',
                  gap: '1rem',
                  border: isUnread ? '1px solid var(--border-active)' : '1px solid var(--border-subtle)',
                  background: isUnread ? 'rgba(99,102,241,0.06)' : 'var(--bg-card)',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: '1rem', flex: 1 }}>
                  <div style={{
                    width: 40,
                    height: 40,
                    borderRadius: 'var(--radius-md)',
                    background: isUnread ? 'rgba(99,102,241,0.2)' : 'rgba(255,255,255,0.04)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flexShrink: 0,
                  }}>
                    <Bell size={18} color={isUnread ? 'var(--primary)' : 'var(--text-muted)'} />
                  </div>
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.25rem' }}>
                      <h4 style={{ fontSize: '0.95rem', fontWeight: isUnread ? 800 : 600, color: '#ffffff' }}>
                        {n.title}
                      </h4>
                      <span className="badge badge-info" style={{ fontSize: '0.65rem' }}>
                        {n.category || 'General'}
                      </span>
                      {isUnread && (
                        <span className="badge badge-warning" style={{ fontSize: '0.65rem' }}>
                          NEW
                        </span>
                      )}
                    </div>
                    <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', lineHeight: 1.5, marginBottom: '0.4rem' }}>
                      {n.message}
                    </p>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                      {new Date(n.createdAt).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })}
                    </div>
                  </div>
                </div>

                {isUnread && (
                  <button
                    onClick={() => handleMarkAsRead(n.id)}
                    className="btn btn-secondary"
                    style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem', flexShrink: 0 }}
                  >
                    Mark as Read
                  </button>
                )}
              </div>
            );
          })}

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

export default NotificationsPage;
