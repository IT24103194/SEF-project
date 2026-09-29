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
};

export default aiWorkflowsApi;
