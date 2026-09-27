import api from './api'

export async function listDepartments() {
  const response = await api.get('/departments')
  return response.data
}

export async function getDepartment(id) {
  const response = await api.get(`/departments/${id}`)
  return response.data
}

export async function createDepartment(data) {
  const response = await api.post('/departments', data)
  return response.data
}

export async function updateDepartment(id, data) {
  const response = await api.put(`/departments/${id}`, data)
  return response.data
}

export async function deactivateDepartment(id) {
  const response = await api.delete(`/departments/${id}`)
  return response.data
}
