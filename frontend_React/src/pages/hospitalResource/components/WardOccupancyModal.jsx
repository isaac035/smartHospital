import { useState, useEffect } from 'react'
import { getWardOccupancy } from '../../../services/hospitalResourceService'

export default function WardOccupancyModal({ isOpen, onClose, ward }) {
  const [occupancy, setOccupancy] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (isOpen && ward) {
      fetchOccupancy()
    }
  }, [isOpen, ward])

  const fetchOccupancy = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await getWardOccupancy(ward.id)
      setOccupancy(data)
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load ward occupancy details.')
    } finally {
      setLoading(false)
    }
  }

  if (!isOpen || !ward) return null

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
              Ward Occupancy
            </h2>
            <p className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
              {ward.name} ({ward.code}) • {ward.floor}
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

        {loading ? (
          <div className="p-8 text-center text-sm opacity-70">
            Loading ward occupancy data...
          </div>
        ) : occupancy ? (
          <div className="flex flex-col gap-4">
            {/* Occupancy Rate Banner */}
            <div
              className="p-4 rounded-xl border flex items-center justify-between"
              style={{
                background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))',
                borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
              }}
            >
              <div>
                <span className="text-xs font-semibold opacity-70 block">Occupancy Rate</span>
                <span className="text-2xl font-bold" style={{ color: 'var(--color-accent)' }}>
                  {occupancy.occupancyRate}%
                </span>
              </div>
              <div className="text-right">
                <span className="text-xs font-semibold opacity-70 block">Availability Status</span>
                {occupancy.isLowAvailability ? (
                  <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-red-100 text-red-800 border border-red-200">
                    Low Availability (&le; 20%)
                  </span>
                ) : (
                  <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
                    Adequate Availability
                  </span>
                )}
              </div>
            </div>

            {/* Bed Breakdown Grid */}
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
              <div className="p-3 rounded-lg border text-center" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <span className="text-xs opacity-70 block">Total Beds</span>
                <strong className="text-lg block mt-1">{occupancy.totalBeds}</strong>
              </div>

              <div className="p-3 rounded-lg border text-center" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <span className="text-xs opacity-70 block">Available</span>
                <strong className="text-lg block mt-1 text-emerald-700">{occupancy.availableBeds}</strong>
              </div>

              <div className="p-3 rounded-lg border text-center" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <span className="text-xs opacity-70 block">Occupied</span>
                <strong className="text-lg block mt-1 text-rose-700">{occupancy.occupiedBeds}</strong>
              </div>

              <div className="p-3 rounded-lg border text-center" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <span className="text-xs opacity-70 block">Maintenance</span>
                <strong className="text-lg block mt-1 text-amber-700">{occupancy.maintenanceBeds}</strong>
              </div>

              <div className="p-3 rounded-lg border text-center" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <span className="text-xs opacity-70 block">Blocked</span>
                <strong className="text-lg block mt-1 text-gray-700">{occupancy.blockedBeds}</strong>
              </div>

              <div className="p-3 rounded-lg border text-center" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <span className="text-xs opacity-70 block">Reserved</span>
                <strong className="text-lg block mt-1 text-indigo-700">{occupancy.reservedBeds}</strong>
              </div>
            </div>

            {/* Ward Details Meta */}
            <div className="p-3 rounded-lg text-xs grid grid-cols-2 gap-2" style={{ background: 'color-mix(in srgb, var(--color-secondary) 5%, var(--color-primary))' }}>
              <div>
                <span className="opacity-70 block">Ward Type:</span>
                <span className="font-semibold">{occupancy.wardType}</span>
              </div>
              <div>
                <span className="opacity-70 block">Max Capacity:</span>
                <span className="font-semibold">{occupancy.capacity} Beds</span>
              </div>
            </div>
          </div>
        ) : null}

        <div className="flex justify-end mt-5 pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <button
            type="button"
            onClick={onClose}
            className="secondary-button text-sm px-5 py-2"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  )
}
