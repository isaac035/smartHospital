import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// Limited-admin roles are sent back to their own home instead of an error page.
const limitedRoleHome = {
  AppointmentManager: '/admin/appointments',
  DoctorManager: '/admin/doctors',
}

export default function RoleRoute({ allowedRoles }) {
  const { user } = useAuth()
  if (allowedRoles.includes(user?.role)) return <Outlet />
  const fallback = limitedRoleHome[user?.role] || '/unauthorized'
  return <Navigate to={fallback} replace />
}
