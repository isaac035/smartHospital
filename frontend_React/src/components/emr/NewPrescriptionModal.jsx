import { useState } from 'react'
import { createPrescription } from '../../services/emrService'
import FieldError from '../common/FieldError'
import { number, parseServerErrors, text } from '../../utils/validators'

const ITEM_RULES = {
  medicineName: (value) => text(value, 'Medicine name', { isRequired: true, max: 150 }),
  dosage: (value) => text(value, 'Dosage', { isRequired: true, max: 50 }),
  route: (value) => text(value, 'Route', { max: 50 }),
  frequency: (value) => text(value, 'Frequency', { isRequired: true, max: 50 }),
  durationDays: (value) => number(value, 'Duration', { isRequired: true, min: 1, max: 365, integer: true, message: 'Duration must be between 1 and 365 days.' }),
  specialInstructions: (value) => text(value, 'Special instructions', { max: 500 }),
}

function validatePrescription(items, generalInstructions) {
  const errors = {}
  const general = text(generalInstructions, 'General instructions', { max: 1000 })
  if (general) errors.generalInstructions = general
  items.forEach((item, index) => {
    for (const [field, rule] of Object.entries(ITEM_RULES)) {
      const message = rule(item[field])
      if (message) errors[`items.${index}.${field}`] = message
    }
  })
  return errors
}

