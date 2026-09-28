import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15000,
});

// Attach Authorization Bearer token if present
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('smartgym_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Intercept responses for centralized error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      console.warn('Session expired or unauthorized. Redirecting to login.');
      localStorage.removeItem('smartgym_token');
      localStorage.removeItem('smartgym_user');
      // In production routes, redirect to /login
    }
    return Promise.reject(error);
  }
);

export default apiClient;
