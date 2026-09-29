import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { BrowserRouter } from 'react-router-dom';
import MembersPage from '../MembersPage';
import membershipApi from '../../services/membershipApi';
import authReducer from '../../store/authSlice';

vi.mock('../../services/membershipApi', () => {
  const api = {
    getMembers: vi.fn(),
    createMember: vi.fn(),
    updateMember: vi.fn(),
  };
  return {
    membershipApi: api,
    default: api,
  };
});

describe('MembersPage Component', () => {
  let store;

  const mockMembers = {
    items: [
      {
        id: 'mem-001',
        firstName: 'John',
        lastName: 'Doe',
        email: 'john.doe@example.com',
        phoneNumber: '0771234567',
        status: 'Active',
        createdAt: '2026-01-15T08:00:00Z',
      },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    membershipApi.getMembers.mockResolvedValue(mockMembers);
    store = configureStore({
      reducer: { auth: authReducer },
      preloadedState: {
        auth: { isAuthenticated: true, role: 'ADMIN', user: { username: 'admin' } },
      },
    });
  });

  it('renders members directory and displays members from API', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <MembersPage />
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('Member Directory')).toBeTruthy();

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeTruthy();
      expect(screen.getByText('john.doe@example.com')).toBeTruthy();
    });
  });

  it('filters members on search form submission', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <MembersPage />
        </BrowserRouter>
      </Provider>
    );

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeTruthy();
    });

    const searchInput = screen.getByPlaceholderText(/Search by name, email, or phone.../i);
    fireEvent.change(searchInput, { target: { value: 'John' } });

    const searchBtn = screen.getByRole('button', { name: /Search/i });
    fireEvent.click(searchBtn);

    await waitFor(() => {
      expect(membershipApi.getMembers).toHaveBeenCalledWith(expect.objectContaining({
        search: 'John',
      }));
    });
  });
});
