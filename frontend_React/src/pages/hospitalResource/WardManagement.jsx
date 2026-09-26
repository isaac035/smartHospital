import { useState, useEffect, useMemo } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'
import { getAllWards } from '../../services/hospitalResourceService'
import WardFormModal from './components/WardFormModal'
import WardOccupancyModal from './components/WardOccupancyModal'
import DeactivateWardModal from './components/DeactivateWardModal'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

const WARD_TYPE_OPTIONS = [
  'General',
  'ICU',
  'Emergency',
  'Maternity',
  'Pediatric',
  'Surgical',
  'Isolation',
]

export default function WardManagement() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  const [wards, setWards] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  // Modals
  const [showAddModal, setShowAddModal] = useState(false)
  const [editWardTarget, setEditWardTarget] = useState(null)
  const [occupancyTarget, setOccupancyTarget] = useState(null)
  const [deactivateTarget, setDeactivateTarget] = useState(null)

  useEffect(() => {
    fetchWards()
  }, [])

  const fetchWards = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await getAllWards()
      setWards(Array.isArray(data) ? data : [])
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load hospital wards.')
    } finally {
      setLoading(false)
    }
  }

  const handleSuccess = (msg) => {
    setSuccessMessage(msg)
    fetchWards()
    setTimeout(() => {
      setSuccessMessage(null)
    }, 5000)
  }

  const handleResetFilters = () => {
    setSearchTerm('')
    setTypeFilter('')
    setStatusFilter('')
  }

  // Summary counts derived directly from loaded wards
  const totalWards = wards.length
  const activeWards = wards.filter((w) => w.isActive).length
  const inactiveWards = wards.filter((w) => !w.isActive).length

  // Filtered wards (client-side)
  const filteredWards = useMemo(() => {
    return wards.filter((ward) => {
      const term = searchTerm.trim().toLowerCase()
      const matchesSearch =
        !term ||
        ward.name?.toLowerCase().includes(term) ||
        ward.code?.toLowerCase().includes(term)

      const matchesType = !typeFilter || ward.type?.toLowerCase() === typeFilter.toLowerCase()

      const matchesStatus =
        !statusFilter ||
        (statusFilter === 'active' && ward.isActive) ||
        (statusFilter === 'inactive' && !ward.isActive)

      return matchesSearch && matchesType && matchesStatus
    })
  }, [wards, searchTerm, typeFilter, statusFilter])

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Ward Management"
      subtitle="Manage hospital wards, capacity, floor, type, and availability."
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
            Hospital Ward Directory
          </h2>
          <p className="text-xs opacity-70 m-0">
            Configure department wards, bed capacity, and monitor room allocation.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={fetchWards}
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
              + Add Ward
            </button>
          )}
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <article className="stat-card">
          <p>Total Wards</p>
          <strong>{totalWards}</strong>
          <span className="text-xs opacity-60">Configured in hospital</span>
        </article>

        <article className="stat-card">
          <p>Active Wards</p>
          <strong style={{ color: '#0d7a42' }}>{activeWards}</strong>
          <span className="text-xs opacity-60">Operational wards</span>
        </article>

        <article className="stat-card">
          <p>Inactive Wards</p>
          <strong style={{ color: '#6b7280' }}>{inactiveWards}</strong>
          <span className="text-xs opacity-60">Deactivated or off-duty</span>
        </article>
      </div>

      {/* Filter / Search Bar */}
      <div
        className="p-4 mb-6 rounded-xl border flex flex-col gap-3"
        style={{
          background: 'var(--color-primary)',
          borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
        }}
      >
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div>
            <label htmlFor="filter-ward-search" className="block text-xs font-semibold mb-1">
              Search by Ward Name / Code
            </label>
            <input
              id="filter-ward-search"
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="e.g. Cardiology, GWA"
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            />
          </div>

          <div>
            <label htmlFor="filter-ward-type" className="block text-xs font-semibold mb-1">
              Ward Type
            </label>
            <select
              id="filter-ward-type"
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Types</option>
              {WARD_TYPE_OPTIONS.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="filter-ward-status" className="block text-xs font-semibold mb-1">
              Status
            </label>
            <select
              id="filter-ward-status"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Statuses</option>
              <option value="active">Active Wards</option>
              <option value="inactive">Inactive Wards</option>
            </select>
          </div>
        </div>

        {(searchTerm || typeFilter || statusFilter) && (
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

      {/* Wards Table */}
      <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse" style={{ minWidth: '800px' }}>
            <thead>
              <tr
                style={{
                  borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
                  background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))',
                }}
              >
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Name
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Code
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Type
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Floor
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Block
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Capacity
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Available Beds / Total Beds
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Status
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider text-right" style={{ color: 'var(--color-accent)' }}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="9" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    Loading hospital wards...
                  </td>
                </tr>
              ) : filteredWards.length === 0 ? (
                <tr>
                  <td colSpan="9" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    No wards found matching the criteria.
                  </td>
                </tr>
              ) : (
                filteredWards.map((ward) => (
                  <tr
                    key={ward.id}
                    style={{
                      borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))',
                    }}
                  >
                    <td className="p-3 text-sm font-semibold" style={{ color: 'var(--color-accent)' }}>
                      {ward.name}
                    </td>

                    <td className="p-3 text-xs font-mono font-medium">
                      {ward.code}
                    </td>

                    <td className="p-3 text-xs">
                      {ward.type}
                    </td>

                    <td className="p-3 text-xs">
                      {ward.floor}
                    </td>

                    <td className="p-3 text-xs">
                      {ward.buildingBlock || '—'}
                    </td>

                    <td className="p-3 text-xs font-medium">
                      {ward.capacity} beds
                    </td>

                    <td className="p-3 text-xs font-medium">
                      <span className="font-semibold" style={{ color: ward.availableBeds > 0 ? '#0d7a42' : '#c43a1a' }}>
                        {ward.availableBeds}
                      </span>
                      <span className="opacity-60"> / {ward.totalBeds}</span>
                    </td>

                    <td className="p-3 text-xs">
                      {ward.isActive ? (
                        <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
                          Active
                        </span>
                      ) : (
                        <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-600 border border-gray-300">
                          Inactive
                        </span>
                      )}
                    </td>

                    <td className="p-3 text-xs text-right whitespace-nowrap">
                      <div className="inline-flex items-center gap-1.5 justify-end">
                        <button
                          type="button"
                          onClick={() => setOccupancyTarget(ward)}
                          className="secondary-button text-xs px-2.5 py-1"
                          title="View detailed bed occupancy"
                        >
                          Occupancy
                        </button>

                        {(role === 'Admin' || role === 'Staff') && (
                          <button
                            type="button"
                            onClick={() => setEditWardTarget(ward)}
                            className="secondary-button text-xs px-2.5 py-1"
                            title="Edit ward configuration"
                          >
                            Edit
                          </button>
                        )}

                        {role === 'Admin' && ward.isActive && (
                          <button
                            type="button"
                            onClick={() => setDeactivateTarget(ward)}
                            className="secondary-button text-xs px-2.5 py-1"
                            style={{ borderColor: '#dc2626', color: '#dc2626' }}
                            title="Deactivate ward"
                          >
                            Deactivate
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal: Add Ward */}
      <WardFormModal
        isOpen={showAddModal}
        onClose={() => setShowAddModal(false)}
        ward={null}
        existingCodes={wards.map((w) => w.code)}
        onSuccess={handleSuccess}
      />

      {/* Modal: Edit Ward */}
      <WardFormModal
        isOpen={Boolean(editWardTarget)}
        onClose={() => setEditWardTarget(null)}
        ward={editWardTarget}
        existingCodes={wards.filter((w) => w.id !== editWardTarget?.id).map((w) => w.code)}
        onSuccess={handleSuccess}
      />


      {/* Modal: Ward Occupancy */}
      <WardOccupancyModal
        isOpen={Boolean(occupancyTarget)}
        onClose={() => setOccupancyTarget(null)}
        ward={occupancyTarget}
      />

      {/* Modal: Deactivate Ward */}
      <DeactivateWardModal
        isOpen={Boolean(deactivateTarget)}
        onClose={() => setDeactivateTarget(null)}
        ward={deactivateTarget}
        onSuccess={handleSuccess}
      />
    </DashboardLayout>
  )
}
