import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ClassSchedulePage from '../ClassSchedulePage';
import classesApi from '../../services/classesApi';

vi.mock('../../services/classesApi', () => {
  return {
    default: {
      getCategories: vi.fn(),
      getClasses: vi.fn(),
      getSchedules: vi.fn(),
      getBookings: vi.fn(),
      getScheduleAttendanceSheet: vi.fn(),
      cancelSchedule: vi.fn(),
      deleteClass: vi.fn(),
      deleteCategory: vi.fn(),
      cancelBooking: vi.fn(),
      recordAttendance: vi.fn(),
      recordBulkAttendance: vi.fn(),
    },
  };
});

describe('ClassSchedulePage Component', () => {
  const mockCategories = [
    {
      id: 'cat-1',
      name: 'High-Intensity Interval Training (HIIT)',
      description: 'Interval cardio training',
      fitnessClassCount: 3,
      createdAt: '2026-01-01T00:00:00Z',
    },
  ];

  const mockClasses = [
    {
      id: 'cls-1',
      categoryId: 'cat-1',
      categoryName: 'High-Intensity Interval Training (HIIT)',
      name: 'Metabolic Blast HIIT',
      description: 'Functional high-intensity circuit',
      durationMinutes: 45,
      defaultCapacity: 18,
      intensityLevel: 'High',
      totalSchedulesCount: 5,
      createdAt: '2026-01-01T00:00:00Z',
    },
  ];

  const mockSchedules = [
    {
      id: 'sch-1',
      classId: 'cls-1',
      className: 'Metabolic Blast HIIT',
      categoryName: 'High-Intensity Interval Training (HIIT)',
      durationMinutes: 45,
      intensityLevel: 'High',
      trainerId: 'trainer-1',
      trainerName: 'Kavinda Fernando',
      room: 'Studio 1 — High Intensity',
      startTime: '2026-10-01T09:00:00Z',
      endTime: '2026-10-01T09:45:00Z',
      capacity: 18,
      bookedCount: 2,
      availableSpots: 16,
      isFull: false,
      status: 1, // Scheduled
      createdAt: '2026-01-01T00:00:00Z',
    },
  ];

  const mockBookings = [
    {
      id: 'bk-1',
      scheduleId: 'sch-1',
      className: 'Metabolic Blast HIIT',
      trainerName: 'Kavinda Fernando',
      room: 'Studio 1',
      classStartTime: '2026-10-01T09:00:00Z',
      classEndTime: '2026-10-01T09:45:00Z',
      memberId: 'mem-1',
      memberName: 'Nuwan Perera',
      memberEmail: 'member@smartgym.com',
      bookingTime: '2026-09-28T10:00:00Z',
      status: 1,
      attendance: null,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    classesApi.getCategories.mockResolvedValue(mockCategories);
    classesApi.getClasses.mockResolvedValue({ items: mockClasses, totalCount: 1, totalPages: 1 });
    classesApi.getSchedules.mockResolvedValue({ items: mockSchedules, totalCount: 1, totalPages: 1 });
    classesApi.getBookings.mockResolvedValue({ items: mockBookings, totalCount: 1, totalPages: 1 });
    classesApi.getScheduleAttendanceSheet.mockResolvedValue(mockBookings);
  });

  it('renders page header and scheduled sessions by default', async () => {
    render(<ClassSchedulePage />);

    expect(screen.getByText('Class Scheduling & Timetables')).toBeDefined();
    await waitFor(() => {
      expect(screen.getByText('Metabolic Blast HIIT')).toBeDefined();
      expect(screen.getByText('16 spots open')).toBeDefined();
    });
  });

  it('switches to Fitness Classes tab and renders class table', async () => {
    render(<ClassSchedulePage />);

    const classesTab = screen.getByRole('button', { name: /Fitness Classes/i });
    fireEvent.click(classesTab);

    await waitFor(() => {
      expect(screen.getByText('45 mins')).toBeDefined();
      expect(screen.getByText('18 spots')).toBeDefined();
      expect(screen.getByText('Functional high-intensity circuit')).toBeDefined();
    });
  });

  it('switches to Categories tab and shows category information', async () => {
    render(<ClassSchedulePage />);

    const catTab = screen.getByRole('button', { name: /Categories/i });
    fireEvent.click(catTab);

    await waitFor(() => {
      expect(screen.getByText('High-Intensity Interval Training (HIIT)')).toBeDefined();
      expect(screen.getByText('3 classes')).toBeDefined();
    });
  });

  it('switches to Member Bookings tab and displays reservations', async () => {
    render(<ClassSchedulePage />);

    const bookingsTab = screen.getByRole('button', { name: /Member Bookings/i });
    fireEvent.click(bookingsTab);

    await waitFor(() => {
      expect(screen.getByText('Nuwan Perera')).toBeDefined();
      expect(screen.getByText('member@smartgym.com')).toBeDefined();
      expect(screen.getByText('Confirmed')).toBeDefined();
    });
  });

  it('opens Attendance Sheet Modal when attendance button clicked', async () => {
    render(<ClassSchedulePage />);

    await waitFor(() => {
      expect(screen.getByText('Attendance (2)')).toBeDefined();
    });

    const attButton = screen.getByText('Attendance (2)');
    fireEvent.click(attButton);

    await waitFor(() => {
      expect(screen.getByText('Class Attendance Sheet')).toBeDefined();
      expect(screen.getByText('Submit All Attendances')).toBeDefined();
    });
  });
});
