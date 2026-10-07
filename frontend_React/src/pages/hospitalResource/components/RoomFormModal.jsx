import { useState, useEffect } from 'react'
import { createRoom, updateRoom } from '../../../services/hospitalResourceService'
import { useFormValidation } from '../../../hooks/useFormValidation'
import FieldError from '../../../components/common/FieldError'
import { selection, text } from '../../../utils/validators'

const ROOM_SCHEMA = {
  roomNumber: (value) => text(value, 'Room number', { isRequired: true, max: 30 }),
  type: (value) => selection(value, 'a room type'),
}

const ROOM_TYPES = [
  { value: 1, label: 'Standard' },
  { value: 2, label: 'SemiPrivate' },
  { value: 3, label: 'Private' },
  { value: 4, label: 'ICU' },
  { value: 5, label: 'Isolation' },
]

const ROOM_TYPE_NAME_TO_INT = {
  Standard: 1,
  SemiPrivate: 2,
  Private: 3,
  ICU: 4,
  Isolation: 5,
}

/**
 * Deterministic Excel-style column mapping (1 -> A, 2 -> B, ..., 26 -> Z, 27 -> AA, 28 -> AB)
 */
export function getExcelColumnName(n) {
  let num = Number(n)
  if (!num || num <= 0) return 'A'
  let result = ''
  while (num > 0) {
    const rem = (num - 1) % 26
    result = String.fromCharCode(65 + rem) + result
    num = Math.floor((num - 1) / 26)
  }
  return result
}

/**
 * Returns deterministic prefix based strictly on the ward's permanent database Id:
 * ward.id = 1 -> A
 * ward.id = 2 -> B
 * ...
 * ward.id = 26 -> Z
 * ward.id = 27 -> AA
 * ward.id = 28 -> AB
 */
export function getWardPrefix(ward) {
  const id = Number(ward?.id)
  return getExcelColumnName(id > 0 ? id : 1)
}

/**
 * Generates the next sequential room number starting at 101:
 * e.g. A-101, A-102, A-103...
 */
export function getNextSequentialRoomNumber(prefix, existingRoomNumbers = []) {
  const cleanPrefix = (prefix || 'A').toUpperCase()
  const pattern = new RegExp(`^${cleanPrefix}-(\\d+)$`, 'i')
  const existingSet = new Set(
    (existingRoomNumbers || []).map((rn) => (rn || '').trim().toUpperCase())
  )

  let maxNum = 100
  for (const rn of existingRoomNumbers || []) {
    if (!rn) continue
    const match = String(rn).trim().match(pattern)
    if (match) {
      const num = parseInt(match[1], 10)
      if (!isNaN(num) && num > maxNum) {
        maxNum = num
      }
    }
  }

  let candidateNum = maxNum + 1
  while (existingSet.has(`${cleanPrefix}-${candidateNum}`)) {
    candidateNum++
  }

  return `${cleanPrefix}-${candidateNum}`
}

