import api from './api'

export async function listDoctors(filters = {}) {
  const response = await api.get('/doctors', { params: filters })
  return response.data
}

export async function getDoctor(id) {
  const response = await api.get(`/doctors/${id}`)
  return response.data
}

export async function createDoctor(data) {
  const response = await api.post('/doctors', data)
  return response.data
}

export async function updateDoctor(id, data) {
  const response = await api.put(`/doctors/${id}`, data)
  return response.data
}

export async function deactivateDoctor(id) {
  const response = await api.delete(`/doctors/${id}`)
  return response.data
}

export async function searchAvailableDoctors(criteria = {}) {
  const response = await api.get('/doctors/available', { params: criteria })
  return response.data
}

export async function getMyDoctorProfile() {
  const response = await api.get('/doctors/me')
  return response.data
}

export async function updateMyDoctorProfile(data) {
  const response = await api.put('/doctors/me', data)
  return response.data
}
