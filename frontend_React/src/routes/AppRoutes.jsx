import { Navigate, Route, Routes } from 'react-router-dom'
import Login from '../pages/auth/Login'
import AdminDashboard from '../pages/admin/AdminDashboard'
import DoctorDashboard from '../pages/doctor/DoctorDashboard'
import DoctorMedicalRecordsPage from '../pages/doctor/emr/DoctorMedicalRecordsPage'
import DoctorPrescriptionsPage from '../pages/doctor/emr/DoctorPrescriptionsPage'
import DoctorLabReportsPage from '../pages/doctor/emr/DoctorLabReportsPage'
import StaffDashboard from '../pages/staff/StaffDashboard'
import Unauthorized from '../pages/Unauthorized'
import NotFound from '../pages/NotFound'
import ProtectedRoute from './ProtectedRoute'
import RoleRoute from './RoleRoute'

import AppointmentsDashboard from '../pages/appointments/AppointmentsDashboard'
import BookAppointment from '../pages/appointments/BookAppointment'
import AppointmentCalendar from '../pages/appointments/AppointmentCalendar'
import AppointmentDetails from '../pages/appointments/AppointmentDetails'
import RescheduleAppointment from '../pages/appointments/RescheduleAppointment'
import QueueDashboard from '../pages/queue/QueueDashboard'

import ResourceDashboard from '../pages/hospitalResource/ResourceDashboard'
import WardManagement from '../pages/hospitalResource/WardManagement'
import BedManagement from '../pages/hospitalResource/BedManagement'
import Admissions from '../pages/hospitalResource/Admissions'
import MedicalResources from '../pages/hospitalResource/MedicalResources'
import Maintenance from '../pages/hospitalResource/Maintenance'

export default function AppRoutes() {
  return <Routes>
    <Route path="/login" element={<Login />} />
    <Route element={<ProtectedRoute />}>
      <Route element={<RoleRoute allowedRoles={['Admin']} />}>
        <Route path="/admin/dashboard" element={<AdminDashboard />} />
        <Route path="/admin/appointments" element={<AppointmentsDashboard />} />
        <Route path="/admin/appointments/book" element={<BookAppointment />} />
        <Route path="/admin/appointments/calendar" element={<AppointmentCalendar />} />
        <Route path="/admin/appointments/:id" element={<AppointmentDetails />} />
        <Route path="/admin/appointments/:id/reschedule" element={<RescheduleAppointment />} />
        <Route path="/admin/queue-management" element={<QueueDashboard />} />
      </Route>
      <Route element={<RoleRoute allowedRoles={['Doctor']} />}>
        <Route path="/doctor/dashboard" element={<DoctorDashboard />} />
        <Route path="/doctor/my-appointments" element={<AppointmentsDashboard />} />
        <Route path="/doctor/appointments/calendar" element={<AppointmentCalendar />} />
        <Route path="/doctor/appointments/:id" element={<AppointmentDetails />} />
        <Route path="/doctor/queue-management" element={<QueueDashboard />} />
        <Route path="/doctor/medical-records" element={<DoctorMedicalRecordsPage />} />
        <Route path="/doctor/prescriptions" element={<DoctorPrescriptionsPage />} />
        <Route path="/doctor/lab-reports" element={<DoctorLabReportsPage />} />
      </Route>
      <Route element={<RoleRoute allowedRoles={['Staff']} />}>
        <Route path="/staff/dashboard" element={<StaffDashboard />} />
        <Route path="/staff/appointments" element={<AppointmentsDashboard />} />
        <Route path="/staff/appointments/book" element={<BookAppointment />} />
        <Route path="/staff/appointments/calendar" element={<AppointmentCalendar />} />
        <Route path="/staff/appointments/:id" element={<AppointmentDetails />} />
        <Route path="/staff/appointments/:id/reschedule" element={<RescheduleAppointment />} />
        <Route path="/staff/queue-management" element={<QueueDashboard />} />
      </Route>
      <Route element={<RoleRoute allowedRoles={['Admin', 'Staff']} />}>
        <Route path="/hospital-resources" element={<ResourceDashboard />} />
        <Route path="/hospital-resources/wards" element={<WardManagement />} />
        <Route path="/hospital-resources/beds" element={<BedManagement />} />
        <Route path="/hospital-resources/admissions" element={<Admissions />} />
        <Route path="/hospital-resources/medical-resources" element={<MedicalResources />} />
        <Route path="/hospital-resources/maintenance" element={<Maintenance />} />
      </Route>
      <Route path="/unauthorized" element={<Unauthorized />} />
    </Route>
    <Route path="/" element={<Navigate to="/login" replace />} />
    <Route path="*" element={<NotFound />} />
  </Routes>
}
