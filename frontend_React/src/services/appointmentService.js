import api from './api'

// -- Appointments --

export const getAppointments = async (filters = {}) => {
  const params = new URLSearchParams()
  if (filters.status) params.append('status', filters.status)
  if (filters.priority) params.append('priority', filters.priority)
  if (filters.doctorId) params.append('doctorId', filters.doctorId)
  if (filters.departmentId) params.append('departmentId', filters.departmentId)
  if (filters.fromDate) params.append('fromDate', filters.fromDate)
  if (filters.toDate) params.append('toDate', filters.toDate)
  if (filters.patientId) params.append('patientId', filters.patientId)
  
  const response = await api.get(`/appointments?${params.toString()}`)
  return response.data
}

export const getAppointmentById = async (id) => {
  const response = await api.get(`/appointments/${id}`)
  return response.data
}

export const bookAppointment = async (data) => {
  const response = await api.post('/appointments', data)
  return response.data
}

export const updateAppointment = async (id, data) => {
  const response = await api.put(`/appointments/${id}`, data)
  return response.data
}

export const cancelAppointment = async (id, reason) => {
  const response = await api.post(`/appointments/${id}/cancel`, { reason })
  return response.data
}

export const rescheduleAppointment = async (id, data) => {
  const response = await api.post(`/appointments/${id}/reschedule`, data)
  return response.data
}

export const getAppointmentHistory = async (id) => {
  const response = await api.get(`/appointments/${id}/history`)
  return response.data
}

export const confirmEmergency = async (id) => {
  const response = await api.post(`/appointments/${id}/confirm-emergency`)
  return response.data
}

// -- Queues --

export const getQueue = async (doctorId) => {
  const response = await api.get(`/queues/${doctorId}`)
  return response.data
}

export const callQueueEntry = async (id) => {
  const response = await api.post(`/queues/${id}/call`)
  return response.data
}

export const checkIn = async (appointmentId) => {
  const response = await api.post(`/queues/check-in/${appointmentId}`)
  return response.data
}

export const markNoShow = async (id) => {
  const response = await api.post(`/queues/${id}/no-show`)
  return response.data
}

export const markCompleted = async (id) => {
  const response = await api.post(`/queues/${id}/complete`)
  return response.data
}

export const getQueueEntryStatus = async (id) => {
  const response = await api.get(`/queues/${id}/status`)
  return response.data
}

// -- Availability --

export const getAvailableSlots = async (date, doctorId, departmentId) => {
  const params = new URLSearchParams()
  params.append('date', date)
  if (doctorId) params.append('doctorId', doctorId)
  if (departmentId) params.append('departmentId', departmentId)
  
  const response = await api.get(`/availability/slots?${params.toString()}`)
  return response.data
}

export const getRescheduleSuggestions = async (doctorId, preferredDate, durationMinutes = 30) => {
  const params = new URLSearchParams()
  params.append('doctorId', doctorId)
  params.append('preferredDate', preferredDate)
  params.append('durationMinutes', durationMinutes)
  
  const response = await api.get(`/availability/suggestions?${params.toString()}`)
  return response.data
}

// -- Notifications --

export const getNotifications = async () => {
  const response = await api.get('/notifications')
  return response.data
}

export const markNotificationRead = async (id) => {
  const response = await api.post(`/notifications/${id}/read`)
  return response.data
}
