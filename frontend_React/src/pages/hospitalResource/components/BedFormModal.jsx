import { useState, useEffect } from 'react'
import { createBed, updateBed, getRoomsByWard, getBeds } from '../../../services/hospitalResourceService'

const BED_TYPES = [
  { value: 1, label: 'Standard' },
  { value: 2, label: 'ICU' },
  { value: 3, label: 'Pediatric' },
  { value: 4, label: 'Bariatric' },
  { value: 5, label: 'Electric' },
  { value: 6, label: 'Birthing' },
]

const BED_TYPE_NAME_TO_INT = {
  Standard: 1,
  ICU: 2,
  Pediatric: 3,
  Bariatric: 4,
  Electric: 5,
  Birthing: 6,
}

/**
 * Calculates the lowest unused positive sequential bed number for a room (BED-01 to BED-04).
 * Returns empty string if room has reached maximum capacity of 4 beds (blocking BED-05).
 */
export const getNextSequentialBedNumber = (existingBeds = []) => {
  const usedNumbers = new Set()
  for (const b of existingBeds || []) {
    if (b && typeof b === 'object' && b.isActive === false) continue
    const raw = typeof b === 'string' ? b : (b?.bedNumber || '')
    const match = raw.trim().match(/^BED-?(\d+)$/i)
    if (match) {
      const num = parseInt(match[1], 10)
      if (num > 0) usedNumbers.add(num)
    }
  }
  let next = 1
  while (usedNumbers.has(next) && next <= 4) {
    next++
  }
  if (next > 4) {
    return '' // Block BED-05
  }
  return `BED-${String(next).padStart(2, '0')}`
}