export default function NewPrescriptionModal({ patientId, medicalRecordId = null, onClose, onSaved }) {
  const [items, setItems] = useState([
    { medicineName: '', dosage: '', route: 'Oral', frequency: 'Once daily', durationDays: 7, specialInstructions: '' },
  ])
  const [generalInstructions, setGeneralInstructions] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState({})
  const [touched, setTouched] = useState({})
  const [submitted, setSubmitted] = useState(false)
  const fieldError = (key) => (submitted || touched[key] ? fieldErrors[key] : undefined)
  const touch = (key) => {
    setTouched((current) => ({ ...current, [key]: true }))
    setFieldErrors(validatePrescription(items, generalInstructions))
  }
  const invalid = (key) => (fieldError(key) ? 'true' : undefined)

  const handleAddItem = () => {
    setItems([
      ...items,
      { medicineName: '', dosage: '', route: 'Oral', frequency: 'Once daily', durationDays: 7, specialInstructions: '' },
    ])
  }

  const handleRemoveItem = (index) => {
    if (items.length > 1) {
      const remaining = items.filter((_, i) => i !== index)
      setItems(remaining)
      setTouched({})
      setFieldErrors(submitted ? validatePrescription(remaining, generalInstructions) : {})
    }
  }

  const handleItemChange = (index, field, value) => {
    const updated = items.map((item, i) => (i === index ? { ...item, [field]: value } : item))
    setItems(updated)
    if (submitted || touched[`items.${index}.${field}`]) setFieldErrors(validatePrescription(updated, generalInstructions))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')

    const errors = validatePrescription(items, generalInstructions)
    setFieldErrors(errors)
    setSubmitted(true)
    const firstInvalid = Object.keys(errors)[0]
    if (firstInvalid) {
      const element = e.currentTarget.elements.namedItem(firstInvalid)
      element?.scrollIntoView?.({ block: 'center', behavior: 'smooth' })
      element?.focus?.({ preventScroll: true })
      return
    }

    try {
      setSubmitting(true)
      const payload = {
        patientId: Number(patientId),
        medicalRecordId: medicalRecordId ? Number(medicalRecordId) : null,
        generalInstructions: generalInstructions.trim() || undefined,
        items: items.map((it) => ({
          medicineName: it.medicineName.trim(),
          dosage: it.dosage.trim(),
          route: it.route || 'Oral',
          frequency: it.frequency.trim(),
          durationDays: Number(it.durationDays),
          specialInstructions: it.specialInstructions ? it.specialInstructions.trim() : '',
        })),
      }

      await createPrescription(payload)
      onSaved()
    } catch (err) {
      if (err.response?.status === 403) {
        setError(err.response?.data?.message || 'Unauthorized: You do not have permission to prescribe for this patient.')
      } else {
        const parsed = parseServerErrors(err, { fallback: 'Failed to issue prescription. Please check fields.' })
        // Server keys look like "items[0].medicineName"; the form uses "items.0.medicineName".
        const mapped = Object.fromEntries(Object.entries(parsed.fieldErrors).map(([key, message]) => [key.replace(/\[(\d+)\]/g, '.$1'), message]))
        setFieldErrors((current) => ({ ...current, ...mapped }))
        setError(parsed.message)
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 750px)' }}>
        <h2>Prescribe Medication</h2>
        <form onSubmit={handleSubmit} noValidate>
          {error && <p className="form-error" role="alert">{error}</p>}

          <div>
            <label htmlFor="genInst">General Instructions</label>
            <input
              id="genInst"
              name="generalInstructions"
              placeholder="e.g. Take with meals, complete the full course of antibiotics"
              maxLength={1000}
              value={generalInstructions}
              aria-invalid={invalid('generalInstructions')}
              onBlur={() => touch('generalInstructions')}
              onChange={(e) => setGeneralInstructions(e.target.value)}
            />
            <FieldError name="generalInstructions" message={fieldError('generalInstructions')} />
          </div>

          <div className="mt-2">
            <div className="flex justify-between items-center mb-2">
              <label className="font-bold text-gray-700">Medications List ({items.length})</label>
              <button
                type="button"
                className="secondary-button text-xs px-2 py-1"
                style={{ marginTop: 0 }}
                onClick={handleAddItem}
              >
                + Add Another Medicine
              </button>
            </div>

            <div className="flex flex-col gap-3">
              {items.map((item, index) => (
                <div key={index} className="p-3 rounded-lg border bg-gray-50 flex flex-col gap-2">
                  <div className="flex justify-between items-center">
                    <span className="font-semibold text-xs text-blue-900">Medication #{index + 1}</span>
                    {items.length > 1 && (
                      <button
                        type="button"
                        className="text-xs text-red-600 hover:underline border-0 bg-transparent cursor-pointer"
                        onClick={() => handleRemoveItem(index)}
                      >
                        Remove
                      </button>
                    )}
                  </div>

                  <div className="field-row">
                    <div>
                      <label className="text-xs">Medicine Name *</label>
                      <input
                        name={`items.${index}.medicineName`}
                        placeholder="e.g. Amoxicillin"
                        maxLength={150}
                        value={item.medicineName}
                        aria-invalid={invalid(`items.${index}.medicineName`)}
                        onBlur={() => touch(`items.${index}.medicineName`)}
                        onChange={(e) => handleItemChange(index, 'medicineName', e.target.value)}
                        required
                      />
                      <FieldError name={`items.${index}.medicineName`.replace(/\./g, '-')} message={fieldError(`items.${index}.medicineName`)} />
                    </div>
                    <div>
                      <label className="text-xs">Dosage *</label>
                      <input
                        name={`items.${index}.dosage`}
                        placeholder="e.g. 500mg"
                        maxLength={50}
                        value={item.dosage}
                        aria-invalid={invalid(`items.${index}.dosage`)}
                        onBlur={() => touch(`items.${index}.dosage`)}
                        onChange={(e) => handleItemChange(index, 'dosage', e.target.value)}
                        required
                      />
                      <FieldError name={`items.${index}.dosage`.replace(/\./g, '-')} message={fieldError(`items.${index}.dosage`)} />
                    </div>
                  </div>

                  <div className="field-row">
                    <div>
                      <label className="text-xs">Route</label>
                      <select
                        className="w-full p-2.5 border rounded-lg bg-white"
                        value={item.route}
                        onChange={(e) => handleItemChange(index, 'route', e.target.value)}
                      >
                        <option value="Oral">Oral</option>
                        <option value="Intravenous (IV)">Intravenous (IV)</option>
                        <option value="Intramuscular (IM)">Intramuscular (IM)</option>
                        <option value="Subcutaneous">Subcutaneous</option>
                        <option value="Topical">Topical</option>
                        <option value="Inhalation">Inhalation</option>
                        <option value="Ophthalmic">Ophthalmic</option>
                      </select>
                    </div>
                    <div>
                      <label className="text-xs">Frequency *</label>
                      <input
                        name={`items.${index}.frequency`}
                        placeholder="e.g. Every 8 hours"
                        maxLength={50}
                        value={item.frequency}
                        aria-invalid={invalid(`items.${index}.frequency`)}
                        onBlur={() => touch(`items.${index}.frequency`)}
                        onChange={(e) => handleItemChange(index, 'frequency', e.target.value)}
                        required
                      />
                      <FieldError name={`items.${index}.frequency`.replace(/\./g, '-')} message={fieldError(`items.${index}.frequency`)} />
                    </div>
                    <div>
                      <label className="text-xs">Duration (days) *</label>
                      <input
                        type="number"
                        min="1"
                        max="365"
                        step="1"
                        name={`items.${index}.durationDays`}
                        value={item.durationDays}
                        aria-invalid={invalid(`items.${index}.durationDays`)}
                        onBlur={() => touch(`items.${index}.durationDays`)}
                        onChange={(e) => handleItemChange(index, 'durationDays', e.target.value)}
                        required
                      />
                      <FieldError name={`items.${index}.durationDays`.replace(/\./g, '-')} message={fieldError(`items.${index}.durationDays`)} />
                    </div>
                  </div>

                  <div>
                    <label className="text-xs">Special Instructions</label>
                    <input
                      name={`items.${index}.specialInstructions`}
                      placeholder="e.g. Take after food, avoid dairy"
                      maxLength={500}
                      value={item.specialInstructions}
                      aria-invalid={invalid(`items.${index}.specialInstructions`)}
                      onBlur={() => touch(`items.${index}.specialInstructions`)}
                      onChange={(e) => handleItemChange(index, 'specialInstructions', e.target.value)}
                    />
                    <FieldError name={`items.${index}.specialInstructions`.replace(/\./g, '-')} message={fieldError(`items.${index}.specialInstructions`)} />
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={submitting}>
              {submitting ? 'Issuing Prescription...' : 'Issue Prescription'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
