import { useState, useEffect } from 'react'
import { createWard, updateWard } from '../../../services/hospitalResourceService'

const WARD_TYPES = [
  { value: 1, label: 'General' },
  { value: 2, label: 'ICU' },
  { value: 3, label: 'Emergency' },
  { value: 4, label: 'Maternity' },
  { value: 5, label: 'Pediatric' },
  { value: 6, label: 'Surgical' },
  { value: 7, label: 'Isolation' },
]

const TYPE_NAME_TO_INT = {
  General: 1,
  ICU: 2,
  Emergency: 3,
  Maternity: 4,
  Pediatric: 5,
  Surgical: 6,
  Isolation: 7,
}

const FLOOR_OPTIONS = [
  'Ground Floor',
  '1st Floor',
  '2nd Floor',
  '3rd Floor',
  '4th Floor',
  '5th Floor',
  '6th Floor',
  '7th Floor',
  '8th Floor',
  '9th Floor',
  '10th Floor',
]

/**
 * Generates the lowest available sequential ward code in the format:
 * WD1, WD2, WD3, WD4...
 * Inspects existing ward codes and fills in the smallest missing positive integer.
 * Examples:
 * - [WD1, WD2, WD3] -> WD4
 * - [WD1, WD2, WD4] -> WD3
 * - [] -> WD1
 */
export const getNextSequentialWardCode = (existingCodes = []) => {
  const usedNumbers = new Set()

  for (const code of existingCodes || []) {
    if (!code) continue
    const match = String(code).trim().match(/^WD(\d+)$/i)
    if (match) {
      const num = parseInt(match[1], 10)
      if (num > 0) {
        usedNumbers.add(num)
      }
    }
  }

  let nextNum = 1
  while (usedNumbers.has(nextNum)) {
    nextNum++
  }

  return `WD${nextNum}`
}

