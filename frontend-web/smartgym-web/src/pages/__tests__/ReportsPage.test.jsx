import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { BrowserRouter } from 'react-router-dom';
import ReportsPage from '../ReportsPage';
import reportsApi from '../../services/reportsApi';
import authReducer from '../../store/authSlice';

vi.mock('../../services/reportsApi', () => ({
  default: {
    getMembershipReport: vi.fn(),
    getClassesReport: vi.fn(),
    getInventoryReport: vi.fn(),
    getFacilityReport: vi.fn(),
    getAiReport: vi.fn(),
    exportReportCsv: vi.fn(),
  },
}));

describe('ReportsPage Component', () => {
  let store;

  beforeEach(() => {
    vi.clearAllMocks();
    reportsApi.getMembershipReport.mockResolvedValue({
      totalMembers: 120,
      activeMembers: 105,
      expiredMemberships: 15,
      expiringSoonCount: 8,
      totalRevenue: 12500,
      planDistribution: [{ planName: 'Gold Pro', count: 65, activeCount: 60, totalRevenue: 9000 }],
      expiringMemberships: [],
    });

    reportsApi.getClassesReport.mockResolvedValue({
      totalClasses: 14,
      totalSchedules: 30,
      totalBookings: 210,
      totalAttended: 180,
      totalAbsent: 20,
      totalCancelled: 10,
      attendanceRatePercentage: 85.7,
      overallUtilizationPercentage: 78.2,
      popularClasses: [],
      classUtilizations: [],
    });

    reportsApi.getInventoryReport.mockResolvedValue({
      totalProducts: 40,
      totalInventoryItems: 55,
      lowStockItemsCount: 2,
      totalSuppliers: 6,
      totalStockMovements: 80,
      movementsByType: [],
      supplierActivity: [],
      criticalStockItems: [],
    });

    reportsApi.getFacilityReport.mockResolvedValue({
      totalIssues: 10,
      openIssues: 2,
      inProgressIssues: 1,
      resolvedIssues: 7,
      averageResolutionHours: 12,
      totalRepairCosts: 1200,
      issuesByPriority: {},
      issuesByEquipment: [],
    });

    reportsApi.getAiReport.mockResolvedValue({
      totalWorkflows: 20,
      pendingApprovalsCount: 1,
      approvedCount: 16,
      rejectedCount: 2,
      revisionsCount: 1,
      safeFailuresCount: 0,
      averageWorkflowDurationSeconds: 4.5,
    });

    store = configureStore({
      reducer: { auth: authReducer },
      preloadedState: {
        auth: { isAuthenticated: true, role: 'ADMIN', user: { username: 'admin' } },
      },
    });
  });

  it('renders reports navigation tabs and defaults to Membership Report', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <ReportsPage />
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('Enterprise Analytics & Reporting')).toBeTruthy();
    expect(screen.getByRole('button', { name: /Memberships/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /Classes & Bookings/i })).toBeTruthy();

    await waitFor(() => {
      expect(screen.getByText('105')).toBeTruthy(); // Active members
    });
  });

  it('switches to Classes report tab when clicked', async () => {
    render(
      <Provider store={store}>
        <BrowserRouter>
          <ReportsPage />
        </BrowserRouter>
      </Provider>
    );

    const classesTab = screen.getByRole('button', { name: /Classes & Bookings/i });
    fireEvent.click(classesTab);

    await waitFor(() => {
      expect(reportsApi.getClassesReport).toHaveBeenCalled();
      expect(screen.getByText('210')).toBeTruthy(); // Total bookings
      expect(screen.getByText('78.2%')).toBeTruthy(); // Utilization
    });
  });
});
