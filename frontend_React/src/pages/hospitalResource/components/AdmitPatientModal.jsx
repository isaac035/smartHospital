import { useState } from 'react'
import { createAdmission } from '../../../services/hospitalResourceService'

export default function AdmitPatientModal({ isOpen, onClose, onSuccess }) {
  const [formData, setFormData] = useState({
    patientId: '',
    admittingDoctorId: '',
    priority: 1, // 1: Normal, 2: Urgent, 3: Emergency
    reasonForAdmission: '',
    diagnosis: '',
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  if (!isOpen) return null

  const handleChange = (e) => {
    const { name, value } = e.target
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'priority' ? parseInt(value, 10) : value,
    }))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    const parsedPatientId = parseInt(formData.patientId, 10)
    if (isNaN(parsedPatientId) || parsedPatientId <= 0) {
      setError('Please provide a valid numeric Patient ID.')
      return
    }

    let parsedDoctorId = null
    if (formData.admittingDoctorId && formData.admittingDoctorId.trim() !== '') {
      parsedDoctorId = parseInt(formData.admittingDoctorId, 10)
      if (isNaN(parsedDoctorId) || parsedDoctorId <= 0) {
        setError('Please provide a valid numeric Doctor ID or leave it empty.')
        return
      }
    }

    if (!formData.reasonForAdmission.trim()) {
      setError('Reason for admission is required.')
      return
    }

    const payload = {
      patientId: parsedPatientId,
      admittingDoctorId: parsedDoctorId,
      priority: formData.priority,
      reasonForAdmission: formData.reasonForAdmission.trim(),
      diagnosis: formData.diagnosis.trim() || null,
    }

    try {
      setLoading(true)
      await createAdmission(payload)
      onSuccess('Patient admission created successfully.')
      onClose()
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to create admission. Verify the Patient ID and Doctor ID.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 overflow-y-auto">
      <div
        className="w-full max-w-lg my-8 p-6 rounded-2xl shadow-2xl relative"
        style={{
          background: 'var(--color-primary)',
          border: '1px solid color-mix(in srgb, var(--color-secondary) 18%, var(--color-primary))',
        }}
      >
        <div className="flex items-center justify-between pb-4 mb-4 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Admit Patient
            </h2>
            <p className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
              Register a new patient admission and initial diagnosis.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-2xl leading-none font-bold"
            aria-label="Close"
          >
            &times;
          </button>
        </div>

        {error && <div className="form-error mb-4">{error}</div>}

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label htmlFor="admit-patientId" className="block text-xs font-semibold mb-1">
                Patient ID <span className="text-red-500">*</span>
              </label>
              <input
                id="admit-patientId"
                name="patientId"
                type="number"
                min="1"
                required
                value={formData.patientId}
                onChange={handleChange}
                placeholder="e.g. 5"
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
                style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
              />
            </div>

            <div>
              <label htmlFor="admit-doctorId" className="block text-xs font-semibold mb-1">
                Admitting Doctor ID <span className="text-xs font-normal opacity-70">(optional)</span>
              </label>
              <input
                id="admit-doctorId"
                name="admittingDoctorId"
                type="number"
                min="1"
                value={formData.admittingDoctorId}
                onChange={handleChange}
                placeholder="e.g. 3"
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
                style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
              />
            </div>
          </div>

          <div>
            <label htmlFor="admit-priority" className="block text-xs font-semibold mb-1">
              Priority <span className="text-red-500">*</span>
            </label>
            <select
              id="admit-priority"
              name="priority"
              value={formData.priority}
              onChange={handleChange}
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value={1}>Normal</option>
              <option value={2}>Urgent</option>
              <option value={3}>Emergency</option>
            </select>
          </div>

          <div>
            <label htmlFor="admit-reason" className="block text-xs font-semibold mb-1">
              Reason for Admission <span className="text-red-500">*</span>
            </label>
            <textarea
              id="admit-reason"
              name="reasonForAdmission"
              required
              maxLength={500}
              rows={3}
              value={formData.reasonForAdmission}
              onChange={handleChange}
              placeholder="Primary symptoms or reason for inpatient stay (max 500 characters)"
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            />
          </div>

          <div>
            <label htmlFor="admit-diagnosis" className="block text-xs font-semibold mb-1">
              Initial Diagnosis <span className="text-xs font-normal opacity-70">(optional)</span>
            </label>
            <textarea
              id="admit-diagnosis"
              name="diagnosis"
              maxLength={1000}
              rows={2}
              value={formData.diagnosis}
              onChange={handleChange}
              placeholder="Clinical diagnosis notes (max 1000 characters)"
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            />
          </div>

          <div className="flex justify-end gap-2 mt-4 pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
            <button
              type="button"
              disabled={loading}
              onClick={onClose}
              className="secondary-button text-sm px-4 py-2"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="primary-button text-sm px-5 py-2"
              style={{ marginTop: 0 }}
            >
              {loading ? 'Submitting...' : 'Admit Patient'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
