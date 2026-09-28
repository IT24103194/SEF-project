import apiClient from './apiClient';

export const facilityApi = {
  // Facility Issues
  getFacilityIssues: async (params = {}) => {
    const response = await apiClient.get('/facility-issues', { params });
    return response.data;
  },
  getFacilityIssue: async (id) => {
    const response = await apiClient.get(`/facility-issues/${id}`);
    return response.data;
  },
  createFacilityIssue: async (data) => {
    const response = await apiClient.post('/facility-issues', data);
    return response.data;
  },
  createFacilityIssueWithImage: async (formData) => {
    const response = await apiClient.post('/facility-issues/with-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
  updateFacilityIssue: async (id, data) => {
    const response = await apiClient.put(`/facility-issues/${id}`, data);
    return response.data;
  },
  transitionStatus: async (id, payload) => {
    const response = await apiClient.post(`/facility-issues/${id}/status`, payload);
    return response.data;
  },
  uploadIssueImage: async (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post(`/facility-issues/${id}/images`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
  getIssueHistory: async (id) => {
    const response = await apiClient.get(`/facility-issues/${id}/history`);
    return response.data;
  },

  // Locations
  getLocations: async (params = {}) => {
    const response = await apiClient.get('/locations', { params });
    return response.data;
  },
  getLocation: async (id) => {
    const response = await apiClient.get(`/locations/${id}`);
    return response.data;
  },
  createLocation: async (data) => {
    const response = await apiClient.post('/locations', data);
    return response.data;
  },
  updateLocation: async (id, data) => {
    const response = await apiClient.put(`/locations/${id}`, data);
    return response.data;
  },
  deleteLocation: async (id) => {
    const response = await apiClient.delete(`/locations/${id}`);
    return response.data;
  },

  // Equipment
  getEquipment: async (params = {}) => {
    const response = await apiClient.get('/equipment', { params });
    return response.data;
  },
  getEquipmentById: async (id) => {
    const response = await apiClient.get(`/equipment/${id}`);
    return response.data;
  },
  createEquipment: async (data) => {
    const response = await apiClient.post('/equipment', data);
    return response.data;
  },
  updateEquipment: async (id, data) => {
    const response = await apiClient.put(`/equipment/${id}`, data);
    return response.data;
  },
  deleteEquipment: async (id) => {
    const response = await apiClient.delete(`/equipment/${id}`);
    return response.data;
  },
  getEquipmentHistory: async (id) => {
    const response = await apiClient.get(`/equipment/${id}/history`);
    return response.data;
  },

  // Repair Orders
  getRepairOrders: async (params = {}) => {
    const response = await apiClient.get('/repair-orders', { params });
    return response.data;
  },
  getRepairOrder: async (id) => {
    const response = await apiClient.get(`/repair-orders/${id}`);
    return response.data;
  },
  createRepairOrder: async (data) => {
    const response = await apiClient.post('/repair-orders', data);
    return response.data;
  },
  updateRepairOrder: async (id, data) => {
    const response = await apiClient.put(`/repair-orders/${id}`, data);
    return response.data;
  },
  deleteRepairOrder: async (id) => {
    const response = await apiClient.delete(`/repair-orders/${id}`);
    return response.data;
  },
  processRepairApproval: async (id, payload) => {
    const response = await apiClient.post(`/repair-orders/${id}/approve`, payload);
    return response.data;
  },

  // Feedback
  getFeedbacks: async (params = {}) => {
    const response = await apiClient.get('/feedback', { params });
    return response.data;
  },
  getFeedback: async (id) => {
    const response = await apiClient.get(`/feedback/${id}`);
    return response.data;
  },
  createFeedback: async (data) => {
    const response = await apiClient.post('/feedback', data);
    return response.data;
  },
  respondFeedback: async (id, payload) => {
    const response = await apiClient.post(`/feedback/${id}/respond`, payload);
    return response.data;
  },
  deleteFeedback: async (id) => {
    const response = await apiClient.delete(`/feedback/${id}`);
    return response.data;
  },
};

export default facilityApi;
