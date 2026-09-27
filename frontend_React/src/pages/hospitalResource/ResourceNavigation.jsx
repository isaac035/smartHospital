import { Link, useLocation } from 'react-router-dom'

const navItems = [
  { label: 'Resource Dashboard', path: '/hospital-resources' },
  { label: 'Admissions', path: '/hospital-resources/admissions' },
  { label: 'Ward Management', path: '/hospital-resources/wards' },
  { label: 'Bed Management', path: '/hospital-resources/beds' },
  { label: 'Medical Resources', path: '/hospital-resources/medical-resources' },
  { label: 'Maintenance', path: '/hospital-resources/maintenance' },
]

export default function ResourceNavigation() {
  const location = useLocation()

  return (
    <nav className="flex flex-wrap gap-2 mb-6 pb-4 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }} aria-label="Hospital Resource module navigation">
      {navItems.map((item) => {
        const isActive = location.pathname === item.path
        return (
          <Link
            key={item.path}
            to={item.path}
            className="text-sm font-semibold transition-colors"
            style={{
              padding: '8px 16px',
              borderRadius: '8px',
              textDecoration: 'none',
              border: isActive
                ? '1px solid var(--color-accent)'
                : '1px solid color-mix(in srgb, var(--color-secondary) 20%, var(--color-primary))',
              background: isActive
                ? 'var(--color-accent)'
                : 'var(--color-primary)',
              color: isActive
                ? 'var(--color-primary)'
                : 'var(--color-accent)',
            }}
          >
            {item.label}
          </Link>
        )
      })}
    </nav>
  )
}
