import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

export default function RoleRoute({ allowedRoles }) {
  const { user } = useAuth()
  if (allowedRoles.includes(user?.role)) return <Outlet />
  const fallback = user?.role === 'AppointmentManager' ? '/admin/appointments' : '/unauthorized'
  return <Navigate to={fallback} replace />
}
