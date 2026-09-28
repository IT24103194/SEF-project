import apiClient from './apiClient';

export const inventoryApi = {
  // ==========================================
  // SUPPLIERS
  // ==========================================
  getSuppliers: async (params = {}) => {
    const response = await apiClient.get('/suppliers', { params });
    return response.data;
  },

  getSupplierById: async (id) => {
    const response = await apiClient.get(`/suppliers/${id}`);
    return response.data;
  },

  createSupplier: async (data) => {
    const response = await apiClient.post('/suppliers', data);
    return response.data;
  },

  updateSupplier: async (id, data) => {
    const response = await apiClient.put(`/suppliers/${id}`, data);
    return response.data;
  },

  deleteSupplier: async (id) => {
    const response = await apiClient.delete(`/suppliers/${id}`);
    return response.data;
  },

  // ==========================================
  // PRODUCTS & CATEGORIES
  // ==========================================
  getProducts: async (params = {}) => {
    const response = await apiClient.get('/products', { params });
    return response.data;
  },

  getProductById: async (id) => {
    const response = await apiClient.get(`/products/${id}`);
    return response.data;
  },

  createProduct: async (data) => {
    const response = await apiClient.post('/products', data);
    return response.data;
  },

  updateProduct: async (id, data) => {
    const response = await apiClient.put(`/products/${id}`, data);
    return response.data;
  },

  deleteProduct: async (id) => {
    const response = await apiClient.delete(`/products/${id}`);
    return response.data;
  },

  getCategories: async () => {
    const response = await apiClient.get('/products/categories');
    return response.data;
  },

  createCategory: async (data) => {
    const response = await apiClient.post('/products/categories', data);
    return response.data;
  },

  // ==========================================
  // INVENTORY ITEMS & STOCK MOVEMENTS
  // ==========================================
  getInventory: async (params = {}) => {
    const response = await apiClient.get('/inventory', { params });
    return response.data;
  },

  getInventoryById: async (id) => {
    const response = await apiClient.get(`/inventory/${id}`);
    return response.data;
  },

  adjustStock: async (id, data) => {
    const response = await apiClient.post(`/inventory/${id}/adjust`, data);
    return response.data;
  },

  reorderStock: async (id, data) => {
    const response = await apiClient.post(`/inventory/${id}/reorder`, data);
    return response.data;
  },

  getStockHistory: async (id) => {
    const response = await apiClient.get(`/inventory/${id}/history`);
    return response.data;
  },

  // ==========================================
  // CSV DATA EXCHANGE
  // ==========================================
  exportInventoryCsv: async () => {
    const response = await apiClient.get('/inventory/export-csv', {
      responseType: 'blob',
    });
    return response.data;
  },

  importInventoryCsv: async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post('/inventory/import-csv', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};

export default inventoryApi;
