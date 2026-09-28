import apiClient from './apiClient';

export const notificationsApi = {
  getNotifications: async (params = {}) => {
    const response = await apiClient.get('/notifications', { params });
    return response.data;
  },
  getUnreadCount: async () => {
    const response = await apiClient.get('/notifications/unread-count');
    return response.data;
  },
  getSummary: async () => {
    const response = await apiClient.get('/notifications/summary');
    return response.data;
  },
  markAsRead: async (id) => {
    const response = await apiClient.put(`/notifications/${id}/read`);
    return response.data;
  },
  markAllAsRead: async () => {
    const response = await apiClient.put('/notifications/read-all');
    return response.data;
  },
  triggerEvent: async (data) => {
    const response = await apiClient.post('/notifications/events/trigger', data);
    return response.data;
  },
};

export default notificationsApi;
