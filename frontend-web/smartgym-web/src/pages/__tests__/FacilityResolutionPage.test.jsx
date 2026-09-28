import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import FacilityResolutionPage from '../FacilityResolutionPage';
import facilityApi from '../../services/facilityApi';

vi.mock('../../services/facilityApi', () => {
  return {
    default: {
      getLocations: vi.fn(),
      getEquipment: vi.fn(),
      getFacilityIssues: vi.fn(),
      getRepairOrders: vi.fn(),
      getFeedbacks: vi.fn(),
      transitionStatus: vi.fn(),
      processRepairApproval: vi.fn(),
      deleteLocation: vi.fn(),
      deleteEquipment: vi.fn(),
      getIssueHistory: vi.fn(),
      getEquipmentHistory: vi.fn(),
    },
  };
});

describe('FacilityResolutionPage Component', () => {
  const mockLocations = [
    {
      id: 'loc-1',
      name: 'Cardio Zone A',
      floor: 'Ground Floor',
      description: 'Treadmills and rowers',
      equipmentCount: 12,
      activeIssuesCount: 1,
      createdAt: '2026-01-01T00:00:00Z',
    },
  ];

  const mockEquipment = [
    {
      id: 'eq-1',
      locationId: 'loc-1',
      locationName: 'Cardio Zone A',
      locationFloor: 'Ground Floor',
      serialNumber: 'LF-TRD-001',
      name: 'LifeFitness Treadmill T1',
      model: 'Elevation T95',
      manufacturer: 'LifeFitness USA',
      purchaseDate: '2023-01-15T00:00:00Z',
      status: 1,
      statusName: 'Operational',
      activeIssuesCount: 1,
    },
  ];

  const mockIssues = [
    {
      id: 'iss-1',
      reportedByMemberId: 'mem-1',
      reporterName: 'Nuwan Perera',
      reporterEmail: 'member@smartgym.com',
      equipmentId: 'eq-1',
      equipmentName: 'LifeFitness Treadmill T1',
      equipmentSerialNumber: 'LF-TRD-001',
      locationId: 'loc-1',
      locationName: 'Cardio Zone A',
      locationFloor: 'Ground Floor',
      title: 'Motor belt slipping during speeds above 12 km/h',
      description: 'Treadmill belt stutters noticeably when accelerating past 12km/h.',
      sanitizedDescription: 'Treadmill belt stutters noticeably when accelerating past 12km/h.',
      moderationStatus: 'Clean',
      severity: 2,
      severityName: 'Medium',
      status: 1,
      statusName: 'SUBMITTED',
      reportedAt: '2026-09-26T10:00:00Z',
      images: [],
    },
  ];

  const mockRepairOrders = [
    {
      id: 'ro-1',
      issueId: 'iss-1',
      issueTitle: 'Motor belt slipping during speeds above 12 km/h',
      equipmentId: 'eq-1',
      equipmentName: 'LifeFitness Treadmill T1',
      orderNumber: 'RO-20260928-1001',
      estimatedCost: 1250.00,
      status: 2,
      statusName: 'PendingApproval',
      technicianName: 'Kamal Gunaratne',
      requiresApproval: true,
      createdAt: '2026-09-27T00:00:00Z',
      items: [
        { id: 'item-1', partName: 'Drive Belt', partNumber: 'LF-B882', quantity: 1, unitCost: 1250.00, totalCost: 1250.00 }
      ],
    },
  ];

  const mockFeedbacks = [
    {
      id: 'fb-1',
      memberId: 'mem-1',
      memberName: 'Nuwan Perera',
      memberEmail: 'member@smartgym.com',
      subject: 'Air conditioning in Studio 1',
      content: 'Studio is too humid during peak hours.',
      rating: 4,
      status: 1,
      statusName: 'Pending',
      adminResponse: null,
      createdAt: '2026-09-28T00:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    facilityApi.getLocations.mockResolvedValue({ items: mockLocations, totalCount: 1 });
    facilityApi.getEquipment.mockResolvedValue({ items: mockEquipment, totalCount: 1 });
    facilityApi.getFacilityIssues.mockResolvedValue({ items: mockIssues, totalCount: 1 });
    facilityApi.getRepairOrders.mockResolvedValue({ items: mockRepairOrders, totalCount: 1 });
    facilityApi.getFeedbacks.mockResolvedValue({ items: mockFeedbacks, totalCount: 1 });
    facilityApi.getIssueHistory.mockResolvedValue([]);
  });

  it('renders page header and tabs correctly', async () => {
    render(<FacilityResolutionPage />);

    expect(screen.getByText(/Facility Resolution & Maintenance/i)).toBeDefined();
    expect(screen.getByText('Facility Issues')).toBeDefined();
    expect(screen.getByText('Equipment & Maintenance')).toBeDefined();
    expect(screen.getByText('Facility Locations')).toBeDefined();
    expect(screen.getByText('Repair Orders & HITL')).toBeDefined();
    expect(screen.getByText('Member Feedback')).toBeDefined();

    await waitFor(() => {
      expect(screen.getByText('Motor belt slipping during speeds above 12 km/h')).toBeDefined();
      expect(screen.getAllByText('SUBMITTED').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('switches to Equipment tab and renders equipment inventory', async () => {
    render(<FacilityResolutionPage />);

    await waitFor(() => {
      expect(screen.getByText('Facility Issues')).toBeDefined();
    });

    fireEvent.click(screen.getByText('Equipment & Maintenance'));

    await waitFor(() => {
      expect(screen.getByText('Gym Equipment Inventory')).toBeDefined();
      expect(screen.getByText('LifeFitness Treadmill T1')).toBeDefined();
      expect(screen.getByText('LF-TRD-001')).toBeDefined();
    });
  });

  it('switches to Facility Locations tab and displays locations', async () => {
    render(<FacilityResolutionPage />);

    await waitFor(() => {
      expect(screen.getByText('Facility Issues')).toBeDefined();
    });

    fireEvent.click(screen.getByText('Facility Locations'));

    await waitFor(() => {
      expect(screen.getByText('Gym Facility Locations')).toBeDefined();
      expect(screen.getByText('Cardio Zone A')).toBeDefined();
      expect(screen.getByText('Treadmills and rowers')).toBeDefined();
    });
  });

  it('switches to Repair Orders tab and allows authorizing high-value repair', async () => {
    facilityApi.processRepairApproval.mockResolvedValue({ ...mockRepairOrders[0], statusName: 'Approved' });

    render(<FacilityResolutionPage />);

    await waitFor(() => {
      expect(screen.getByText('Facility Issues')).toBeDefined();
    });

    fireEvent.click(screen.getByText('Repair Orders & HITL'));

    await waitFor(() => {
      expect(screen.getByText('RO-20260928-1001')).toBeDefined();
      expect(screen.getByText('$1250.00')).toBeDefined();
      expect(screen.getByText('Authorize')).toBeDefined();
    });

    fireEvent.click(screen.getByText('Authorize'));

    await waitFor(() => {
      expect(facilityApi.processRepairApproval).toHaveBeenCalledWith('ro-1', expect.objectContaining({
        decision: 1,
      }));
    });
  });

  it('switches to Member Feedback tab and displays feedback items', async () => {
    render(<FacilityResolutionPage />);

    await waitFor(() => {
      expect(screen.getByText('Facility Issues')).toBeDefined();
    });

    fireEvent.click(screen.getByText('Member Feedback'));

    await waitFor(() => {
      expect(screen.getByText('Member Feedback & Suggestions')).toBeDefined();
      expect(screen.getByText('Air conditioning in Studio 1')).toBeDefined();
      expect(screen.getByText('"Studio is too humid during peak hours."')).toBeDefined();
      expect(screen.getByText('Respond to Member')).toBeDefined();
    });
  });

  it('opens issue details review modal on click', async () => {
    render(<FacilityResolutionPage />);

    await waitFor(() => {
      expect(screen.getByText('Motor belt slipping during speeds above 12 km/h')).toBeDefined();
    });

    fireEvent.click(screen.getByText('Review'));

    await waitFor(() => {
      expect(screen.getByText('Advance Issue State Machine')).toBeDefined();
      expect(screen.getByText('Issue History & Audit Trail')).toBeDefined();
      expect(screen.getByText('-- Select Target Status --')).toBeDefined();
    });
  });
});
