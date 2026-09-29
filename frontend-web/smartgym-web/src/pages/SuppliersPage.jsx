import React, { useState, useEffect } from 'react';
import { inventoryApi } from '../services/inventoryApi';
import PageHeader from '../components/common/PageHeader';
import SearchBar from '../components/common/SearchBar';
import Pagination from '../components/common/Pagination';
import SupplierModal from '../components/inventory/SupplierModal';
import ConfirmationModal from '../components/common/ConfirmationModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Truck, Plus, Mail, Phone, MapPin, Edit2, Trash2 } from 'lucide-react';

export const SuppliersPage = () => {
  const [suppliers, setSuppliers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Modals
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedSupplier, setSelectedSupplier] = useState(null);
  const [deleteTargetId, setDeleteTargetId] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const fetchSuppliers = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await inventoryApi.getSuppliers({
        search: search.trim() || undefined,
        page: pageNumber,
        pageSize,
      });
      setSuppliers(data.items || data || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load suppliers:', err);
      setError('Unable to load suppliers directory.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSuppliers();
  }, [pageNumber, pageSize]);

  const handleSearchSubmit = (e) => {
    e?.preventDefault();
    setPageNumber(1);
    fetchSuppliers();
  };

  const handleSaveSupplier = async (supplierData) => {
    if (selectedSupplier) {
      await inventoryApi.updateSupplier(selectedSupplier.id, supplierData);
    } else {
      await inventoryApi.createSupplier(supplierData);
    }
    setIsModalOpen(false);
    setSelectedSupplier(null);
    fetchSuppliers();
  };

  const handleDeleteConfirm = async () => {
    if (!deleteTargetId) return;
    setIsDeleting(true);
    try {
      await inventoryApi.deleteSupplier(deleteTargetId);
      setDeleteTargetId(null);
      fetchSuppliers();
    } catch (err) {
      console.error('Failed to delete supplier:', err);
      setError('Failed to delete supplier. It may be associated with existing inventory products.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Suppliers & Vendors"
        subtitle="Manage certified supplement and gym equipment maintenance vendors"
        icon={Truck}
        badge={`${totalCount} Partners`}
        actions={
          <button
            onClick={() => {
              setSelectedSupplier(null);
              setIsModalOpen(true);
            }}
            className="btn btn-primary"
          >
            <Plus size={16} /> New Supplier
          </button>
        }
      />

      {/* Filter / Search Bar */}
      <div className="glass-card" style={{ padding: '1rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center' }}>
        <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.75rem', flex: 1, maxWidth: 380 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search supplier name or contact..."
            onClear={() => {
              setSearch('');
              setPageNumber(1);
              fetchSuppliers();
            }}
          />
          <button type="submit" className="btn btn-primary" style={{ padding: '0.5rem 1rem' }}>
            Search
          </button>
        </form>
      </div>

      {error && (
        <div style={{ padding: '1rem', background: 'rgba(244,63,94,0.15)', border: '1px solid rgba(244,63,94,0.3)', borderRadius: 'var(--radius-sm)', color: '#fb7185', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {loading ? (
        <LoadingSpinner message="Loading suppliers..." />
      ) : suppliers.length === 0 ? (
        <EmptyState
          icon={Truck}
          title="No suppliers found"
          description="Register your first supplier partner to associate supplement products and purchase orders."
          action={
            <button
              onClick={() => {
                setSelectedSupplier(null);
                setIsModalOpen(true);
              }}
              className="btn btn-primary"
            >
              <Plus size={16} /> Add Supplier
            </button>
          }
        />
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1.5rem' }}>
          {suppliers.map((s) => (
            <div
              key={s.id}
              className="glass-card"
              style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}
            >
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
                  <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: '#ffffff' }}>
                    {s.name}
                  </h3>
                  <span className="badge badge-success" style={{ fontSize: '0.7rem' }}>
                    Active Vendor
                  </span>
                </div>

                <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
                  Contact: <strong style={{ color: 'var(--text-primary)' }}>{s.contactPerson || 'Official Representative'}</strong>
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.45rem', fontSize: '0.825rem', color: 'var(--text-secondary)', borderTop: '1px solid var(--border-subtle)', paddingTop: '0.75rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Mail size={14} color="var(--primary)" /> {s.email || 'N/A'}
                  </div>
                  {s.phone && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <Phone size={14} color="var(--accent-cyan)" /> {s.phone}
                    </div>
                  )}
                  {s.address && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--text-muted)' }}>
                      <MapPin size={14} /> {s.address}
                    </div>
                  )}
                </div>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginTop: '1.5rem', borderTop: '1px solid var(--border-subtle)', paddingTop: '1rem' }}>
                <button
                  onClick={() => {
                    setSelectedSupplier(s);
                    setIsModalOpen(true);
                  }}
                  className="btn btn-secondary"
                  style={{ padding: '0.35rem 0.65rem', fontSize: '0.8rem' }}
                >
                  <Edit2 size={13} /> Edit
                </button>
                <button
                  onClick={() => setDeleteTargetId(s.id)}
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

      {totalPages > 1 && (
        <Pagination
          pageNumber={pageNumber}
          pageSize={pageSize}
          totalCount={totalCount}
          totalPages={totalPages}
          onPageChange={setPageNumber}
          onPageSizeChange={setPageSize}
        />
      )}

      {isModalOpen && (
        <SupplierModal
          supplier={selectedSupplier}
          onClose={() => {
            setIsModalOpen(false);
            setSelectedSupplier(null);
          }}
          onSave={handleSaveSupplier}
        />
      )}

      <ConfirmationModal
        isOpen={!!deleteTargetId}
        title="Delete Supplier"
        message="Are you sure you want to delete this supplier? This action cannot be undone."
        confirmText="Delete Supplier"
        isDestructive={true}
        isLoading={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTargetId(null)}
      />
    </div>
  );
};

export default SuppliersPage;
