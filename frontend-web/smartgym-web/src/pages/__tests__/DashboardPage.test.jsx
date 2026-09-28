import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { BrowserRouter } from 'react-router-dom';
import DashboardPage from '../DashboardPage';
import reportsApi from '../../services/reportsApi';
import systemReducer from '../../store/systemSlice';
import authReducer from '../../store/authSlice';

vi.mock('../../services/reportsApi', () => {
  return {
    default: {
      getExecutiveDashboard: vi.fn(),
      getMembershipReport: vi.fn(),
      getClassesReport: vi.fn(),
      getInventoryReport: vi.fn(),
      getFacilityReport: vi.fn(),
      getAiReport: vi.fn(),
    },
  };
});

describe('DashboardPage Component', () => {
  let store;

  const mockExecutiveDashboard = {
    membership: {
      totalMembers: 84,
      activeMembers: 76,
      expiredMemberships: 8,
      expiringSoonCount: 5,
      totalRevenue: 5490.0,
      planDistribution: [],
      expiringMemberships: [],
    },
    classes: {
      totalClasses: 12,
      totalSchedules: 28,
      totalBookings: 145,
      totalAttended: 130,
      totalAbsent: 15,
      totalCancelled: 8,
      attendanceRatePercentage: 89.7,
      overallCapacityUtilizationPercentage: 74.5,
      popularClasses: [],
    },
    inventory: {
      totalProducts: 45,
      totalInventoryItems: 60,
      lowStockItemsCount: 3,
      totalSuppliers: 8,
      totalStockMovements: 120,
      movementsByType: [],
      supplierActivity: [],
      criticalStockItems: [],
    },
    facility: {
      totalIssues: 18,
      openIssues: 4,
      inProgressIssues: 2,
      resolvedIssues: 12,
      averageResolutionHours: 14.5,
      totalRepairCosts: 1850.0,
      issuesByPriority: {},
      issuesByEquipment: [],
    },
    ai: {
      totalWorkflows: 35,
      pendingApprovalsCount: 2,
      approvedCount: 28,
      rejectedCount: 4,
      revisionsCount: 1,
      safeFailuresCount: 0,
      averageWorkflowDurationSeconds: 4.8,
    },
    generatedAt: '2026-09-28T12:00:00Z',
  };

  beforeEach(() => {
    vi.clearAllMocks();
    reportsApi.getExecutiveDashboard.mockResolvedValue(mockExecutiveDashboard);

    store = configureStore({
      reducer: {
        system: systemReducer,
        auth: authReducer,
      },
      preloadedState: {
        system: {
          info: {
            application: 'SmartGym.Api',
            environment: 'Production',
            roles: ['ADMIN', 'TRAINER', 'MEMBER'],
          },
          status: 'succeeded',
        },
        auth: {
          isAuthenticated: true,
          user: { username: 'admin' },
          role: 'ADMIN',
        },
      },
    });
  });

  it('renders the Dashboard header and module links', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <DashboardPage />
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('System Dashboard & Executive Cockpit')).toBeTruthy();
    expect(screen.getByText('Supplier & Supplement Inventory')).toBeTruthy();
    expect(screen.getByText('Facility Resolution & AI')).toBeTruthy();
    expect(screen.getByText('Class Scheduling & Booking')).toBeTruthy();
    expect(screen.getByText('Membership & Goal Tracking')).toBeTruthy();
  });

  it('loads and displays PostgreSQL live executive metrics', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <DashboardPage />
        </BrowserRouter>
      </Provider>
    );

    await waitFor(() => {
      expect(reportsApi.getExecutiveDashboard).toHaveBeenCalled();
    });

    // Check live report figures
    expect(screen.getByText('76')).toBeTruthy(); // Active members
    expect(screen.getByText('$5490.00')).toBeTruthy(); // Revenue
    expect(screen.getByText('89.7%')).toBeTruthy(); // Attendance rate
    expect(screen.getByText('3')).toBeTruthy(); // Low stock alert
    expect(screen.getByText('Facility & Repairs Status')).toBeTruthy();
    expect(screen.getByText('AI Agentic Workflows')).toBeTruthy();
  });
});
