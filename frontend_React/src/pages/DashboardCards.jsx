export default function DashboardCards({ cards }) {
  return <div className="dashboard-cards">{cards.map(([label, value]) => <article className="stat-card" key={label}><p>{label}</p><strong>{value}</strong><span>Placeholder data</span></article>)}</div>
}
