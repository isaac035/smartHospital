import api from './api'

export const listUsers = async () => {
  const response = await api.get('/users')
  return response.data
}

export const updateUser = async (id, payload) => {
  const response = await api.put(`/users/${id}`, payload)
  return response.data
}

export const deactivateUser = async (id) => {
  const response = await api.delete(`/users/${id}`)
  return response.data
}
