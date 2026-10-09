import { Navigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import NotFound from '../pages/NotFound'

const limitedRoleHome = {
  AppointmentManager: '/admin/appointments',
  ResourceAdmin: '/hospital-resources',
  DoctorManager: '/admin/doctors',
  ClinicalCareManager: '/admin/reports',
}

export default function RoleAwareNotFound() {
  const { user } = useAuth()
  const home = limitedRoleHome[user?.role]
  return home ? <Navigate to={home} replace /> : <NotFound />
}
