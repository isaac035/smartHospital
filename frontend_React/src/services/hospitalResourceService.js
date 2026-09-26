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

// ── Admissions Services ───────────────────────────────────────────────────────

export const getAdmissions = async (params = {}) => {
  const response = await api.get('/admissions', { params })
  return response.data
}

export const getAdmissionById = async (id) => {
  const response = await api.get(`/admissions/${id}`)
  return response.data
}

export const createAdmission = async (data) => {
  const response = await api.post('/admissions', data)
  return response.data
}

export const updateAdmission = async (id, data) => {
  const response = await api.put(`/admissions/${id}`, data)
  return response.data
}

export const allocateBed = async (id, data) => {
  const response = await api.post(`/admissions/${id}/allocate-bed`, data)
  return response.data
}

export const transferPatient = async (id, data) => {
  const response = await api.post(`/admissions/${id}/transfer`, data)
  return response.data
}

export const dischargePatient = async (id, data) => {
  const response = await api.post(`/admissions/${id}/discharge`, data)
  return response.data
}

// ── Beds & Wards Services ─────────────────────────────────────────────────────

export const getAvailableBeds = async (params = {}) => {
  const response = await api.get('/beds/available', { params })
  return response.data
}

export const getAllWards = async (params = {}) => {
  const response = await api.get('/wards', { params })
  return response.data
}

export const createWard = async (data) => {
  const response = await api.post('/wards', data)
  return response.data
}

export const updateWard = async (id, data) => {
  const response = await api.put(`/wards/${id}`, data)
  return response.data
}

export const deactivateWard = async (id) => {
  const response = await api.delete(`/wards/${id}`)
  return response.data
}

export const getWardOccupancy = async (id) => {
  const response = await api.get(`/wards/${id}/occupancy`)
  return response.data
}


