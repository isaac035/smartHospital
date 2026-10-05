import { useState } from 'react'
import { deactivateWard } from '../../../services/hospitalResourceService'

export default function DeactivateWardModal({ isOpen, onClose, ward, onSuccess }) {
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  if (!isOpen || !ward) return null

  const handleDeactivate = async () => {
    try {
      setSubmitting(true)
      setError(null)
      await deactivateWard(ward.id)
      onSuccess(`Ward '${ward.name}' has been deactivated.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to deactivate ward.')
    } finally {
      setSubmitting(false)
    }
  }

  const hasOccupiedBeds = (ward.occupiedBeds || 0) > 0

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 overflow-y-auto">
      <div
        className="w-full max-w-md my-8 p-6 rounded-2xl shadow-2xl relative"
        style={{
          background: 'var(--color-primary)',
          border: '1px solid color-mix(in srgb, var(--color-secondary) 18%, var(--color-primary))',
        }}
      >
        <div className="flex items-center justify-between pb-3 mb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <h2 className="text-xl font-bold text-red-600">
            Deactivate Ward
          </h2>
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

        <p className="text-sm mb-3">
          Are you sure you want to deactivate <strong style={{ color: 'var(--color-accent)' }}>{ward.name}</strong> ({ward.code})?
        </p>

        {hasOccupiedBeds ? (
          <div className="p-3 mb-4 rounded-lg text-xs bg-amber-50 text-amber-900 border border-amber-200">
            <strong>Warning:</strong> This ward currently has <strong>{ward.occupiedBeds} occupied bed(s)</strong>.
            The hospital policy requires transferring or discharging all patients before deactivating a ward.
          </div>
        ) : (
          <div className="p-3 mb-4 rounded-lg text-xs bg-slate-50 text-slate-700 border border-slate-200">
            This will perform a soft deactivation. Rooms and beds in this ward will also become inactive. You can reactivate this ward later by editing it.
          </div>
        )}

        <div className="flex justify-end gap-2 pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <button
            type="button"
            disabled={submitting}
            onClick={onClose}
            className="secondary-button text-sm px-4 py-2"
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={submitting || hasOccupiedBeds}
            onClick={handleDeactivate}
            className="primary-button text-sm px-5 py-2"
            style={{
              marginTop: 0,
              backgroundColor: '#dc2626',
              borderColor: '#dc2626',
            }}
          >
            {submitting ? 'Deactivating...' : 'Confirm Deactivation'}
          </button>
        </div>
      </div>
    </div>
  )
}
