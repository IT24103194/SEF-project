import apiClient from './apiClient';

export const approvalsApi = {
  getApprovals: async (params = {}) => {
    const response = await apiClient.get('/approvals', { params });
    return response.data;
  },
  submitDecision: async (id, payload) => {
    const response = await apiClient.post(`/approvals/${id}/decision`, payload);
    return response.data;
  },
};

export default approvalsApi;
