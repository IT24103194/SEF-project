import apiClient from './apiClient';

export const authApi = {
  login: async (credentials) => {
    const response = await apiClient.post('/auth/login', credentials);
    return response.data;
  },
  register: async (data) => {
    const response = await apiClient.post('/auth/register', data);
    return response.data;
  },
  getCurrentUser: async () => {
    const response = await apiClient.get('/auth/me');
    return response.data;
  },
  logout: async (refreshToken) => {
    try {
      await apiClient.post('/auth/logout', { refreshToken });
    } catch (e) {
      console.warn('Backend logout non-fatal error:', e);
    }
  },
  refreshToken: async (refreshToken) => {
    const response = await apiClient.post('/auth/refresh', { refreshToken });
    return response.data;
  },
};

export default authApi;
