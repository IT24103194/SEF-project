import apiClient from './apiClient';

export const reportsApi = {
  getExecutiveDashboard: async () => {
    const response = await apiClient.get('/reports/executive-dashboard');
    return response.data;
  },
  getMembershipReport: async () => {
    const response = await apiClient.get('/reports/membership');
    return response.data;
  },
  getClassesReport: async () => {
    const response = await apiClient.get('/reports/classes');
    return response.data;
  },
  getInventoryReport: async () => {
    const response = await apiClient.get('/reports/inventory');
    return response.data;
  },
  getFacilityReport: async () => {
    const response = await apiClient.get('/reports/facility');
    return response.data;
  },
  getAiReport: async () => {
    const response = await apiClient.get('/reports/ai');
    return response.data;
  },
};

export default reportsApi;
