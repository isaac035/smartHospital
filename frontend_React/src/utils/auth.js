const AUTH_STORAGE_KEY = 'smartHospitalAuth'

export function getStoredAuth() {
  try {
    return JSON.parse(localStorage.getItem(AUTH_STORAGE_KEY))
  } catch {
    return null
  }
}

export function saveAuth(auth) {
  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth))
}

export function clearStoredAuth() {
  localStorage.removeItem(AUTH_STORAGE_KEY)
}

export function isSessionExpired(auth) {
  return !auth?.expiresAt || new Date(auth.expiresAt).getTime() <= Date.now()
}

export function dashboardPathForRole(role) {
  const paths = {
    Admin: '/admin/dashboard',
    Doctor: '/doctor/dashboard',
    Staff: '/staff/dashboard',
  }

  return paths[role] || null
}
