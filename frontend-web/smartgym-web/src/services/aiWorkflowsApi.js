import apiClient from './apiClient';

export const aiWorkflowsApi = {
  getWorkflows: async (params = {}) => {
    const response = await apiClient.get('/ai-workflows', { params });
    return response.data;
  },
  getWorkflowById: async (id) => {
    const response = await apiClient.get(`/ai-workflows/${id}`);
    return response.data;
  },
  approveWorkflow: async (id, comments = '') => {
    const response = await apiClient.post(`/ai-workflows/${id}/approve`, { comments });
    return response.data;
  },
  rejectWorkflow: async (id, comments = '') => {
    const response = await apiClient.post(`/ai-workflows/${id}/reject`, { comments });
    return response.data;
  },
  reviseWorkflow: async (id, comments = '') => {
    const response = await apiClient.post(`/ai-workflows/${id}/revise`, { comments });
    return response.data;
  },
};

export default aiWorkflowsApi;
