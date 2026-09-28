import React, { useState, useEffect } from 'react';
import { X, Save, Calendar, Clock } from 'lucide-react';
import classesApi from '../../services/classesApi';

export const ScheduleModal = ({ schedule, classes, trainers, onClose, onSaved }) => {
  const [formData, setFormData] = useState({
    classId: '',
    trainerId: '',
    room: 'Studio 1 — High Intensity',
    startTime: '',
    endTime: '',
    capacity: 20,
  });
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (schedule) {
      // Convert UTC to local datetime-local format YYYY-MM-DDTHH:mm
      const formatLocal = (isoStr) => {
        if (!isoStr) return '';
        const d = new Date(isoStr);
        const pad = (n) => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
      };

      setFormData({
        classId: schedule.classId || (classes[0]?.id ?? ''),
        trainerId: schedule.trainerId || (trainers[0]?.id ?? ''),
        room: schedule.room || 'Studio 1',
        startTime: formatLocal(schedule.startTime),
        endTime: formatLocal(schedule.endTime),
        capacity: schedule.capacity || 20,
      });
    } else {
      // Default tomorrow 9:00 AM
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      tomorrow.setHours(9, 0, 0, 0);

      const end = new Date(tomorrow);
      end.setHours(10, 0, 0, 0);

      const pad = (n) => String(n).padStart(2, '0');
      const startStr = `${tomorrow.getFullYear()}-${pad(tomorrow.getMonth() + 1)}-${pad(tomorrow.getDate())}T09:00`;
      const endStr = `${end.getFullYear()}-${pad(end.getMonth() + 1)}-${pad(end.getDate())}T10:00`;

      setFormData({
        classId: classes[0]?.id || '',
        trainerId: trainers[0]?.id || '',
        room: 'Studio 1 — High Intensity',
        startTime: startStr,
        endTime: endStr,
        capacity: classes[0]?.defaultCapacity || 20,
      });
    }
  }, [schedule, classes, trainers]);

  // When class changes, adjust capacity to default capacity
  const handleClassChange = (selectedClassId) => {
    const selectedClass = classes.find((c) => c.id === selectedClassId);
    setFormData((prev) => ({
      ...prev,
      classId: selectedClassId,
      capacity: selectedClass?.defaultCapacity || prev.capacity,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.classId || !formData.trainerId || !formData.startTime || !formData.endTime) {
      setErrorMsg('Please complete all schedule parameters.');
      return;
    }

    const startDate = new Date(formData.startTime);
    const endDate = new Date(formData.endTime);

    if (endDate <= startDate) {
      setErrorMsg('Schedule End Time must be later than Start Time.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      const payload = {
        classId: formData.classId,
        trainerId: formData.trainerId,
        room: formData.room.trim(),
        startTime: startDate.toISOString(),
        endTime: endDate.toISOString(),
        capacity: parseInt(formData.capacity, 10),
      };

      if (schedule?.id) {
        await classesApi.updateSchedule(schedule.id, {
          ...payload,
          status: schedule.status ?? 1,
        });
      } else {
        await classesApi.createSchedule(payload);
      }

      onSaved();
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || err.message || 'Operation failed';
      setErrorMsg(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="modal-backdrop" onClick={onClose} style={{ zIndex: 1000 }}>
      <div
        className="glass-card modal-content"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: '580px', padding: '1.5rem', maxHeight: '90vh', overflowY: 'auto' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
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
              <Calendar size={20} />
            </div>
            <div>
              <h2 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>
                {schedule ? 'Modify Class Schedule' : 'Schedule New Class Session'}
              </h2>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                Assign trainer, room, and capacity with conflict verification
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

        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
              Fitness Class *
            </label>
            <select
              className="input-field"
              value={formData.classId}
              onChange={(e) => handleClassChange(e.target.value)}
              required
            >
              <option value="" disabled>
                Select class...
              </option>
              {classes.map((cls) => (
                <option key={cls.id} value={cls.id}>
                  {cls.name} ({cls.durationMinutes} min, {cls.intensityLevel})
                </option>
              ))}
            </select>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', marginBottom: '1rem' }}>
            <div>
              <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
                Instructor / Trainer *
              </label>
              <select
                className="input-field"
                value={formData.trainerId}
                onChange={(e) => setFormData({ ...formData, trainerId: e.target.value })}
                required
              >
                <option value="" disabled>
                  Select trainer...
                </option>
                {trainers.map((tr) => (
                  <option key={tr.id} value={tr.id}>
                    {tr.name} ({tr.email})
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
                Studio / Room *
              </label>
              <input
                type="text"
                className="input-field"
                value={formData.room}
                onChange={(e) => setFormData({ ...formData, room: e.target.value })}
                placeholder="e.g. Studio 1 — High Intensity"
                required
              />
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', marginBottom: '1rem' }}>
            <div>
              <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
                Start Time *
              </label>
              <input
                type="datetime-local"
                className="input-field"
                value={formData.startTime}
                onChange={(e) => setFormData({ ...formData, startTime: e.target.value })}
                required
              />
            </div>
            <div>
              <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
                End Time *
              </label>
              <input
                type="datetime-local"
                className="input-field"
                value={formData.endTime}
                onChange={(e) => setFormData({ ...formData, endTime: e.target.value })}
                required
              />
            </div>
          </div>

          <div style={{ marginBottom: '1.25rem' }}>
            <label style={{ display: 'block', fontSize: '0.825rem', fontWeight: 600, marginBottom: '0.4rem' }}>
              Session Capacity *
            </label>
            <input
              type="number"
              min="1"
              max="100"
              className="input-field"
              value={formData.capacity}
              onChange={(e) => setFormData({ ...formData, capacity: e.target.value })}
              required
            />
            <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
              Maximum confirmed bookings allowed before class is locked.
            </span>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              <Save size={16} />
              {submitting ? 'Saving...' : schedule ? 'Update Schedule' : 'Schedule Session'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ScheduleModal;
