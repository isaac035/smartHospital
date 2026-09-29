import { useState, useEffect } from 'react'
import { dischargePatient } from '../../../services/hospitalResourceService'

export default function DischargePatientModal({ isOpen, onClose, admission, onSuccess }) {
  const [dischargeSummary, setDischargeSummary] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen && admission) {
      setDischargeSummary('')
      setError(null)
    }
  }, [isOpen, admission])

  if (!isOpen || !admission) return null

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    if (!dischargeSummary.trim()) {
      setError('Discharge summary is required.')
      return
    }

    try {
      setSubmitting(true)
      await dischargePatient(admission.id, {
        dischargeSummary: dischargeSummary.trim(),
      })
      onSuccess(`Patient discharged successfully for admission ${admission.admissionNumber}.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to discharge patient.')
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
        <div className="flex items-center justify-between pb-4 mb-4 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Discharge Patient
            </h2>
            <p className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
              Finalize stay and release assigned bed for {admission.patientName || `Patient #${admission.patientId}`}.
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

        <div className="p-3 mb-4 rounded-lg text-xs grid grid-cols-2 gap-2" style={{ background: 'color-mix(in srgb, var(--color-accent) 6%, var(--color-primary))' }}>
          <div>
            <span className="font-semibold block opacity-70">Admission:</span>
            <span className="font-mono font-medium">{admission.admissionNumber}</span>
          </div>
          <div>
            <span className="font-semibold block opacity-70">Patient:</span>
            <span className="font-medium">{admission.patientName || `ID: ${admission.patientId}`}</span>
          </div>
          <div>
            <span className="font-semibold block opacity-70">Admission Date:</span>
            <span>{admission.admissionDate ? new Date(admission.admissionDate).toLocaleDateString() : '-'}</span>
          </div>
          <div>
            <span className="font-semibold block opacity-70">Assigned Bed:</span>
            <span>{admission.activeBedNumber ? `${admission.activeWardName || 'Ward'} / Bed ${admission.activeBedNumber}` : 'None'}</span>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <div>
            <label htmlFor="discharge-summary" className="block text-xs font-semibold mb-1">
              Discharge Summary <span className="text-red-500">*</span>
            </label>
            <textarea
              id="discharge-summary"
              required
              maxLength={1000}
              rows={4}
              value={dischargeSummary}
              onChange={(e) => setDischargeSummary(e.target.value)}
              placeholder="Clinical condition at discharge, medications prescribed, follow-up advice (max 1000 characters)"
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            />
          </div>

          <div className="flex justify-end gap-2 mt-4 pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
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
              disabled={submitting || !dischargeSummary.trim()}
              className="primary-button text-sm px-5 py-2"
              style={{
                marginTop: 0,
                backgroundColor: '#b91c1c',
                borderColor: '#b91c1c',
              }}
            >
              {submitting ? 'Processing...' : 'Confirm Discharge'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
