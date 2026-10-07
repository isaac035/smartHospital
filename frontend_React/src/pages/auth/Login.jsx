import { useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { dashboardPathForRole } from '../../utils/auth'
import { email as validateEmail, focusFirstError, required } from '../../utils/validators'

export default function Login() {
  const { loginUser, isAuthenticated, user } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState({})
  const [touched, setTouched] = useState({})
  const [submitting, setSubmitting] = useState(false)

  const validate = (values) => {
    const result = {}
    const emailError = validateEmail(values.email)
    const passwordError = required(values.password, 'Password')
    if (emailError) result.email = emailError
    if (passwordError) result.password = passwordError
    return result
  }
  const touch = (field) => {
    setTouched((current) => ({ ...current, [field]: true }))
    setFieldErrors(validate({ email, password }))
  }
  const update = (field, setter) => (event) => {
    setter(event.target.value)
    if (touched[field]) setFieldErrors(validate({ email, password, [field]: event.target.value }))
  }

  if (isAuthenticated) return <Navigate to={dashboardPathForRole(user?.role) || '/unauthorized'} replace />

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    const errors = validate({ email, password })
    setFieldErrors(errors)
    setTouched({ email: true, password: true })
    if (Object.keys(errors).length) return focusFirstError(errors, ['email', 'password'])

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
      <label htmlFor="email" className="required">Email address</label>
      <input id="email" type="email" value={email} onChange={update('email', setEmail)} onBlur={() => touch('email')} autoComplete="email" maxLength={255} disabled={submitting} aria-invalid={touched.email && fieldErrors.email ? 'true' : undefined} aria-describedby={touched.email && fieldErrors.email ? 'email-error' : undefined} />
      {touched.email && fieldErrors.email && <p id="email-error" className="field-error">{fieldErrors.email}</p>}
      <label htmlFor="password" className="required">Password</label>
      <div className="password-field"><input id="password" type={showPassword ? 'text' : 'password'} value={password} onChange={update('password', setPassword)} onBlur={() => touch('password')} autoComplete="current-password" disabled={submitting} aria-invalid={touched.password && fieldErrors.password ? 'true' : undefined} aria-describedby={touched.password && fieldErrors.password ? 'password-error' : undefined} /><button type="button" onClick={() => setShowPassword(!showPassword)} aria-label={showPassword ? 'Hide password' : 'Show password'}>{showPassword ? 'Hide' : 'Show'}</button></div>
      {touched.password && fieldErrors.password && <p id="password-error" className="field-error">{fieldErrors.password}</p>}
      <button className="primary-button" type="submit" disabled={submitting}>{submitting ? 'Signing in...' : 'Login'}</button>
    </form>
  </section></main>
}
