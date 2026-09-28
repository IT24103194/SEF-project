import apiClient from './apiClient';

export const classesApi = {
  // Categories
  getCategories: async () => {
    const response = await apiClient.get('/class-categories');
    return response.data;
  },
  getCategory: async (id) => {
    const response = await apiClient.get(`/class-categories/${id}`);
    return response.data;
  },
  createCategory: async (data) => {
    const response = await apiClient.post('/class-categories', data);
    return response.data;
  },
  updateCategory: async (id, data) => {
    const response = await apiClient.put(`/class-categories/${id}`, data);
    return response.data;
  },
  deleteCategory: async (id) => {
    const response = await apiClient.delete(`/class-categories/${id}`);
    return response.data;
  },

  // Fitness Classes
  getClasses: async (params = {}) => {
    const response = await apiClient.get('/fitness-classes', { params });
    return response.data;
  },
  getClass: async (id) => {
    const response = await apiClient.get(`/fitness-classes/${id}`);
    return response.data;
  },
  createClass: async (data) => {
    const response = await apiClient.post('/fitness-classes', data);
    return response.data;
  },
  updateClass: async (id, data) => {
    const response = await apiClient.put(`/fitness-classes/${id}`, data);
    return response.data;
  },
  deleteClass: async (id) => {
    const response = await apiClient.delete(`/fitness-classes/${id}`);
    return response.data;
  },

  // Schedules & Calendar
  getSchedules: async (params = {}) => {
    const response = await apiClient.get('/class-schedules', { params });
    return response.data;
  },
  getSchedule: async (id) => {
    const response = await apiClient.get(`/class-schedules/${id}`);
    return response.data;
  },
  getScheduleAvailability: async (id, memberId) => {
    const params = memberId ? { memberId } : {};
    const response = await apiClient.get(`/class-schedules/${id}/availability`, { params });
    return response.data;
  },
  getTrainerSchedules: async (trainerId, params = {}) => {
    const response = await apiClient.get(`/class-schedules/trainer/${trainerId}`, { params });
    return response.data;
  },
  createSchedule: async (data) => {
    const response = await apiClient.post('/class-schedules', data);
    return response.data;
  },
  updateSchedule: async (id, data) => {
    const response = await apiClient.put(`/class-schedules/${id}`, data);
    return response.data;
  },
  cancelSchedule: async (id) => {
    const response = await apiClient.put(`/class-schedules/${id}/cancel`);
    return response.data;
  },
  deleteSchedule: async (id) => {
    const response = await apiClient.delete(`/class-schedules/${id}`);
    return response.data;
  },

  // Bookings
  getBookings: async (params = {}) => {
    const response = await apiClient.get('/bookings', { params });
    return response.data;
  },
  getMyBookings: async (params = {}) => {
    const response = await apiClient.get('/bookings/my-bookings', { params });
    return response.data;
  },
  getBooking: async (id) => {
    const response = await apiClient.get(`/bookings/${id}`);
    return response.data;
  },
  bookClass: async (data) => {
    const response = await apiClient.post('/bookings', data);
    return response.data;
  },
  cancelBooking: async (id, data = {}) => {
    const response = await apiClient.post(`/bookings/${id}/cancel`, data);
    return response.data;
  },

  // Attendance
  getAttendances: async (params = {}) => {
    const response = await apiClient.get('/attendances', { params });
    return response.data;
  },
  getScheduleAttendanceSheet: async (scheduleId) => {
    const response = await apiClient.get(`/attendances/schedule/${scheduleId}`);
    return response.data;
  },
  recordAttendance: async (data) => {
    const response = await apiClient.post('/attendances', data);
    return response.data;
  },
  recordBulkAttendance: async (data) => {
    const response = await apiClient.post('/attendances/bulk', data);
    return response.data;
  },
};

export default classesApi;
