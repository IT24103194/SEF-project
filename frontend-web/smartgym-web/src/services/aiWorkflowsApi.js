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
  getWorkflowSummary: async (id) => {
    const response = await apiClient.get(`/ai-workflows/${id}/summary`);
    return response.data;
  },
  getWorkflowHistory: async (id) => {
    const response = await apiClient.get(`/ai-workflows/${id}/history`);
    return response.data;
  },
  getWorkflowAudit: async (id) => {
    const response = await apiClient.get(`/ai-workflows/${id}/audit`);
    return response.data;
  },
  getWorkflowStatus: async (id) => {
    const response = await apiClient.get(`/ai-workflows/${id}/status`);
    return response.data;
  },
  getWorkflowByIssueId: async (issueId) => {
    const response = await apiClient.get(`/ai-workflows/by-issue/${issueId}`);
    return response.data;
  },
};

export default aiWorkflowsApi;
