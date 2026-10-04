import { Navigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import NotFound from '../pages/NotFound'

const limitedRoleHome = {
  AppointmentManager: '/admin/appointments',
  DoctorManager: '/admin/doctors',
}

export default function RoleAwareNotFound() {
  const { user } = useAuth()
  const home = limitedRoleHome[user?.role]
  return home ? <Navigate to={home} replace /> : <NotFound />
}
