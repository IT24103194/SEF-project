import React, { useState, useEffect, useCallback } from 'react';
import {
  Calendar,
  Clock,
  Users,
  Plus,
  Search,
  Filter,
  Dumbbell,
  Tags,
  CheckCircle,
  XCircle,
  AlertTriangle,
  RefreshCw,
  UserCheck,
  ChevronLeft,
  ChevronRight,
  Edit2,
  Trash2,
  Ban,
} from 'lucide-react';
import classesApi from '../services/classesApi';
import ClassModal from '../components/classes/ClassModal';
import ScheduleModal from '../components/classes/ScheduleModal';
import CategoryModal from '../components/classes/CategoryModal';
import AttendanceSheetModal from '../components/classes/AttendanceSheetModal';

export const ClassSchedulePage = () => {
  const [activeTab, setActiveTab] = useState('schedules'); // 'schedules', 'classes', 'categories', 'bookings'
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  // Dropdown options
  const [categories, setCategories] = useState([]);
  const [classesList, setClassesList] = useState([]);
  const [trainers, setTrainers] = useState([
    { id: '00000000-0000-0000-0000-000000000000', name: 'Kavinda Fernando', email: 'trainer@smartgym.com' },
  ]);

  // Data collections
  const [schedules, setSchedules] = useState([]);
  const [classes, setClasses] = useState([]);
  const [categoriesData, setCategoriesData] = useState([]);
  const [bookings, setBookings] = useState([]);

  // Pagination & Filters
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [intensityFilter, setIntensityFilter] = useState('');
  const [dateFilter, setDateFilter] = useState('');

  // Modals state
  const [showClassModal, setShowClassModal] = useState(false);
  const [editingClass, setEditingClass] = useState(null);

  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [editingSchedule, setEditingSchedule] = useState(null);

  const [showCategoryModal, setShowCategoryModal] = useState(false);
  const [editingCategory, setEditingCategory] = useState(null);

  const [selectedScheduleForAttendance, setSelectedScheduleForAttendance] = useState(null);

  // Load auxiliary data (categories, classes for dropdowns)
  const loadAuxiliaryData = async () => {
    try {
      const [cats, clsResult] = await Promise.all([
        classesApi.getCategories(),
        classesApi.getClasses({ page: 1, pageSize: 100 }),
      ]);
      setCategories(cats || []);
      setClassesList(clsResult.items || []);

      // Derive trainers from existing schedules if possible
      const schedResult = await classesApi.getSchedules({ page: 1, pageSize: 50 });
      if (schedResult.items?.length > 0) {
        const uniqueTrainers = [];
        const seen = new Set();
        schedResult.items.forEach((s) => {
          if (!seen.has(s.trainerId)) {
            seen.add(s.trainerId);
            uniqueTrainers.push({
              id: s.trainerId,
              name: s.trainerName,
              email: 'trainer@smartgym.com',
            });
          }
        });
        if (uniqueTrainers.length > 0) {
          setTrainers(uniqueTrainers);
        }
      }
    } catch (err) {
      console.error('Failed to load auxiliary scheduling data:', err);
    }
  };

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setErrorMsg('');

      if (activeTab === 'schedules') {
        const params = { page, pageSize: 10 };
        if (dateFilter) {
          params.startDate = `${dateFilter}T00:00:00Z`;
          params.endDate = `${dateFilter}T23:59:59Z`;
        }
        const data = await classesApi.getSchedules(params);
        setSchedules(data.items || []);
        setTotalPages(data.totalPages || 1);
        setTotalCount(data.totalCount || 0);
      } else if (activeTab === 'classes') {
        const params = {
          page,
          pageSize: 10,
          search: searchTerm || undefined,
          categoryId: categoryFilter || undefined,
          intensity: intensityFilter || undefined,
        };
        const data = await classesApi.getClasses(params);
        setClasses(data.items || []);
        setTotalPages(data.totalPages || 1);
        setTotalCount(data.totalCount || 0);
      } else if (activeTab === 'categories') {
        const data = await classesApi.getCategories();
        setCategoriesData(data || []);
      } else if (activeTab === 'bookings') {
        const data = await classesApi.getBookings({ page, pageSize: 15 });
        setBookings(data.items || []);
        setTotalPages(data.totalPages || 1);
        setTotalCount(data.totalCount || 0);
      }
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to fetch data.');
    } finally {
      setLoading(false);
    }
  }, [activeTab, page, searchTerm, categoryFilter, intensityFilter, dateFilter]);

  useEffect(() => {
    loadAuxiliaryData();
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Actions
  const handleCancelSchedule = async (scheduleId) => {
    if (!window.confirm('Are you sure you want to cancel this class schedule? All confirmed reservations will be notified.')) {
      return;
    }
    try {
      await classesApi.cancelSchedule(scheduleId);
      setSuccessMsg('Class session cancelled successfully.');
      setTimeout(() => setSuccessMsg(''), 3000);
      loadData();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || 'Failed to cancel session.');
    }
  };

  const handleDeleteClass = async (classId, name) => {
    if (!window.confirm(`Are you sure you want to delete fitness class "${name}"?`)) {
      return;
    }
    try {
      await classesApi.deleteClass(classId);
      setSuccessMsg(`Class "${name}" deleted.`);
      setTimeout(() => setSuccessMsg(''), 3000);
      loadData();
      loadAuxiliaryData();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || 'Failed to delete class.');
    }
  };

  const handleDeleteCategory = async (catId, name) => {
    if (!window.confirm(`Are you sure you want to delete category "${name}"?`)) {
      return;
    }
    try {
      await classesApi.deleteCategory(catId);
      setSuccessMsg(`Category "${name}" deleted.`);
      setTimeout(() => setSuccessMsg(''), 3000);
      loadData();
      loadAuxiliaryData();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || 'Failed to delete category.');
    }
  };

  const handleCancelBooking = async (bookingId) => {
    const reason = window.prompt('Enter cancellation reason:', 'Staff administrative cancellation');
    if (reason === null) return;

    try {
      await classesApi.cancelBooking(bookingId, { reason });
      setSuccessMsg('Booking cancelled and spot restored.');
      setTimeout(() => setSuccessMsg(''), 3000);
      loadData();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || 'Failed to cancel booking.');
    }
  };

  return (
    <div style={{ maxWidth: '1400px', margin: '0 auto', paddingBottom: '3rem' }}>
      {/* Page Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '2rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, margin: 0 }}>Class Scheduling & Timetables</h1>
          <p style={{ color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
            Component 3 — Timetables, capacity control, bookings, and attendance sheets
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          {activeTab === 'schedules' && (
            <button
              className="btn btn-primary"
              onClick={() => {
                setEditingSchedule(null);
                setShowScheduleModal(true);
              }}
            >
              <Plus size={16} /> Schedule Session
            </button>
          )}
          {activeTab === 'classes' && (
            <button
              className="btn btn-primary"
              onClick={() => {
                setEditingClass(null);
                setShowClassModal(true);
              }}
            >
              <Plus size={16} /> New Fitness Class
            </button>
          )}
          {activeTab === 'categories' && (
            <button
              className="btn btn-primary"
              onClick={() => {
                setEditingCategory(null);
                setShowCategoryModal(true);
              }}
            >
              <Plus size={16} /> New Category
            </button>
          )}
          <button className="btn btn-secondary" onClick={loadData} title="Refresh">
            <RefreshCw size={16} className={loading ? 'spin' : ''} />
          </button>
        </div>
      </div>

      {/* Alerts */}
      {errorMsg && (
        <div
          style={{
            padding: '0.85rem 1.25rem',
            borderRadius: '8px',
            background: 'rgba(239, 68, 68, 0.12)',
            border: '1px solid rgba(239, 68, 68, 0.3)',
            color: '#ef4444',
            marginBottom: '1.5rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.75rem',
          }}
        >
          <AlertTriangle size={18} />
          <span>{errorMsg}</span>
        </div>
      )}

      {successMsg && (
        <div
          style={{
            padding: '0.85rem 1.25rem',
            borderRadius: '8px',
            background: 'rgba(16, 185, 129, 0.12)',
            border: '1px solid rgba(16, 185, 129, 0.3)',
            color: '#10b981',
            marginBottom: '1.5rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.75rem',
          }}
        >
          <CheckCircle size={18} />
          <span>{successMsg}</span>
        </div>
      )}

      {/* Tabs */}
      <div
        style={{
          display: 'flex',
          gap: '0.5rem',
          borderBottom: '1px solid var(--border-color)',
          marginBottom: '1.5rem',
        }}
      >
        <button
          className={`tab-btn ${activeTab === 'schedules' ? 'active' : ''}`}
          onClick={() => {
            setActiveTab('schedules');
            setPage(1);
          }}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'schedules' ? '2px solid #6366f1' : '2px solid transparent',
            color: activeTab === 'schedules' ? '#6366f1' : 'var(--text-secondary)',
            fontWeight: activeTab === 'schedules' ? 700 : 500,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
          }}
        >
          <Calendar size={16} /> Sessions & Timetable
        </button>

        <button
          className={`tab-btn ${activeTab === 'classes' ? 'active' : ''}`}
          onClick={() => {
            setActiveTab('classes');
            setPage(1);
          }}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'classes' ? '2px solid #6366f1' : '2px solid transparent',
            color: activeTab === 'classes' ? '#6366f1' : 'var(--text-secondary)',
            fontWeight: activeTab === 'classes' ? 700 : 500,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
          }}
        >
          <Dumbbell size={16} /> Fitness Classes
        </button>

        <button
          className={`tab-btn ${activeTab === 'categories' ? 'active' : ''}`}
          onClick={() => {
            setActiveTab('categories');
            setPage(1);
          }}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'categories' ? '2px solid #6366f1' : '2px solid transparent',
            color: activeTab === 'categories' ? '#6366f1' : 'var(--text-secondary)',
            fontWeight: activeTab === 'categories' ? 700 : 500,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
          }}
        >
          <Tags size={16} /> Categories
        </button>

        <button
          className={`tab-btn ${activeTab === 'bookings' ? 'active' : ''}`}
          onClick={() => {
            setActiveTab('bookings');
            setPage(1);
          }}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'bookings' ? '2px solid #6366f1' : '2px solid transparent',
            color: activeTab === 'bookings' ? '#6366f1' : 'var(--text-secondary)',
            fontWeight: activeTab === 'bookings' ? 700 : 500,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
          }}
        >
          <Users size={16} /> Member Bookings
        </button>
      </div>

      {/* TAB 1: SCHEDULES & TIMETABLE */}
      {activeTab === 'schedules' && (
        <div>
          {/* Filters Bar */}
          <div
            className="glass-card"
            style={{
              padding: '1rem',
              marginBottom: '1.5rem',
              display: 'flex',
              alignItems: 'center',
              gap: '1rem',
              flexWrap: 'wrap',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>Filter by Date:</span>
              <input
                type="date"
                className="input-field"
                value={dateFilter}
                onChange={(e) => {
                  setDateFilter(e.target.value);
                  setPage(1);
                }}
                style={{ width: 'auto' }}
              />
              {dateFilter && (
                <button
                  className="btn btn-secondary"
                  onClick={() => setDateFilter('')}
                  style={{ padding: '0.35rem 0.6rem', fontSize: '0.8rem' }}
                >
                  Clear
                </button>
              )}
            </div>

            <div style={{ marginLeft: 'auto', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
              Showing {schedules.length} of {totalCount} scheduled sessions
            </div>
          </div>

          {loading ? (
            <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
              Loading scheduled sessions...
            </div>
          ) : schedules.length === 0 ? (
            <div className="glass-card" style={{ padding: '3rem', textAlign: 'center' }}>
              <Calendar size={48} color="var(--text-secondary)" style={{ marginBottom: '1rem' }} />
              <h3 style={{ fontSize: '1.1rem', fontWeight: 600 }}>No Sessions Scheduled</h3>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Use the "Schedule Session" button above to publish gym workout sessions to member apps.
              </p>
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))', gap: '1.25rem' }}>
              {schedules.map((s) => {
                const startTime = new Date(s.startTime);
                const endTime = new Date(s.endTime);
                const isCancelled = s.status === 3;
                const isFull = s.isFull;

                return (
                  <div
                    key={s.id}
                    className="glass-card"
                    style={{
                      padding: '1.25rem',
                      display: 'flex',
                      flexDirection: 'column',
                      justifyContent: 'space-between',
                      opacity: isCancelled ? 0.65 : 1,
                      borderLeft: isCancelled
                        ? '4px solid #ef4444'
                        : isFull
                        ? '4px solid #f59e0b'
                        : '4px solid #10b981',
                    }}
                  >
                    <div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
                        <div>
                          <span
                            style={{
                              fontSize: '0.7rem',
                              fontWeight: 700,
                              textTransform: 'uppercase',
                              padding: '0.2rem 0.5rem',
                              borderRadius: '4px',
                              background: 'rgba(99, 102, 241, 0.15)',
                              color: '#6366f1',
                            }}
                          >
                            {s.categoryName}
                          </span>
                          <h3 style={{ fontSize: '1.1rem', fontWeight: 700, margin: '0.4rem 0 0.2rem' }}>
                            {s.className}
                          </h3>
                        </div>

                        <span
                          className={`badge ${
                            isCancelled ? 'badge-cancelled' : isFull ? 'badge-warning' : 'badge-active'
                          }`}
                        >
                          {isCancelled ? 'Cancelled' : isFull ? 'Full (0 spots)' : `${s.availableSpots} spots open`}
                        </span>
                      </div>

                      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.35rem', marginTop: '0.75rem', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <Clock size={14} color="#6366f1" />
                          <span>
                            {startTime.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })} • {startTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - {endTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} ({s.durationMinutes} min)
                          </span>
                        </div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <Users size={14} color="#10b981" />
                          <span>Trainer: <strong>{s.trainerName}</strong></span>
                        </div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <Dumbbell size={14} color="#f59e0b" />
                          <span>Location: {s.room}</span>
                        </div>
                      </div>

                      {/* Capacity Progress Bar */}
                      <div style={{ marginTop: '1rem' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', marginBottom: '0.3rem' }}>
                          <span>Capacity Utilization</span>
                          <span><strong>{s.bookedCount}</strong> / {s.capacity} Booked</span>
                        </div>
                        <div style={{ width: '100%', height: 6, background: 'rgba(255, 255, 255, 0.1)', borderRadius: 3, overflow: 'hidden' }}>
                          <div
                            style={{
                              width: `${Math.min(100, (s.bookedCount / s.capacity) * 100)}%`,
                              height: '100%',
                              background: isFull ? '#ef4444' : s.bookedCount > s.capacity * 0.75 ? '#f59e0b' : '#10b981',
                              borderRadius: 3,
                            }}
                          />
                        </div>
                      </div>
                    </div>

                    {/* Actions */}
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.25rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color)' }}>
                      <button
                        className="btn btn-secondary"
                        style={{ fontSize: '0.8rem', padding: '0.35rem 0.75rem' }}
                        onClick={() => setSelectedScheduleForAttendance(s)}
                      >
                        <UserCheck size={14} /> Attendance ({s.bookedCount})
                      </button>

                      <div style={{ display: 'flex', gap: '0.4rem' }}>
                        <button
                          className="btn-icon"
                          title="Edit Session"
                          onClick={() => {
                            setEditingSchedule(s);
                            setShowScheduleModal(true);
                          }}
                        >
                          <Edit2 size={15} />
                        </button>
                        {!isCancelled && (
                          <button
                            className="btn-icon"
                            title="Cancel Session"
                            style={{ color: '#ef4444' }}
                            onClick={() => handleCancelSchedule(s.id)}
                          >
                            <Ban size={15} />
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '1rem', marginTop: '2rem' }}>
              <button
                className="btn btn-secondary"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                <ChevronLeft size={16} /> Prev
              </button>
              <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                Page {page} of {totalPages}
              </span>
              <button
                className="btn btn-secondary"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                Next <ChevronRight size={16} />
              </button>
            </div>
          )}
        </div>
      )}

      {/* TAB 2: FITNESS CLASSES */}
      {activeTab === 'classes' && (
        <div>
          {/* Search & Filters */}
          <div
            className="glass-card"
            style={{
              padding: '1rem',
              marginBottom: '1.5rem',
              display: 'flex',
              gap: '1rem',
              flexWrap: 'wrap',
              alignItems: 'center',
            }}
          >
            <div style={{ position: 'relative', flex: 1, minWidth: '220px' }}>
              <Search size={16} style={{ position: 'absolute', left: '0.75rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input
                type="text"
                className="input-field"
                style={{ paddingLeft: '2.25rem' }}
                placeholder="Search classes by title or description..."
                value={searchTerm}
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setPage(1);
                }}
              />
            </div>

            <select
              className="input-field"
              style={{ width: 'auto' }}
              value={categoryFilter}
              onChange={(e) => {
                setCategoryFilter(e.target.value);
                setPage(1);
              }}
            >
              <option value="">All Categories</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>

            <select
              className="input-field"
              style={{ width: 'auto' }}
              value={intensityFilter}
              onChange={(e) => {
                setIntensityFilter(e.target.value);
                setPage(1);
              }}
            >
              <option value="">All Intensities</option>
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Extreme">Extreme</option>
            </select>
          </div>

          {loading ? (
            <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
              Loading classes catalog...
            </div>
          ) : classes.length === 0 ? (
            <div className="glass-card" style={{ padding: '3rem', textAlign: 'center' }}>
              <Dumbbell size={48} color="var(--text-secondary)" style={{ marginBottom: '1rem' }} />
              <h3 style={{ fontSize: '1.1rem', fontWeight: 600 }}>No Fitness Classes Found</h3>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Add your first class definition using the "New Fitness Class" button.
              </p>
            </div>
          ) : (
            <div className="glass-card" style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)', fontSize: '0.8rem', textTransform: 'uppercase' }}>
                    <th style={{ padding: '1rem' }}>Class Name</th>
                    <th style={{ padding: '1rem' }}>Category</th>
                    <th style={{ padding: '1rem' }}>Duration</th>
                    <th style={{ padding: '1rem' }}>Capacity</th>
                    <th style={{ padding: '1rem' }}>Intensity</th>
                    <th style={{ padding: '1rem' }}>Total Sessions</th>
                    <th style={{ padding: '1rem', textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {classes.map((cls) => (
                    <tr key={cls.id} style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.05)', fontSize: '0.875rem' }}>
                      <td style={{ padding: '1rem' }}>
                        <div style={{ fontWeight: 600 }}>{cls.name}</div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', maxWidth: '320px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                          {cls.description}
                        </div>
                      </td>
                      <td style={{ padding: '1rem' }}>
                        <span style={{ fontSize: '0.8rem', padding: '0.2rem 0.5rem', background: 'rgba(99, 102, 241, 0.12)', color: '#6366f1', borderRadius: '4px' }}>
                          {cls.categoryName}
                        </span>
                      </td>
                      <td style={{ padding: '1rem' }}>{cls.durationMinutes} mins</td>
                      <td style={{ padding: '1rem' }}>{cls.defaultCapacity} spots</td>
                      <td style={{ padding: '1rem' }}>
                        <span
                          style={{
                            fontSize: '0.75rem',
                            fontWeight: 600,
                            padding: '0.2rem 0.5rem',
                            borderRadius: '4px',
                            background:
                              cls.intensityLevel === 'High' || cls.intensityLevel === 'Extreme'
                                ? 'rgba(239, 68, 68, 0.15)'
                                : 'rgba(16, 185, 129, 0.15)',
                            color:
                              cls.intensityLevel === 'High' || cls.intensityLevel === 'Extreme'
                                ? '#ef4444'
                                : '#10b981',
                          }}
                        >
                          {cls.intensityLevel}
                        </span>
                      </td>
                      <td style={{ padding: '1rem' }}>{cls.totalSchedulesCount}</td>
                      <td style={{ padding: '1rem', textAlign: 'right' }}>
                        <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                          <button
                            className="btn-icon"
                            title="Edit Class"
                            onClick={() => {
                              setEditingClass(cls);
                              setShowClassModal(true);
                            }}
                          >
                            <Edit2 size={16} />
                          </button>
                          <button
                            className="btn-icon"
                            title="Delete Class"
                            style={{ color: '#ef4444' }}
                            onClick={() => handleDeleteClass(cls.id, cls.name)}
                          >
                            <Trash2 size={16} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* TAB 3: CATEGORIES */}
      {activeTab === 'categories' && (
        <div>
          {loading ? (
            <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
              Loading categories...
            </div>
          ) : categoriesData.length === 0 ? (
            <div className="glass-card" style={{ padding: '3rem', textAlign: 'center' }}>
              <Tags size={48} color="var(--text-secondary)" style={{ marginBottom: '1rem' }} />
              <h3 style={{ fontSize: '1.1rem', fontWeight: 600 }}>No Categories Created</h3>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Create categories to group classes by discipline (e.g. HIIT, Yoga, Strength).
              </p>
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1.25rem' }}>
              {categoriesData.map((cat) => (
                <div key={cat.id} className="glass-card" style={{ padding: '1.25rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <div>
                      <h3 style={{ fontSize: '1.05rem', fontWeight: 700, margin: '0 0 0.4rem' }}>{cat.name}</h3>
                      <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', margin: 0 }}>
                        {cat.description || 'No description provided.'}
                      </p>
                    </div>
                    <div style={{ display: 'flex', gap: '0.3rem' }}>
                      <button
                        className="btn-icon"
                        title="Edit Category"
                        onClick={() => {
                          setEditingCategory(cat);
                          setShowCategoryModal(true);
                        }}
                      >
                        <Edit2 size={15} />
                      </button>
                      <button
                        className="btn-icon"
                        title="Delete Category"
                        style={{ color: '#ef4444' }}
                        onClick={() => handleDeleteCategory(cat.id, cat.name)}
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  </div>

                  <div style={{ marginTop: '1rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color)', fontSize: '0.8rem', color: 'var(--text-secondary)', display: 'flex', justifyContent: 'space-between' }}>
                    <span>Assigned Classes</span>
                    <strong>{cat.fitnessClassCount} classes</strong>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* TAB 4: MEMBER BOOKINGS */}
      {activeTab === 'bookings' && (
        <div>
          {loading ? (
            <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
              Loading member reservations...
            </div>
          ) : bookings.length === 0 ? (
            <div className="glass-card" style={{ padding: '3rem', textAlign: 'center' }}>
              <Users size={48} color="var(--text-secondary)" style={{ marginBottom: '1rem' }} />
              <h3 style={{ fontSize: '1.1rem', fontWeight: 600 }}>No Bookings On Record</h3>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                When gym members reserve class spots from the Flutter mobile app, they will appear here.
              </p>
            </div>
          ) : (
            <div className="glass-card" style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)', fontSize: '0.8rem', textTransform: 'uppercase' }}>
                    <th style={{ padding: '1rem' }}>Member</th>
                    <th style={{ padding: '1rem' }}>Class & Session</th>
                    <th style={{ padding: '1rem' }}>Trainer</th>
                    <th style={{ padding: '1rem' }}>Date & Time</th>
                    <th style={{ padding: '1rem' }}>Booking Status</th>
                    <th style={{ padding: '1rem' }}>Attendance</th>
                    <th style={{ padding: '1rem', textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {bookings.map((b) => {
                    const isConfirmed = b.status === 1;
                    const isCancelled = b.status === 2;

                    return (
                      <tr key={b.id} style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.05)', fontSize: '0.875rem' }}>
                        <td style={{ padding: '1rem' }}>
                          <div style={{ fontWeight: 600 }}>{b.memberName}</div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{b.memberEmail}</div>
                        </td>
                        <td style={{ padding: '1rem' }}>
                          <div style={{ fontWeight: 600 }}>{b.className}</div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{b.room}</div>
                        </td>
                        <td style={{ padding: '1rem' }}>{b.trainerName}</td>
                        <td style={{ padding: '1rem' }}>
                          <div>{new Date(b.classStartTime).toLocaleDateString()}</div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                            {new Date(b.classStartTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </div>
                        </td>
                        <td style={{ padding: '1rem' }}>
                          <span
                            style={{
                              fontSize: '0.75rem',
                              fontWeight: 600,
                              padding: '0.2rem 0.5rem',
                              borderRadius: '4px',
                              background: isConfirmed ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.15)',
                              color: isConfirmed ? '#10b981' : '#ef4444',
                            }}
                          >
                            {isConfirmed ? 'Confirmed' : 'Cancelled'}
                          </span>
                        </td>
                        <td style={{ padding: '1rem' }}>
                          {b.attendance ? (
                            <span
                              style={{
                                fontSize: '0.75rem',
                                padding: '0.2rem 0.5rem',
                                borderRadius: '4px',
                                background: 'rgba(99, 102, 241, 0.15)',
                                color: '#6366f1',
                              }}
                            >
                              {b.attendance.status === 1 ? 'Attended' : b.attendance.status === 2 ? 'Absent' : 'Excused'}
                            </span>
                          ) : (
                            <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Unmarked</span>
                          )}
                        </td>
                        <td style={{ padding: '1rem', textAlign: 'right' }}>
                          {isConfirmed && (
                            <button
                              className="btn btn-secondary"
                              style={{ fontSize: '0.75rem', padding: '0.25rem 0.5rem', color: '#ef4444' }}
                              onClick={() => handleCancelBooking(b.id)}
                            >
                              Cancel Booking
                            </button>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* MODALS */}
      {showClassModal && (
        <ClassModal
          fitnessClass={editingClass}
          categories={categories}
          onClose={() => setShowClassModal(false)}
          onSaved={() => {
            setShowClassModal(false);
            loadData();
            loadAuxiliaryData();
          }}
        />
      )}

      {showScheduleModal && (
        <ScheduleModal
          schedule={editingSchedule}
          classes={classesList}
          trainers={trainers}
          onClose={() => setShowScheduleModal(false)}
          onSaved={() => {
            setShowScheduleModal(false);
            loadData();
          }}
        />
      )}

      {showCategoryModal && (
        <CategoryModal
          category={editingCategory}
          onClose={() => setShowCategoryModal(false)}
          onSaved={() => {
            setShowCategoryModal(false);
            loadData();
            loadAuxiliaryData();
          }}
        />
      )}

      {selectedScheduleForAttendance && (
        <AttendanceSheetModal
          schedule={selectedScheduleForAttendance}
          onClose={() => setSelectedScheduleForAttendance(null)}
          onUpdated={() => {
            loadData();
          }}
        />
      )}
    </div>
  );
};

export default ClassSchedulePage;
