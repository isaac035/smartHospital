import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

export default function DashboardLayout({ role, navigation, title, subtitle, children }) {
  const [menuOpen, setMenuOpen] = useState(false)
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const dashboardPath = `/${role.toLowerCase()}/dashboard`

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const getItemPath = (item) => {
    if (item === 'Dashboard') return dashboardPath
    const slug = item.toLowerCase().replace(/\s+/g, '-')
    return `/${role.toLowerCase()}/${slug}`
  }

  return <div className="dashboard-shell">
    <aside className={`sidebar ${menuOpen ? 'sidebar-open' : ''}`}>
      <div className="brand"><span className="brand-mark">+</span><span>Smart Hospital</span></div>
      <nav className="sidebar-nav" aria-label={`${role} navigation`}>
        {navigation.map((item) => {
          const itemPath = getItemPath(item)
          const isActive = location.pathname === itemPath
          return <button
            className={isActive ? 'nav-item active' : 'nav-item'}
            key={item}
            onClick={() => { navigate(itemPath); setMenuOpen(false) }}
          >{item}</button>
        })}
      </nav>
      <button className="nav-item logout-button" onClick={handleLogout}>Logout</button>
    </aside>
    {menuOpen && <button className="sidebar-overlay" aria-label="Close navigation" onClick={() => setMenuOpen(false)} />}
    <main className="dashboard-main">
      <header className="dashboard-header">
        <button className="menu-toggle" onClick={() => setMenuOpen(true)} aria-label="Open navigation">☰</button>
        <div><p className="eyebrow">{role} portal</p><h1>{title}</h1><p className="page-subtitle">{subtitle}</p></div>
        <div className="user-summary"><span className="user-avatar">{user?.firstName?.[0] || 'U'}</span><div><strong>{user?.firstName} {user?.lastName}</strong><span>{user?.role}</span></div></div>
      </header>
      <section className="dashboard-content">{children}</section>
    </main>
  </div>
}
