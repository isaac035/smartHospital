import api from './api'

export async function listSchedules(filters = {}) {
  const response = await api.get('/schedules', { params: filters })
  return response.data
}

export async function createSchedule(data) {
  const response = await api.post('/schedules', data)
  return response.data
}

export async function updateSchedule(id, data) {
  const response = await api.put(`/schedules/${id}`, data)
  return response.data
}

export async function removeSchedule(id) {
  const response = await api.delete(`/schedules/${id}`)
  return response.data
}
