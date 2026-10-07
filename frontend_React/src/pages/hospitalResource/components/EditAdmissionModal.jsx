import { useState, useEffect } from 'react'
import { updateAdmission } from '../../../services/hospitalResourceService'
import { useFormValidation } from '../../../hooks/useFormValidation'
import FieldError from '../../../components/common/FieldError'
import { number, selection, text } from '../../../utils/validators'

export default function EditAdmissionModal({ isOpen, onClose, admission, onSuccess }) {
  const [formData, setFormData] = useState({
    priority: 1, // 1: Normal, 2: Urgent, 3: Emergency
    reasonForAdmission: '',
    diagnosis: '',
    admittingDoctorId: '',
  })
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const formValues = formData
  const validation = useFormValidation({
    priority: (value) => selection(value, 'a priority'),
    admittingDoctorId: (value) => number(value, 'Admitting doctor ID', { min: 1, max: 2147483647, integer: true, message: 'Admitting Doctor ID must be a valid positive integer.' }),
    reasonForAdmission: (value) => text(value, 'Reason for admission', { isRequired: true, max: 500 }),
    diagnosis: (value) => text(value, 'Diagnosis', { max: 1000 }),
  }, formValues)
  const resetValidation = validation.reset

  const mapPriorityToNumber = (priority) => {
    if (typeof priority === 'number') return priority
    const p = String(priority || '').toLowerCase()
    if (p === 'urgent') return 2
    if (p === 'emergency') return 3
    return 1 // default Normal
  }

  useEffect(() => {
    if (isOpen && admission) {
      setFormData({
        priority: mapPriorityToNumber(admission.priority),
        reasonForAdmission: admission.reasonForAdmission || '',
        diagnosis: admission.diagnosis || '',
        admittingDoctorId:
          admission.admittingDoctorId != null ? String(admission.admittingDoctorId) : '',
      })
      setError(null)
      resetValidation()
    }
  }, [isOpen, admission, resetValidation])

  if (!isOpen || !admission) return null

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

    if (!validation.validateAll(formValues, e.currentTarget)) return
    const trimmedReason = formData.reasonForAdmission.trim()
    const trimmedDiagnosis = formData.diagnosis ? formData.diagnosis.trim() : ''

    let doctorIdToSend = null
    if (formData.admittingDoctorId && formData.admittingDoctorId.trim() !== '') {
      const parsedId = parseInt(formData.admittingDoctorId, 10)
      if (isNaN(parsedId) || parsedId <= 0) {
        setError('Admitting Doctor ID must be a valid positive integer.')
        return
      }
      doctorIdToSend = parsedId
    } else if (admission.admittingDoctorId) {
      // Backend does not support clearing assigned doctor; retain existing doctor ID
      doctorIdToSend = admission.admittingDoctorId
    }

    const payload = {
      priority: formData.priority,
      reasonForAdmission: trimmedReason,
      diagnosis: trimmedDiagnosis || null,
      admittingDoctorId: doctorIdToSend,
    }

    try {
      setSubmitting(true)
      await updateAdmission(admission.id, payload)
      onSuccess(`Admission ${admission.admissionNumber} updated successfully.`)
      onClose()
    } catch (err) {
      const msg = validation.applyServerErrors(err, { conflicts: [{ match: /doctor/i, field: 'admittingDoctorId' }], fallback: 'Failed to update admission.' })
      setError(msg || null)
    } finally {
      setSubmitting(false)
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
        {/* Header */}
        <div
          className="flex items-center justify-between pb-4 mb-4 border-b"
          style={{
            borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
          }}
        >
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Edit Admission
            </h2>
            <p
              className="text-xs mt-1"
              style={{
                color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))',
              }}
            >
              Update clinical details and doctor assignment for active inpatient stay.
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

        {/* Backend / Form Error Banner */}
        {error && <div className="form-error mb-4">{error}</div>}

        {/* Read-Only Context Information Card */}
        <div
          className="p-3 mb-4 rounded-xl text-xs flex flex-col gap-2"
          style={{
            background: 'color-mix(in srgb, var(--color-accent) 5%, var(--color-primary))',
            border: '1px solid color-mix(in srgb, var(--color-accent) 15%, var(--color-primary))',
          }}
        >
          <div className="flex items-center justify-between font-semibold">
            <span style={{ color: 'var(--color-accent)' }}>Stay Context (Read-Only)</span>
            <span className="inline-block px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-emerald-100 text-emerald-800 border border-emerald-200">
              {admission.status || 'Admitted'}
            </span>
          </div>

          <div className="grid grid-cols-2 gap-x-4 gap-y-2 pt-1 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-accent) 10%, var(--color-primary))' }}>
            <div>
              <span className="opacity-70 block">Admission Number:</span>
              <span className="font-mono font-semibold">{admission.admissionNumber}</span>
            </div>
            <div>
              <span className="opacity-70 block">Patient:</span>
              <span className="font-semibold">{admission.patientName || `Patient #${admission.patientId}`}</span>
            </div>
            <div>
              <span className="opacity-70 block">Ward & Room:</span>
              <span>
                {admission.activeWardName || 'No ward assigned'}
                {admission.activeRoomNumber ? ` (Room ${admission.activeRoomNumber})` : ''}
              </span>
            </div>
            <div>
              <span className="opacity-70 block">Current Bed:</span>
              <span>
                {admission.activeBedNumber ? `Bed ${admission.activeBedNumber}` : 'No bed allocated'}
              </span>
            </div>
          </div>
          <p className="text-[11px] opacity-60 m-0 italic">
            Patient ID, Admission No, Bed allocation, and Status are managed via allocation, transfer, and discharge workflows.
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-3">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {/* Priority */}
            <div>
              <label htmlFor="edit-priority" className="block text-xs font-semibold mb-1">
                Priority <span className="text-red-500">*</span>
              </label>
              <select
                id="edit-priority"
                onBlur={() => validation.touch('priority', formValues)}
                {...validation.fieldProps('priority')}
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
              <FieldError name="priority" message={validation.errorFor('priority')} />
            </div>

            {/* Admitting Doctor ID */}
            <div>
              <label htmlFor="edit-doctorId" className="block text-xs font-semibold mb-1">
                Admitting Doctor ID <span className="text-xs font-normal opacity-70">(optional)</span>
              </label>
              <input
                id="edit-doctorId"
                onBlur={() => validation.touch('admittingDoctorId', formValues)}
                {...validation.fieldProps('admittingDoctorId')}
                name="admittingDoctorId"
                type="number"
                min="1"
                step="1"
                value={formData.admittingDoctorId}
                onChange={handleChange}
                placeholder="e.g. 3"
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
              />
              <FieldError name="admittingDoctorId" message={validation.errorFor('admittingDoctorId')} />
              {admission.admittingDoctorName && (
                <span className="text-[11px] opacity-60 block mt-0.5 truncate">
                  Current: {admission.admittingDoctorName} (ID: {admission.admittingDoctorId})
                </span>
              )}
            </div>
          </div>

          {/* Reason for Admission */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label htmlFor="edit-reason" className="block text-xs font-semibold">
                Reason for Admission <span className="text-red-500">*</span>
              </label>
              <span className="text-[11px] opacity-60">
                {formData.reasonForAdmission.length}/500
              </span>
            </div>
            <textarea
              id="edit-reason"
                onBlur={() => validation.touch('reasonForAdmission', formValues)}
                {...validation.fieldProps('reasonForAdmission')}
              name="reasonForAdmission"
              required
              maxLength={500}
              rows={3}
              value={formData.reasonForAdmission}
              onChange={handleChange}
              placeholder="Primary symptoms or reason for inpatient stay (max 500 characters)"
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
            />
              <FieldError name="reasonForAdmission" message={validation.errorFor('reasonForAdmission')} />
          </div>

          {/* Diagnosis */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label htmlFor="edit-diagnosis" className="block text-xs font-semibold">
                Diagnosis <span className="text-xs font-normal opacity-70">(optional)</span>
              </label>
              <span className="text-[11px] opacity-60">
                {formData.diagnosis.length}/1000
              </span>
            </div>
            <textarea
              id="edit-diagnosis"
                onBlur={() => validation.touch('diagnosis', formValues)}
                {...validation.fieldProps('diagnosis')}
              name="diagnosis"
              maxLength={1000}
              rows={3}
              value={formData.diagnosis}
              onChange={handleChange}
              placeholder="Clinical diagnosis notes (max 1000 characters)"
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
            />
              <FieldError name="diagnosis" message={validation.errorFor('diagnosis')} />
          </div>

          {/* Actions */}
          <div
            className="flex justify-end gap-2 mt-3 pt-3 border-t"
            style={{
              borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
            }}
          >
            <button
              type="button"
              disabled={submitting}
              onClick={onClose}
              className="secondary-button text-sm px-4 py-2"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="primary-button text-sm px-5 py-2"
              style={{ marginTop: 0 }}
            >
              {submitting ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
