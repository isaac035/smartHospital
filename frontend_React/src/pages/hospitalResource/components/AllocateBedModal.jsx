import { useState, useEffect } from 'react'
import { getAvailableBeds, allocateBed } from '../../../services/hospitalResourceService'

export default function AllocateBedModal({ isOpen, onClose, admission, onSuccess }) {
  const [availableBeds, setAvailableBeds] = useState([])
  const [selectedBedId, setSelectedBedId] = useState('')
  const [notes, setNotes] = useState('')
  const [selectedWardFilter, setSelectedWardFilter] = useState('ALL')
  const [loadingBeds, setLoadingBeds] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen && admission) {
      fetchBeds()
      setSelectedBedId('')
      setNotes('')
      setError(null)
    }
  }, [isOpen, admission])

  const fetchBeds = async () => {
    try {
      setLoadingBeds(true)
      setError(null)
      const beds = await getAvailableBeds()
      setAvailableBeds(beds || [])
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load available beds.')
    } finally {
      setLoadingBeds(false)
    }
  }

  if (!isOpen || !admission) return null

  // Extract distinct ward names for easy filtering
  const distinctWards = Array.from(
    new Set(availableBeds.map((b) => b.wardName).filter(Boolean))
  ).sort()

  const filteredBeds = selectedWardFilter === 'ALL'
    ? availableBeds
    : availableBeds.filter((b) => b.wardName === selectedWardFilter)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    const bedId = parseInt(selectedBedId, 10)
    if (isNaN(bedId) || bedId <= 0) {
      setError('Please select an available bed.')
      return
    }

    try {
      setSubmitting(true)
      await allocateBed(admission.id, {
        admissionId: admission.id,
        bedId,
        notes: notes.trim() || null,
      })
      onSuccess(`Bed successfully allocated for admission ${admission.admissionNumber}.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to allocate bed.')
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
              Allocate Bed
            </h2>
            <p className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
              Assign an available hospital bed to patient {admission.patientName || `(Patient #${admission.patientId})`}.
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

        <div className="p-3 mb-4 rounded-lg text-xs grid grid-cols-2 gap-2" style={{ background: 'color-mix(in srgb, var(--color-accent) 6%, var(--color-primary))' }}>
          <div>
            <span className="font-semibold block opacity-70">Admission No:</span>
            <span className="font-mono font-medium">{admission.admissionNumber}</span>
          </div>
          <div>
            <span className="font-semibold block opacity-70">Priority:</span>
            <span className="font-medium">{admission.priority}</span>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          {distinctWards.length > 1 && (
            <div>
              <label htmlFor="allocate-ward-filter" className="block text-xs font-semibold mb-1">
                Filter by Ward
              </label>
              <select
                id="allocate-ward-filter"
                value={selectedWardFilter}
                onChange={(e) => setSelectedWardFilter(e.target.value)}
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                <option value="ALL">All Available Wards ({availableBeds.length} beds)</option>
                {distinctWards.map((w) => (
                  <option key={w} value={w}>
                    {w}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div>
            <label htmlFor="allocate-bed-select" className="block text-xs font-semibold mb-1">
              Select Available Bed <span className="text-red-500">*</span>
            </label>
            {loadingBeds ? (
              <div className="text-xs p-3 text-center opacity-70">Loading available beds...</div>
            ) : availableBeds.length === 0 ? (
              <div className="text-xs p-3 rounded text-red-600 bg-red-50 border border-red-200">
                No available beds found in the hospital. Please check Bed Management or discharge patients first.
              </div>
            ) : (
              <select
                id="allocate-bed-select"
                required
                value={selectedBedId}
                onChange={(e) => setSelectedBedId(e.target.value)}
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                <option value="">-- Choose an available bed --</option>
                {filteredBeds.map((bed) => (
                  <option key={bed.id} value={bed.id}>
                    {bed.wardName || 'Ward'} • Room {bed.roomNumber || '-'} • Bed {bed.bedNumber} ({bed.type})
                  </option>
                ))}
              </select>
            )}
          </div>

          <div>
            <label htmlFor="allocate-notes" className="block text-xs font-semibold mb-1">
              Allocation Notes <span className="text-xs font-normal opacity-70">(optional)</span>
            </label>
            <textarea
              id="allocate-notes"
              maxLength={500}
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="e.g. Near nurse station, telemetry required"
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            />
          </div>

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
              disabled={submitting || availableBeds.length === 0 || !selectedBedId}
              className="primary-button text-sm px-5 py-2"
              style={{ marginTop: 0 }}
            >
              {submitting ? 'Allocating...' : 'Confirm Allocation'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