export default function WardFormModal({ isOpen, onClose, ward, existingCodes = [], onSuccess }) {
  const isEdit = Boolean(ward)

  const [formData, setFormData] = useState({
    name: '',
    code: 'WD1',
    floor: 'Ground Floor',
    buildingBlock: '',
    capacity: 20,
    type: 1,
    isActive: true,
  })
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen) {
      setError(null)
      if (ward) {
        // Edit mode: retain the existing ward code as read-only
        setFormData({
          name: ward.name || '',
          code: ward.code || '',
          floor: ward.floor || 'Ground Floor',
          buildingBlock: ward.buildingBlock || '',
          capacity: ward.capacity || 20,
          type: TYPE_NAME_TO_INT[ward.type] || 1,
          isActive: ward.isActive ?? true,
        })
      } else {
        // Create mode: compute the next sequential code (WD1, WD2, WD3...)
        const nextCode = getNextSequentialWardCode(existingCodes)
        setFormData({
          name: '',
          code: nextCode,
          floor: 'Ground Floor',
          buildingBlock: '',
          capacity: 20,
          type: 1,
          isActive: true,
        })
      }
    }
  }, [isOpen, ward, existingCodes])

  if (!isOpen) return null

  // Ensure floor options include the current ward's floor if it's a legacy or custom string
  const floorList = isEdit && ward?.floor && !FLOOR_OPTIONS.includes(ward.floor)
    ? [ward.floor, ...FLOOR_OPTIONS]
    : FLOOR_OPTIONS

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target
    setFormData((prev) => ({
      ...prev,
      [name]:
        type === 'checkbox'
          ? checked
          : name === 'capacity' || name === 'type'
          ? parseInt(value, 10)
          : value,
    }))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    if (!formData.name.trim()) {
      setError('Ward name is required.')
      return
    }

    if (!formData.floor.trim()) {
      setError('Please select a floor location.')
      return
    }

    if (isNaN(formData.capacity) || formData.capacity < 1 || formData.capacity > 500) {
      setError('Capacity must be between 1 and 500.')
      return
    }

    try {
      setSubmitting(true)
      if (isEdit) {
        await updateWard(ward.id, {
          name: formData.name.trim(),
          floor: formData.floor.trim(),
          buildingBlock: formData.buildingBlock.trim() || null,
          capacity: formData.capacity,
          type: formData.type,
          isActive: formData.isActive,
        })
        onSuccess(`Ward '${formData.name.trim()}' updated successfully.`)
      } else {
        await createWard({
          name: formData.name.trim(),
          code: formData.code.trim().toUpperCase(),
          floor: formData.floor.trim(),
          buildingBlock: formData.buildingBlock.trim() || null,
          capacity: formData.capacity,
          type: formData.type,
        })
        onSuccess(`Ward '${formData.name.trim()}' created successfully with code ${formData.code}.`)
      }
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || `Failed to ${isEdit ? 'update' : 'create'} ward.`)
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
        <div className="flex items-center justify-between pb-4 mb-4 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              {isEdit ? 'Edit Ward' : 'Add New Ward'}
            </h2>
            <p className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
              {isEdit ? `Update configuration for ward: ${ward.code}` : 'Register a new hospital ward with auto-assigned code.'}
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

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label htmlFor="ward-name" className="block text-xs font-semibold mb-1">
                Ward Name <span className="text-red-500">*</span>
              </label>
              <input
                id="ward-name"
                name="name"
                type="text"
                required
                maxLength={100}
                value={formData.name}
                onChange={handleChange}
                placeholder="e.g. Cardiology Ward"
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
                style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
              />
            </div>

            <div>
              <label htmlFor="ward-code" className="block text-xs font-semibold mb-1">
                Ward Code <span className="text-xs font-normal opacity-70">({isEdit ? 'Permanent' : 'Auto-generated'})</span>
              </label>
              <input
                id="ward-code"
                name="code"
                type="text"
                readOnly
                value={formData.code}
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none font-mono uppercase bg-slate-50 text-slate-700 cursor-not-allowed"
                style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
              />
              <span className="text-xs opacity-60 block mt-0.5">
                {isEdit
                  ? 'Ward code cannot be modified.'
                  : 'Assigned automatically as sequential code (WD1, WD2, etc.).'}
              </span>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label htmlFor="ward-type" className="block text-xs font-semibold mb-1">
                Ward Type <span className="text-red-500">*</span>
              </label>
              <select
                id="ward-type"
                name="type"
                value={formData.type}
                onChange={handleChange}
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                {WARD_TYPES.map((t) => (
                  <option key={t.value} value={t.value}>
                    {t.label}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label htmlFor="ward-capacity" className="block text-xs font-semibold mb-1">
                Capacity (Max Beds) <span className="text-red-500">*</span>
              </label>
              <input
                id="ward-capacity"
                name="capacity"
                type="number"
                min="1"
                max="500"
                required
                value={formData.capacity}
                onChange={handleChange}
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
                style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label htmlFor="ward-floor" className="block text-xs font-semibold mb-1">
                Floor Location <span className="text-red-500">*</span>
              </label>
              <select
                id="ward-floor"
                name="floor"
                required
                value={formData.floor}
                onChange={handleChange}
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                {floorList.map((f) => (
                  <option key={f} value={f}>
                    {f}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label htmlFor="ward-buildingBlock" className="block text-xs font-semibold mb-1">
                Building Block <span className="text-xs font-normal opacity-70">(optional)</span>
              </label>
              <input
                id="ward-buildingBlock"
                name="buildingBlock"
                type="text"
                maxLength={50}
                value={formData.buildingBlock}
                onChange={handleChange}
                placeholder="e.g. Block A, West Wing"
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
                style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
              />
            </div>
          </div>

          {isEdit && (
            <div className="pt-2">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  name="isActive"
                  checked={formData.isActive}
                  onChange={handleChange}
                  className="w-4 h-4 rounded"
                />
                <span className="text-xs font-semibold">Active Ward</span>
              </label>
              <span className="text-xs opacity-60 block mt-0.5">
                Note: Deactivating requires all beds in this ward to be unoccupied.
              </span>
            </div>
          )}

          <div className="flex justify-end gap-2 mt-4 pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
            <button
              type="button"
              disabled={submitting}
              onClick={onClose}
              className="secondary-button text-sm px-4 py-2"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="primary-button text-sm px-5 py-2"
              style={{ marginTop: 0 }}
            >
              {submitting ? 'Saving...' : isEdit ? 'Update Ward' : 'Create Ward'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
