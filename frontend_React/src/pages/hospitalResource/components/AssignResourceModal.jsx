import { useState, useEffect } from 'react'
import {
  assignMedicalResource,
  getRoomsByWard,
  getBeds,
} from '../../../services/hospitalResourceService'
import { useFormValidation } from '../../../hooks/useFormValidation'
import FieldError from '../../../components/common/FieldError'
import { text } from '../../../utils/validators'

export default function AssignResourceModal({
  isOpen,
  onClose,
  resource,
  wards = [],
  onSuccess,
}) {
  const [wardId, setWardId] = useState('')
  const [roomId, setRoomId] = useState('')
  const [bedId, setBedId] = useState('')
  const [locationDescription, setLocationDescription] = useState('')

  const [rooms, setRooms] = useState([])
  const [beds, setBeds] = useState([])
  const [loadingRooms, setLoadingRooms] = useState(false)
  const [loadingBeds, setLoadingBeds] = useState(false)

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const formValues = { locationDescription }
  const validation = useFormValidation({
    locationDescription: (value) => text(value, 'Location description', { max: 200 }),
  }, formValues)
  const resetValidation = validation.reset

  useEffect(() => {
    if (isOpen && resource) {
      setError(null)
      resetValidation()
      const currentWardId = resource.wardId ? String(resource.wardId) : ''
      const currentRoomId = resource.roomId ? String(resource.roomId) : ''
      const currentBedId = resource.bedId ? String(resource.bedId) : ''

      setWardId(currentWardId)
      setRoomId(currentRoomId)
      setBedId(currentBedId)
      setLocationDescription(resource.locationDescription || '')

      if (currentWardId) {
        getRoomsByWard(currentWardId).then((r) => setRooms(Array.isArray(r) ? r : []))
      } else {
        setRooms([])
      }

      if (currentRoomId) {
        getBeds({ roomId: parseInt(currentRoomId, 10), pageSize: 100 }).then((b) =>
          setBeds(Array.isArray(b) ? b : [])
        )
      } else {
        setBeds([])
      }
    }
  }, [isOpen, resource, resetValidation])

  if (!isOpen || !resource) return null

  // Handle Ward change
  const handleWardChange = async (e) => {
    const selectedWardId = e.target.value
    setWardId(selectedWardId)
    setRoomId('')
    setBedId('')
    setRooms([])
    setBeds([])

    if (!selectedWardId) {
      setLocationDescription('Central Storage')
      return
    }

    try {
      setLoadingRooms(true)
      const data = await getRoomsByWard(selectedWardId)
      setRooms(Array.isArray(data) ? data : [])
      const matchedWard = wards.find((w) => String(w.id) === String(selectedWardId))
      if (matchedWard) {
        setLocationDescription(`Stationed in ${matchedWard.name} (${matchedWard.code})`)
      }
    } catch {
      setRooms([])
    } finally {
      setLoadingRooms(false)
    }
  }

  // Handle Room change
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
      const matchedRoom = rooms.find((r) => String(r.id) === String(selectedRoomId))
      const matchedWard = wards.find((w) => String(w.id) === String(wardId))
      if (matchedRoom) {
        setLocationDescription(
          `Stationed in ${matchedWard?.name || 'Ward'} · Room ${matchedRoom.roomNumber}`
        )
      }
    } catch {
      setBeds([])
    } finally {
      setLoadingBeds(false)
    }
  }

  // Handle Bed change
  const handleBedChange = (e) => {
    const selectedBedId = e.target.value
    setBedId(selectedBedId)

    if (selectedBedId) {
      const matchedBed = beds.find((b) => String(b.id) === String(selectedBedId))
      const matchedRoom = rooms.find((r) => String(r.id) === String(roomId))
      const matchedWard = wards.find((w) => String(w.id) === String(wardId))
      if (matchedBed && matchedRoom) {
        setLocationDescription(
          `Stationed at ${matchedWard?.code || 'Ward'} · Room ${matchedRoom.roomNumber} · Bed ${matchedBed.bedNumber}`
        )
      }
    }
  }

  // Clear assignment action (Unassign to Storage)
  const handleClearAssignment = () => {
    setWardId('')
    setRoomId('')
    setBedId('')
    setRooms([])
    setBeds([])
    setLocationDescription('Unassigned / Central Storage')
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    if (!validation.validateAll(formValues, e.currentTarget)) return
    try {
      setSubmitting(true)
      const payload = {
        wardId: wardId ? Number(wardId) : null,
        roomId: roomId ? Number(roomId) : null,
        bedId: bedId ? Number(bedId) : null,
        locationDescription: locationDescription.trim() || undefined,
      }

      await assignMedicalResource(resource.id, payload)
      onSuccess(`Resource '${resource.resourceCode}' location updated.`)
      onClose()
    } catch (err) {
      setError(validation.applyServerErrors(err, { conflicts: [{ match: /bed/i, field: 'bedId' }, { match: /room/i, field: 'roomId' }, { match: /ward/i, field: 'wardId' }], fallback: 'Failed to update resource assignment.' }) || null)
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
              Assign / Relocate Resource
            </h2>
            <p className="text-xs opacity-70 m-0">
              Station equipment at a hospital Ward, Room, or Bed, or return to central storage.
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

        {/* Current Resource Details Card */}
        <div
          className="p-3 mb-4 rounded-xl border text-xs"
          style={{
            background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
            borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
          }}
        >
          <div className="flex items-center justify-between mb-1">
            <span className="font-mono font-bold" style={{ color: 'var(--color-accent)' }}>
              {resource.resourceCode}
            </span>
            <span className="inline-block px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700">
              {resource.category}
            </span>
          </div>
          <p className="font-semibold text-xs mb-1" style={{ color: 'var(--color-secondary)' }}>
            {resource.name}
          </p>
          <p className="text-xs opacity-70 m-0">
            <strong>Current:</strong>{' '}
            {resource.wardName
              ? `${resource.wardName}${resource.roomNumber ? ` · Room ${resource.roomNumber}` : ''}${
                  resource.bedNumber ? ` · Bed ${resource.bedNumber}` : ''
                }`
              : 'Unassigned (Storage)'}{' '}
            ({resource.locationDescription})
          </p>
        </div>

        <form onSubmit={handleSubmit} noValidate>
          {/* Quick Clear Action */}
          <div className="flex justify-end mb-3">
            <button
              type="button"
              onClick={handleClearAssignment}
              className="text-xs font-semibold text-amber-700 hover:underline cursor-pointer"
            >
              Clear Assignment (Return to Central Storage)
            </button>
          </div>

          {/* Cascading Location Hierarchy: Ward -> Room -> Bed */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-3">
            <div>
              <label htmlFor="assign-ward-select" className="block text-xs font-semibold mb-1">
                Hospital Ward
              </label>
              <select
                id="assign-ward-select"
                value={wardId}
                onChange={handleWardChange}
                className="w-full px-2.5 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                <option value="">-- Unassigned (Storage) --</option>
                {wards.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.code})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label htmlFor="assign-room-select" className="block text-xs font-semibold mb-1">
                Room
              </label>
              <select
                id="assign-room-select"
                value={roomId}
                onChange={handleRoomChange}
                disabled={!wardId || loadingRooms}
                className="w-full px-2.5 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
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
                    : '-- Entire Ward / Select Room --'}
                </option>
                {rooms.map((r) => (
                  <option key={r.id} value={r.id}>
                    Room {r.roomNumber} ({r.type})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label htmlFor="assign-bed-select" className="block text-xs font-semibold mb-1">
                Bed
              </label>
              <select
                id="assign-bed-select"
                value={bedId}
                onChange={handleBedChange}
                disabled={!roomId || loadingBeds}
                className="w-full px-2.5 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
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
                    ? '-- No beds in room --'
                    : '-- Entire Room / Select Bed --'}
                </option>
                {beds.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.bedNumber} ({b.status})
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Location Description Input */}
          <div className="mb-4">
            <label htmlFor="assign-location-desc" className="block text-xs font-semibold mb-1">
              Location Description / Notes
            </label>
            <input
              id="assign-location-desc"
                name="locationDescription"
                onBlur={() => validation.touch('locationDescription', formValues)}
                {...validation.fieldProps('locationDescription')}
              type="text"
              value={locationDescription}
              onChange={(e) => setLocationDescription(e.target.value)}
              maxLength={200}
              placeholder="e.g. Mounted beside Bed 01, or Central Equipment Storage"
              className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
            />
              <FieldError name="locationDescription" message={validation.errorFor('locationDescription')} />
            <p className="text-xs opacity-60 mt-1">
              Descriptive note for clinical staff identifying the equipment&apos;s physical location.
            </p>
          </div>

          {/* Footer Actions */}
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
              {submitting ? 'Saving...' : 'Save Assignment'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
