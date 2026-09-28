import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  Bell,
  CheckCircle,
  Calendar,
  Wrench,
  ShieldAlert,
  Info,
  Check,
  Send,
  X
} from 'lucide-react';
import notificationsApi from '../../services/notificationsApi';

export const NotificationCenter = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(false);
  const [isEventModalOpen, setIsEventModalOpen] = useState(false);
  const [eventStatus, setEventStatus] = useState('');
  const dropdownRef = useRef(null);

  const fetchSummary = useCallback(async () => {
    try {
      const data = await notificationsApi.getSummary();
      setUnreadCount(data.unreadCount || 0);
      setNotifications(data.recentNotifications || []);
    } catch (err) {
      console.error('Failed to fetch notification summary', err);
    }
  }, []);

  useEffect(() => {
    fetchSummary();
    const interval = setInterval(fetchSummary, 30000); // 30s polling
    return () => clearInterval(interval);
  }, [fetchSummary]);

  // Click outside to close
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleMarkAsRead = async (id, e) => {
    e.stopPropagation();
    try {
      await notificationsApi.markAsRead(id);
      fetchSummary();
    } catch (err) {
      console.error(err);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      setLoading(true);
      await notificationsApi.markAllAsRead();
      fetchSummary();
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleTriggerEvent = async (eventType, referenceName, details, amount) => {
    try {
      setEventStatus('Triggering...');
      await notificationsApi.triggerEvent({
        eventType,
        referenceName,
        details,
        amount,
      });
      setEventStatus('Event emitted successfully!');
      fetchSummary();
      setTimeout(() => setEventStatus(''), 2500);
    } catch (err) {
      setEventStatus('Failed to emit event');
    }
  };

  const getIconForType = (type) => {
    const t = String(type).toLowerCase();
    if (t.includes('booking') || t === '2') {
      return <Calendar size={16} color="var(--accent-emerald, #10b981)" />;
    }
    if (t.includes('maintenance') || t === '3') {
      return <Wrench size={16} color="var(--accent-cyan, #06b6d4)" />;
    }
    if (t.includes('approval') || t === '4') {
      return <ShieldAlert size={16} color="var(--accent-amber, #f59e0b)" />;
    }
    return <Info size={16} color="var(--primary-color, #6366f1)" />;
  };

  return (
    <div style={{ position: 'relative' }} ref={dropdownRef}>
      {/* Bell Button */}
      <button
        className="btn-icon"
        onClick={() => setIsOpen(!isOpen)}
        aria-label="Notification center"
        style={{
          position: 'relative',
          padding: '0.5rem',
          background: isOpen ? 'rgba(255, 255, 255, 0.1)' : 'transparent',
          borderRadius: '8px',
          border: 'none',
          cursor: 'pointer',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Bell size={20} color="var(--text-secondary)" />
        {unreadCount > 0 && (
          <span
            style={{
              position: 'absolute',
              top: '2px',
              right: '2px',
              background: '#ef4444',
              color: '#ffffff',
              fontSize: '0.65rem',
              fontWeight: 800,
              padding: '1px 5px',
              borderRadius: '10px',
              minWidth: '16px',
              textAlign: 'center',
              boxShadow: '0 0 8px rgba(239, 68, 68, 0.6)',
            }}
          >
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div
          className="glass-card"
          style={{
            position: 'absolute',
            top: 'calc(100% + 8px)',
            right: 0,
            width: '360px',
            maxHeight: '480px',
            display: 'flex',
            flexDirection: 'column',
            zIndex: 1000,
            padding: 0,
            boxShadow: '0 10px 25px rgba(0, 0, 0, 0.5)',
            border: '1px solid var(--border-color)',
            overflow: 'hidden',
          }}
        >
          {/* Header */}
          <div
            style={{
              padding: '0.85rem 1rem',
              borderBottom: '1px solid var(--border-color)',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span style={{ fontWeight: 700, fontSize: '0.95rem' }}>Notifications</span>
              {unreadCount > 0 && (
                <span className="badge badge-info" style={{ fontSize: '0.7rem' }}>
                  {unreadCount} unread
                </span>
              )}
            </div>

            <div style={{ display: 'flex', gap: '0.5rem' }}>
              {unreadCount > 0 && (
                <button
                  onClick={handleMarkAllAsRead}
                  disabled={loading}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: 'var(--primary-color, #6366f1)',
                    fontSize: '0.75rem',
                    fontWeight: 600,
                    cursor: 'pointer',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.2rem',
                  }}
                >
                  <Check size={12} /> Mark all read
                </button>
              )}
              <button
                onClick={() => setIsEventModalOpen(true)}
                title="Test Business Events"
                style={{
                  background: 'none',
                  border: 'none',
                  color: 'var(--text-secondary)',
                  cursor: 'pointer',
                  padding: '2px',
                }}
              >
                <Send size={14} />
              </button>
            </div>
          </div>

          {/* List */}
          <div style={{ overflowY: 'auto', maxHeight: '360px' }}>
            {notifications.length === 0 ? (
              <div style={{ padding: '2.5rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                <CheckCircle size={32} style={{ opacity: 0.4, marginBottom: '0.5rem' }} />
                <div style={{ fontSize: '0.85rem' }}>You're all caught up!</div>
                <div style={{ fontSize: '0.75rem', opacity: 0.7 }}>No notifications right now.</div>
              </div>
            ) : (
              notifications.map((n) => (
                <div
                  key={n.id}
                  style={{
                    padding: '0.85rem 1rem',
                    borderBottom: '1px solid rgba(255, 255, 255, 0.05)',
                    background: n.isRead ? 'transparent' : 'rgba(99, 102, 241, 0.06)',
                    display: 'flex',
                    gap: '0.75rem',
                    alignItems: 'flex-start',
                    cursor: 'pointer',
                    transition: 'background 0.2s',
                  }}
                  onClick={(e) => !n.isRead && handleMarkAsRead(n.id, e)}
                >
                  <div style={{ marginTop: '2px' }}>{getIconForType(n.type)}</div>
                  <div style={{ flex: 1 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.2rem' }}>
                      <span style={{ fontWeight: n.isRead ? 600 : 700, fontSize: '0.85rem', color: n.isRead ? 'var(--text-primary)' : '#ffffff' }}>
                        {n.title}
                      </span>
                      {!n.isRead && (
                        <span
                          style={{
                            width: '6px',
                            height: '6px',
                            borderRadius: '50%',
                            background: 'var(--primary-color, #6366f1)',
                          }}
                        />
                      )}
                    </div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', lineHeight: 1.3 }}>
                      {n.message}
                    </div>
                    <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', marginTop: '0.35rem' }}>
                      {new Date(n.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} • {new Date(n.createdAt).toLocaleDateString()}
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* Business Event Simulator Dialog */}
      {isEventModalOpen && (
        <div className="modal-backdrop" onClick={() => setIsEventModalOpen(false)} style={{ zIndex: 1100 }}>
          <div
            className="glass-card modal-content"
            onClick={(e) => e.stopPropagation()}
            style={{ width: '100%', maxWidth: '440px', padding: '1.5rem' }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <Send size={18} color="var(--primary-color, #6366f1)" />
                <h3 style={{ fontSize: '1.1rem', fontWeight: 700, margin: 0 }}>Trigger System Events</h3>
              </div>
              <button className="btn-icon" onClick={() => setIsEventModalOpen(false)}>
                <X size={18} />
              </button>
            </div>

            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
              Simulate Phase 08 cross-cutting event notifications stored in PostgreSQL:
            </p>

            {eventStatus && (
              <div style={{ padding: '0.5rem', background: 'rgba(16, 185, 129, 0.15)', color: '#10b981', borderRadius: '6px', fontSize: '0.8rem', marginBottom: '0.75rem', textAlign: 'center' }}>
                {eventStatus}
              </div>
            )}

            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              <button
                className="btn-secondary"
                style={{ textAlign: 'left', padding: '0.6rem 0.75rem', fontSize: '0.825rem' }}
                onClick={() => handleTriggerEvent(1, 'Pilates Core Flow', 'Studio 2')}
              >
                📅 <strong>Booking Confirmation</strong>: Class reserved
              </button>

              <button
                className="btn-secondary"
                style={{ textAlign: 'left', padding: '0.6rem 0.75rem', fontSize: '0.825rem' }}
                onClick={() => handleTriggerEvent(2, 'Power Spin', 'Instructor unavailable')}
              >
                ❌ <strong>Booking Cancellation</strong>: Session cancelled
              </button>

              <button
                className="btn-secondary"
                style={{ textAlign: 'left', padding: '0.6rem 0.75rem', fontSize: '0.825rem' }}
                onClick={() => handleTriggerEvent(3, 'Platinum All-Access', 'Expiring in 7 days')}
              >
                ⏰ <strong>Membership Expiry</strong>: Expiration countdown
              </button>

              <button
                className="btn-secondary"
                style={{ textAlign: 'left', padding: '0.6rem 0.75rem', fontSize: '0.825rem' }}
                onClick={() => handleTriggerEvent(4, 'Treadmill Cable Tension', 'Status changed to IN_PROGRESS')}
              >
                🔧 <strong>Facility Issue Update</strong>: Status progression
              </button>

              <button
                className="btn-secondary"
                style={{ textAlign: 'left', padding: '0.6rem 0.75rem', fontSize: '0.825rem' }}
                onClick={() => handleTriggerEvent(5, 'LifeFitness Dual Pulley', 'Approval required', 1850.00)}
              >
                🛡️ <strong>Repair Approval</strong>: HITL Financial gate ($1,850)
              </button>

              <button
                className="btn-secondary"
                style={{ textAlign: 'left', padding: '0.6rem 0.75rem', fontSize: '0.825rem' }}
                onClick={() => handleTriggerEvent(6, 'Rowing Machine Drive Belt', 'Scheduled for Thursday')}
              >
                🗓️ <strong>Repair Scheduled</strong>: Technician visit booked
              </button>

              <button
                className="btn-secondary"
                style={{ textAlign: 'left', padding: '0.6rem 0.75rem', fontSize: '0.825rem' }}
                onClick={() => handleTriggerEvent(7, 'PO-2026-0812', 'Whey Isolate Restock', 3200.00)}
              >
                📦 <strong>Vendor Request</strong>: Purchase order fulfillment
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationCenter;
