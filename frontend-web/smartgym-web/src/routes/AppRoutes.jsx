import React from 'react';
import { Routes, Route } from 'react-router-dom';
import AdminLayout from '../layouts/AdminLayout';
import DashboardPage from '../pages/DashboardPage';
import InventoryPage from '../pages/InventoryPage';
import FacilityResolutionPage from '../pages/FacilityResolutionPage';
import ClassSchedulePage from '../pages/ClassSchedulePage';
import MembershipsPage from '../pages/MembershipsPage';

export const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<AdminLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="inventory" element={<InventoryPage />} />
        <Route path="facility" element={<FacilityResolutionPage />} />
        <Route path="classes" element={<ClassSchedulePage />} />
        <Route path="memberships" element={<MembershipsPage />} />
      </Route>
    </Routes>
  );
};

export default AppRoutes;
