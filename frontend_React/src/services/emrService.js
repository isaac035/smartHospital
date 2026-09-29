import api from './api'

export async function getPatientMedicalProfile(patientId) {
  const response = await api.get(`/patientprofiles/patient/${patientId}`)
  return response.data
}

export async function upsertPatientMedicalProfile(patientId, profileData) {
  const response = await api.put(`/patientprofiles/patient/${patientId}`, profileData)
  return response.data
}

export async function getPatientMedicalRecords(patientId) {
  const response = await api.get(`/medicalrecords/patient/${patientId}`)
  return response.data
}

export async function getMedicalRecordById(id) {
  const response = await api.get(`/medicalrecords/${id}`)
  return response.data
}

export async function createMedicalRecord(recordData) {
  const response = await api.post('/medicalrecords', recordData)
  return response.data
}

export async function updateMedicalRecord(id, recordData) {
  const response = await api.put(`/medicalrecords/${id}`, recordData)
  return response.data
}

export async function getPatientVitals(patientId) {
  const response = await api.get(`/vitals/patient/${patientId}`)
  return response.data
}

export async function recordVitalSign(vitalData) {
  const response = await api.post('/vitals', vitalData)
  return response.data
}

export async function getPatientPrescriptions(patientId) {
  const response = await api.get(`/prescriptions/patient/${patientId}`)
  return response.data
}

export async function createPrescription(prescriptionData) {
  const response = await api.post('/prescriptions', prescriptionData)
  return response.data
}

export async function getPatientLabOrders(patientId) {
  const response = await api.get(`/laborders/patient/${patientId}`)
  return response.data
}

export async function getLabOrderById(id) {
  const response = await api.get(`/laborders/${id}`)
  return response.data
}

export async function createLabOrder(orderData) {
  const response = await api.post('/laborders', orderData)
  return response.data
}

export async function updateLabOrderStatus(orderId, statusData) {
  const response = await api.patch(`/laborders/${orderId}/status`, statusData)
  return response.data
}

export async function recordLabReport(orderId, reportData) {
  const response = await api.post(`/laborders/${orderId}/report`, reportData)
  return response.data
}

export async function getPrescriptionById(id) {
  const response = await api.get(`/prescriptions/${id}`)
  return response.data
}

export async function getPatientTimeline(patientId) {
  const response = await api.get(`/medicalhistory/patient/${patientId}`)
  return response.data
}

export async function searchMedicalRecords(filters = {}) {
  const response = await api.get('/medicalrecords', { params: filters })
  return response.data
}

export async function getMedicalRecordDiagnoses(recordId) {
  const response = await api.get(`/medicalrecords/${recordId}/diagnoses`)
  return response.data
}

export async function addMedicalRecordDiagnosis(recordId, diagnosisData) {
  const response = await api.post(`/medicalrecords/${recordId}/diagnoses`, diagnosisData)
  return response.data
}

export async function getMedicalRecordTreatmentPlans(recordId) {
  const response = await api.get(`/medicalrecords/${recordId}/treatment-plans`)
  return response.data
}

export async function addMedicalRecordTreatmentPlan(recordId, planData) {
  const response = await api.post(`/medicalrecords/${recordId}/treatment-plans`, planData)
  return response.data
}

