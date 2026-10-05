import { useState } from 'react'
import { cancelScheduledMaintenance } from '../../../services/hospitalResourceService'

export default function CancelMaintenanceModal({
  isOpen,
  onClose,
  record,
  onSuccess,
}) {
  const [reason, setReason] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  if (!isOpen || !record) return null

  const handleCancel = async () => {
    try {
      setSubmitting(true)
      setError(null)
      await cancelScheduledMaintenance(record.id, {
        reason: reason.trim() || undefined,
      })
      onSuccess(`Maintenance '${record.maintenanceCode}' has been cancelled.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to cancel scheduled maintenance.')
    } finally {
      setSubmitting(false)
    }
  }

  const targetDisplayName =
    record.targetType === 'Bed'
      ? record.targetName?.startsWith('Bed')
        ? record.targetName
        : `Bed ${record.bedNumber || record.targetName}`
      : record.targetName

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 overflow-y-auto">
      <div
        className="w-full max-w-md my-8 p-6 rounded-2xl shadow-2xl relative"
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
            <h3 className="text-base font-bold text-red-600 m-0">
              Cancel Scheduled Maintenance
            </h3>
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

        <p className="text-xs mb-3">
          Are you sure you want to cancel scheduled maintenance for{' '}
          <strong style={{ color: 'var(--color-accent)' }}>
            {targetDisplayName}
          </strong>?
        </p>

        {/* Target Info */}
        <div
          className="p-3 mb-4 rounded-xl border text-xs"
          style={{
            background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
            borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
          }}
        >
          <div className="grid grid-cols-2 gap-2">
            <div>
              <span className="opacity-60 block">Type:</span>
              <span className="font-semibold">{record.type}</span>
            </div>
            <div>
              <span className="opacity-60 block">Status:</span>
              <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-amber-100 text-amber-800">
                Scheduled
              </span>
            </div>
          </div>
        </div>

        {/* Optional Reason */}
        <div className="mb-4">
          <label htmlFor="cancel-mnt-reason" className="block text-xs font-semibold mb-1">
            Cancellation Reason (Optional)
          </label>
          <textarea
            id="cancel-mnt-reason"
            rows={2}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="e.g. Schedule postponed, asset currently needed for urgent care..."
            maxLength={500}
            className="w-full px-3 py-2 text-xs border rounded-lg outline-none resize-none"
            style={{
              borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
            }}
          />
        </div>

        <p className="text-[11px] opacity-70 mb-4">
          This record will be marked as Cancelled and preserved in the maintenance history. The target asset will remain operational.
        </p>

        {/* Action Buttons */}
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
            Keep Scheduled
          </button>
          <button
            type="button"
            disabled={submitting}
            onClick={handleCancel}
            className="primary-button text-xs px-4 py-2"
            style={{
              marginTop: 0,
              backgroundColor: '#dc2626',
              borderColor: '#dc2626',
            }}
          >
            {submitting ? 'Cancelling...' : 'Cancel Maintenance'}
          </button>
        </div>
      </div>
    </div>
  )
}
