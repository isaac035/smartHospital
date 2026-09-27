import { useState, useEffect } from 'react'
import {
  createMedicalResource,
  updateMedicalResource,
  getRoomsByWard,
  getBeds,
} from '../../../services/hospitalResourceService'

export const RESOURCE_CATEGORIES = [
  { value: 1, label: 'Ventilator' },
  { value: 2, label: 'Monitor' },
  { value: 3, label: 'Oxygen Supply' },
  { value: 4, label: 'Dialysis' },
  { value: 5, label: 'Wheelchair' },
  { value: 6, label: 'Infusion Pump' },
  { value: 7, label: 'Defibrillator' },
  { value: 8, label: 'Other' },
]

export const RESOURCE_CATEGORY_NAME_TO_INT = {
  Ventilator: 1,
  Monitor: 2,
  OxygenSupply: 3,
  Dialysis: 4,
  Wheelchair: 5,
  InfusionPump: 6,
  Defibrillator: 7,
  Other: 8,
}

export const RESOURCE_STATUSES = [
  { value: 1, label: 'Available' },
  { value: 2, label: 'In Use' },
  { value: 3, label: 'Maintenance' },
  { value: 4, label: 'Out of Service' },
]

export const RESOURCE_STATUS_NAME_TO_INT = {
  Available: 1,
  InUse: 2,
  Maintenance: 3,
  OutOfService: 4,
}

