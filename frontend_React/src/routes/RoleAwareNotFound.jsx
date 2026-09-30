import { Navigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import NotFound from '../pages/NotFound'

export default function RoleAwareNotFound() {
  const { user } = useAuth()
  if (user?.role === 'AppointmentManager') {
    return <Navigate to="/admin/appointments" replace />
  }
  if (user?.role === 'ResourceAdmin') {
    return <Navigate to="/hospital-resources" replace />
  }
  return <NotFound />
}
