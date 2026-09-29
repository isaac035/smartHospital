import { Link } from 'react-router-dom'

export default function DashboardCards({ cards, loading, error, onRetry }) {
  if (loading) {
    return <div className="dashboard-cards" aria-label="Loading dashboard summary" aria-busy="true">
      {cards.map(({ label }) => <article className="stat-card dashboard-stat-card dashboard-stat-loading" key={label}>
        <p>{label}</p>
        <span className="dashboard-skeleton" aria-hidden="true" />
        <span className="dashboard-skeleton dashboard-skeleton-caption" aria-hidden="true" />
      </article>)}
    </div>
  }

  return <div className="dashboard-cards">
    {cards.map(({ label, value, to }) => <article className="stat-card dashboard-stat-card" key={label}>
      <Link className="dashboard-stat-link" to={to} aria-label={`${label}: ${error ? 'count unavailable' : value}`}>
        <p>{label}</p>
        {error
          ? <span className="dashboard-card-error">Unable to load this count.</span>
          : <strong>{value.toLocaleString()}</strong>}
      </Link>
      {error && <button className="dashboard-retry" type="button" onClick={onRetry}>Retry</button>}
    </article>)}
  </div>
}
