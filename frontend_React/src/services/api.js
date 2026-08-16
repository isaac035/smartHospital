import axios from 'axios'
import { getStoredAuth } from '../utils/auth'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const auth = getStoredAuth()

  if (auth?.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }

  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && !error.config?.url?.toLowerCase().includes('/auth/login')) {
      window.dispatchEvent(new CustomEvent('auth:session-expired'))
    }

    return Promise.reject(error)
  },
)

export default api
