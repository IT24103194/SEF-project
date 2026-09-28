import apiClient from './apiClient';

export const membershipApi = {
  // Membership Plans
  getPlans: async (includeInactive = false) => {
    const response = await apiClient.get('/membership-plans', {
      params: { includeInactive },
    });
    return response.data;
  },
  getPlan: async (id) => {
    const response = await apiClient.get(`/membership-plans/${id}`);
    return response.data;
  },
  createPlan: async (data) => {
    const response = await apiClient.post('/membership-plans', data);
    return response.data;
  },
  updatePlan: async (id, data) => {
    const response = await apiClient.put(`/membership-plans/${id}`, data);
    return response.data;
  },
  deletePlan: async (id) => {
    const response = await apiClient.delete(`/membership-plans/${id}`);
    return response.data;
  },

  // Members
  getMembers: async (params = {}) => {
    const response = await apiClient.get('/members', { params });
    return response.data;
  },
  getMember: async (id) => {
    const response = await apiClient.get(`/members/${id}`);
    return response.data;
  },
  getMyProfile: async () => {
    const response = await apiClient.get('/members/me');
    return response.data;
  },
  updateMember: async (id, data) => {
    const response = await apiClient.put(`/members/${id}`, data);
    return response.data;
  },

  // Memberships & Subscriptions
  getMemberships: async (params = {}) => {
    const response = await apiClient.get('/memberships', { params });
    return response.data;
  },
  getMyMembership: async () => {
    const response = await apiClient.get('/memberships/my-membership');
    return response.data;
  },
  getMembershipHistory: async (memberId) => {
    const response = await apiClient.get(`/memberships/history/${memberId}`);
    return response.data;
  },
  createMembership: async (data) => {
    const response = await apiClient.post('/memberships', data);
    return response.data;
  },
  renewMembership: async (data) => {
    const response = await apiClient.post('/memberships/renew', data);
    return response.data;
  },
  cancelMembership: async (id, reason = 'Member requested cancellation') => {
    const response = await apiClient.put(`/memberships/${id}/cancel`, { reason });
    return response.data;
  },

  // Goals
  getGoals: async (params = {}) => {
    const response = await apiClient.get('/goals', { params });
    return response.data;
  },
  getMyGoals: async () => {
    const response = await apiClient.get('/goals/my-goals');
    return response.data;
  },
  getGoal: async (id) => {
    const response = await apiClient.get(`/goals/${id}`);
    return response.data;
  },
  createGoal: async (data) => {
    const response = await apiClient.post('/goals', data);
    return response.data;
  },
  updateGoal: async (id, data) => {
    const response = await apiClient.put(`/goals/${id}`, data);
    return response.data;
  },
  deleteGoal: async (id) => {
    const response = await apiClient.delete(`/goals/${id}`);
    return response.data;
  },
  completeGoal: async (id) => {
    const response = await apiClient.post(`/goals/${id}/complete`);
    return response.data;
  },

  // Progress Records
  getGoalProgress: async (goalId) => {
    const response = await apiClient.get(`/goals/${goalId}/progress`);
    return response.data;
  },
  recordProgress: async (data) => {
    const response = await apiClient.post('/progress-records', data);
    return response.data;
  },

  // Analytics
  getAnalytics: async () => {
    const response = await apiClient.get('/memberships/analytics');
    return response.data;
  },
};

export default membershipApi;