export default function RoomFormModal({
  isOpen,
  onClose,
  ward,
  allWards = [],
  room,
  existingRoomNumbers = [],
  onSuccess,
}) {
  const isEdit = Boolean(room)

  const [roomNumber, setRoomNumber] = useState('')
  const [type, setType] = useState(1)
  const [isActive, setIsActive] = useState(true)

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const validation = useFormValidation(ROOM_SCHEMA, { roomNumber, type })
  const resetValidation = validation.reset

  useEffect(() => {
    if (isOpen) {
      setError(null)
      resetValidation()
      if (room) {
        setRoomNumber(room.roomNumber || '')
        setType(ROOM_TYPE_NAME_TO_INT[room.type] || 1)
        setIsActive(room.isActive !== false)
      } else {
        const prefix = getWardPrefix(ward)
        const autoNumber = getNextSequentialRoomNumber(prefix, existingRoomNumbers)
        setRoomNumber(autoNumber)
        setType(1)
        setIsActive(true)
      }
    }
  }, [isOpen, room, ward, existingRoomNumbers, resetValidation])

  if (!isOpen || !ward) return null

  const wardPrefix = getWardPrefix(ward)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    const trimmedNumber = roomNumber.trim().toUpperCase()
    if (!validation.validateAll({ roomNumber: trimmedNumber, type }, e.currentTarget)) return

    try {
      setSubmitting(true)
      if (isEdit) {
        const payload = {
          roomNumber: room.roomNumber,
          type: Number(type),
          capacity: 4,
          isActive,
        }
        await updateRoom(room.id, payload)
        onSuccess(`Room '${room.roomNumber}' updated successfully.`)
      } else {
        const payload = {
          wardId: ward.id,
          roomNumber: trimmedNumber,
          type: Number(type),
          capacity: 4,
        }
        await createRoom(payload)
        onSuccess(`Room '${trimmedNumber}' created in ward '${ward.name}'.`)
      }
      onClose()
    } catch (err) {
      setError(validation.applyServerErrors(err, { conflicts: [{ match: /room number/i, field: 'roomNumber' }], fallback: `Failed to ${isEdit ? 'update' : 'create'} room.` }) || null)
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
              {isEdit ? 'Edit Room' : 'Add Room'}
            </h2>
            <p className="text-xs opacity-70 m-0">
              {ward.name} ({ward.code}) · Prefix: <span className="font-bold">{wardPrefix}</span>
              {ward.floor ? ` · ${ward.floor}` : ''}
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
          {/* Room Number Input - Always Read-Only */}
          <div className="mb-3">
            <label htmlFor="room-number-input" className="block text-xs font-semibold mb-1">
              Room Number <span className="text-red-500">*</span>
            </label>
            <input
              id="room-number-input"
              name="roomNumber"
              type="text"
              value={roomNumber}
              readOnly
              maxLength={30}
              className="w-full px-3 py-2 text-xs border rounded-lg outline-none font-mono uppercase cursor-not-allowed opacity-80"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                backgroundColor: 'color-mix(in srgb, var(--color-secondary) 6%, var(--color-primary))',
              }}
              required
              {...validation.fieldProps('roomNumber')}
            />
            <FieldError name="roomNumber" message={validation.errorFor('roomNumber')} />
            <p className="text-xs opacity-60 mt-1">
              {isEdit
                ? 'Room number is fixed and read-only.'
                : `Auto-generated globally unique room identifier (${wardPrefix}-101, ${wardPrefix}-102...).`}
            </p>
          </div>

          {/* Room Type & Capacity */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3">
            <div>
              <label htmlFor="room-type-select" className="block text-xs font-semibold mb-1">
                Room Type <span className="text-red-500">*</span>
              </label>
              <select
                id="room-type-select"
                name="type"
                value={type}
                onChange={(e) => setType(parseInt(e.target.value, 10))}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                {ROOM_TYPES.map((rt) => (
                  <option key={rt.value} value={rt.value}>
                    {rt.label}
                  </option>
                ))}
              </select>
              <FieldError name="type" message={validation.errorFor('type')} />
            </div>

            <div>
              <label htmlFor="room-capacity-input" className="block text-xs font-semibold mb-1">
                Bed Capacity <span className="text-red-500">*</span>
              </label>
              <input
                id="room-capacity-input"
                type="number"
                value={4}
                readOnly
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none cursor-not-allowed opacity-80"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  backgroundColor: 'color-mix(in srgb, var(--color-secondary) 6%, var(--color-primary))',
                }}
              />
              <p className="text-xs opacity-60 mt-1">
                Fixed: maximum 4 beds per room.
              </p>
            </div>
          </div>

          {/* Active Status Toggle (Edit Mode Only) */}
          {isEdit && (
            <div className="mb-4 pt-1">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={isActive}
                  disabled={room.occupiedBeds > 0}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="rounded text-xs"
                />
                <span className="text-xs font-medium">Room is Active</span>
              </label>
              {room.occupiedBeds > 0 && (
                <p className="text-xs text-amber-700 mt-1">
                  Cannot deactivate room with {room.occupiedBeds} occupied bed(s).
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
              disabled={submitting || !roomNumber}
              className="primary-button text-xs px-5 py-2"
              style={{ marginTop: 0 }}
            >
              {submitting ? 'Saving...' : isEdit ? 'Save Changes' : '+ Create Room'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

