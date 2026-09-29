import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { BrowserRouter } from 'react-router-dom';
import TrainersPage from '../TrainersPage';
import trainersApi from '../../services/trainersApi';
import authReducer from '../../store/authSlice';

vi.mock('../../services/trainersApi', () => {
  const api = {
    getTrainers: vi.fn(),
    getTrainerById: vi.fn(),
  };
  return {
    trainersApi: api,
    default: api,
  };
});

describe('TrainersPage Component', () => {
  let store;

  const mockTrainers = {
    items: [
      {
        id: 'tr-01',
        name: 'Alex Johnson',
        email: 'alex@smartgym.com',
        phoneNumber: '0779876543',
        specialization: 'HIIT & Strength',
        bio: 'Certified strength and conditioning specialist.',
        activeClassesCount: 4,
      },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    trainersApi.getTrainers.mockResolvedValue(mockTrainers);
    store = configureStore({
      reducer: { auth: authReducer },
      preloadedState: {
        auth: { isAuthenticated: true, role: 'ADMIN', user: { username: 'admin' } },
      },
    });
  });

  it('renders trainers directory and displays trainer roster', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <TrainersPage />
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('Trainer Roster')).toBeTruthy();

    await waitFor(() => {
      expect(screen.getByText('Alex Johnson')).toBeTruthy();
      expect(screen.getByText('HIIT & Strength')).toBeTruthy();
      expect(screen.getByText('alex@smartgym.com')).toBeTruthy();
    });
  });
});
