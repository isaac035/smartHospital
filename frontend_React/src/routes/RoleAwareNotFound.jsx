import { Navigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import NotFound from '../pages/NotFound'

export default function RoleAwareNotFound() {
  const { user } = useAuth()
  return user?.role === 'AppointmentManager'
    ? <Navigate to="/admin/appointments" replace />
    : <NotFound />
}