export default function BedFormModal({ isOpen, onClose, bed, wards = [], onSuccess }) {
  const isEdit = Boolean(bed)

  const [wardId, setWardId] = useState('')
  const [roomId, setRoomId] = useState('')
  const [rooms, setRooms] = useState([])
  const [loadingRooms, setLoadingRooms] = useState(false)

  const [bedNumber, setBedNumber] = useState('')
  const [generatingBedNumber, setGeneratingBedNumber] = useState(false)
  const [type, setType] = useState(1)
  const [isActive, setIsActive] = useState(true)

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen) {
      setError(null)
      if (bed) {
        // Edit Mode: keep existing values
        setWardId(bed.wardId ? String(bed.wardId) : '')
        setRoomId(bed.roomId ? String(bed.roomId) : '')
        setBedNumber(bed.bedNumber || '')
        setType(BED_TYPE_NAME_TO_INT[bed.type] || 1)
        setIsActive(bed.isActive !== false)
      } else {
        // Add Mode
        setWardId('')
        setRoomId('')
        setRooms([])
        setBedNumber('')
        setType(1)
        setIsActive(true)
      }
    }
  }, [isOpen, bed])

  // When Ward changes in Add Mode, load rooms for that ward
  useEffect(() => {
    if (!isOpen || isEdit) return

    if (!wardId) {
      setRooms([])
      setRoomId('')
      setBedNumber('')
      return
    }

    const fetchRooms = async () => {
      try {
        setLoadingRooms(true)
        setRooms([])
        setRoomId('')
        setBedNumber('')
        const data = await getRoomsByWard(wardId, { isActive: true })
        setRooms(Array.isArray(data) ? data : [])
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to load rooms for the selected ward.')
      } finally {
        setLoadingRooms(false)
      }
    }

    fetchRooms()
  }, [wardId, isOpen, isEdit])

  // When Room changes in Add Mode, automatically generate next sequential bed number (BED-01 to BED-04)
  useEffect(() => {
    if (!isOpen || isEdit || !roomId) {
      if (!isEdit && !roomId) setBedNumber('')
      return
    }

    const generateBedNumberForRoom = async () => {
      try {
        setGeneratingBedNumber(true)
        const existingBeds = await getBeds({ roomId: parseInt(roomId, 10), pageSize: 100 })
        const activeBeds = Array.isArray(existingBeds) ? existingBeds.filter((b) => b.isActive !== false) : []
        if (activeBeds.length >= 4) {
          setBedNumber('')
          return
        }
        const nextBedNum = getNextSequentialBedNumber(activeBeds)
        setBedNumber(nextBedNum || '')
      } catch {
        setBedNumber('')
      } finally {
        setGeneratingBedNumber(false)
      }
    }

    generateBedNumberForRoom()
  }, [roomId, isOpen, isEdit])

  if (!isOpen) return null

  const isOccupied = isEdit && bed?.status?.toLowerCase() === 'occupied'

  // Room capacity check: exactly 4 beds maximum per room
  const selectedRoom = rooms.find((r) => String(r.id) === String(roomId))
  const isCapacityReached = !isEdit && selectedRoom
    ? (selectedRoom.totalBeds >= 4 || (!generatingBedNumber && !bedNumber && Boolean(roomId)))
    : false

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    if (!isEdit && !wardId) {
      setError('Please select a ward.')
      return
    }

    if (!isEdit && !roomId) {
      setError('Please select a room.')
      return
    }

    if (!isEdit && (isCapacityReached || (selectedRoom && selectedRoom.totalBeds >= 4))) {
      setError('Room capacity reached. Maximum 4 beds are allowed.')
      return
    }

    const trimmedBed = bedNumber.trim().toUpperCase()
    if (!trimmedBed) {
      setError('Room capacity reached. Maximum 4 beds are allowed.')
      return
    }

    // Explicitly block BED-05 or higher
    const bedMatch = trimmedBed.match(/^BED-?(\d+)$/i)
    if (bedMatch && parseInt(bedMatch[1], 10) > 4) {
      setError('Room capacity reached. Maximum 4 beds are allowed.')
      return
    }

    try {
      setSubmitting(true)
      if (isEdit) {
        const payload = {
          bedNumber: trimmedBed,
          type: Number(type),
          isActive,
        }
        await updateBed(bed.id, payload)
        onSuccess(`Bed '${trimmedBed}' updated successfully.`)
      } else {
        const payload = {
          roomId: Number(roomId),
          bedNumber: trimmedBed,
          type: Number(type),
        }
        await createBed(payload)
        onSuccess(`Bed '${trimmedBed}' created successfully.`)
      }
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || `Failed to ${isEdit ? 'update' : 'create'} bed.`)
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
        <div
          className="flex items-center justify-between pb-3 mb-4 border-b"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}
        >
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              {isEdit ? 'Edit Bed Configuration' : 'Add New Hospital Bed'}
            </h2>
            <p className="text-xs opacity-70 m-0">
              {isEdit
                ? 'Update bed identifier, specialty type, or active state.'
                : 'Configure a new physical bed in an existing ward and room.'}
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

        <form onSubmit={handleSubmit}>
          {/* Ward Selection */}
          <div className="mb-3">
            <label className="block text-xs font-semibold mb-1">
              Hospital Ward {!isEdit && <span className="text-red-500">*</span>}
            </label>
            {isEdit ? (
              <div
                className="w-full px-3 py-2 text-xs border rounded-lg opacity-80"
                style={{
                  background: 'color-mix(in srgb, var(--color-secondary) 5%, var(--color-primary))',
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 20%, var(--color-primary))',
                }}
              >
                <strong>{bed.wardName || '—'}</strong>
                {bed.wardCode && <span className="opacity-70 font-mono ml-2">({bed.wardCode})</span>}
                {bed.floor && <span className="opacity-70 ml-2">· {bed.floor}</span>}
              </div>
            ) : (
              <select
                value={wardId}
                onChange={(e) => setWardId(e.target.value)}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
                required
              >
                <option value="">-- Select Ward --</option>
                {wards.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.code}) {w.floor ? `· ${w.floor}` : ''}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Room Selection */}
          <div className="mb-3">
            <label className="block text-xs font-semibold mb-1">
              Room {!isEdit && <span className="text-red-500">*</span>}
            </label>
            {isEdit ? (
              <div
                className="w-full px-3 py-2 text-xs border rounded-lg opacity-80"
                style={{
                  background: 'color-mix(in srgb, var(--color-secondary) 5%, var(--color-primary))',
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 20%, var(--color-primary))',
                }}
              >
                <strong>Room {bed.roomNumber || '—'}</strong>
              </div>
            ) : (
              <select
                value={roomId}
                onChange={(e) => setRoomId(e.target.value)}
                disabled={!wardId || loadingRooms}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                  opacity: !wardId || loadingRooms ? 0.6 : 1,
                  cursor: !wardId || loadingRooms ? 'not-allowed' : 'pointer',
                }}
                required
              >
                <option value="">
                  {loadingRooms
                    ? 'Loading rooms...'
                    : !wardId
                    ? '-- Select a ward first --'
                    : rooms.length === 0
                    ? '-- No active rooms found in this ward --'
                    : '-- Select Room --'}
                </option>
                {rooms.map((r) => {
                  const isFull = (r.totalBeds >= 4) || (r.totalBeds >= (r.capacity || 4))
                  return (
                    <option key={r.id} value={r.id} disabled={isFull}>
                      Room {r.roomNumber} ({r.type || 'General'}) — {r.totalBeds}/4 beds{isFull ? ' [FULL - Max 4]' : ''}
                    </option>
                  )
                })}
              </select>
            )}

            {/* Room Capacity Reached Warning */}
            {isCapacityReached && (
              <div className="p-2.5 mt-2 rounded-lg text-xs bg-red-50 text-red-700 border border-red-200 font-semibold">
                Room capacity reached. Maximum 4 beds are allowed.
              </div>
            )}

            {!isEdit && wardId && rooms.length === 0 && !loadingRooms && (
              <p className="text-xs text-amber-700 mt-1">
                This ward has no active rooms. Create a room first before adding beds.
              </p>
            )}
          </div>

          {/* Bed Number & Bed Type */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3">
            <div>
              <label htmlFor="bed-number-input" className="block text-xs font-semibold mb-1">
                Bed Number <span className="text-red-500">*</span>
              </label>
              <input
                id="bed-number-input"
                type="text"
                value={bedNumber}
                onChange={(e) => isEdit && setBedNumber(e.target.value)}
                readOnly={!isEdit}
                placeholder={
                  generatingBedNumber
                    ? 'Generating bed number...'
                    : isCapacityReached
                    ? 'Room capacity reached'
                    : 'e.g. BED-01'
                }
                maxLength={30}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none font-mono uppercase"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  backgroundColor: !isEdit
                    ? 'color-mix(in srgb, var(--color-secondary) 6%, var(--color-primary))'
                    : undefined,
                  cursor: !isEdit ? 'not-allowed' : undefined,
                }}
                required
              />
              {!isEdit && (
                <p className="text-xs opacity-60 mt-1">
                  Auto-generated sequentially (BED-01 to BED-04) for the selected room.
                </p>
              )}
            </div>

            <div>
              <label htmlFor="bed-type-select" className="block text-xs font-semibold mb-1">
                Bed Type <span className="text-red-500">*</span>
              </label>
              <select
                id="bed-type-select"
                value={type}
                onChange={(e) => setType(parseInt(e.target.value, 10))}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                {BED_TYPES.map((bt) => (
                  <option key={bt.value} value={bt.value}>
                    {bt.label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Active Status Toggle (Edit Mode Only) */}
          {isEdit && (
            <div className="mb-4 pt-2">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={isActive}
                  disabled={isOccupied}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="rounded text-xs"
                />
                <span className="text-xs font-medium">Bed is Operational and Active</span>
              </label>
              {isOccupied && (
                <p className="text-xs text-amber-700 mt-1">
                  Cannot deactivate an occupied bed. Please discharge or transfer the patient first.
                </p>
              )}
            </div>
          )}

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
              disabled={
                submitting ||
                (!isEdit && (!roomId || isCapacityReached || generatingBedNumber || !bedNumber))
              }
              className="primary-button text-xs px-5 py-2"
              style={{ marginTop: 0 }}
            >
              {submitting ? 'Saving...' : isEdit ? 'Save Changes' : '+ Create Bed'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
