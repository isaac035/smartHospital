import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

export default function DashboardLayout({ role, navigation, title, subtitle, children }) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [menuOpenState, setMenuOpenState] = useState({})
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return <div className="dashboard-shell">
    <aside className={`sidebar ${menuOpen ? 'sidebar-open' : ''}`}>
      <div className="brand"><span className="brand-mark">+</span><span>Smart Hospital</span></div>
      <nav className="sidebar-nav" aria-label={`${role} navigation`}>
        {navigation.map((item) => {
          const hasChildren = item.children && item.children.length > 0;
          const isExpanded = menuOpenState[item.label] ?? (hasChildren && (navigation.length === 1 || item.children.some(c => c.path === location.pathname)));
          
          let isActive = false;
          if (item.path === '/hospital-resources') {
            isActive = location.pathname.startsWith('/hospital-resources');
          } else if (item.path) {
            isActive = location.pathname === item.path;
          }
          if (hasChildren && !isActive) {
            isActive = item.children.some(c => c.path === location.pathname);
          }

          return (
            <div key={item.label} className="nav-item-container">
              <button
                className={isActive ? 'nav-item active' : 'nav-item'}
                style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
                onClick={() => {
                  if (hasChildren) {
                    setMenuOpenState(prev => ({ ...prev, [item.label]: !prev[item.label] }));
                  } else if (item.path) {
                    navigate(item.path);
                    setMenuOpen(false);
                  }
                }}
              >
                <span className="nav-item-label">{item.label}</span>
                {hasChildren && <span style={{ fontSize: '0.75rem', opacity: 0.8, transform: isExpanded ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s' }}>▼</span>}
              </button>
              
              {hasChildren && isExpanded && (
                <div className="nav-sub-menu" style={{ display: 'grid', gap: '2px', paddingLeft: '18px', marginTop: '2px' }}>
                  {item.children.map(child => {
                    const isChildActive = child.path === location.pathname;
                    return (
                      <button
                        key={child.label}
                        className={isChildActive ? 'nav-item nav-sub-item active' : 'nav-item nav-sub-item'}
                        style={{ padding: '9px 12px', fontSize: '0.92rem', opacity: 0.9 }}
                        onClick={() => {
                          if (child.path) navigate(child.path);
                          setMenuOpen(false);
                        }}
                      >
                        {child.label}
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          );
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

