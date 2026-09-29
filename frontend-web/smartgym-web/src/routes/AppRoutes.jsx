import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import AdminLayout from '../layouts/AdminLayout';
import ProtectedRoute from './ProtectedRoute';

// Pages
import LoginPage from '../pages/LoginPage';
import DashboardPage from '../pages/DashboardPage';
import MembersPage from '../pages/MembersPage';
import TrainersPage from '../pages/TrainersPage';
import MembershipPlansPage from '../pages/MembershipPlansPage';
import MembershipsListPage from '../pages/MembershipsListPage';
import GoalsPage from '../pages/GoalsPage';
import FitnessClassesPage from '../pages/FitnessClassesPage';
import SchedulesPage from '../pages/SchedulesPage';
import BookingsPage from '../pages/BookingsPage';
import AttendancePage from '../pages/AttendancePage';
import SuppliersPage from '../pages/SuppliersPage';
import ProductsPage from '../pages/ProductsPage';
import InventoryPage from '../pages/InventoryPage';
import FeedbackPage from '../pages/FeedbackPage';
import FacilityIssuesPage from '../pages/FacilityIssuesPage';
import RepairOrdersPage from '../pages/RepairOrdersPage';
import NotificationsPage from '../pages/NotificationsPage';
import ReportsPage from '../pages/ReportsPage';
import AiWorkflowsPage from '../pages/AiWorkflowsPage';
import ApprovalsPage from '../pages/ApprovalsPage';
import FacilityResolutionPage from '../pages/FacilityResolutionPage';

export const AppRoutes = () => {
  return (
    <Routes>
      {/* Public Route */}
      <Route path="/login" element={<LoginPage />} />

      {/* Protected Routes inside AdminLayout */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AdminLayout />
          </ProtectedRoute>
        }
      >
        {/* Default redirect to /dashboard */}
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />

        {/* Memberships & Training */}
        <Route
          path="members"
          element={
            <ProtectedRoute allowedRoles={['ADMIN', 'TRAINER']}>
              <MembersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="trainers"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <TrainersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="membership-plans"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <MembershipPlansPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="memberships"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <MembershipsListPage />
            </ProtectedRoute>
          }
        />
        <Route path="goals" element={<GoalsPage />} />

        {/* Classes & Schedules */}
        <Route path="classes" element={<FitnessClassesPage />} />
        <Route
          path="schedules"
          element={
            <ProtectedRoute allowedRoles={['ADMIN', 'TRAINER']}>
              <SchedulesPage />
            </ProtectedRoute>
          }
        />
        <Route path="bookings" element={<BookingsPage />} />
        <Route
          path="attendance"
          element={
            <ProtectedRoute allowedRoles={['ADMIN', 'TRAINER']}>
              <AttendancePage />
            </ProtectedRoute>
          }
        />

        {/* Supplements & Stock */}
        <Route
          path="suppliers"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <SuppliersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="products"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <ProductsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="inventory"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <InventoryPage />
            </ProtectedRoute>
          }
        />

        {/* Facility & Equipment */}
        <Route path="facility-issues" element={<FacilityIssuesPage />} />
        <Route path="facility" element={<FacilityResolutionPage defaultTab="issues" />} />
        <Route
          path="repair-orders"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <RepairOrdersPage />
            </ProtectedRoute>
          }
        />
        <Route path="feedback" element={<FeedbackPage />} />

        {/* AI & Governance */}
        <Route
          path="ai-workflows"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <AiWorkflowsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="approvals"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <ApprovalsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="reports"
          element={
            <ProtectedRoute allowedRoles={['ADMIN']}>
              <ReportsPage />
            </ProtectedRoute>
          }
        />

        {/* Notifications */}
        <Route path="notifications" element={<NotificationsPage />} />

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Route>
    </Routes>
  );
};

export default AppRoutes;
