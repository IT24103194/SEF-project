import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import ProtectedRoute from '../ProtectedRoute';
import authReducer from '../../store/authSlice';

describe('ProtectedRoute Component', () => {
  const renderWithAuth = (initialState, allowedRoles, initialEntry = '/protected') => {
    const store = configureStore({
      reducer: { auth: authReducer },
      preloadedState: { auth: initialState },
    });

    return render(
      <Provider store={store}>
        <MemoryRouter initialEntries={[initialEntry]}>
          <Routes>
            <Route path="/login" element={<div>Login Page Screen</div>} />
            <Route
              path="/protected"
              element={
                <ProtectedRoute allowedRoles={allowedRoles}>
                  <div>Protected Content Visible</div>
                </ProtectedRoute>
              }
            />
          </Routes>
        </MemoryRouter>
      </Provider>
    );
  };

  it('redirects to /login when user is not authenticated', () => {
    renderWithAuth({ isAuthenticated: false, token: null, role: null }, ['ADMIN']);
    expect(screen.getByText('Login Page Screen')).toBeTruthy();
  });

  it('renders protected content when user is authenticated with permitted role', () => {
    renderWithAuth(
      { isAuthenticated: true, token: 'token-123', role: 'ADMIN', user: { username: 'admin' } },
      ['ADMIN', 'TRAINER']
    );
    expect(screen.getByText('Protected Content Visible')).toBeTruthy();
  });

  it('shows 403 Access Denied when user role is not authorized', () => {
    renderWithAuth(
      { isAuthenticated: true, token: 'token-123', role: 'MEMBER', user: { username: 'member' } },
      ['ADMIN']
    );
    expect(screen.getByText('Access Restricted')).toBeTruthy();
    expect(screen.getByText(/Required roles: ADMIN/i)).toBeTruthy();
  });
});
