import { useState } from 'react'
import { deactivateMedicalResource } from '../../../services/hospitalResourceService'

export default function DeactivateResourceModal({
  isOpen,
  onClose,
  resource,
  onSuccess,
}) {
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  if (!isOpen || !resource) return null

  const isInUse = resource.status === 'InUse'

  const handleConfirm = async () => {
    if (isInUse) return

    try {
      setSubmitting(true)
      setError(null)
      await deactivateMedicalResource(resource.id)
      onSuccess(`Medical resource '${resource.resourceCode}' has been deactivated.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to deactivate medical resource.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 overflow-y-auto">
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
          <h3 className="text-base font-bold text-red-600 m-0">Deactivate Medical Resource</h3>
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

        <p className="text-xs mb-3">
          Are you sure you want to deactivate{' '}
          <strong className="font-mono" style={{ color: 'var(--color-accent)' }}>
            {resource.resourceCode}
          </strong>{' '}
          ({resource.name})?
        </p>

        {isInUse ? (
          <div className="p-3 mb-4 rounded-lg text-xs bg-red-50 text-red-700 border border-red-200 font-semibold">
            Cannot deactivate an equipment item that is currently In Use. Please unassign or release it from the active clinical procedure first.
          </div>
        ) : (
          <p className="text-xs opacity-70 mb-4">
            This will mark the equipment as inactive. It will no longer be available for assignment to hospital wards, rooms, or beds.
          </p>
        )}

        <div
          className="flex justify-end gap-2 pt-3 border-t"
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
            type="button"
            disabled={submitting || isInUse}
            onClick={handleConfirm}
            className="primary-button text-xs px-4 py-2"
            style={{
              marginTop: 0,
              backgroundColor: '#dc2626',
              borderColor: '#dc2626',
              opacity: isInUse ? 0.5 : 1,
              cursor: isInUse ? 'not-allowed' : 'pointer',
            }}
          >
            {submitting ? 'Deactivating...' : 'Confirm Deactivation'}
          </button>
        </div>
      </div>
    </div>
  )
}
