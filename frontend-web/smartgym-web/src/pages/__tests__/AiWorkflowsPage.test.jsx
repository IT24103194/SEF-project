import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { BrowserRouter } from 'react-router-dom';
import AiWorkflowsPage from '../AiWorkflowsPage';
import aiWorkflowsApi from '../../services/aiWorkflowsApi';
import authReducer from '../../store/authSlice';

vi.mock('../../services/aiWorkflowsApi', () => ({
  default: {
    getWorkflows: vi.fn(),
    getWorkflowById: vi.fn(),
  },
}));

describe('AiWorkflowsPage Component', () => {
  let store;

  const mockWorkflows = {
    items: [
      {
        id: 'wf-101',
        workflowType: 'FacilityTriage',
        issueTitle: 'Treadmill Belt Slipping',
        equipmentName: 'Treadmill Matrix T70',
        status: 'Completed',
        currentStep: 'DecisionFinalized',
        diagnosisSummary: 'Motor belt wear detected.',
        recommendedAction: 'Replace drive belt.',
        estimatedConfidenceScore: 0.94,
        requiresHumanApproval: false,
        totalTokensUsed: 1420,
        modelIdentifier: 'gemini-1.5-pro',
        startedAt: '2026-09-28T09:00:00Z',
      },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    aiWorkflowsApi.getWorkflows.mockResolvedValue(mockWorkflows);
    store = configureStore({
      reducer: { auth: authReducer },
      preloadedState: {
        auth: { isAuthenticated: true, role: 'ADMIN', user: { username: 'admin' } },
      },
    });
  });

  it('renders AI Workflows dashboard and loads trace items', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <AiWorkflowsPage />
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('AI Workflow Operations')).toBeTruthy();

    await waitFor(() => {
      expect(screen.getByText('FacilityTriage')).toBeTruthy();
      expect(screen.getByText(/Treadmill Matrix T70/i)).toBeTruthy();
      expect(screen.getByText('gemini-1.5-pro')).toBeTruthy();
      expect(screen.getByText('Treadmill Belt Slipping')).toBeTruthy();
    });
  });
});
