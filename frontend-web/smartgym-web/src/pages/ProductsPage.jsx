import React, { useState, useEffect } from 'react';
import { inventoryApi } from '../services/inventoryApi';
import PageHeader from '../components/common/PageHeader';
import SearchBar from '../components/common/SearchBar';
import Pagination from '../components/common/Pagination';
import ProductModal from '../components/inventory/ProductModal';
import ConfirmationModal from '../components/common/ConfirmationModal';
import EmptyState from '../components/common/EmptyState';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { Package, Plus, Edit2, Trash2, AlertTriangle, Tag } from 'lucide-react';

export const ProductsPage = () => {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters & Pagination
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Modals
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [deleteTargetId, setDeleteTargetId] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const fetchProducts = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await inventoryApi.getProducts({
        search: search.trim() || undefined,
        categoryId: selectedCategory || undefined,
        page: pageNumber,
        pageSize,
      });
      setProducts(data.items || data || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      console.error('Failed to load products:', err);
      setError('Unable to load product catalogue.');
    } finally {
      setLoading(false);
    }
  };

  const loadDependencies = async () => {
    try {
      const [cats, supps] = await Promise.all([
        inventoryApi.getCategories(),
        inventoryApi.getSuppliers({ pageSize: 50 }),
      ]);
      setCategories(cats || []);
      setSuppliers(supps.items || supps || []);
    } catch (err) {
      console.error('Failed to load categories/suppliers:', err);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, [selectedCategory, pageNumber, pageSize]);

  useEffect(() => {
    loadDependencies();
  }, []);

  const handleSearchSubmit = (e) => {
    e?.preventDefault();
    setPageNumber(1);
    fetchProducts();
  };

  const handleSaveProduct = async (productData) => {
    if (selectedProduct) {
      await inventoryApi.updateProduct(selectedProduct.id, productData);
    } else {
      await inventoryApi.createProduct(productData);
    }
    setIsModalOpen(false);
    setSelectedProduct(null);
    fetchProducts();
  };

  const handleDeleteConfirm = async () => {
    if (!deleteTargetId) return;
    setIsDeleting(true);
    try {
      await inventoryApi.deleteProduct(deleteTargetId);
      setDeleteTargetId(null);
      fetchProducts();
    } catch (err) {
      console.error('Failed to delete product:', err);
      setError('Failed to delete product.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Supplement Catalogue"
        subtitle="Manage inventory items, SKUs, wholesale pricing, suppliers and thresholds"
        icon={Package}
        badge={`${totalCount} Products`}
        actions={
          <button
            onClick={() => {
              setSelectedProduct(null);
              setIsModalOpen(true);
            }}
            className="btn btn-primary"
          >
            <Plus size={16} /> New Product
          </button>
        }
      />

      {/* Filter / Search Bar */}
      <div className="glass-card" style={{ padding: '1rem 1.25rem', marginBottom: '1.5rem', display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.75rem', flex: 1, maxWidth: 360 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search by product name or SKU..."
            onClear={() => {
              setSearch('');
              setPageNumber(1);
              fetchProducts();
            }}
          />
        </form>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Category:</span>
          <select
            className="form-select"
            style={{ width: 'auto', padding: '0.35rem 0.75rem' }}
            value={selectedCategory}
            onChange={(e) => {
              setSelectedCategory(e.target.value);
              setPageNumber(1);
            }}
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
        <LoadingSpinner message="Loading product catalogue..." />
      ) : products.length === 0 ? (
        <EmptyState
          icon={Package}
          title="No products found"
          description="Register your first inventory product or adjust active search filters."
          action={
            <button
              onClick={() => {
                setSelectedProduct(null);
                setIsModalOpen(true);
              }}
              className="btn btn-primary"
            >
              <Plus size={16} /> Add Product
            </button>
          }
        />
      ) : (
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Product & SKU</th>
                <th>Category</th>
                <th>Supplier</th>
                <th>Unit Price</th>
                <th>Stock on Hand</th>
                <th>Threshold</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {products.map((p) => {
                const isLow = (p.quantityInStock || 0) <= (p.reorderLevel || p.reorderThreshold || 10);
                return (
                  <tr key={p.id}>
                    <td>
                      <div style={{ fontWeight: 600, color: '#ffffff' }}>
                        {p.name}
                      </div>
                      <div style={{ fontSize: '0.75rem', fontFamily: 'monospace', color: 'var(--text-muted)' }}>
                        SKU: {p.sku || 'N/A'}
                      </div>
                    </td>
                    <td>
                      <span className="badge badge-info" style={{ fontSize: '0.75rem' }}>
                        {p.categoryName || 'Supplement'}
                      </span>
                    </td>
                    <td>
                      <span style={{ fontSize: '0.85rem', color: 'var(--text-primary)' }}>
                        {p.supplierName || 'Primary Supplier'}
                      </span>
                    </td>
                    <td>
                      <strong style={{ color: '#ffffff', fontSize: '0.9rem' }}>
                        ${p.price?.toFixed(2)}
                      </strong>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        <span style={{ fontWeight: 700, color: isLow ? 'var(--accent-rose)' : 'var(--accent-emerald)', fontSize: '0.95rem' }}>
                          {p.quantityInStock ?? 0}
                        </span>
                        {isLow && (
                          <span className="badge badge-danger" style={{ fontSize: '0.65rem' }}>
                            LOW
                          </span>
                        )}
                      </div>
                    </td>
                    <td>
                      <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                        {p.reorderLevel ?? p.reorderThreshold ?? 10} units
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                        <button
                          onClick={() => {
                            setSelectedProduct(p);
                            setIsModalOpen(true);
                          }}
                          className="btn btn-secondary"
                          style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                        >
                          <Edit2 size={13} /> Edit
                        </button>
                        <button
                          onClick={() => setDeleteTargetId(p.id)}
                          className="btn btn-danger"
                          style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                        >
                          <Trash2 size={13} /> Delete
                        </button>
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

      {isModalOpen && (
        <ProductModal
          product={selectedProduct}
          categories={categories}
          suppliers={suppliers}
          onClose={() => {
            setIsModalOpen(false);
            setSelectedProduct(null);
          }}
          onSave={handleSaveProduct}
        />
      )}

      <ConfirmationModal
        isOpen={!!deleteTargetId}
        title="Delete Product"
        message="Are you sure you want to delete this supplement product? Historical inventory logs will be archived."
        confirmText="Delete Product"
        isDestructive={true}
        isLoading={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTargetId(null)}
      />
    </div>
  );
};

export default ProductsPage;
