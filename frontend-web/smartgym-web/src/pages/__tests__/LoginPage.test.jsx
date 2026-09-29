import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { BrowserRouter } from 'react-router-dom';
import LoginPage from '../LoginPage';
import authReducer from '../../store/authSlice';
import authApi from '../../services/authApi';

vi.mock('../../services/authApi', () => ({
  default: {
    login: vi.fn(),
  },
}));

describe('LoginPage Component', () => {
  let store;

  beforeEach(() => {
    vi.clearAllMocks();
    store = configureStore({
      reducer: {
        auth: authReducer,
      },
      preloadedState: {
        auth: {
          token: null,
          user: null,
          role: null,
          isAuthenticated: false,
          loading: false,
          error: null,
        },
      },
    });
  });

  it('renders login form with title and inputs', () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <LoginPage />
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('SmartGym Enterprise')).toBeTruthy();
    expect(screen.getByLabelText(/Email Address/i)).toBeTruthy();
    expect(screen.getByLabelText(/Password/i)).toBeTruthy();
    expect(screen.getByRole('button', { name: /Sign In to SmartGym/i })).toBeTruthy();
  });

  it('populates credentials when quick demo role buttons are clicked', () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <LoginPage />
        </BrowserRouter>
      </Provider>
    );

    const trainerBtn = screen.getByRole('button', { name: /Trainer/i });
    fireEvent.click(trainerBtn);

    const emailInput = screen.getByLabelText(/Email Address/i);
    expect(emailInput.value).toBe('trainer@smartgym.com');
  });

  it('calls authApi.login and authenticates on valid submit', async () => {
    authApi.login.mockResolvedValue({
      token: 'fake-jwt-token',
      user: { id: '1', username: 'admin', email: 'admin@smartgym.com' },
      role: 'ADMIN',
    });

    render(
      <Provider store={store}>
        <BrowserRouter>
          <LoginPage />
        </BrowserRouter>
      </Provider>
    );

    const emailInput = screen.getByLabelText(/Email Address/i);
    const passwordInput = screen.getByLabelText(/Password/i);
    const submitBtn = screen.getByRole('button', { name: /Sign In to SmartGym/i });

    fireEvent.change(emailInput, { target: { value: 'admin@smartgym.com' } });
    fireEvent.change(passwordInput, { target: { value: 'Admin@123' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(authApi.login).toHaveBeenCalledWith({
        email: 'admin@smartgym.com',
        password: 'Admin@123',
      });
    });
  });

  it('displays error message when login fails', async () => {
    authApi.login.mockRejectedValue({
      response: { data: { message: 'Invalid credentials provided' } },
    });

    render(
      <Provider store={store}>
        <BrowserRouter>
          <LoginPage />
        </BrowserRouter>
      </Provider>
    );

    const emailInput = screen.getByLabelText(/Email Address/i);
    const passwordInput = screen.getByLabelText(/Password/i);
    const submitBtn = screen.getByRole('button', { name: /Sign In to SmartGym/i });

    fireEvent.change(emailInput, { target: { value: 'bad@user.com' } });
    fireEvent.change(passwordInput, { target: { value: 'wrongpass' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Invalid credentials provided')).toBeTruthy();
    });
  });
});
