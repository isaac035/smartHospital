import { useState } from 'react'
import { createPrescription } from '../../services/emrService'

export default function NewPrescriptionModal({ patientId, medicalRecordId = null, onClose, onSaved }) {
  const [items, setItems] = useState([
    { medicineName: '', dosage: '', route: 'Oral', frequency: 'Once daily', durationDays: 7, specialInstructions: '' },
  ])
  const [generalInstructions, setGeneralInstructions] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleAddItem = () => {
    setItems([
      ...items,
      { medicineName: '', dosage: '', route: 'Oral', frequency: 'Once daily', durationDays: 7, specialInstructions: '' },
    ])
  }

  const handleRemoveItem = (index) => {
    if (items.length > 1) {
      setItems(items.filter((_, i) => i !== index))
    }
  }

  const handleItemChange = (index, field, value) => {
    const updated = [...items]
    updated[index][field] = value
    setItems(updated)
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')

    // Validation
    for (let i = 0; i < items.length; i++) {
      const it = items[i]
      if (!it.medicineName.trim()) {
        setError(`Item #${i + 1}: Medicine name is required.`)
        return
      }
      if (!it.dosage.trim()) {
        setError(`Item #${i + 1}: Dosage is required.`)
        return
      }
      if (!it.frequency.trim()) {
        setError(`Item #${i + 1}: Frequency is required.`)
        return
      }
      if (!it.durationDays || Number(it.durationDays) < 1) {
        setError(`Item #${i + 1}: Duration must be at least 1 day.`)
        return
      }
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
      setError(
        err.response?.data?.message ||
        (err.response?.status === 403
          ? 'Unauthorized: You do not have permission to prescribe for this patient.'
          : 'Failed to issue prescription. Please check fields.')
      )
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
              placeholder="e.g. Take with meals, complete the full course of antibiotics"
              value={generalInstructions}
              onChange={(e) => setGeneralInstructions(e.target.value)}
            />
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
                        placeholder="e.g. Amoxicillin"
                        value={item.medicineName}
                        onChange={(e) => handleItemChange(index, 'medicineName', e.target.value)}
                        required
                      />
                    </div>
                    <div>
                      <label className="text-xs">Dosage *</label>
                      <input
                        placeholder="e.g. 500mg"
                        value={item.dosage}
                        onChange={(e) => handleItemChange(index, 'dosage', e.target.value)}
                        required
                      />
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
                        placeholder="e.g. Every 8 hours"
                        value={item.frequency}
                        onChange={(e) => handleItemChange(index, 'frequency', e.target.value)}
                        required
                      />
                    </div>
                    <div>
                      <label className="text-xs">Duration (days) *</label>
                      <input
                        type="number"
                        min="1"
                        max="365"
                        value={item.durationDays}
                        onChange={(e) => handleItemChange(index, 'durationDays', e.target.value)}
                        required
                      />
                    </div>
                  </div>

                  <div>
                    <label className="text-xs">Special Instructions</label>
                    <input
                      placeholder="e.g. Take after food, avoid dairy"
                      value={item.specialInstructions}
                      onChange={(e) => handleItemChange(index, 'specialInstructions', e.target.value)}
                    />
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
