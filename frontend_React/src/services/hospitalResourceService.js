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

export const getBeds = async (params = {}) => {
  const response = await api.get('/beds', { params })
  return response.data
}

export const getBedById = async (id) => {
  const response = await api.get(`/beds/${id}`)
  return response.data
}

export const createBed = async (data) => {
  const response = await api.post('/beds', data)
  return response.data
}

export const updateBed = async (id, data) => {
  const response = await api.put(`/beds/${id}`, data)
  return response.data
}

export const updateBedStatus = async (id, data) => {
  const response = await api.patch(`/beds/${id}/status`, data)
  return response.data
}

export const deactivateBed = async (id) => {
  const response = await api.delete(`/beds/${id}`)
  return response.data
}

export const getRoomsByWard = async (wardId, params = {}) => {
  const response = await api.get(`/rooms/by-ward/${wardId}`, { params })
  return response.data
}

export const createRoom = async (data) => {
  const response = await api.post('/rooms', data)
  return response.data
}

export const updateRoom = async (id, data) => {
  const response = await api.put(`/rooms/${id}`, data)
  return response.data
}

export const deactivateRoom = async (id) => {
  const response = await api.delete(`/rooms/${id}`)
  return response.data
}

// ── Medical Resources Services ────────────────────────────────────────────────

export const getMedicalResources = async (params = {}) => {
  const response = await api.get('/medical-resources', { params })
  return response.data
}

export const getMedicalResourceById = async (id) => {
  const response = await api.get(`/medical-resources/${id}`)
  return response.data
}

export const createMedicalResource = async (data) => {
  const response = await api.post('/medical-resources', data)
  return response.data
}

export const updateMedicalResource = async (id, data) => {
  const response = await api.put(`/medical-resources/${id}`, data)
  return response.data
}

export const assignMedicalResource = async (id, data) => {
  const response = await api.post(`/medical-resources/${id}/assign`, data)
  return response.data
}

export const deactivateMedicalResource = async (id) => {
  const response = await api.delete(`/medical-resources/${id}`)
  return response.data
}

export const generateNextResourceCode = (resources = []) => {
  const usedNumbers = new Set()

  if (Array.isArray(resources)) {
    for (const item of resources) {
      const code = item?.resourceCode
      if (typeof code === 'string') {
        const match = code.trim().match(/^RES-(\d+)$/i)
        if (match) {
          const num = parseInt(match[1], 10)
          if (!isNaN(num) && num > 0) {
            usedNumbers.add(num)
          }
        }
      }
    }
  }

  let nextNumber = 1
  while (usedNumbers.has(nextNumber)) {
    nextNumber++
  }

  return `RES-${String(nextNumber).padStart(3, '0')}`
}

export const getNextAvailableResourceCode = async () => {
  let allResources = []
  let page = 1
  let hasMore = true

  while (hasMore) {
    const data = await getMedicalResources({ page, pageSize: 100 })
    const list = Array.isArray(data) ? data : []
    allResources = allResources.concat(list)
    if (list.length < 100) {
      hasMore = false
    } else {
      page++
    }
  }

  return generateNextResourceCode(allResources)
}

// ── Resource Maintenance Services ─────────────────────────────────────────────

export const getMaintenanceRecords = async (params = {}) => {
  const response = await api.get('/resource-maintenances', { params })
  return response.data
}

export const getMaintenanceById = async (id) => {
  const response = await api.get(`/resource-maintenances/${id}`)
  return response.data
}

export const scheduleMaintenance = async (data) => {
  const response = await api.post('/resource-maintenances', data)
  return response.data
}

export const startMaintenance = async (id) => {
  const response = await api.post(`/resource-maintenances/${id}/start`)
  return response.data
}

export const completeMaintenance = async (id, data) => {
  const response = await api.post(`/resource-maintenances/${id}/complete`, data)
  return response.data
}


