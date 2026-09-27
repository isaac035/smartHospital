import api from './api'

export async function listConsultationTypes() {
  const response = await api.get('/consultation-types')
  return response.data
}

export async function getConsultationType(id) {
  const response = await api.get(`/consultation-types/${id}`)
  return response.data
}

export async function createConsultationType(data) {
  const response = await api.post('/consultation-types', data)
  return response.data
}

export async function updateConsultationType(id, data) {
  const response = await api.put(`/consultation-types/${id}`, data)
  return response.data
}

export async function deactivateConsultationType(id) {
  const response = await api.delete(`/consultation-types/${id}`)
  return response.data
}
