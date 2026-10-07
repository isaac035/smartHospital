import { useState, useEffect } from 'react'
import {
  createMedicalResource,
  updateMedicalResource,
  getNextAvailableResourceCode,
} from '../../../services/hospitalResourceService'
import { useFormValidation } from '../../../hooks/useFormValidation'
import FieldError from '../../../components/common/FieldError'
import { selection, text } from '../../../utils/validators'

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
  onSuccess,
}) {
  const isEdit = Boolean(resource)

  const [resourceCode, setResourceCode] = useState('')
  const [loadingCode, setLoadingCode] = useState(false)
  const [name, setName] = useState('')
  const [category, setCategory] = useState(1)
  const [status, setStatus] = useState(1)
  const [locationDescription, setLocationDescription] = useState('')
  const [serialNumber, setSerialNumber] = useState('')

  const [isActive, setIsActive] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const formValues = { resourceCode, name, category, status, serialNumber, locationDescription }
  const validation = useFormValidation({
    resourceCode: (value) => text(value, 'Resource code', { isRequired: true, max: 50 }),
    name: (value) => text(value, 'Resource name', { isRequired: true, max: 120 }),
    category: (value) => selection(value, 'a category'),
    serialNumber: (value) => text(value, 'Serial number', { max: 100 }),
    locationDescription: (value) => text(value, 'Location description', { isRequired: true, max: 200 }),
  }, formValues)
  const resetValidation = validation.reset

  useEffect(() => {
    if (isOpen) {
      setError(null)
      resetValidation()
      if (resource) {
        setResourceCode(resource.resourceCode || '')
        setName(resource.name || '')
        setCategory(RESOURCE_CATEGORY_NAME_TO_INT[resource.category] || 1)
        setStatus(RESOURCE_STATUS_NAME_TO_INT[resource.status] || 1)
        setLocationDescription(resource.locationDescription || '')
        setSerialNumber(resource.serialNumber || '')
        setIsActive(resource.isActive !== false)
      } else {
        setResourceCode('')
        setName('')
        setCategory(1)
        setStatus(1)
        setLocationDescription('')
        setSerialNumber('')
        setIsActive(true)

        let isCancelled = false
        setLoadingCode(true)
        getNextAvailableResourceCode()
          .then((nextCode) => {
            if (!isCancelled) {
              setResourceCode(nextCode)
            }
          })
          .catch(() => {
            if (!isCancelled) {
              setResourceCode('')
              setError('Unable to generate Resource Code. Please try again.')
            }
          })
          .finally(() => {
            if (!isCancelled) {
              setLoadingCode(false)
            }
          })

        return () => {
          isCancelled = true
        }
      }
    }
  }, [isOpen, resource, resetValidation])

  if (!isOpen) return null

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    const trimmedCode = resourceCode.trim().toUpperCase()
    const trimmedName = name.trim()
    const trimmedLocation = locationDescription.trim()

    if (!validation.validateAll(formValues, e.currentTarget)) return

    try {
      setSubmitting(true)
      if (isEdit) {
        const payload = {
          name: trimmedName,
          category: Number(category),
          status: Number(status),
          locationDescription: trimmedLocation,
          serialNumber: serialNumber.trim() || null,
          manufacturer: resource?.manufacturer ?? null,
          modelNumber: resource?.modelNumber ?? null,
          wardId: resource?.wardId ?? null,
          roomId: resource?.roomId ?? null,
          bedId: resource?.bedId ?? null,
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
          manufacturer: null,
          modelNumber: null,
          wardId: null,
          roomId: null,
          bedId: null,
        }
        await createMedicalResource(payload)
        onSuccess(`Resource '${trimmedCode}' registered successfully.`)
      }
      onClose()
    } catch (err) {
      setError(validation.applyServerErrors(err, { conflicts: [{ match: /code/i, field: 'resourceCode' }], fallback: `Failed to ${isEdit ? 'update' : 'create'} resource.` }) || null)
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

        <form onSubmit={handleSubmit} noValidate>
          {/* Row 1: Resource Code & Name */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3">
            <div>
              <label htmlFor="resource-code-input" className="block text-xs font-semibold mb-1">
                Resource Code <span className="text-red-500">*</span>
              </label>
              <input
                id="resource-code-input"
                name="resourceCode"
                onBlur={() => validation.touch('resourceCode', formValues)}
                {...validation.fieldProps('resourceCode')}
                type="text"
                value={loadingCode ? 'Generating...' : resourceCode}
                readOnly
                maxLength={50}
                placeholder={!isEdit && !loadingCode && !resourceCode ? 'Unable to generate code' : undefined}
                className="w-full px-3 py-2 text-xs border rounded-lg outline-none font-mono uppercase"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  backgroundColor: 'color-mix(in srgb, var(--color-secondary) 6%, var(--color-primary))',
                  cursor: 'not-allowed',
                  opacity: 0.85,
                }}
                required
              />
              <FieldError name="resourceCode" message={validation.errorFor('resourceCode')} />
              <p className="text-xs opacity-60 mt-1">
                {isEdit
                  ? 'Resource code is permanent and read-only.'
                  : 'Auto-generated sequential code (read-only).'}
              </p>
            </div>

            <div>
              <label htmlFor="resource-name-input" className="block text-xs font-semibold mb-1">
                Resource Name <span className="text-red-500">*</span>
              </label>
              <input
                id="resource-name-input"
                name="name"
                onBlur={() => validation.touch('name', formValues)}
                {...validation.fieldProps('name')}
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
              <FieldError name="name" message={validation.errorFor('name')} />
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
                name="category"
                onBlur={() => validation.touch('category', formValues)}
                {...validation.fieldProps('category')}
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
              <FieldError name="category" message={validation.errorFor('category')} />
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

          {/* Row 3: Specifications (Serial Number) */}
          <div className="mb-3">
            <label htmlFor="resource-serial-input" className="block text-xs font-semibold mb-1">
              Serial Number <span className="text-xs font-normal opacity-60">(Optional)</span>
            </label>
            <input
              id="resource-serial-input"
                name="serialNumber"
                onBlur={() => validation.touch('serialNumber', formValues)}
                {...validation.fieldProps('serialNumber')}
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
              <FieldError name="serialNumber" message={validation.errorFor('serialNumber')} />
          </div>

          {/* Location Description */}
          <div className="mb-3">
            <label htmlFor="resource-location-input" className="block text-xs font-semibold mb-1">
              Location Description <span className="text-red-500">*</span>
            </label>
            <input
              id="resource-location-input"
                name="locationDescription"
                onBlur={() => validation.touch('locationDescription', formValues)}
                {...validation.fieldProps('locationDescription')}
              type="text"
              value={locationDescription}
              onChange={(e) => setLocationDescription(e.target.value)}
              maxLength={200}
              placeholder="e.g. Central Equipment Storage, Main Medical Store, ICU Equipment Store"
              className="w-full px-3 py-2 text-xs border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
              required
            />
              <FieldError name="locationDescription" message={validation.errorFor('locationDescription')} />
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
              disabled={submitting || (!isEdit && (loadingCode || !resourceCode.trim()))}
              className="primary-button text-xs px-5 py-2"
              style={{ marginTop: 0 }}
            >
              {submitting
                ? 'Saving...'
                : !isEdit && loadingCode
                ? 'Generating Code...'
                : isEdit
                ? 'Save Changes'
                : '+ Register Resource'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
