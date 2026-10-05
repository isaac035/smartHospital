import api from './api'

export const listUsers = async (params = {}) => {
  const response = await api.get('/users', { params })
  return response.data
}

export const searchPatients = async (query, limit = 10) => {
  const response = await api.get('/users/patients/search', {
    params: { query, limit },
  })
  return response.data
}

export const updateUser = async (id, payload) => {
  const response = await api.put(`/users/${id}`, payload)
  return response.data
}

export const createDoctorManager = async (payload) => {
  const response = await api.post('/users/doctor-manager', payload)
  return response.data
}

export const deactivateUser = async (id) => {
  const response = await api.delete(`/users/${id}`)
  return response.data
}

export const activateUser = async (id) => {
  const response = await api.post(`/users/${id}/activate`)
  return response.data
}
