import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import MembershipsPage from '../MembershipsPage';
import membershipApi from '../../services/membershipApi';

vi.mock('../../services/membershipApi', () => {
  return {
    default: {
      getPlans: vi.fn(),
      getMemberships: vi.fn(),
      getMembers: vi.fn(),
      getGoals: vi.fn(),
      getAnalytics: vi.fn(),
      deletePlan: vi.fn(),
      cancelMembership: vi.fn(),
      completeGoal: vi.fn(),
      deleteGoal: vi.fn(),
      getMembershipHistory: vi.fn(),
    },
  };
});

describe('MembershipsPage Component', () => {
  const mockPlans = [
    {
      id: 'plan-1',
      name: 'Platinum All-Access',
      description: 'Full gym and classes access',
      price: 99.99,
      durationDays: 30,
      maxClassesPerWeek: 7,
      hasTrainerAccess: true,
      isActive: true,
      activeSubscribersCount: 25,
      createdAt: '2026-01-01T00:00:00Z',
    },
    {
      id: 'plan-2',
      name: 'Basic Access',
      description: 'Standard gym floor access',
      price: 39.99,
      durationDays: 30,
      maxClassesPerWeek: 0,
      hasTrainerAccess: false,
      isActive: true,
      activeSubscribersCount: 12,
      createdAt: '2026-01-01T00:00:00Z',
    },
  ];

  const mockMemberships = {
    items: [
      {
        id: 'mem-1',
        memberId: 'member-1',
        memberName: 'John Doe',
        memberEmail: 'john@example.com',
        planId: 'plan-1',
        planName: 'Platinum All-Access',
        startDate: '2026-02-01T00:00:00Z',
        endDate: '2026-03-03T00:00:00Z',
        status: 'Active',
        autoRenew: true,
        pricePaid: 99.99,
        createdAt: '2026-02-01T00:00:00Z',
      },
    ],
    totalCount: 1,
    totalPages: 1,
  };

  const mockMembers = {
    items: [
      {
        id: 'member-1',
        userId: 'user-1',
        userName: 'John Doe',
        fullName: 'John Doe',
        email: 'john@example.com',
        phoneNumber: '+94771234567',
        joinDate: '2026-01-15T00:00:00Z',
        currentPlanName: 'Platinum All-Access',
        currentMembershipStatus: 'Active',
        activeGoalsCount: 2,
        totalBookingsCount: 8,
      },
    ],
    totalCount: 1,
    totalPages: 1,
  };

  const mockGoals = {
    items: [
      {
        id: 'goal-1',
        memberId: 'member-1',
        memberName: 'John Doe',
        title: 'Reach 80kg Body Weight',
        targetValue: 80,
        currentValue: 75,
        unit: 'kg',
        targetDate: '2026-06-01T00:00:00Z',
        status: 'InProgress',
        totalLogsCount: 3,
        createdAt: '2026-01-01T00:00:00Z',
        recentLogs: [
          {
            id: 'log-1',
            goalId: 'goal-1',
            recordedDate: '2026-02-15T00:00:00Z',
            value: 75,
            notes: 'Steady progress',
          },
        ],
      },
    ],
    totalCount: 1,
    totalPages: 1,
  };

  const mockAnalytics = {
    totalMembers: 45,
    activeMemberships: 37,
    expiredMemberships: 5,
    cancelledMemberships: 3,
    totalRevenue: 3450.5,
    totalGoals: 28,
    achievedGoals: 12,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    membershipApi.getPlans.mockResolvedValue(mockPlans);
    membershipApi.getMemberships.mockResolvedValue(mockMemberships);
    membershipApi.getMembers.mockResolvedValue(mockMembers);
    membershipApi.getGoals.mockResolvedValue(mockGoals);
    membershipApi.getAnalytics.mockResolvedValue(mockAnalytics);
  });

  it('renders the header and navigation tabs', async () => {
    render(<MembershipsPage />);

    expect(screen.getByText('Membership & Goal Tracking')).toBeTruthy();
    expect(screen.getByText('Membership Plans')).toBeTruthy();
    expect(screen.getByText('Subscriptions & Renewals')).toBeTruthy();
    expect(screen.getByText('Member Directory')).toBeTruthy();
    expect(screen.getByText('Fitness Goals & Progress')).toBeTruthy();
    expect(screen.getByText('Analytics & Reports')).toBeTruthy();
  });

  it('loads and displays membership plans on the plans tab', async () => {
    render(<MembershipsPage />);

    await waitFor(() => {
      expect(membershipApi.getPlans).toHaveBeenCalled();
    });

    expect(screen.getByText('Platinum All-Access')).toBeTruthy();
    expect(screen.getByText('Basic Access')).toBeTruthy();
    expect(screen.getByText('$99.99')).toBeTruthy();
    expect(screen.getByText('$39.99')).toBeTruthy();
  });

  it('switches to Subscriptions tab and renders subscriptions', async () => {
    render(<MembershipsPage />);

    const subTab = screen.getByText('Subscriptions & Renewals');
    fireEvent.click(subTab);

    await waitFor(() => {
      expect(membershipApi.getMemberships).toHaveBeenCalled();
    });

    expect(screen.getByText('John Doe')).toBeTruthy();
    expect(screen.getByText('john@example.com')).toBeTruthy();
    expect(screen.getByText('$99.99')).toBeTruthy();
  });

  it('switches to Member Directory tab and displays members', async () => {
    render(<MembershipsPage />);

    const memberTab = screen.getByText('Member Directory');
    fireEvent.click(memberTab);

    await waitFor(() => {
      expect(membershipApi.getMembers).toHaveBeenCalled();
    });

    expect(screen.getByText('John Doe')).toBeTruthy();
    expect(screen.getByText('Assign Plan')).toBeTruthy();
  });

  it('switches to Fitness Goals tab and displays goals with progress', async () => {
    render(<MembershipsPage />);

    const goalsTab = screen.getByText('Fitness Goals & Progress');
    fireEvent.click(goalsTab);

    await waitFor(() => {
      expect(membershipApi.getGoals).toHaveBeenCalled();
    });

    expect(screen.getByText('Reach 80kg Body Weight')).toBeTruthy();
    expect(screen.getByText('Log Progress')).toBeTruthy();
  });

  it('switches to Analytics tab and displays summary KPIs', async () => {
    render(<MembershipsPage />);

    const analyticsTab = screen.getByText('Analytics & Reports');
    fireEvent.click(analyticsTab);

    await waitFor(() => {
      expect(membershipApi.getAnalytics).toHaveBeenCalled();
    });

    expect(screen.getByText('45')).toBeTruthy();
    expect(screen.getByText('37')).toBeTruthy();
    expect(screen.getByText('$3450.50')).toBeTruthy();
  });
});
