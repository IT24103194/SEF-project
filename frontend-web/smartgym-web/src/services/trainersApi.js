import apiClient from './apiClient';

export const trainersApi = {
  getTrainers: async (params = {}) => {
    const response = await apiClient.get('/trainers', { params });
    return response.data;
  },
  getTrainerById: async (id) => {
    const response = await apiClient.get(`/trainers/${id}`);
    return response.data;
  },
  getTrainerSchedules: async (id, params = {}) => {
    const response = await apiClient.get(`/class-schedules/trainer/${id}`, { params });
    return response.data;
  },
};

export default trainersApi;