export default function MedicalResourceFormModal({
  isOpen,
  onClose,
  resource,
  wards = [],
  onSuccess,
}) {
  const isEdit = Boolean(resource)

  const [resourceCode, setResourceCode] = useState('')
  const [name, setName] = useState('')
  const [category, setCategory] = useState(1)
  const [status, setStatus] = useState(1)
  const [locationDescription, setLocationDescription] = useState('')
  const [serialNumber, setSerialNumber] = useState('')
  const [manufacturer, setManufacturer] = useState('')
  const [modelNumber, setModelNumber] = useState('')

  // Location hierarchy
  const [wardId, setWardId] = useState('')
  const [roomId, setRoomId] = useState('')
  const [bedId, setBedId] = useState('')
  const [rooms, setRooms] = useState([])
  const [beds, setBeds] = useState([])
  const [loadingRooms, setLoadingRooms] = useState(false)
  const [loadingBeds, setLoadingBeds] = useState(false)

  const [isActive, setIsActive] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen) {
      setError(null)
      if (resource) {
        setResourceCode(resource.resourceCode || '')
        setName(resource.name || '')
        setCategory(RESOURCE_CATEGORY_NAME_TO_INT[resource.category] || 1)
        setStatus(RESOURCE_STATUS_NAME_TO_INT[resource.status] || 1)
        setLocationDescription(resource.locationDescription || '')
        setSerialNumber(resource.serialNumber || '')
        setManufacturer(resource.manufacturer || '')
        setModelNumber(resource.modelNumber || '')
        setWardId(resource.wardId ? String(resource.wardId) : '')
        setRoomId(resource.roomId ? String(resource.roomId) : '')
        setBedId(resource.bedId ? String(resource.bedId) : '')
        setIsActive(resource.isActive !== false)

        // Load initial rooms if ward exists
        if (resource.wardId) {
          getRoomsByWard(resource.wardId).then((r) => setRooms(Array.isArray(r) ? r : []))
        } else {
          setRooms([])
        }

        // Load initial beds if room exists
        if (resource.roomId) {
          getBeds({ roomId: resource.roomId, pageSize: 100 }).then((b) =>
            setBeds(Array.isArray(b) ? b : [])
          )
        } else {
          setBeds([])
        }
      } else {
        setResourceCode('')
        setName('')
        setCategory(1)
        setStatus(1)
        setLocationDescription('')
        setSerialNumber('')
        setManufacturer('')
        setModelNumber('')
        setWardId('')
        setRoomId('')
        setBedId('')
        setRooms([])
        setBeds([])
        setIsActive(true)
      }
    }
  }, [isOpen, resource])

  // Handle Ward change: reset Room & Bed, fetch rooms
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

  // Handle Room change: reset Bed, fetch beds
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

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    const trimmedCode = resourceCode.trim().toUpperCase()
    const trimmedName = name.trim()
    const trimmedLocation = locationDescription.trim()

    if (!isEdit && !trimmedCode) {
      setError('Resource Code is required.')
      return
    }

    if (!trimmedName) {
      setError('Resource Name is required.')
      return
    }

    if (!trimmedLocation) {
      setError('Location description is required.')
      return
    }

    try {
      setSubmitting(true)
      if (isEdit) {
        const payload = {
          name: trimmedName,
          category: Number(category),
          status: Number(status),
          locationDescription: trimmedLocation,
          serialNumber: serialNumber.trim() || null,
          manufacturer: manufacturer.trim() || null,
          modelNumber: modelNumber.trim() || null,
          wardId: wardId ? Number(wardId) : null,
          roomId: roomId ? Number(roomId) : null,
          bedId: bedId ? Number(bedId) : null,
          isActive,
        }
        await updateMedicalResource(resource.id, payload)
        onSuccess(`Resource '${resource.resourceCode}' updated successfully.`)
      } else {
        const payload = {
          resourceCode: trimmedCode,
          name: trimmedName,
          category: Number(category),
          locationDescription: trimmedLocation,
          serialNumber: serialNumber.trim() || null,
          manufacturer: manufacturer.trim() || null,
          modelNumber: modelNumber.trim() || null,
          wardId: wardId ? Number(wardId) : null,
          roomId: roomId ? Number(roomId) : null,
          bedId: bedId ? Number(bedId) : null,
        }
        await createMedicalResource(payload)
        onSuccess(`Resource '${trimmedCode}' registered successfully.`)
      }
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || `Failed to ${isEdit ? 'update' : 'create'} resource.`)
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
              {isEdit ? 'Edit Medical Resource' : 'Register New Medical Resource'}
            </h2>
            <p className="text-xs opacity-70 m-0">
              {isEdit
                ? `Update equipment details for ${resource.resourceCode}.`
                : 'Add physical clinical equipment to the hospital inventory.'}
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
          {/* Row 1: Resource Code & Name */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3">
            <div>
              <label htmlFor="resource-code-input" className="block text-xs font-semibold mb-1">
                Resource Code <span className="text-red-500">*</span>
              </label>
              <input
                id="resource-code-input"
                type="text"
                value={resourceCode}
                onChange={(e) => !isEdit && setResourceCode(e.target.value)}
                readOnly={isEdit}
                maxLength={50}
                placeholder="e.g. VNT-001 or MON-101"
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none font-mono uppercase"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  backgroundColor: isEdit
                    ? 'color-mix(in srgb, var(--color-secondary) 6%, var(--color-primary))'
                    : undefined,
                  cursor: isEdit ? 'not-allowed' : undefined,
                  opacity: isEdit ? 0.8 : 1,
                }}
                required
              />
              <p className="text-xs opacity-60 mt-1">
                {isEdit
                  ? 'Resource code is permanent and read-only.'
                  : 'Must be globally unique (e.g., VNT-001).'}
              </p>
            </div>

            <div>
              <label htmlFor="resource-name-input" className="block text-xs font-semibold mb-1">
                Resource Name <span className="text-red-500">*</span>
              </label>
              <input
                id="resource-name-input"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                maxLength={120}
                placeholder="e.g. Hamilton-C6 Ventilator"
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
                required
              />
            </div>
          </div>

          {/* Row 2: Category & Status */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3">
            <div>
              <label htmlFor="resource-category-select" className="block text-xs font-semibold mb-1">
                Category / Type <span className="text-red-500">*</span>
              </label>
              <select
                id="resource-category-select"
                value={category}
                onChange={(e) => setCategory(parseInt(e.target.value, 10))}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                {RESOURCE_CATEGORIES.map((cat) => (
                  <option key={cat.value} value={cat.value}>
                    {cat.label}
                  </option>
                ))}
              </select>
            </div>

            {isEdit ? (
              <div>
                <label htmlFor="resource-status-select" className="block text-xs font-semibold mb-1">
                  Operational Status <span className="text-red-500">*</span>
                </label>
                <select
                  id="resource-status-select"
                  value={status}
                  onChange={(e) => setStatus(parseInt(e.target.value, 10))}
                  className="w-full px-3 py-2 text-xs border rounded-lg outline-none bg-transparent"
                  style={{
                    borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                    color: 'var(--color-secondary)',
                  }}
                >
                  {RESOURCE_STATUSES.map((st) => (
                    <option key={st.value} value={st.value}>
                      {st.label}
                    </option>
                  ))}
                </select>
              </div>
            ) : (
              <div>
                <label className="block text-xs font-semibold mb-1">Initial Status</label>
                <div
                  className="w-full px-3 py-2 text-xs border rounded-lg opacity-80"
                  style={{
                    borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                    background: 'color-mix(in srgb, var(--color-secondary) 5%, var(--color-primary))',
                  }}
                >
                  <span className="font-semibold text-emerald-700">Available</span> (Default on creation)
                </div>
              </div>
            )}
          </div>

          {/* Row 3: Specifications (Serial, Manufacturer, Model) */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-3">
            <div>
              <label htmlFor="resource-serial-input" className="block text-xs font-semibold mb-1">
                Serial Number
              </label>
              <input
                id="resource-serial-input"
                type="text"
                value={serialNumber}
                onChange={(e) => setSerialNumber(e.target.value)}
                maxLength={100}
                placeholder="e.g. SN-884920"
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none font-mono"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
              />
            </div>

            <div>
              <label htmlFor="resource-manufacturer-input" className="block text-xs font-semibold mb-1">
                Manufacturer
              </label>
              <input
                id="resource-manufacturer-input"
                type="text"
                value={manufacturer}
                onChange={(e) => setManufacturer(e.target.value)}
                maxLength={100}
                placeholder="e.g. Philips, GE"
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
              />
            </div>

            <div>
              <label htmlFor="resource-model-input" className="block text-xs font-semibold mb-1">
                Model Number
              </label>
              <input
                id="resource-model-input"
                type="text"
                value={modelNumber}
                onChange={(e) => setModelNumber(e.target.value)}
                maxLength={100}
                placeholder="e.g. C6, MX800"
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                }}
              />
            </div>
          </div>

          {/* Location Description */}
          <div className="mb-3">
            <label htmlFor="resource-location-input" className="block text-xs font-semibold mb-1">
              Location Description <span className="text-red-500">*</span>
            </label>
            <input
              id="resource-location-input"
              type="text"
              value={locationDescription}
              onChange={(e) => setLocationDescription(e.target.value)}
              maxLength={200}
              placeholder="e.g. Central Storage Bay 2, or Mounted beside Bed A-101 BED-01"
              className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
              required
            />
          </div>

          {/* Optional Initial Location Assignment (Cascading Ward -> Room -> Bed) */}
          <div
            className="p-3 mb-3 rounded-xl border text-xs"
            style={{
              background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
              borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
            }}
          >
            <span className="block font-semibold mb-2" style={{ color: 'var(--color-accent)' }}>
              Physical Location Assignment (Optional)
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
                  <option value="">-- No Ward (Storage) --</option>
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
              </div>
            </div>
          </div>

          {/* Active Status (Edit Mode Only) */}
          {isEdit && (
            <div className="mb-4 pt-1">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={isActive}
                  disabled={resource.status === 'InUse'}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="rounded text-xs"
                />
                <span className="text-xs font-medium">Resource is Active in Inventory</span>
              </label>
              {resource.status === 'InUse' && (
                <p className="text-xs text-amber-700 mt-1">
                  Cannot deactivate an equipment item that is currently In Use.
                </p>
              )}
            </div>
          )}

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
              {submitting ? 'Saving...' : isEdit ? 'Save Changes' : '+ Register Resource'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
