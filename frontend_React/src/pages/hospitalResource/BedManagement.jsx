import { useState, useEffect, useCallback } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'
import { adminNavigation } from '../admin/adminNavigation'
import { getBeds, getAllWards, getRoomsByWard } from '../../services/hospitalResourceService'
import BedFormModal from './components/BedFormModal'
import ChangeBedStatusModal from './components/ChangeBedStatusModal'
import DeactivateBedModal from './components/DeactivateBedModal'

const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function BedManagement() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNavigation : staffNav

  // Data states
  const [beds, setBeds] = useState([])
  const [wards, setWards] = useState([])
  const [filterRooms, setFilterRooms] = useState([])
  const [loading, setLoading] = useState(true)
  const [loadingRooms, setLoadingRooms] = useState(false)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  // Filter states
  const [wardFilter, setWardFilter] = useState('')
  const [roomFilter, setRoomFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  // Modal states
  const [showAddModal, setShowAddModal] = useState(false)
  const [editBedTarget, setEditBedTarget] = useState(null)
  const [statusBedTarget, setStatusBedTarget] = useState(null)
  const [deactivateBedTarget, setDeactivateBedTarget] = useState(null)

  // Fetch initial active wards once
  useEffect(() => {
    getAllWards({ isActive: true })
      .then((data) => setWards(Array.isArray(data) ? data : []))
      .catch(() => {})
  }, [])

  // Load rooms ONLY when a Ward is selected in the filter
  useEffect(() => {
    if (!wardFilter) {
      setFilterRooms([])
      setRoomFilter('')
      return
    }

    const fetchRoomsForFilter = async () => {
      try {
        setLoadingRooms(true)
        setRoomFilter('')
        const roomsData = await getRoomsByWard(wardFilter, { isActive: true })
        setFilterRooms(Array.isArray(roomsData) ? roomsData : [])
      } catch {
        setFilterRooms([])
      } finally {
        setLoadingRooms(false)
      }
    }

    fetchRoomsForFilter()
  }, [wardFilter])

  // Fetch beds based on active filters
  const fetchBedsData = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)

      const params = {
        pageSize: 100,
      }
      if (wardFilter) params.wardId = parseInt(wardFilter, 10)
      if (roomFilter) params.roomId = parseInt(roomFilter, 10)
      if (statusFilter) params.status = parseInt(statusFilter, 10)

      const bedsData = await getBeds(params)
      setBeds(Array.isArray(bedsData) ? bedsData : [])
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load hospital beds.')
    } finally {
      setLoading(false)
    }
  }, [wardFilter, roomFilter, statusFilter])

  useEffect(() => {
    fetchBedsData()
  }, [fetchBedsData])

  const handleResetFilters = () => {
    setWardFilter('')
    setRoomFilter('')
    setStatusFilter('')
  }

  const handleSuccessFeedback = (msg) => {
    setSuccessMessage(msg)
    fetchBedsData()
    setTimeout(() => {
      setSuccessMessage(null)
    }, 5000)
  }

  // Summary Metrics derived from loaded beds dataset
  const totalBeds = beds.length
  const availableBeds = beds.filter((b) => b.status?.toLowerCase() === 'available').length
  const occupiedBeds = beds.filter((b) => b.status?.toLowerCase() === 'occupied').length
  const maintenanceBeds = beds.filter((b) => b.status?.toLowerCase() === 'maintenance').length

  const renderStatusBadge = (status) => {
    const s = (status || '').toLowerCase()
    if (s === 'available') {
      return (
        <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
          Available
        </span>
      )
    }
    if (s === 'occupied') {
      return (
        <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-blue-100 text-blue-800 border border-blue-200">
          Occupied
        </span>
      )
    }
    if (s === 'maintenance') {
      return (
        <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
          Maintenance
        </span>
      )
    }
    if (s === 'reserved') {
      return (
        <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-purple-100 text-purple-800 border border-purple-200">
          Reserved
        </span>
      )
    }
    return (
      <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-700 border border-gray-300">
        {status || 'Blocked'}
      </span>
    )
  }

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Bed Management"
      subtitle="Monitor bed statuses, room allocation, and availability."
    >
      <ResourceNavigation />

      {/* Success Notification */}
      {successMessage && (
        <div className="mb-4 p-3 rounded-lg text-sm bg-emerald-50 text-emerald-800 border border-emerald-200 flex items-center justify-between">
          <span>{successMessage}</span>
          <button
            type="button"
            onClick={() => setSuccessMessage(null)}
            className="text-emerald-700 hover:text-emerald-900 font-bold ml-4"
          >
            &times;
          </button>
        </div>
      )}

      {/* Error Notification */}
      {error && (
        <div className="form-error mb-4 flex items-center justify-between">
          <span>{error}</span>
          <button
            type="button"
            onClick={() => setError(null)}
            className="font-bold ml-4"
          >
            &times;
          </button>
        </div>
      )}

      {/* Action Header */}
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6">
        <div>
          <h2 className="text-base font-bold" style={{ color: 'var(--color-accent)' }}>
            Hospital Bed Directory
          </h2>
          <p className="text-xs opacity-70 m-0">
            Track physical beds, operational status, room allocations, and availability.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={fetchBedsData}
            disabled={loading}
            className="secondary-button text-xs px-3 py-2"
          >
            {loading ? 'Refreshing...' : 'Refresh'}
          </button>
          {(role === 'Admin' || role === 'Staff') && (
            <button
              type="button"
              onClick={() => setShowAddModal(true)}
              className="primary-button text-xs px-4 py-2"
              style={{ marginTop: 0 }}
            >
              + Add Bed
            </button>
          )}
        </div>
      </div>

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <article className="stat-card">
          <p>Total Beds</p>
          <strong>{totalBeds}</strong>
          <span className="text-xs opacity-60">Configured in current view</span>
        </article>

        <article className="stat-card">
          <p>Available Beds</p>
          <strong style={{ color: '#0d7a42' }}>{availableBeds}</strong>
          <span className="text-xs opacity-60">Ready for admissions</span>
        </article>

        <article className="stat-card">
          <p>Occupied Beds</p>
          <strong style={{ color: '#1e40af' }}>{occupiedBeds}</strong>
          <span className="text-xs opacity-60">Currently assigned to patients</span>
        </article>

        <article className="stat-card">
          <p>Maintenance Beds</p>
          <strong style={{ color: '#d97706' }}>{maintenanceBeds}</strong>
          <span className="text-xs opacity-60">Servicing, cleaning, or repair</span>
        </article>
      </div>

      {/* Filters Bar */}
      <div
        className="p-4 mb-6 rounded-xl border flex flex-col gap-3"
        style={{
          background: 'var(--color-primary)',
          borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
        }}
      >
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {/* Ward Filter */}
          <div>
            <label htmlFor="filter-ward-select" className="block text-xs font-semibold mb-1">
              Filter by Ward
            </label>
            <select
              id="filter-ward-select"
              value={wardFilter}
              onChange={(e) => setWardFilter(e.target.value)}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Wards</option>
              {wards.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.code})
                </option>
              ))}
            </select>
          </div>

          {/* Room Filter (Only enabled after a Ward is selected) */}
          <div>
            <label htmlFor="filter-room-select" className="block text-xs font-semibold mb-1">
              Filter by Room
            </label>
            <select
              id="filter-room-select"
              value={roomFilter}
              onChange={(e) => setRoomFilter(e.target.value)}
              disabled={!wardFilter || loadingRooms}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
                opacity: !wardFilter || loadingRooms ? 0.6 : 1,
                cursor: !wardFilter || loadingRooms ? 'not-allowed' : 'pointer',
              }}
            >
              <option value="">
                {loadingRooms
                  ? 'Loading rooms...'
                  : !wardFilter
                  ? 'Select a ward first'
                  : filterRooms.length === 0
                  ? 'No rooms in this ward'
                  : 'All Rooms'}
              </option>
              {filterRooms.map((r) => (
                <option key={r.id} value={r.id}>
                  Room {r.roomNumber} ({r.type || 'General'})
                </option>
              ))}
            </select>
          </div>

          {/* Status Filter */}
          <div>
            <label htmlFor="filter-status-select" className="block text-xs font-semibold mb-1">
              Filter by Status
            </label>
            <select
              id="filter-status-select"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Statuses</option>
              <option value="1">Available</option>
              <option value="3">Occupied</option>
              <option value="4">Maintenance</option>
              <option value="2">Reserved</option>
              <option value="5">Blocked</option>
            </select>
          </div>
        </div>

        {(wardFilter || roomFilter || statusFilter) && (
          <div className="flex justify-end pt-1">
            <button
              type="button"
              onClick={handleResetFilters}
              className="text-xs font-semibold underline opacity-70 hover:opacity-100"
            >
              Reset Filters
            </button>
          </div>
        )}
      </div>

      {/* Bed Directory Table */}
      <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse" style={{ minWidth: '780px' }}>
            <thead>
              <tr
                style={{
                  borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
                  background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))',
                }}
              >
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Bed Number
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Ward
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Room
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Type
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Status
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Current Patient
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider text-right" style={{ color: 'var(--color-accent)' }}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="7" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    Loading hospital beds...
                  </td>
                </tr>
              ) : beds.length === 0 ? (
                <tr>
                  <td colSpan="7" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    No beds found matching the criteria.
                  </td>
                </tr>
              ) : (
                beds.map((bed) => {
                  const isOccupied = bed.status?.toLowerCase() === 'occupied'

                  return (
                    <tr
                      key={bed.id}
                      style={{
                        borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))',
                      }}
                    >
                      <td className="p-3 text-xs font-mono font-bold" style={{ color: 'var(--color-accent)' }}>
                        {bed.bedNumber}
                        {!bed.isActive && (
                          <span className="ml-2 text-xs font-normal text-red-600 bg-red-50 border border-red-200 rounded px-1.5 py-0.5">
                            Inactive
                          </span>
                        )}
                      </td>

                      <td className="p-3 text-xs">
                        <span className="font-semibold">{bed.wardName || '—'}</span>
                        {bed.floor && <span className="opacity-60 ml-1">· {bed.floor}</span>}
                      </td>

                      <td className="p-3 text-xs font-medium">
                        Room {bed.roomNumber || '—'}
                      </td>

                      <td className="p-3 text-xs">
                        <span className="inline-block px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700 border border-slate-200">
                          {bed.type}
                        </span>
                      </td>

                      <td className="p-3 text-xs">
                        {renderStatusBadge(bed.status)}
                      </td>

                      <td className="p-3 text-xs">
                        {isOccupied && bed.currentPatientName ? (
                          <div>
                            <span className="font-semibold text-emerald-800">
                              {bed.currentPatientName}
                            </span>
                            {bed.currentAdmissionId && (
                              <span className="opacity-60 text-xs block">
                                Adm #{bed.currentAdmissionId}
                              </span>
                            )}
                          </div>
                        ) : (
                          <span className="italic opacity-50">—</span>
                        )}
                      </td>

                      <td className="p-3 text-xs text-right whitespace-nowrap">
                        <div className="inline-flex items-center gap-1.5 justify-end">
                          <button
                            type="button"
                            onClick={() => setEditBedTarget(bed)}
                            className="secondary-button text-xs px-2.5 py-1"
                            title="Edit bed details"
                          >
                            Edit
                          </button>

                          <button
                            type="button"
                            onClick={() => setStatusBedTarget(bed)}
                            disabled={isOccupied}
                            className="secondary-button text-xs px-2.5 py-1"
                            style={{
                              opacity: isOccupied ? 0.5 : 1,
                              cursor: isOccupied ? 'not-allowed' : 'pointer',
                            }}
                            title={isOccupied ? 'Occupied bed status managed in Admissions' : 'Change status'}
                          >
                            Change Status
                          </button>

                          {role === 'Admin' && (
                            <button
                              type="button"
                              onClick={() => setDeactivateBedTarget(bed)}
                              disabled={isOccupied}
                              className="secondary-button text-xs px-2.5 py-1"
                              style={{
                                borderColor: '#dc2626',
                                color: '#dc2626',
                                opacity: isOccupied ? 0.5 : 1,
                                cursor: isOccupied ? 'not-allowed' : 'pointer',
                              }}
                              title={isOccupied ? 'Cannot deactivate occupied bed' : 'Deactivate bed'}
                            >
                              Deactivate
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modals */}
      <BedFormModal
        isOpen={showAddModal}
        onClose={() => setShowAddModal(false)}
        bed={null}
        wards={wards}
        onSuccess={handleSuccessFeedback}
      />

      <BedFormModal
        isOpen={Boolean(editBedTarget)}
        onClose={() => setEditBedTarget(null)}
        bed={editBedTarget}
        wards={wards}
        onSuccess={handleSuccessFeedback}
      />

      <ChangeBedStatusModal
        isOpen={Boolean(statusBedTarget)}
        onClose={() => setStatusBedTarget(null)}
        bed={statusBedTarget}
        onSuccess={handleSuccessFeedback}
      />

      <DeactivateBedModal
        isOpen={Boolean(deactivateBedTarget)}
        onClose={() => setDeactivateBedTarget(null)}
        bed={deactivateBedTarget}
        onSuccess={handleSuccessFeedback}
      />
    </DashboardLayout>
  )
}
