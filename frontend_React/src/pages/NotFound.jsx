import { Link } from 'react-router-dom'

export default function NotFound() { return <main className="status-page"><section className="status-card"><p className="status-code">404</p><h1>Page not found</h1><p>The page you requested does not exist or may have moved.</p><Link className="primary-button button-link" to="/login">Go to Login</Link></section></main> }
