import { Navigate, Route, Routes } from 'react-router-dom'
import Login from '../pages/auth/Login'
import AdminDashboard from '../pages/admin/AdminDashboard'
import DoctorDashboard from '../pages/doctor/DoctorDashboard'
import StaffDashboard from '../pages/staff/StaffDashboard'
import Unauthorized from '../pages/Unauthorized'
import NotFound from '../pages/NotFound'
import ProtectedRoute from './ProtectedRoute'
import RoleRoute from './RoleRoute'

export default function AppRoutes() {
  return <Routes>
    <Route path="/login" element={<Login />} />
    <Route element={<ProtectedRoute />}>
      <Route element={<RoleRoute allowedRoles={['Admin']} />}><Route path="/admin/dashboard" element={<AdminDashboard />} /></Route>
      <Route element={<RoleRoute allowedRoles={['Doctor']} />}><Route path="/doctor/dashboard" element={<DoctorDashboard />} /></Route>
      <Route element={<RoleRoute allowedRoles={['Staff']} />}><Route path="/staff/dashboard" element={<StaffDashboard />} /></Route>
      <Route path="/unauthorized" element={<Unauthorized />} />
    </Route>
    <Route path="/" element={<Navigate to="/login" replace />} />
    <Route path="*" element={<NotFound />} />
  </Routes>
}
