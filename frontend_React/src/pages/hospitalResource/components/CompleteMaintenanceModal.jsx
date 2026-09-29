import { useState, useEffect } from 'react'
import { completeMaintenance } from '../../../services/hospitalResourceService'

export default function CompleteMaintenanceModal({
  isOpen,
  onClose,
  record,
  onSuccess,
}) {
  const [resolutionNotes, setResolutionNotes] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen) {
      setError(null)
      setResolutionNotes('')
    }
  }, [isOpen])

  if (!isOpen || !record) return null

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    if (!resolutionNotes.trim()) {
      setError('Resolution notes are required.')
      return
    }

    try {
      setSubmitting(true)
      await completeMaintenance(record.id, {
        resolutionNotes: resolutionNotes.trim(),
      })
      onSuccess(`Maintenance '${record.maintenanceCode}' marked as Completed. Target restored to Available.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to complete maintenance task.')
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
          className="flex items-center justify-between pb-3 mb-4 border-b"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}
        >
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Complete Maintenance
            </h2>
            <p className="text-xs opacity-70 m-0 font-mono">
              {record.maintenanceCode}
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

        {/* Target Info Summary */}
        <div
          className="p-3 mb-4 rounded-xl border text-xs"
          style={{
            background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
            borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
          }}
        >
          <div className="grid grid-cols-2 gap-2">
            <div>
              <span className="opacity-60 block">Target:</span>
              <strong className="font-semibold" style={{ color: 'var(--color-accent)' }}>
                {record.targetName}
              </strong>
            </div>
            <div>
              <span className="opacity-60 block">Type:</span>
              <span className="font-medium">{record.type}</span>
            </div>
          </div>
          <div className="mt-2 text-emerald-700 bg-emerald-50 p-2 rounded border border-emerald-200">
            ✓ Completing this task will automatically restore this {record.targetType === 'Bed' ? 'bed' : 'equipment'} status back to <strong>Available</strong>.
          </div>
        </div>

        <form onSubmit={handleSubmit}>
          {/* Resolution Notes */}
          <div className="mb-4">
            <label htmlFor="resolution-notes-input" className="block text-xs font-semibold mb-1">
              Resolution Notes <span className="text-red-500">*</span>
            </label>
            <textarea
              id="resolution-notes-input"
              rows={4}
              value={resolutionNotes}
              onChange={(e) => setResolutionNotes(e.target.value)}
              maxLength={1000}
              placeholder="Detail all repairs completed, components sanitized or replaced, calibration outcomes, and confirmation of operational safety..."
              className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
              required
            />
            <div className="text-right text-xs opacity-50 mt-0.5">
              {resolutionNotes.length}/1000
            </div>
          </div>

          {/* Action Buttons */}
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
              {submitting ? 'Completing...' : '✓ Complete Maintenance'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
