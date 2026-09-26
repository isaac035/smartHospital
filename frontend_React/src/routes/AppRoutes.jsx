import { Navigate, Route, Routes } from 'react-router-dom'
import Login from '../pages/auth/Login'
import AdminDashboard from '../pages/admin/AdminDashboard'
import DoctorManagement from '../pages/admin/DoctorManagement'
import DepartmentManagement from '../pages/admin/DepartmentManagement'
import ConsultationTypeManagement from '../pages/admin/ConsultationTypeManagement'
import AvailabilityCalendar from '../pages/admin/AvailabilityCalendar'
import LeaveManagement from '../pages/admin/LeaveManagement'
import DoctorDashboard from '../pages/doctor/DoctorDashboard'
import MyProfile from '../pages/doctor/MyProfile'
import MySchedule from '../pages/doctor/MySchedule'
import MyLeave from '../pages/doctor/MyLeave'
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
      <Route element={<RoleRoute allowedRoles={['Admin', 'Staff']} />}>
        <Route path="/admin/doctors" element={<DoctorManagement />} />
        <Route path="/admin/departments" element={<DepartmentManagement />} />
        <Route path="/admin/consultation-types" element={<ConsultationTypeManagement />} />
        <Route path="/admin/schedules" element={<AvailabilityCalendar />} />
        <Route path="/admin/leaves" element={<LeaveManagement />} />
      </Route>
      <Route element={<RoleRoute allowedRoles={['Doctor']} />}>
        <Route path="/doctor/dashboard" element={<DoctorDashboard />} />
        <Route path="/doctor/profile" element={<MyProfile />} />
        <Route path="/doctor/schedule" element={<MySchedule />} />
        <Route path="/doctor/leave" element={<MyLeave />} />
      </Route>
      <Route element={<RoleRoute allowedRoles={['Staff']} />}><Route path="/staff/dashboard" element={<StaffDashboard />} /></Route>
      <Route path="/unauthorized" element={<Unauthorized />} />
    </Route>
    <Route path="/" element={<Navigate to="/login" replace />} />
    <Route path="*" element={<NotFound />} />
  </Routes>
}
