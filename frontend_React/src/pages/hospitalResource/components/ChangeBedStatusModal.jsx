import { useState, useEffect } from 'react'
import { updateBedStatus } from '../../../services/hospitalResourceService'

const STATUS_OPTIONS = [
  { value: 1, label: 'Available', desc: 'Clean and operational; ready for admission.' },
  { value: 2, label: 'Reserved', desc: 'Held temporarily for a scheduled patient.' },
  { value: 4, label: 'Maintenance', desc: 'Under mechanical/electrical servicing or sanitation.' },
  { value: 5, label: 'Blocked', desc: 'Temporarily out of service for isolation or hazard.' },
]

const STATUS_NAME_TO_INT = {
  Available: 1,
  Reserved: 2,
  Occupied: 3,
  Maintenance: 4,
  Blocked: 5,
}

export default function ChangeBedStatusModal({ isOpen, onClose, bed, onSuccess }) {
  const [status, setStatus] = useState(1)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen && bed) {
      setError(null)
      const currentInt = STATUS_NAME_TO_INT[bed.status] || 1
      // If current is Occupied (shouldn't happen, but fallback to Available)
      setStatus(currentInt === 3 ? 1 : currentInt)
    }
  }, [isOpen, bed])

  if (!isOpen || !bed) return null

  const isOccupied = bed.status?.toLowerCase() === 'occupied'

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (isOccupied) return

    try {
      setSubmitting(true)
      setError(null)
      await updateBedStatus(bed.id, { status: Number(status) })
      const statusLabel = STATUS_OPTIONS.find((s) => s.value === Number(status))?.label || status
      onSuccess(`Status for bed '${bed.bedNumber}' updated to ${statusLabel}.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update bed status.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 overflow-y-auto">
      <div
        className="w-full max-w-md my-8 p-6 rounded-2xl shadow-2xl relative"
        style={{
          background: 'var(--color-primary)',
          border: '1px solid color-mix(in srgb, var(--color-secondary) 18%, var(--color-primary))',
        }}
      >
        <div
          className="flex items-center justify-between pb-3 mb-4 border-b"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}
        >
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Change Bed Status
            </h2>
            <p className="text-xs opacity-70 m-0">
              Update operational or maintenance availability status.
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

        {/* Bed Summary Info */}
        <div
          className="p-3 mb-4 rounded-xl border text-xs"
          style={{
            background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))',
            borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
          }}
        >
          <div className="flex justify-between items-center mb-1">
            <span className="font-semibold">Bed:</span>
            <span className="font-mono font-bold text-sm" style={{ color: 'var(--color-accent)' }}>
              {bed.bedNumber}
            </span>
          </div>
          <div className="flex justify-between items-center mb-1">
            <span className="font-semibold">Location:</span>
            <span>
              {bed.wardName} · Room {bed.roomNumber}
            </span>
          </div>
          <div className="flex justify-between items-center">
            <span className="font-semibold">Current Status:</span>
            <span className="font-semibold uppercase tracking-wider">{bed.status}</span>
          </div>
        </div>

        {isOccupied ? (
          <div className="p-3 mb-4 rounded-lg text-xs bg-amber-50 text-amber-900 border border-amber-200">
            <strong>Bed is currently Occupied:</strong> An occupied bed cannot be manually updated.
            Patient-bed allocation must continue through the Admissions module.
          </div>
        ) : (
          <form onSubmit={handleSubmit} noValidate>
            <div className="mb-4">
              <label htmlFor="target-status-select" className="block text-xs font-semibold mb-1">
                New Bed Status <span className="text-red-500">*</span>
              </label>
              <select
                id="target-status-select"
                value={status}
                onChange={(e) => setStatus(parseInt(e.target.value, 10))}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                {STATUS_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label} — {opt.desc}
                  </option>
                ))}
              </select>
            </div>

            <p className="text-xs opacity-60 mb-4">
              Note: Occupied status cannot be set manually. Patient assignment is managed in Admissions.
            </p>

            <div
              className="flex justify-end gap-2 pt-4 border-t"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}
            >
              <button
                type="button"
                disabled={submitting}
                onClick={onClose}
                className="secondary-button text-xs px-4 py-2"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="primary-button text-xs px-5 py-2"
                style={{ marginTop: 0 }}
              >
                {submitting ? 'Updating...' : 'Update Status'}
              </button>
            </div>
          </form>
        )}

        {isOccupied && (
          <div className="flex justify-end pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
            <button
              type="button"
              onClick={onClose}
              className="secondary-button text-xs px-4 py-2"
            >
              Close
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
