import React, { useState, useEffect, useCallback } from 'react';
import {
  Package,
  AlertTriangle,
  UploadCloud,
  DownloadCloud,
  Search,
  Filter,
  ArrowUpDown,
  Plus,
  RefreshCw,
  Building2,
  Boxes,
  CheckCircle2,
  Trash2,
  Edit,
  History,
  ShoppingCart,
  Layers,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import inventoryApi from '../services/inventoryApi';
import StockAdjustmentModal from '../components/inventory/StockAdjustmentModal';
import ReorderModal from '../components/inventory/ReorderModal';
import StockHistoryModal from '../components/inventory/StockHistoryModal';
import SupplierModal from '../components/inventory/SupplierModal';
import ProductModal from '../components/inventory/ProductModal';
import CsvImportExportModal from '../components/inventory/CsvImportExportModal';

export const InventoryPage = () => {
  // Navigation Tabs: 'inventory' | 'lowStock' | 'products' | 'suppliers'
  const [activeTab, setActiveTab] = useState('inventory');

  // Search & Filter State
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedSupplier, setSelectedSupplier] = useState('');
  const [sortBy, setSortBy] = useState('ProductName');
  const [sortDesc, setSortDesc] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Data State
  const [inventoryData, setInventoryData] = useState({ items: [], totalCount: 0, totalPages: 1 });
  const [productsData, setProductsData] = useState({ items: [], totalCount: 0, totalPages: 1 });
  const [suppliersData, setSuppliersData] = useState({ items: [], totalCount: 0, totalPages: 1 });
  const [categories, setCategories] = useState([]);
  const [suppliersList, setSuppliersList] = useState([]);

  // Stats
  const [kpiStats, setKpiStats] = useState({
    totalItems: 0,
    lowStockCount: 0,
    totalUnits: 0,
    totalSuppliers: 0,
  });

  // UI States
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);

  // Modal Control States
  const [adjustModalItem, setAdjustModalItem] = useState(null);
  const [reorderModalItem, setReorderModalItem] = useState(null);
  const [historyModalItem, setHistoryModalItem] = useState(null);
  const [productModalItem, setProductModalItem] = useState(null); // null for new, obj for edit
  const [isProductModalOpen, setIsProductModalOpen] = useState(false);
  const [supplierModalItem, setSupplierModalItem] = useState(null);
  const [isSupplierModalOpen, setIsSupplierModalOpen] = useState(false);
  const [isCsvModalOpen, setIsCsvModalOpen] = useState(false);

  // Notification helper
  const showSuccess = (msg) => {
    setSuccessMessage(msg);
    setTimeout(() => setSuccessMessage(null), 4000);
  };

  // Initial Dropdowns & KPI Loading
  const loadMetadata = async () => {
    try {
      const [catRes, supRes] = await Promise.all([
        inventoryApi.getCategories(),
        inventoryApi.getSuppliers({ pageSize: 100 }),
      ]);
      setCategories(catRes || []);
      setSuppliersList(supRes.items || []);
    } catch (err) {
      console.error('Failed to load metadata', err);
    }
  };

  useEffect(() => {
    loadMetadata();
  }, []);

  // Main data fetch based on active tab
  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      if (activeTab === 'inventory' || activeTab === 'lowStock') {
        const isLow = activeTab === 'lowStock' ? true : undefined;
        const res = await inventoryApi.getInventory({
          pageNumber,
          pageSize,
          searchTerm: searchTerm.trim() || undefined,
          categoryId: selectedCategory || undefined,
          supplierId: selectedSupplier || undefined,
          isLowStock: isLow,
          sortBy,
          sortDirection: sortDesc ? 1 : 0,
        });
        setInventoryData(res);

        // Update KPI stats
        const allItemsRes = await inventoryApi.getInventory({ pageSize: 200 });
        const allItems = allItemsRes.items || [];
        setKpiStats({
          totalItems: allItemsRes.totalCount || allItems.length,
          lowStockCount: allItems.filter((i) => i.isLowStock).length,
          totalUnits: allItems.reduce((acc, i) => acc + (i.quantityInStock || 0), 0),
          totalSuppliers: suppliersList.length,
        });
      } else if (activeTab === 'products') {
        const res = await inventoryApi.getProducts({
          pageNumber,
          pageSize,
          searchTerm: searchTerm.trim() || undefined,
          categoryId: selectedCategory || undefined,
          supplierId: selectedSupplier || undefined,
          sortBy,
          sortDirection: sortDesc ? 1 : 0,
        });
        setProductsData(res);
      } else if (activeTab === 'suppliers') {
        const res = await inventoryApi.getSuppliers({
          pageNumber,
          pageSize,
          searchTerm: searchTerm.trim() || undefined,
          sortBy: sortBy === 'ProductName' ? 'Name' : sortBy,
          sortDirection: sortDesc ? 1 : 0,
        });
        setSuppliersData(res);
      }
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Failed to fetch data from the server.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [activeTab, pageNumber, pageSize, searchTerm, selectedCategory, selectedSupplier, sortBy, sortDesc, suppliersList.length]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Reset page when switching tabs or filters
  const handleTabChange = (tab) => {
    setActiveTab(tab);
    setPageNumber(1);
    setSearchTerm('');
  };

  // Delete product handler
  const handleDeleteProduct = async (product) => {
    if (!window.confirm(`Are you sure you want to delete product "${product.name}"?`)) return;
    try {
      setLoading(true);
      await inventoryApi.deleteProduct(product.id);
      showSuccess(`Product "${product.name}" deleted successfully.`);
      fetchData();
    } catch (err) {
      setError(err.response?.data?.detail || 'Failed to delete product.');
    } finally {
      setLoading(false);
    }
  };

  // Delete supplier handler
  const handleDeleteSupplier = async (supplier) => {
    if (!window.confirm(`Are you sure you want to delete supplier "${supplier.name}"?`)) return;
    try {
      setLoading(true);
      await inventoryApi.deleteSupplier(supplier.id);
      showSuccess(`Supplier "${supplier.name}" deleted successfully.`);
      loadMetadata();
      fetchData();
    } catch (err) {
      setError(err.response?.data?.detail || 'Failed to delete supplier.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: 1400, margin: '0 auto' }}>
      {/* Page Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
            <Boxes size={28} color="var(--primary)" />
            Supplier & Supplement Inventory
          </h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
            Production inventory operations: stock tracking, threshold alarms, automated reorders & CSV data exchange.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
          <button className="btn btn-secondary" onClick={() => setIsCsvModalOpen(true)}>
            <UploadCloud size={16} /> CSV Import / Export
          </button>
          {activeTab === 'suppliers' ? (
            <button
              className="btn btn-primary"
              onClick={() => {
                setSupplierModalItem(null);
                setIsSupplierModalOpen(true);
              }}
            >
              <Plus size={16} /> Add Supplier
            </button>
          ) : (
            <button
              className="btn btn-primary"
              onClick={() => {
                setProductModalItem(null);
                setIsProductModalOpen(true);
              }}
            >
              <Plus size={16} /> Add Supplement Product
            </button>
          )}
        </div>
      </div>

      {/* KPI Stats Overview Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem', marginBottom: '1.75rem' }}>
        <div className="glass-card" style={{ padding: '1.25rem' }}>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase', fontWeight: 700 }}>
            Total Catalog Products
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 800, marginTop: '0.35rem' }}>
            {kpiStats.totalItems}
          </div>
        </div>

        <div
          className="glass-card"
          style={{
            padding: '1.25rem',
            cursor: 'pointer',
            borderColor: kpiStats.lowStockCount > 0 ? 'rgba(244, 63, 94, 0.4)' : undefined,
          }}
          onClick={() => handleTabChange('lowStock')}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: '0.75rem', color: '#fb7185', textTransform: 'uppercase', fontWeight: 700 }}>
              Low-Stock Alarms
            </span>
            <AlertTriangle size={18} color="var(--accent-rose)" />
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 800, marginTop: '0.35rem', color: kpiStats.lowStockCount > 0 ? 'var(--accent-rose)' : 'inherit' }}>
            {kpiStats.lowStockCount}
          </div>
        </div>

        <div className="glass-card" style={{ padding: '1.25rem' }}>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase', fontWeight: 700 }}>
            Units In Stock
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 800, marginTop: '0.35rem', color: 'var(--accent-cyan)' }}>
            {kpiStats.totalUnits}
          </div>
        </div>

        <div
          className="glass-card"
          style={{ padding: '1.25rem', cursor: 'pointer' }}
          onClick={() => handleTabChange('suppliers')}
        >
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase', fontWeight: 700 }}>
            Active Suppliers
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 800, marginTop: '0.35rem', color: 'var(--accent-emerald)' }}>
            {suppliersList.length}
          </div>
        </div>
      </div>

      {/* Global Alerts */}
      {successMessage && (
        <div className="alert alert-success">
          <CheckCircle2 size={18} />
          <span>{successMessage}</span>
        </div>
      )}

      {error && (
        <div className="alert alert-error">
          <AlertTriangle size={18} />
          <span>{error}</span>
          <button className="btn btn-secondary" style={{ marginLeft: 'auto', padding: '0.25rem 0.6rem', fontSize: '0.75rem' }} onClick={fetchData}>
            Retry
          </button>
        </div>
      )}

      {/* Navigation Tabs */}
      <div className="tabs-container">
        <button
          className={`tab-button ${activeTab === 'inventory' ? 'active' : ''}`}
          onClick={() => handleTabChange('inventory')}
        >
          <Boxes size={16} /> Inventory Stock
        </button>
        <button
          className={`tab-button ${activeTab === 'lowStock' ? 'active' : ''}`}
          onClick={() => handleTabChange('lowStock')}
        >
          <AlertTriangle size={16} color={kpiStats.lowStockCount > 0 ? 'var(--accent-rose)' : 'currentColor'} />
          Low Stock Alerts
          {kpiStats.lowStockCount > 0 && (
            <span style={{
              background: 'var(--accent-rose)',
              color: '#fff',
              fontSize: '0.7rem',
              borderRadius: '999px',
              padding: '0.1rem 0.45rem',
              fontWeight: 800
            }}>
              {kpiStats.lowStockCount}
            </span>
          )}
        </button>
        <button
          className={`tab-button ${activeTab === 'products' ? 'active' : ''}`}
          onClick={() => handleTabChange('products')}
        >
          <Package size={16} /> Products Catalog
        </button>
        <button
          className={`tab-button ${activeTab === 'suppliers' ? 'active' : ''}`}
          onClick={() => handleTabChange('suppliers')}
        >
          <Building2 size={16} /> Suppliers Directory
        </button>
      </div>

      {/* Filter and Control Bar */}
      <div style={{
        background: 'rgba(255, 255, 255, 0.02)',
        border: '1px solid var(--border-subtle)',
        borderRadius: 'var(--radius-md)',
        padding: '1rem',
        display: 'flex',
        alignItems: 'center',
        gap: '0.75rem',
        marginBottom: '1.25rem',
        flexWrap: 'wrap'
      }}>
        {/* Search */}
        <div style={{ position: 'relative', flex: '1 1 240px' }}>
          <Search size={16} style={{ position: 'absolute', left: 12, top: 12, color: 'var(--text-muted)' }} />
          <input
            type="text"
            className="form-input"
            style={{ paddingLeft: '2.2rem' }}
            placeholder={`Search ${activeTab}...`}
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value);
              setPageNumber(1);
            }}
          />
        </div>

        {/* Category Filter (if applicable) */}
        {activeTab !== 'suppliers' && (
          <select
            className="form-select"
            style={{ flex: '0 1 180px' }}
            value={selectedCategory}
            onChange={(e) => {
              setSelectedCategory(e.target.value);
              setPageNumber(1);
            }}
          >
            <option value="">All Categories</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        )}

        {/* Supplier Filter (if applicable) */}
        {activeTab !== 'suppliers' && (
          <select
            className="form-select"
            style={{ flex: '0 1 180px' }}
            value={selectedSupplier}
            onChange={(e) => {
              setSelectedSupplier(e.target.value);
              setPageNumber(1);
            }}
          >
            <option value="">All Suppliers</option>
            {suppliersList.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        )}

        {/* Sort selector */}
        <select
          className="form-select"
          style={{ flex: '0 1 160px' }}
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value)}
        >
          <option value="ProductName">Name</option>
          <option value="SKU">SKU</option>
          <option value="QuantityInStock">Stock Level</option>
          <option value="UpdatedAt">Last Updated</option>
        </select>

        {/* Sort Direction Toggle */}
        <button
          className="btn btn-secondary"
          title="Toggle Ascending/Descending"
          onClick={() => setSortDesc(!sortDesc)}
        >
          <ArrowUpDown size={15} />
          {sortDesc ? 'Desc' : 'Asc'}
        </button>

        {/* Refresh Button */}
        <button className="btn btn-secondary" title="Refresh" onClick={fetchData} disabled={loading}>
          <RefreshCw size={15} className={loading ? 'animate-spin' : ''} />
        </button>
      </div>

      {/* Main Content Tables */}
      <div className="table-wrapper">
        {loading && (
          <div style={{ textAlign: 'center', padding: '3.5rem', color: 'var(--text-secondary)' }}>
            <RefreshCw size={24} className="animate-spin" style={{ marginBottom: '0.75rem', color: 'var(--primary)' }} />
            <div>Loading live inventory data from ASP.NET Core API...</div>
          </div>
        )}

        {/* TAB 1 & 2: INVENTORY & LOW-STOCK */}
        {!loading && (activeTab === 'inventory' || activeTab === 'lowStock') && (
          <>
            {inventoryData.items.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '3.5rem', color: 'var(--text-secondary)' }}>
                <Boxes size={36} color="var(--text-muted)" style={{ marginBottom: '0.75rem' }} />
                <h3 style={{ fontSize: '1.1rem', fontWeight: 600 }}>No inventory items found</h3>
                <p style={{ fontSize: '0.85rem' }}>Try adjusting your filters or import products via CSV.</p>
              </div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>SKU</th>
                    <th>Product</th>
                    <th>Category</th>
                    <th>Supplier</th>
                    <th>Price</th>
                    <th>Stock</th>
                    <th>Threshold</th>
                    <th>Bin</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {inventoryData.items.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <strong style={{ color: 'var(--accent-cyan)' }}>{item.productSKU}</strong>
                      </td>
                      <td>
                        <div style={{ fontWeight: 600 }}>{item.productName}</div>
                      </td>
                      <td>
                        <span className="badge badge-info">{item.categoryName}</span>
                      </td>
                      <td>{item.supplierName}</td>
                      <td>${item.unitPrice?.toFixed(2)}</td>
                      <td>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <span style={{
                            fontWeight: 800,
                            fontSize: '1rem',
                            color: item.isLowStock ? 'var(--accent-rose)' : 'inherit'
                          }}>
                            {item.quantityInStock}
                          </span>
                          {item.isLowStock && (
                            <span className="badge badge-danger">Low Stock</span>
                          )}
                        </div>
                      </td>
                      <td>{item.reorderThreshold}</td>
                      <td><code>{item.locationBin || '—'}</code></td>
                      <td>
                        <div style={{ display: 'flex', gap: '0.4rem' }}>
                          <button
                            className="btn btn-secondary"
                            style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                            title="Adjust Stock"
                            onClick={() => setAdjustModalItem(item)}
                          >
                            <ArrowUpDown size={13} /> Adjust
                          </button>
                          <button
                            className="btn btn-primary"
                            style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                            title="Reorder"
                            onClick={() => setReorderModalItem(item)}
                          >
                            <ShoppingCart size={13} /> Reorder
                          </button>
                          <button
                            className="btn btn-secondary"
                            style={{ padding: '0.35rem 0.5rem', fontSize: '0.75rem' }}
                            title="Movement History"
                            onClick={() => setHistoryModalItem(item)}
                          >
                            <History size={13} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}

        {/* TAB 3: PRODUCTS CATALOG */}
        {!loading && activeTab === 'products' && (
          <>
            {productsData.items.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '3.5rem', color: 'var(--text-secondary)' }}>
                <Package size={36} color="var(--text-muted)" style={{ marginBottom: '0.75rem' }} />
                <h3 style={{ fontSize: '1.1rem', fontWeight: 600 }}>No products found</h3>
              </div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>SKU</th>
                    <th>Product Name</th>
                    <th>Category</th>
                    <th>Supplier</th>
                    <th>Unit Price</th>
                    <th>Cost Price</th>
                    <th>Current Stock</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {productsData.items.map((prod) => (
                    <tr key={prod.id}>
                      <td><strong>{prod.sku}</strong></td>
                      <td>
                        <div style={{ fontWeight: 600 }}>{prod.name}</div>
                        {prod.description && (
                          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', maxWidth: 260, textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                            {prod.description}
                          </div>
                        )}
                      </td>
                      <td><span className="badge badge-info">{prod.categoryName}</span></td>
                      <td>{prod.supplierName}</td>
                      <td>${prod.unitPrice?.toFixed(2)}</td>
                      <td>${prod.costPrice?.toFixed(2)}</td>
                      <td>
                        <strong style={{ color: prod.isLowStock ? 'var(--accent-rose)' : 'inherit' }}>
                          {prod.quantityInStock}
                        </strong>
                      </td>
                      <td>
                        <span className={`badge ${prod.isActive ? 'badge-success' : 'badge-danger'}`}>
                          {prod.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td>
                        <div style={{ display: 'flex', gap: '0.4rem' }}>
                          <button
                            className="btn btn-secondary"
                            style={{ padding: '0.35rem 0.6rem', fontSize: '0.75rem' }}
                            title="Edit Product"
                            onClick={() => {
                              setProductModalItem(prod);
                              setIsProductModalOpen(true);
                            }}
                          >
                            <Edit size={13} />
                          </button>
                          <button
                            className="btn btn-secondary"
                            style={{ padding: '0.35rem 0.6rem', fontSize: '0.75rem', color: 'var(--accent-rose)' }}
                            title="Delete Product"
                            onClick={() => handleDeleteProduct(prod)}
                          >
                            <Trash2 size={13} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}

        {/* TAB 4: SUPPLIERS DIRECTORY */}
        {!loading && activeTab === 'suppliers' && (
          <>
            {suppliersData.items.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '3.5rem', color: 'var(--text-secondary)' }}>
                <Building2 size={36} color="var(--text-muted)" style={{ marginBottom: '0.75rem' }} />
                <h3 style={{ fontSize: '1.1rem', fontWeight: 600 }}>No suppliers registered</h3>
              </div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Supplier Name</th>
                    <th>Contact Person</th>
                    <th>Email</th>
                    <th>Phone</th>
                    <th>Address</th>
                    <th>Products</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {suppliersData.items.map((sup) => (
                    <tr key={sup.id}>
                      <td><strong style={{ color: 'var(--text-primary)' }}>{sup.name}</strong></td>
                      <td>{sup.contactPerson || '—'}</td>
                      <td><a href={`mailto:${sup.email}`} style={{ color: 'var(--accent-cyan)' }}>{sup.email}</a></td>
                      <td>{sup.phone}</td>
                      <td style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>{sup.address || '—'}</td>
                      <td><span className="badge badge-info">{sup.productCount} SKUs</span></td>
                      <td>
                        <span className={`badge ${sup.isActive ? 'badge-success' : 'badge-danger'}`}>
                          {sup.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td>
                        <div style={{ display: 'flex', gap: '0.4rem' }}>
                          <button
                            className="btn btn-secondary"
                            style={{ padding: '0.35rem 0.6rem', fontSize: '0.75rem' }}
                            title="Edit Supplier"
                            onClick={() => {
                              setSupplierModalItem(sup);
                              setIsSupplierModalOpen(true);
                            }}
                          >
                            <Edit size={13} />
                          </button>
                          <button
                            className="btn btn-secondary"
                            style={{ padding: '0.35rem 0.6rem', fontSize: '0.75rem', color: 'var(--accent-rose)' }}
                            title="Delete Supplier"
                            onClick={() => handleDeleteSupplier(sup)}
                          >
                            <Trash2 size={13} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}
      </div>

      {/* Pagination Footer */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '1rem 0',
        marginTop: '0.5rem',
        flexWrap: 'wrap',
        gap: '1rem'
      }}>
        <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
          Showing Page <strong>{pageNumber}</strong> of{' '}
          <strong>
            {activeTab === 'suppliers'
              ? suppliersData.totalPages || 1
              : activeTab === 'products'
              ? productsData.totalPages || 1
              : inventoryData.totalPages || 1}
          </strong>{' '}
          (
          {activeTab === 'suppliers'
            ? suppliersData.totalCount
            : activeTab === 'products'
            ? productsData.totalCount
            : inventoryData.totalCount}{' '}
          total entries)
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <select
            className="form-select"
            style={{ width: 'auto', padding: '0.35rem 0.75rem', fontSize: '0.8rem' }}
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value));
              setPageNumber(1);
            }}
          >
            <option value={10}>10 per page</option>
            <option value={25}>25 per page</option>
            <option value={50}>50 per page</option>
          </select>

          <button
            className="btn btn-secondary"
            style={{ padding: '0.45rem 0.75rem' }}
            disabled={pageNumber <= 1 || loading}
            onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
          >
            <ChevronLeft size={16} /> Prev
          </button>

          <button
            className="btn btn-secondary"
            style={{ padding: '0.45rem 0.75rem' }}
            disabled={
              loading ||
              pageNumber >=
                (activeTab === 'suppliers'
                  ? suppliersData.totalPages
                  : activeTab === 'products'
                  ? productsData.totalPages
                  : inventoryData.totalPages)
            }
            onClick={() => setPageNumber((p) => p + 1)}
          >
            Next <ChevronRight size={16} />
          </button>
        </div>
      </div>

      {/* Modals */}
      <StockAdjustmentModal
        item={adjustModalItem}
        isOpen={!!adjustModalItem}
        onClose={() => setAdjustModalItem(null)}
        onSuccess={() => {
          showSuccess(`Stock for ${adjustModalItem?.productName} adjusted successfully.`);
          fetchData();
        }}
      />

      <ReorderModal
        item={reorderModalItem}
        isOpen={!!reorderModalItem}
        onClose={() => setReorderModalItem(null)}
        onSuccess={() => {
          showSuccess(`Purchase order created successfully.`);
          fetchData();
        }}
      />

      <StockHistoryModal
        item={historyModalItem}
        isOpen={!!historyModalItem}
        onClose={() => setHistoryModalItem(null)}
      />

      <ProductModal
        product={productModalItem}
        isOpen={isProductModalOpen}
        onClose={() => {
          setIsProductModalOpen(false);
          setProductModalItem(null);
        }}
        onSuccess={() => {
          showSuccess(productModalItem ? 'Product updated successfully.' : 'Product created successfully.');
          fetchData();
        }}
      />

      <SupplierModal
        supplier={supplierModalItem}
        isOpen={isSupplierModalOpen}
        onClose={() => {
          setIsSupplierModalOpen(false);
          setSupplierModalItem(null);
        }}
        onSuccess={() => {
          showSuccess(supplierModalItem ? 'Supplier updated successfully.' : 'Supplier registered successfully.');
          loadMetadata();
          fetchData();
        }}
      />

      <CsvImportExportModal
        isOpen={isCsvModalOpen}
        onClose={() => setIsCsvModalOpen(false)}
        onSuccess={() => {
          showSuccess('CSV items imported into inventory catalog.');
          fetchData();
        }}
      />
    </div>
  );
};

export default InventoryPage;
