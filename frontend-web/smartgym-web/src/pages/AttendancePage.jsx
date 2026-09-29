import React, { useState, useEffect } from 'react';
import { classesApi } from '../services/classesApi';
import PageHeader from '../components/common/PageHeader';
import Pagination from '../components/common/Pagination';
import AttendanceSheetModal from '../components/classes/AttendanceSheetModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { CheckSquare, Calendar, UserCheck, Clock } from 'lucide-react';

export const AttendancePage = () => {
  const [attendances, setAttendances] = useState([]);
  const [schedules, setSchedules] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedScheduleId, setSelectedScheduleId] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Roster modal
  const [activeRosterSchedule, setActiveRosterSchedule] = useState(null);

  const fetchAttendances = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await classesApi.getAttendances({
        scheduleId: selectedScheduleId || undefined,
        page: pageNumber,
        pageSize,
      });
      setAttendances(data.items || data || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load attendances:', err);
      setError('Unable to load attendance records.');
    } finally {
      setLoading(false);
    }
  };

  const loadSchedules = async () => {
    try {
      const data = await classesApi.getSchedules({ pageSize: 50 });
      setSchedules(data.items || data || []);
    } catch (err) {
      console.error('Failed to load schedules for filter:', err);
    }
  };

  useEffect(() => {
    fetchAttendances();
  }, [selectedScheduleId, pageNumber, pageSize]);

  useEffect(() => {
    loadSchedules();
  }, []);

  const handleOpenRoster = () => {
    if (!selectedScheduleId) {
      if (schedules.length > 0) {
        setActiveRosterSchedule(schedules[0]);
      }
      return;
    }
    const sched = schedules.find((s) => s.id === selectedScheduleId);
    if (sched) setActiveRosterSchedule(sched);
  };

  return (
    <div>
      <PageHeader
        title="Class Attendance"
        subtitle="Record workout check-ins, verify member passes, and track session attendance rates"
        icon={CheckSquare}
        badge={`${totalCount} Records`}
        actions={
          <button
            onClick={handleOpenRoster}
            disabled={schedules.length === 0}
            className="btn btn-primary"
          >
            <UserCheck size={16} /> Open Attendance Roster
          </button>
        }
      />

      {/* Filter by Schedule */}
      <div className="glass-card" style={{ padding: '0.75rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600 }}>Filter by Class Session:</span>
        <select
          className="form-select"
          style={{ width: 'auto', minWidth: 260, padding: '0.35rem 0.75rem' }}
          value={selectedScheduleId}
          onChange={(e) => {
            setSelectedScheduleId(e.target.value);
            setPageNumber(1);
          }}
        >
          <option value="">All Scheduled Sessions</option>
          {schedules.map((s) => (
            <option key={s.id} value={s.id}>
              {s.className} - {new Date(s.startTime).toLocaleDateString()} {new Date(s.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} ({s.trainerName})
            </option>
          ))}
        </select>
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading attendance records..." />
      ) : attendances.length === 0 ? (
        <EmptyState
          icon={CheckSquare}
          title="No attendance records found"
          description="Select a class session or open the attendance roster to record member check-ins."
          action={
            schedules.length > 0 && (
              <button onClick={handleOpenRoster} className="btn btn-primary">
                <UserCheck size={16} /> Take Attendance
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
                <th>Class Session</th>
                <th>Check-In Status</th>
                <th>Recorded At</th>
                <th>Check-In Notes</th>
              </tr>
            </thead>
            <tbody>
              {attendances.map((a) => (
                <tr key={a.id}>
                  <td>
                    <div style={{ fontWeight: 600, color: '#ffffff' }}>
                      {a.memberName || 'Member'}
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                      ID: {a.memberId?.substring(0, 8)}...
                    </div>
                  </td>
                  <td>
                    <div style={{ fontWeight: 600, color: 'var(--accent-cyan)' }}>
                      {a.className || 'Class'}
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                      {a.classStartTime ? new Date(a.classStartTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' }) : 'N/A'}
                    </div>
                  </td>
                  <td>
                    <span className={`badge ${
                      a.status === 'Attended' || a.attended
                        ? 'badge-success'
                        : a.status === 'Absent'
                        ? 'badge-danger'
                        : 'badge-warning'
                    }`}>
                      {a.status || (a.attended ? 'Attended' : 'Absent')}
                    </span>
                  </td>
                  <td>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                      <Clock size={13} color="var(--primary)" />
                      {a.checkedInAt || a.createdAt ? new Date(a.checkedInAt || a.createdAt).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' }) : 'N/A'}
                    </div>
                  </td>
                  <td>
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                      {a.notes || 'Standard check-in'}
                    </span>
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

      {activeRosterSchedule && (
        <AttendanceSheetModal
          schedule={activeRosterSchedule}
          onClose={() => setActiveRosterSchedule(null)}
          onAttendanceUpdated={fetchAttendances}
        />
      )}
    </div>
  );
};

export default AttendancePage;
