import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

export default function RoleRoute({ allowedRoles }) {
  const { user } = useAuth()
  return allowedRoles.includes(user?.role) ? <Outlet /> : <Navigate to="/unauthorized" replace />
}
