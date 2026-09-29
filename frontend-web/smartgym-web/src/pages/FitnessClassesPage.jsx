import React, { useState, useEffect } from 'react';
import { classesApi } from '../services/classesApi';
import PageHeader from '../components/common/PageHeader';
import SearchBar from '../components/common/SearchBar';
import ClassModal from '../components/classes/ClassModal';
import CategoryModal from '../components/classes/CategoryModal';
import ConfirmationModal from '../components/common/ConfirmationModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Calendar, Plus, Clock, Users, Flame, Edit2, Trash2, FolderPlus } from 'lucide-react';

export const FitnessClassesPage = () => {
  const [classes, setClasses] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');

  // Modals
  const [isClassModalOpen, setIsClassModalOpen] = useState(false);
  const [isCategoryModalOpen, setIsCategoryModalOpen] = useState(false);
  const [selectedClass, setSelectedClass] = useState(null);
  const [deleteTargetId, setDeleteTargetId] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const fetchClassesAndCategories = async () => {
    setLoading(true);
    setError(null);
    try {
      const [classData, catData] = await Promise.all([
        classesApi.getClasses({
          search: search.trim() || undefined,
          categoryId: selectedCategory || undefined,
        }),
        classesApi.getCategories(),
      ]);
      setClasses(classData.items || classData || []);
      setCategories(catData || []);
    } catch (err) {
      console.error('Failed to load classes:', err);
      setError('Unable to load fitness classes.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchClassesAndCategories();
  }, [selectedCategory]);

  const handleSearchSubmit = (e) => {
    e?.preventDefault();
    fetchClassesAndCategories();
  };

  const handleSaveClass = async (classData) => {
    if (selectedClass) {
      await classesApi.updateClass(selectedClass.id, classData);
    } else {
      await classesApi.createClass(classData);
    }
    setIsClassModalOpen(false);
    setSelectedClass(null);
    fetchClassesAndCategories();
  };

  const handleDeleteConfirm = async () => {
    if (!deleteTargetId) return;
    setIsDeleting(true);
    try {
      await classesApi.deleteClass(deleteTargetId);
      setDeleteTargetId(null);
      fetchClassesAndCategories();
    } catch (err) {
      console.error('Failed to delete class:', err);
      setError('Failed to delete class. It may have existing scheduled sessions.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Fitness Classes"
        subtitle="Catalog of workout types, capacity quotas, intensity levels and categories"
        icon={Calendar}
        badge={`${classes.length} Classes`}
        actions={
          <>
            <button
              onClick={() => setIsCategoryModalOpen(true)}
              className="btn btn-secondary"
            >
              <FolderPlus size={16} /> Categories
            </button>
            <button
              onClick={() => {
                setSelectedClass(null);
                setIsClassModalOpen(true);
              }}
              className="btn btn-primary"
            >
              <Plus size={16} /> New Class
            </button>
          </>
        }
      />

      {/* Filter and Search Bar */}
      <div className="glass-card" style={{ padding: '1rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.75rem', flex: 1, maxWidth: 360 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search class title..."
            onClear={() => {
              setSearch('');
              fetchClassesAndCategories();
            }}
          />
        </form>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Category:</span>
          <select
            className="form-select"
            style={{ width: 'auto', padding: '0.35rem 0.75rem' }}
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
          >
            <option value="">All Categories</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
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
        <LoadingSpinner message="Loading fitness classes..." />
      ) : classes.length === 0 ? (
        <EmptyState
          icon={Calendar}
          title="No fitness classes found"
          description="Create your first fitness class or clear active search filters."
          action={
            <button
              onClick={() => {
                setSelectedClass(null);
                setIsClassModalOpen(true);
              }}
              className="btn btn-primary"
            >
              <Plus size={16} /> Create Class
            </button>
          }
        />
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1.5rem' }}>
          {classes.map((cls) => (
            <div
              key={cls.id}
              className="glass-card"
              style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}
            >
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: '#ffffff' }}>
                    {cls.name}
                  </h3>
                  <span className="badge badge-info" style={{ fontSize: '0.7rem' }}>
                    {cls.categoryName || 'General'}
                  </span>
                </div>

                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: '1.25rem', minHeight: 40 }}>
                  {cls.description || 'Dynamic fitness class designed for progressive athletic conditioning.'}
                </p>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '0.5rem', padding: '0.75rem', background: 'rgba(255,255,255,0.02)', borderRadius: 'var(--radius-sm)', marginBottom: '1rem' }}>
                  <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Duration</div>
                    <div style={{ fontWeight: 700, color: '#ffffff', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.2rem', marginTop: '0.2rem' }}>
                      <Clock size={13} color="var(--primary)" /> {cls.durationMinutes}m
                    </div>
                  </div>
                  <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Capacity</div>
                    <div style={{ fontWeight: 700, color: '#ffffff', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.2rem', marginTop: '0.2rem' }}>
                      <Users size={13} color="var(--accent-cyan)" /> {cls.defaultCapacity}
                    </div>
                  </div>
                  <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Intensity</div>
                    <div style={{ fontWeight: 700, color: 'var(--accent-amber)', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.2rem', marginTop: '0.2rem' }}>
                      <Flame size={13} /> {cls.difficultyLevel || 'All'}
                    </div>
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', borderTop: '1px solid var(--border-subtle)', paddingTop: '1rem' }}>
                <button
                  onClick={() => {
                    setSelectedClass(cls);
                    setIsClassModalOpen(true);
                  }}
                  className="btn btn-secondary"
                  style={{ padding: '0.35rem 0.65rem', fontSize: '0.8rem' }}
                >
                  <Edit2 size={13} /> Edit
                </button>
                <button
                  onClick={() => setDeleteTargetId(cls.id)}
                  className="btn btn-danger"
                  style={{ padding: '0.35rem 0.65rem', fontSize: '0.8rem' }}
                >
                  <Trash2 size={13} /> Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {isClassModalOpen && (
        <ClassModal
          fitnessClass={selectedClass}
          categories={categories}
          onClose={() => {
            setIsClassModalOpen(false);
            setSelectedClass(null);
          }}
          onSave={handleSaveClass}
        />
      )}

      {isCategoryModalOpen && (
        <CategoryModal
          categories={categories}
          onClose={() => setIsCategoryModalOpen(false)}
          onCategoryUpdated={fetchClassesAndCategories}
        />
      )}

      <ConfirmationModal
        isOpen={!!deleteTargetId}
        title="Delete Fitness Class"
        message="Are you sure you want to permanently delete this fitness class? Scheduled sessions will be impacted."
        confirmText="Delete Class"
        isDestructive={true}
        isLoading={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTargetId(null)}
      />
    </div>
  );
};

export default FitnessClassesPage;
