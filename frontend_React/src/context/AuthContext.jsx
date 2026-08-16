import { useEffect, useMemo, useState } from 'react'
import { clearStoredAuth, getStoredAuth, isSessionExpired, saveAuth } from '../utils/auth'
import * as authService from '../services/authService'
import { AuthContext } from './authContextValue'

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState(null)
  const [loading, setLoading] = useState(true)

  const logout = () => {
    clearStoredAuth()
    setAuth(null)
  }

  useEffect(() => {
    const savedAuth = getStoredAuth()
    if (savedAuth && !isSessionExpired(savedAuth)) setAuth(savedAuth)
    else clearStoredAuth()
    setLoading(false)

    const handleExpiredSession = () => logout()
    window.addEventListener('auth:session-expired', handleExpiredSession)
    return () => window.removeEventListener('auth:session-expired', handleExpiredSession)
  }, [])

  const loginUser = async (email, password) => {
    const response = await authService.login(email, password)
    const allowedRoles = ['Admin', 'Doctor', 'Staff']

    if (!allowedRoles.includes(response.role)) {
      const error = new Error('This account is not permitted to access the web application.')
      error.response = { status: 403 }
      throw error
    }

    const nextAuth = {
      userId: response.userId,
      firstName: response.firstName,
      lastName: response.lastName,
      email: response.email,
      role: response.role,
      token: response.token,
      expiresAt: response.expiresAt,
    }

    saveAuth(nextAuth)
    setAuth(nextAuth)
    return nextAuth
  }

  const value = useMemo(() => ({
    user: auth,
    token: auth?.token || null,
    isAuthenticated: Boolean(auth),
    loading,
    loginUser,
    logout,
  }), [auth, loading])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
