import { useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { dashboardPathForRole } from '../../utils/auth'

export default function Login() {
  const { loginUser, isAuthenticated, user } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  if (isAuthenticated) return <Navigate to={dashboardPathForRole(user?.role) || '/unauthorized'} replace />

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    if (!email.trim() || !password) return setError('Please enter your email and password.')
    if (!/^\S+@\S+\.\S+$/.test(email)) return setError('Please enter a valid email address.')

    try {
      setSubmitting(true)
      const loggedInUser = await loginUser(email.trim(), password)
      const destination = dashboardPathForRole(loggedInUser.role)
      if (!destination) {
        navigate('/unauthorized', { replace: true })
        return
      }
      navigate(location.state?.from?.pathname || destination, { replace: true })
    } catch (requestError) {
      if (!requestError.response) setError('Unable to connect to the server. Please try again.')
      else if (requestError.response.status === 403) setError('You are not authorized to access this application.')
      else setError('Invalid email or password.')
    } finally {
      setSubmitting(false)
    }
  }

  return <main className="auth-page"><section className="login-card">
    <div className="login-brand"><span className="brand-mark">+</span><span>Smart Hospital</span></div>
    <div className="login-heading"><p className="eyebrow">Secure staff access</p><h1>Welcome back</h1><p>Sign in to access your hospital workspace.</p></div>
    <form onSubmit={handleSubmit} noValidate>
      {error && <p className="form-error" role="alert">{error}</p>}
      <label htmlFor="email">Email address</label>
      <input id="email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} autoComplete="email" disabled={submitting} />
      <label htmlFor="password">Password</label>
      <div className="password-field"><input id="password" type={showPassword ? 'text' : 'password'} value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" disabled={submitting} /><button type="button" onClick={() => setShowPassword(!showPassword)} aria-label={showPassword ? 'Hide password' : 'Show password'}>{showPassword ? 'Hide' : 'Show'}</button></div>
      <button className="primary-button" type="submit" disabled={submitting}>{submitting ? 'Signing in...' : 'Login'}</button>
    </form>
  </section></main>
}
