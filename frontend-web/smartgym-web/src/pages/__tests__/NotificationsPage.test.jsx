import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { BrowserRouter } from 'react-router-dom';
import NotificationsPage from '../NotificationsPage';
import notificationsApi from '../../services/notificationsApi';
import authReducer from '../../store/authSlice';

vi.mock('../../services/notificationsApi', () => ({
  default: {
    getNotifications: vi.fn(),
    getSummary: vi.fn(),
    markAsRead: vi.fn(),
    markAllAsRead: vi.fn(),
    triggerEvent: vi.fn(),
  },
}));

describe('NotificationsPage Component', () => {
  let store;

  const mockNotifications = {
    items: [
      {
        id: 'notif-01',
        title: 'Low Stock Alert',
        message: 'Whey Protein Isolate 1kg is below minimum threshold.',
        notificationType: 'LowStock',
        priority: 'High',
        isRead: false,
        createdAt: '2026-09-28T08:00:00Z',
      },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    notificationsApi.getNotifications.mockResolvedValue(mockNotifications);
    notificationsApi.getSummary.mockResolvedValue({
      totalNotifications: 1,
      unreadCount: 1,
      highPriorityCount: 1,
    });

    store = configureStore({
      reducer: { auth: authReducer },
      preloadedState: {
        auth: { isAuthenticated: true, role: 'ADMIN', user: { username: 'admin' } },
      },
    });
  });

  it('renders Notification Center and loads notifications', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <NotificationsPage />
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('Notification Center')).toBeTruthy();

    await waitFor(() => {
      expect(screen.getByText('Low Stock Alert')).toBeTruthy();
      expect(screen.getByText(/Whey Protein Isolate 1kg/i)).toBeTruthy();
    });
  });
});
