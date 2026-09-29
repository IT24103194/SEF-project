import React, { useState, useEffect } from 'react';
import { classesApi } from '../services/classesApi';
import { trainersApi } from '../services/trainersApi';
import PageHeader from '../components/common/PageHeader';
import Pagination from '../components/common/Pagination';
import ScheduleModal from '../components/classes/ScheduleModal';
import AttendanceSheetModal from '../components/classes/AttendanceSheetModal';
import ConfirmationModal from '../components/common/ConfirmationModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Calendar, Plus, Clock, UserCheck, XCircle, CheckCircle, MapPin } from 'lucide-react';

export const SchedulesPage = () => {
  const [schedules, setSchedules] = useState([]);
  const [trainers, setTrainers] = useState([]);
  const [classes, setClasses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters & Pagination
  const [selectedTrainer, setSelectedTrainer] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Modals
  const [isScheduleModalOpen, setIsScheduleModalOpen] = useState(false);
  const [selectedScheduleForAttendance, setSelectedScheduleForAttendance] = useState(null);
  const [cancelTargetId, setCancelTargetId] = useState(null);
  const [isCancelling, setIsCancelling] = useState(false);

  const fetchSchedules = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await classesApi.getSchedules({
        trainerId: selectedTrainer || undefined,
        status: statusFilter || undefined,
        page: pageNumber,
        pageSize,
      });
      setSchedules(data.items || data || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load schedules:', err);
      setError('Unable to load class timetable.');
    } finally {
      setLoading(false);
    }
  };

  const loadDependencies = async () => {
    try {
      const [trainersData, classesData] = await Promise.all([
        trainersApi.getTrainers({ pageSize: 50 }),
        classesApi.getClasses({ pageSize: 50 }),
      ]);
      setTrainers(trainersData.items || []);
      setClasses(classesData.items || classesData || []);
    } catch (err) {
      console.error('Failed to load dependency dropdowns:', err);
    }
  };

  useEffect(() => {
    fetchSchedules();
  }, [selectedTrainer, statusFilter, pageNumber, pageSize]);

  useEffect(() => {
    loadDependencies();
  }, []);

  const handleSaveSchedule = async (scheduleData) => {
    await classesApi.createSchedule(scheduleData);
    setIsScheduleModalOpen(false);
    fetchSchedules();
  };

  const handleCancelConfirm = async () => {
    if (!cancelTargetId) return;
    setIsCancelling(true);
    try {
      await classesApi.cancelSchedule(cancelTargetId);
      setCancelTargetId(null);
      fetchSchedules();
    } catch (err) {
      console.error('Failed to cancel schedule:', err);
      setError('Failed to cancel schedule session.');
    } finally {
      setIsCancelling(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Class Timetable"
        subtitle="Manage upcoming class slots, trainer assignments, capacity quotas, and rosters"
        icon={Calendar}
        badge={`${totalCount} Sessions`}
        actions={
          <button
            onClick={() => setIsScheduleModalOpen(true)}
            className="btn btn-primary"
          >
            <Plus size={16} /> Schedule Session
          </button>
        }
      />

      {/* Filter Bar */}
      <div className="glass-card" style={{ padding: '0.75rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Status:</span>
          <select
            className="form-select"
            style={{ width: 'auto', padding: '0.35rem 0.75rem' }}
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPageNumber(1);
            }}
          >
            <option value="">All Statuses</option>
            <option value="Scheduled">Scheduled</option>
            <option value="InProgress">InProgress</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Trainer:</span>
          <select
            className="form-select"
            style={{ width: 'auto', padding: '0.35rem 0.75rem' }}
            value={selectedTrainer}
            onChange={(e) => {
              setSelectedTrainer(e.target.value);
              setPageNumber(1);
            }}
          >
            <option value="">All Trainers</option>
            {trainers.map((t) => (
              <option key={t.id} value={t.id}>
                {t.fullName}
              </option>
            ))}
          </select>
        </div>
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading class timetable..." />
      ) : schedules.length === 0 ? (
        <EmptyState
          icon={Calendar}
          title="No scheduled sessions"
          description="There are no class schedules matching the selected filter criteria."
          action={
            <button onClick={() => setIsScheduleModalOpen(true)} className="btn btn-primary">
              <Plus size={16} /> Schedule Session
            </button>
          }
        />
      ) : (
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Class</th>
                <th>Date & Time</th>
                <th>Trainer</th>
                <th>Studio / Room</th>
                <th>Capacity</th>
                <th>Status</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {schedules.map((s) => {
                const booked = s.bookedSpots || s.bookedCount || 0;
                const cap = s.capacity || 20;
                const isFull = booked >= cap;
                const isCancelled = s.status === 'Cancelled';

                return (
                  <tr key={s.id}>
                    <td>
                      <div style={{ fontWeight: 600, color: '#ffffff' }}>
                        {s.className || 'Fitness Class'}
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                        {s.categoryName || 'General'}
                      </div>
                    </td>
                    <td>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.15rem', fontSize: '0.825rem' }}>
                        <span style={{ color: '#ffffff', fontWeight: 600 }}>
                          {new Date(s.startTime).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })}
                        </span>
                        <span style={{ color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                          <Clock size={12} color="var(--primary)" />
                          {new Date(s.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - {new Date(s.endTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </span>
                      </div>
                    </td>
                    <td>
                      <span style={{ color: 'var(--text-primary)', fontSize: '0.85rem' }}>
                        {s.trainerName || 'Assigned Instructor'}
                      </span>
                    </td>
                    <td>
                      <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem', display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                        <MapPin size={13} color="var(--accent-cyan)" /> {s.room || 'Studio A'}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        <span style={{ fontWeight: 700, color: isFull ? 'var(--accent-rose)' : '#ffffff' }}>
                          {booked} / {cap}
                        </span>
                        {isFull && <span className="badge badge-danger" style={{ fontSize: '0.65rem' }}>FULL</span>}
                      </div>
                    </td>
                    <td>
                      <span className={`badge ${
                        s.status === 'Scheduled'
                          ? 'badge-info'
                          : s.status === 'Completed'
                          ? 'badge-success'
                          : isCancelled
                          ? 'badge-danger'
                          : 'badge-warning'
                      }`}>
                        {s.status}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                        <button
                          onClick={() => setSelectedScheduleForAttendance(s)}
                          className="btn btn-secondary"
                          style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                          title="Attendance Sheet"
                        >
                          <UserCheck size={13} /> Roster
                        </button>
                        {!isCancelled && s.status !== 'Completed' && (
                          <button
                            onClick={() => setCancelTargetId(s.id)}
                            className="btn btn-danger"
                            style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                            title="Cancel Session"
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

      {isScheduleModalOpen && (
        <ScheduleModal
          classes={classes}
          trainers={trainers}
          onClose={() => setIsScheduleModalOpen(false)}
          onSave={handleSaveSchedule}
        />
      )}

      {selectedScheduleForAttendance && (
        <AttendanceSheetModal
          schedule={selectedScheduleForAttendance}
          onClose={() => setSelectedScheduleForAttendance(null)}
          onAttendanceUpdated={fetchSchedules}
        />
      )}

      <ConfirmationModal
        isOpen={!!cancelTargetId}
        title="Cancel Scheduled Class"
        message="Are you sure you want to cancel this class session? Booked members will be notified immediately."
        confirmText="Cancel Class"
        isDestructive={true}
        isLoading={isCancelling}
        onConfirm={handleCancelConfirm}
        onCancel={() => setCancelTargetId(null)}
      />
    </div>
  );
};

export default SchedulesPage;
