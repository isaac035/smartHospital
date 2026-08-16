import { useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { dashboardPathForRole } from '../utils/auth'

export default function Unauthorized() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  return <main className="status-page"><section className="status-card"><p className="status-code">403</p><h1>Access Denied</h1><p>You do not have permission to access this page.</p><div className="status-actions"><button className="primary-button" onClick={() => navigate(dashboardPathForRole(user?.role) || '/login')}>Back to Dashboard</button><button className="secondary-button" onClick={() => { logout(); navigate('/login', { replace: true }) }}>Logout</button></div></section></main>
}
