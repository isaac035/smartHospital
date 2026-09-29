import api from './api'

export async function getDashboardSummary() {
  const response = await api.get('/dashboard/summary', { timeout: 10000 })
  return response.data
}
