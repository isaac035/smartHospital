import api from './api'

export async function listLeaves(filters = {}) {
  const response = await api.get('/leaves', { params: filters })
  return response.data
}

export async function createLeave(data) {
  const response = await api.post('/leaves', data)
  return response.data
}

export async function updateLeave(id, data) {
  const response = await api.put(`/leaves/${id}`, data)
  return response.data
}

export async function cancelLeave(id) {
  const response = await api.delete(`/leaves/${id}`)
  return response.data
}
