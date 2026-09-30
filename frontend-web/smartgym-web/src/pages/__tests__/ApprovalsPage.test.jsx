import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { BrowserRouter } from 'react-router-dom';
import ApprovalsPage from '../ApprovalsPage';
import approvalsApi from '../../services/approvalsApi';
import authReducer from '../../store/authSlice';

vi.mock('../../services/approvalsApi', () => ({
  default: {
    getApprovals: vi.fn(),
    submitDecision: vi.fn(),
  },
}));

describe('ApprovalsPage Component', () => {
  let store;

  const mockApprovals = {
    items: [
      {
        id: 'appr-01',
        repairOrderId: 'ro-01',
        orderNumber: 'RO-2026-001',
        equipmentName: 'Cable Cross Machine',
        issueTitle: 'Snapped Pulley Cable',
        estimatedCost: 38000,
        approvalThreshold: 25000,
        decision: 'Pending',
        requestedAt: '2026-09-28T10:00:00Z',
      },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    approvalsApi.getApprovals.mockResolvedValue(mockApprovals);
    store = configureStore({
      reducer: { auth: authReducer },
      preloadedState: {
        auth: {
          isAuthenticated: true,
          role: 'ADMIN',
          user: { username: 'admin' },
        },
      },
    });
  });

  it('renders approvals queue header and loads approval items', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <ApprovalsPage />
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('Human-in-the-Loop Approvals')).toBeTruthy();

    await waitFor(() => {
      expect(screen.getByText(/RO-2026-001/i)).toBeTruthy();
      expect(screen.getByText('Cable Cross Machine')).toBeTruthy();
      expect(screen.getByText('$38000.00')).toBeTruthy();
    });
  });

  it('allows administrator to submit an approval decision', async () => {
    approvalsApi.submitDecision.mockResolvedValue({ success: true });

    render(
      <Provider store={store}>
        <BrowserRouter>
          <ApprovalsPage />
        </BrowserRouter>
      </Provider>
    );

    await waitFor(() => {
      expect(screen.getByText(/RO-2026-001/i)).toBeTruthy();
    });

    const approveBtn = screen.getByRole('button', { name: /Authorize/i });
    fireEvent.click(approveBtn);

    // Modal opens
    expect(screen.getAllByText(/Confirm Approval/i).length).toBeGreaterThan(0);
    const confirmBtn = screen.getByRole('button', { name: /^Confirm Approval$/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(approvalsApi.submitDecision).toHaveBeenCalledWith('appr-01', expect.objectContaining({
        decision: 'Approved',
      }));
    });
  });
});
