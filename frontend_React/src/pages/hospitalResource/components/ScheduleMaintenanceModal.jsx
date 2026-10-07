import { useState, useEffect } from 'react'
import {
  scheduleMaintenance,
  getAllWards,
  getRoomsByWard,
  getBeds,
  getMedicalResources,
} from '../../../services/hospitalResourceService'
import { useFormValidation } from '../../../hooks/useFormValidation'
import FieldError from '../../../components/common/FieldError'
import { required, selection, text } from '../../../utils/validators'

export const MAINTENANCE_TYPES = [
  { value: 1, label: 'Routine Inspection' },
  { value: 2, label: 'Cleaning & Sanitization' },
  { value: 3, label: 'Repair' },
  { value: 4, label: 'Calibration' },
  { value: 5, label: 'Emergency Repair' },
]

export default function ScheduleMaintenanceModal({
  isOpen,
  onClose,
  onSuccess,
}) {
  const [targetType, setTargetType] = useState('Bed')

  // Bed cascading selection
  const [wards, setWards] = useState([])
  const [wardId, setWardId] = useState('')
  const [rooms, setRooms] = useState([])
  const [roomId, setRoomId] = useState('')
  const [beds, setBeds] = useState([])
  const [bedId, setBedId] = useState('')
  const [loadingRooms, setLoadingRooms] = useState(false)
  const [loadingBeds, setLoadingBeds] = useState(false)

  // Medical Resource selection
  const [resources, setResources] = useState([])
  const [medicalResourceId, setMedicalResourceId] = useState('')
  const [loadingResources, setLoadingResources] = useState(false)

  // Maintenance details
  const [type, setType] = useState(1)
  const [description, setDescription] = useState('')
  const [scheduledStart, setScheduledStart] = useState('')
  const [scheduledEnd, setScheduledEnd] = useState('')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const formValues = { targetType, bedId, medicalResourceId, type, description, scheduledStart, scheduledEnd }
  const validation = useFormValidation({
    bedId: (value, values) => (values.targetType === 'Bed' && !value ? 'Please select a specific bed for maintenance.' : undefined),
    medicalResourceId: (value, values) => (values.targetType === 'MedicalResource' && !value ? 'Please select a medical resource for maintenance.' : undefined),
    type: (value) => selection(value, 'a maintenance type'),
    scheduledStart: (value) => {
      const missing = required(value, 'Scheduled start date and time')
      if (missing) return missing
      const now = new Date()
      const currentMinute = new Date(now.getFullYear(), now.getMonth(), now.getDate(), now.getHours(), now.getMinutes()).getTime()
      const start = new Date(value).getTime()
      return Number.isNaN(start) || start < currentMinute ? 'Scheduled start cannot be earlier than the current date and time.' : undefined
    },
    scheduledEnd: (value, values) => (value && values.scheduledStart && new Date(value) <= new Date(values.scheduledStart) ? 'Scheduled end time must be after scheduled start time.' : undefined),
    description: (value) => text(value, 'Description', { isRequired: true, max: 500 }),
  }, formValues)
  const resetValidation = validation.reset
  const [minStartDateTime, setMinStartDateTime] = useState('')

  // Format Date object to local YYYY-MM-DDTHH:mm string for datetime-local input
  const formatToLocalDatetimeString = (date = new Date()) => {
    const pad = (n) => String(n).padStart(2, '0')
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
  }

  // Helper to get local date-time string for input default (10 minutes in future)
  const getDefaultDateTime = () => {
    const d = new Date()
    d.setMinutes(d.getMinutes() + 10)
    return formatToLocalDatetimeString(d)
  }

  useEffect(() => {
    if (isOpen) {
      setError(null)
      resetValidation()
      setTargetType('Bed')
      setWardId('')
      setRoomId('')
      setBedId('')
      setRooms([])
      setBeds([])
      setMedicalResourceId('')
      setType(1)
      setDescription('')
      const now = new Date()
      setMinStartDateTime(formatToLocalDatetimeString(now))
      setScheduledStart(getDefaultDateTime())
      setScheduledEnd('')

      // Fetch wards
      getAllWards()
        .then((w) => setWards(Array.isArray(w) ? w : []))
        .catch(() => setWards([]))

      // Fetch medical resources
      setLoadingResources(true)
      getMedicalResources({ pageSize: 100 })
        .then((r) => setResources(Array.isArray(r) ? r : []))
        .catch(() => setResources([]))
        .finally(() => setLoadingResources(false))
    }
  }, [isOpen, resetValidation])

  // Ward change
  const handleWardChange = async (e) => {
    const selectedWardId = e.target.value
    setWardId(selectedWardId)
    setRoomId('')
    setBedId('')
    setRooms([])
    setBeds([])

    if (!selectedWardId) return

    try {
      setLoadingRooms(true)
      const data = await getRoomsByWard(selectedWardId)
      setRooms(Array.isArray(data) ? data : [])
    } catch {
      setRooms([])
    } finally {
      setLoadingRooms(false)
    }
  }

  // Room change
  const handleRoomChange = async (e) => {
    const selectedRoomId = e.target.value
    setRoomId(selectedRoomId)
    setBedId('')
    setBeds([])

    if (!selectedRoomId) return

    try {
      setLoadingBeds(true)
      const data = await getBeds({ roomId: parseInt(selectedRoomId, 10), pageSize: 100 })
      setBeds(Array.isArray(data) ? data : [])
    } catch {
      setBeds([])
    } finally {
      setLoadingBeds(false)
    }
  }

  if (!isOpen) return null

  // Check selected target warnings
  const selectedBed = beds.find((b) => String(b.id) === String(bedId))
  const selectedResource = resources.find((r) => String(r.id) === String(medicalResourceId))

  const isBedOccupied = selectedBed?.status === 'Occupied'
  const isResourceInUse = selectedResource?.status === 'InUse'

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    if (!validation.validateAll(formValues, e.currentTarget)) return

    try {
      setSubmitting(true)
      const payload = {
        bedId: targetType === 'Bed' ? Number(bedId) : null,
        medicalResourceId: targetType === 'MedicalResource' ? Number(medicalResourceId) : null,
        type: Number(type),
        description: description.trim(),
        scheduledStart: new Date(scheduledStart).toISOString(),
        scheduledEnd: scheduledEnd ? new Date(scheduledEnd).toISOString() : null,
      }

      const result = await scheduleMaintenance(payload)
      onSuccess(`Maintenance task '${result.maintenanceCode}' scheduled successfully.`)
      onClose()
    } catch (err) {
      setError(validation.applyServerErrors(err, { conflicts: [{ match: /end time/i, field: 'scheduledEnd' }, { match: /bed/i, field: 'bedId' }, { match: /resource|equipment/i, field: 'medicalResourceId' }], fallback: 'Failed to schedule maintenance task.' }) || null)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 overflow-y-auto">
      <div
        className="w-full max-w-xl my-8 p-6 rounded-2xl shadow-2xl relative"
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
              Schedule Resource Maintenance
            </h2>
            <p className="text-xs opacity-70 m-0">
              Create a scheduled maintenance task for a bed or medical equipment.
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

        <form onSubmit={handleSubmit} noValidate>
          {/* Target Type Selector */}
          <div className="mb-4">
            <label className="block text-xs font-semibold mb-1.5">
              Target Resource Type <span className="text-red-500">*</span>
            </label>
            <div className="flex gap-4">
              <label className="flex items-center gap-2 cursor-pointer text-xs">
                <input
                  type="radio"
                  name="targetType"
                  value="Bed"
                  checked={targetType === 'Bed'}
                  onChange={() => {
                    setTargetType('Bed')
                    setMedicalResourceId('')
                  }}
                  className="accent-current"
                />
                <span className="font-semibold">Hospital Bed</span>
              </label>

              <label className="flex items-center gap-2 cursor-pointer text-xs">
                <input
                  type="radio"
                  name="targetType"
                  value="MedicalResource"
                  checked={targetType === 'MedicalResource'}
                  onChange={() => {
                    setTargetType('MedicalResource')
                    setWardId('')
                    setRoomId('')
                    setBedId('')
                  }}
                  className="accent-current"
                />
                <span className="font-semibold">Medical Equipment / Resource</span>
              </label>
            </div>
          </div>

          {/* Conditional Target Selection: Bed */}
          {targetType === 'Bed' && (
            <div
              className="p-3 mb-4 rounded-xl border text-xs"
              style={{
                background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
                borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
              }}
            >
              <span className="block font-semibold mb-2" style={{ color: 'var(--color-accent)' }}>
                Select Bed Location <span className="text-red-500">*</span>
              </span>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                <div>
                  <label className="block text-xs font-medium mb-1">Ward</label>
                  <select
                    value={wardId}
                    onChange={handleWardChange}
                    className="w-full px-2.5 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
                    style={{
                      borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                    }}
                  >
                    <option value="">-- Select Ward --</option>
                    {wards.map((w) => (
                      <option key={w.id} value={w.id}>
                        {w.name} ({w.code})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-medium mb-1">Room</label>
                  <select
                    value={roomId}
                    onChange={handleRoomChange}
                    disabled={!wardId || loadingRooms}
                    className="w-full px-2.5 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
                    style={{
                      borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                      opacity: !wardId || loadingRooms ? 0.6 : 1,
                      cursor: !wardId || loadingRooms ? 'not-allowed' : 'pointer',
                    }}
                  >
                    <option value="">
                      {loadingRooms
                        ? 'Loading rooms...'
                        : !wardId
                        ? '-- Select ward first --'
                        : rooms.length === 0
                        ? '-- No rooms --'
                        : '-- Select Room --'}
                    </option>
                    {rooms.map((r) => (
                      <option key={r.id} value={r.id}>
                        Room {r.roomNumber} ({r.type})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-medium mb-1">Bed</label>
                  <select
                    value={bedId}
                name="bedId"
                onBlur={() => validation.touch('bedId', formValues)}
                {...validation.fieldProps('bedId')}
                    onChange={(e) => setBedId(e.target.value)}
                    disabled={!roomId || loadingBeds}
                    className="w-full px-2.5 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
                    style={{
                      borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                      opacity: !roomId || loadingBeds ? 0.6 : 1,
                      cursor: !roomId || loadingBeds ? 'not-allowed' : 'pointer',
                    }}
                  >
                    <option value="">
                      {loadingBeds
                        ? 'Loading beds...'
                        : !roomId
                        ? '-- Select room first --'
                        : beds.length === 0
                        ? '-- No beds --'
                        : '-- Select Bed --'}
                    </option>
                    {beds.map((b) => (
                      <option key={b.id} value={b.id}>
                        {b.bedNumber} ({b.status})
                      </option>
                    ))}
                  </select>
              <FieldError name="bedId" message={validation.errorFor('bedId')} />
                </div>
              </div>

              {isBedOccupied && (
                <p className="text-xs text-amber-700 mt-2 font-medium">
                  Note: This bed is currently occupied. Maintenance can be scheduled in advance, but the bed must be discharged/vacated before starting maintenance.
                </p>
              )}
            </div>
          )}

          {/* Conditional Target Selection: Medical Resource */}
          {targetType === 'MedicalResource' && (
            <div
              className="p-3 mb-4 rounded-xl border text-xs"
              style={{
                background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
                borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
              }}
            >
              <label htmlFor="select-medical-resource" className="block font-semibold mb-1" style={{ color: 'var(--color-accent)' }}>
                Select Medical Resource <span className="text-red-500">*</span>
              </label>
              <select
                id="select-medical-resource"
                name="medicalResourceId"
                onBlur={() => validation.touch('medicalResourceId', formValues)}
                {...validation.fieldProps('medicalResourceId')}
                value={medicalResourceId}
                onChange={(e) => setMedicalResourceId(e.target.value)}
                disabled={loadingResources}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
              >
                <option value="">
                  {loadingResources ? 'Loading resources...' : '-- Select Equipment / Device --'}
                </option>
                {resources.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.resourceCode} — {r.name} ({r.category}) [{r.status}]
                  </option>
                ))}
              </select>
              <FieldError name="medicalResourceId" message={validation.errorFor('medicalResourceId')} />

              {isResourceInUse && (
                <p className="text-xs text-amber-700 mt-2 font-medium">
                  Note: This equipment is currently in use. Maintenance can be scheduled in advance, but it cannot be started until released from patient care.
                </p>
              )}
            </div>
          )}

          {/* Maintenance Type & Dates */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-3">
            <div>
              <label htmlFor="maintenance-type-select" className="block text-xs font-semibold mb-1">
                Maintenance Type <span className="text-red-500">*</span>
              </label>
              <select
                id="maintenance-type-select"
                name="type"
                onBlur={() => validation.touch('type', formValues)}
                {...validation.fieldProps('type')}
                value={type}
                onChange={(e) => setType(parseInt(e.target.value, 10))}
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
              <FieldError name="type" message={validation.errorFor('type')} />
            </div>

            <div>
              <label htmlFor="scheduled-start-input" className="block text-xs font-semibold mb-1">
                Scheduled Start <span className="text-red-500">*</span>
              </label>
              <input
                id="scheduled-start-input"
                name="scheduledStart"
                onBlur={() => validation.touch('scheduledStart', formValues)}
                {...validation.fieldProps('scheduledStart')}
                type="datetime-local"
                value={scheduledStart}
                min={minStartDateTime || formatToLocalDatetimeString(new Date())}
                onChange={(e) => setScheduledStart(e.target.value)}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
                required
              />
              <FieldError name="scheduledStart" message={validation.errorFor('scheduledStart')} />
            </div>

            <div>
              <label htmlFor="scheduled-end-input" className="block text-xs font-semibold mb-1">
                Scheduled End <span className="text-xs font-normal opacity-60">(Optional)</span>
              </label>
              <input
                id="scheduled-end-input"
                name="scheduledEnd"
                onBlur={() => validation.touch('scheduledEnd', formValues)}
                {...validation.fieldProps('scheduledEnd')}
                type="datetime-local"
                value={scheduledEnd}
                min={scheduledStart || minStartDateTime || formatToLocalDatetimeString(new Date())}
                onChange={(e) => setScheduledEnd(e.target.value)}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
              />
              <FieldError name="scheduledEnd" message={validation.errorFor('scheduledEnd')} />
            </div>
          </div>

          {/* Description */}
          <div className="mb-4">
            <label htmlFor="maintenance-description-input" className="block text-xs font-semibold mb-1">
              Description / Reason for Maintenance <span className="text-red-500">*</span>
            </label>
            <textarea
              id="maintenance-description-input"
                name="description"
                onBlur={() => validation.touch('description', formValues)}
                {...validation.fieldProps('description')}
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={500}
              placeholder="e.g. Routine quarterly calibration, periodic deep sanitization, or motor malfunction repair..."
              className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
              required
            />
              <FieldError name="description" message={validation.errorFor('description')} />
            <div className="text-right text-xs opacity-50 mt-0.5">
              {description.length}/500
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
              {submitting ? 'Scheduling...' : '+ Schedule Maintenance'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
