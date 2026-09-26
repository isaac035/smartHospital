import api from './api'

// ── Hospital Resource & Bed Management Services ──────────────────────────────

export const getOccupancyOverview = async () => {
  const response = await api.get('/occupancy/overview')
  return response.data
}

export const getWardOccupancies = async () => {
  const response = await api.get('/occupancy/wards')
  return response.data
}

export const getWardOccupancyById = async (wardId) => {
  const response = await api.get(`/occupancy/wards/${wardId}`)
  return response.data
}
