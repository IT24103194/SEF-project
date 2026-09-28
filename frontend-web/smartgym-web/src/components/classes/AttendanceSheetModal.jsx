import React, { useState, useEffect } from 'react';
import { X, CheckCircle, Clock, UserCheck, AlertCircle, Save } from 'lucide-react';
import classesApi from '../../services/classesApi';

export const AttendanceSheetModal = ({ schedule, onClose, onUpdated }) => {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  // Map of bookingId -> status (1: Attended, 2: Absent, 3: Excused)
  const [attendanceMap, setAttendanceMap] = useState({});

  useEffect(() => {
    const fetchSheet = async () => {
      try {
        setLoading(true);
        const data = await classesApi.getScheduleAttendanceSheet(schedule.id);
        setBookings(data || []);

        const initialMap = {};
        (data || []).forEach((b) => {
          initialMap[b.id] = b.attendance?.status || 1; // Default to Attended
        });
        setAttendanceMap(initialMap);
      } catch (err) {
        setErrorMsg('Failed to load class attendance sheet.');
      } finally {
        setLoading(false);
      }
    };

    if (schedule?.id) {
      fetchSheet();
    }
  }, [schedule]);

  const handleStatusChange = (bookingId, status) => {
    setAttendanceMap((prev) => ({
      ...prev,
      [bookingId]: status,
    }));
  };

  const handleSaveSingle = async (bookingId) => {
    try {
      setSaving(true);
      setErrorMsg('');
      const status = attendanceMap[bookingId] || 1;

      await classesApi.recordAttendance({
        bookingId,
        status,
      });

      setSuccessMsg('Attendance recorded successfully.');
      setTimeout(() => setSuccessMsg(''), 3000);
      onUpdated();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || 'Failed to record attendance.');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveAll = async () => {
    if (bookings.length === 0) return;

    try {
      setSaving(true);
      setErrorMsg('');

      const records = bookings.map((b) => ({
        bookingId: b.id,
        status: attendanceMap[b.id] || 1,
      }));

      await classesApi.recordBulkAttendance({
        scheduleId: schedule.id,
        records,
      });

      setSuccessMsg('All attendance records submitted successfully!');
      setTimeout(() => setSuccessMsg(''), 3000);
      onUpdated();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || 'Failed to record bulk attendance.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal-backdrop" onClick={onClose} style={{ zIndex: 1000 }}>
      <div
        className="glass-card modal-content"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: '720px', padding: '1.5rem', maxHeight: '90vh', overflowY: 'auto' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: '8px',
                background: 'rgba(16, 185, 129, 0.15)',
                color: '#10b981',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <UserCheck size={20} />
            </div>
            <div>
              <h2 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>
                Class Attendance Sheet
              </h2>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                {schedule.className} • {schedule.room} • {new Date(schedule.startTime).toLocaleDateString()} {new Date(schedule.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
              </span>
            </div>
          </div>
          <button className="btn-icon" onClick={onClose} title="Close">
            <X size={18} />
          </button>
        </div>

        {errorMsg && (
          <div
            style={{
              padding: '0.75rem 1rem',
              borderRadius: '8px',
              background: 'rgba(239, 68, 68, 0.12)',
              border: '1px solid rgba(239, 68, 68, 0.25)',
              color: '#ef4444',
              fontSize: '0.85rem',
              marginBottom: '1rem',
            }}
          >
            {errorMsg}
          </div>
        )}

        {successMsg && (
          <div
            style={{
              padding: '0.75rem 1rem',
              borderRadius: '8px',
              background: 'rgba(16, 185, 129, 0.12)',
              border: '1px solid rgba(16, 185, 129, 0.25)',
              color: '#10b981',
              fontSize: '0.85rem',
              marginBottom: '1rem',
            }}
          >
            {successMsg}
          </div>
        )}

        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
            Confirmed Participants: <strong style={{ color: 'var(--text-primary)' }}>{bookings.length}</strong> / {schedule.capacity}
          </div>
          {bookings.length > 0 && (
            <button
              className="btn btn-primary"
              onClick={handleSaveAll}
              disabled={saving || loading}
              style={{ fontSize: '0.85rem', padding: '0.4rem 0.8rem' }}
            >
              <Save size={14} />
              {saving ? 'Saving...' : 'Submit All Attendances'}
            </button>
          )}
        </div>

        {loading ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
            Loading attendance records...
          </div>
        ) : bookings.length === 0 ? (
          <div
            style={{
              padding: '2.5rem 1rem',
              textAlign: 'center',
              background: 'rgba(255, 255, 255, 0.02)',
              borderRadius: '8px',
              border: '1px dashed var(--border-color)',
            }}
          >
            <AlertCircle size={32} color="var(--text-secondary)" style={{ marginBottom: '0.5rem' }} />
            <div style={{ fontWeight: 600 }}>No Confirmed Bookings Yet</div>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
              Members who book this session will automatically appear here for check-in.
            </p>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
            {bookings.map((b) => {
              const currentStatus = attendanceMap[b.id] ?? 1;
              const hasRecorded = !!b.attendance;

              return (
                <div
                  key={b.id}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    padding: '0.75rem 1rem',
                    background: 'rgba(255, 255, 255, 0.03)',
                    borderRadius: '8px',
                    border: '1px solid var(--border-color)',
                  }}
                >
                  <div>
                    <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{b.memberName}</div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                      {b.memberEmail} • Booked: {new Date(b.bookingTime).toLocaleDateString()}
                    </div>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <select
                      className="input-field"
                      style={{ padding: '0.3rem 0.6rem', fontSize: '0.8rem', width: 'auto' }}
                      value={currentStatus}
                      onChange={(e) => handleStatusChange(b.id, parseInt(e.target.value, 10))}
                    >
                      <option value={1}>Attended</option>
                      <option value={2}>Absent</option>
                      <option value={3}>Excused</option>
                    </select>

                    <button
                      className="btn btn-secondary"
                      style={{ padding: '0.35rem 0.6rem', fontSize: '0.8rem' }}
                      onClick={() => handleSaveSingle(b.id)}
                      disabled={saving}
                      title="Save Individual Status"
                    >
                      {hasRecorded ? 'Update' : 'Mark'}
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}

        <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1.5rem' }}>
          <button className="btn btn-secondary" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default AttendanceSheetModal;
