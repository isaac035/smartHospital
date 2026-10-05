import { useState, useEffect } from 'react'
import { updateScheduledMaintenance } from '../../../services/hospitalResourceService'
import { MAINTENANCE_TYPES } from './ScheduleMaintenanceModal'

const TYPE_NAME_TO_INT = {
  RoutineInspection: 1,
  CleaningAndSanitization: 2,
  Repair: 3,
  Calibration: 4,
  EmergencyRepair: 5,
}

const toDatetimeLocal = (isoStr) => {
  if (!isoStr) return ''
  const d = new Date(isoStr)
  if (isNaN(d.getTime())) return ''
  const pad = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export default function EditScheduledMaintenanceModal({
  isOpen,
  onClose,
  record,
  onSuccess,
}) {
  const [type, setType] = useState(1)
  const [description, setDescription] = useState('')
  const [scheduledStart, setScheduledStart] = useState('')
  const [scheduledEnd, setScheduledEnd] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen && record) {
      setError(null)
      const mappedType =
        typeof record.type === 'number'
          ? record.type
          : TYPE_NAME_TO_INT[record.type] || 1
      setType(mappedType)
      setDescription(record.description || '')
      setScheduledStart(toDatetimeLocal(record.scheduledStart))
      setScheduledEnd(toDatetimeLocal(record.scheduledEnd))
    }
  }, [isOpen, record])

  if (!isOpen || !record) return null

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    if (!description.trim()) {
      setError('Description is required.')
      return
    }

    if (!scheduledStart) {
      setError('Scheduled start date and time is required.')
      return
    }

    if (scheduledEnd && new Date(scheduledEnd) <= new Date(scheduledStart)) {
      setError('Scheduled end time must be after scheduled start time.')
      return
    }

    try {
      setSubmitting(true)
      const payload = {
        type: Number(type),
        description: description.trim(),
        scheduledStart: new Date(scheduledStart).toISOString(),
        scheduledEnd: scheduledEnd ? new Date(scheduledEnd).toISOString() : null,
      }

      await updateScheduledMaintenance(record.id, payload)
      onSuccess(`Maintenance '${record.maintenanceCode}' updated successfully.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update scheduled maintenance.')
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

  const locationInfo = [
    record.wardName,
    record.roomNumber ? `Room ${record.roomNumber}` : null,
    record.locationDescription,
  ]
    .filter(Boolean)
    .join(' • ')

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
              Edit Scheduled Maintenance
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

        {/* Read-Only Target Summary */}
        <div
          className="p-3 mb-4 rounded-xl border text-xs"
          style={{
            background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
            borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
          }}
        >
          <div className="grid grid-cols-2 gap-2">
            <div>
              <span className="opacity-60 block">Target Asset:</span>
              <strong className="font-semibold" style={{ color: 'var(--color-accent)' }}>
                {targetDisplayName}
              </strong>
              {locationInfo && (
                <span className="block opacity-75 mt-0.5">{locationInfo}</span>
              )}
            </div>
            <div>
              <span className="opacity-60 block">Asset Type:</span>
              <span className="inline-block mt-0.5 px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700 border border-slate-200">
                {record.targetType === 'Bed' ? 'Hospital Bed' : 'Medical Equipment'}
              </span>
              <span className="block text-[11px] opacity-60 mt-1 italic">
                Target is read-only during edit
              </span>
            </div>
          </div>
        </div>

        {/* Edit Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Maintenance Type */}
          <div>
            <label htmlFor="edit-mnt-type" className="block text-xs font-semibold mb-1">
              Maintenance Type *
            </label>
            <select
              id="edit-mnt-type"
              value={type}
              onChange={(e) => setType(Number(e.target.value))}
              required
              className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              {MAINTENANCE_TYPES.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </select>
          </div>

          {/* Scheduled Dates */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label htmlFor="edit-mnt-start" className="block text-xs font-semibold mb-1">
                Scheduled Start *
              </label>
              <input
                id="edit-mnt-start"
                type="datetime-local"
                value={scheduledStart}
                onChange={(e) => setScheduledStart(e.target.value)}
                required
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
              />
            </div>

            <div>
              <label htmlFor="edit-mnt-end" className="block text-xs font-semibold mb-1">
                Estimated End (Optional)
              </label>
              <input
                id="edit-mnt-end"
                type="datetime-local"
                value={scheduledEnd}
                onChange={(e) => setScheduledEnd(e.target.value)}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
              />
            </div>
          </div>

          {/* Description */}
          <div>
            <label htmlFor="edit-mnt-description" className="block text-xs font-semibold mb-1">
              Reason / Work Scope Description *
            </label>
            <textarea
              id="edit-mnt-description"
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe the maintenance requirements or inspection scope..."
              required
              maxLength={500}
              className="w-full px-3 py-2 text-xs border rounded-lg outline-none resize-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
            />
            <span className="text-[11px] opacity-50 block text-right">
              {description.length}/500
            </span>
          </div>

          {/* Actions */}
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
              type="submit"
              disabled={submitting}
              className="primary-button text-xs px-4 py-2"
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
