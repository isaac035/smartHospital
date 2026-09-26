import { useState, useEffect } from 'react'
import { getAvailableBeds, transferPatient } from '../../../services/hospitalResourceService'

export default function TransferPatientModal({ isOpen, onClose, admission, onSuccess }) {
  const [availableBeds, setAvailableBeds] = useState([])
  const [selectedBedId, setSelectedBedId] = useState('')
  const [transferReason, setTransferReason] = useState('')
  const [selectedWardFilter, setSelectedWardFilter] = useState('ALL')
  const [loadingBeds, setLoadingBeds] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen && admission) {
      fetchBeds()
      setSelectedBedId('')
      setTransferReason('')
      setError(null)
    }
  }, [isOpen, admission])

  const fetchBeds = async () => {
    try {
      setLoadingBeds(true)
      setError(null)
      const beds = await getAvailableBeds()
      // Filter out the currently assigned bed
      const validBeds = (beds || []).filter((b) => b.id !== admission?.activeBedId)
      setAvailableBeds(validBeds)
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load available destination beds.')
    } finally {
      setLoadingBeds(false)
    }
  }

  if (!isOpen || !admission) return null

  const distinctWards = Array.from(
    new Set(availableBeds.map((b) => b.wardName).filter(Boolean))
  ).sort()

  const filteredBeds = selectedWardFilter === 'ALL'
    ? availableBeds
    : availableBeds.filter((b) => b.wardName === selectedWardFilter)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    const newBedId = parseInt(selectedBedId, 10)
    if (isNaN(newBedId) || newBedId <= 0) {
      setError('Please select a destination bed.')
      return
    }

    if (newBedId === admission.activeBedId) {
      setError('The destination bed cannot be the same as the current bed.')
      return
    }

    if (!transferReason.trim()) {
      setError('Transfer reason is required.')
      return
    }

    try {
      setSubmitting(true)
      await transferPatient(admission.id, {
        newBedId,
        transferReason: transferReason.trim(),
      })
      onSuccess(`Patient successfully transferred to new bed for admission ${admission.admissionNumber}.`)
      onClose()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to transfer patient.')
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
              Transfer Patient
            </h2>
            <p className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
              Transfer {admission.patientName || `Patient #${admission.patientId}`} to another available bed.
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

        {/* Current Allocation Details */}
        <div className="p-3 mb-4 rounded-lg text-xs grid grid-cols-2 gap-2" style={{ background: 'color-mix(in srgb, var(--color-accent) 6%, var(--color-primary))' }}>
          <div>
            <span className="font-semibold block opacity-70">Admission:</span>
            <span className="font-mono font-medium">{admission.admissionNumber}</span>
          </div>
          <div>
            <span className="font-semibold block opacity-70">Current Ward / Bed:</span>
            <span className="font-medium text-amber-700">
              {admission.activeWardName || 'Ward'} • Room {admission.activeRoomNumber || '-'} • Bed {admission.activeBedNumber || '-'}
            </span>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          {distinctWards.length > 1 && (
            <div>
              <label htmlFor="transfer-ward-filter" className="block text-xs font-semibold mb-1">
                Filter Destination by Ward
              </label>
              <select
                id="transfer-ward-filter"
                value={selectedWardFilter}
                onChange={(e) => setSelectedWardFilter(e.target.value)}
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                <option value="ALL">All Available Wards ({availableBeds.length} destination beds)</option>
                {distinctWards.map((w) => (
                  <option key={w} value={w}>
                    {w}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div>
            <label htmlFor="transfer-bed-select" className="block text-xs font-semibold mb-1">
              Select Destination Bed <span className="text-red-500">*</span>
            </label>
            {loadingBeds ? (
              <div className="text-xs p-3 text-center opacity-70">Loading available destination beds...</div>
            ) : availableBeds.length === 0 ? (
              <div className="text-xs p-3 rounded text-amber-800 bg-amber-50 border border-amber-200">
                No alternative available beds found. Destination bed must be available and different from the current bed.
              </div>
            ) : (
              <select
                id="transfer-bed-select"
                required
                value={selectedBedId}
                onChange={(e) => setSelectedBedId(e.target.value)}
                className="w-full px-3 py-2 text-sm border rounded-lg outline-none bg-transparent"
                style={{
                  borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                  color: 'var(--color-secondary)',
                }}
              >
                <option value="">-- Choose destination bed --</option>
                {filteredBeds.map((bed) => (
                  <option key={bed.id} value={bed.id}>
                    {bed.wardName || 'Ward'} • Room {bed.roomNumber || '-'} • Bed {bed.bedNumber} ({bed.type})
                  </option>
                ))}
              </select>
            )}
          </div>

          <div>
            <label htmlFor="transfer-reason" className="block text-xs font-semibold mb-1">
              Transfer Reason <span className="text-red-500">*</span>
            </label>
            <textarea
              id="transfer-reason"
              required
              maxLength={500}
              rows={3}
              value={transferReason}
              onChange={(e) => setTransferReason(e.target.value)}
              placeholder="Clinical reason or operational requirement for transferring patient (max 500 characters)"
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
              {submitting ? 'Transferring...' : 'Execute Transfer'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
