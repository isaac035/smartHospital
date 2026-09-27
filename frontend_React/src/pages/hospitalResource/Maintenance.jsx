import { useState, useEffect, useCallback } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'
import { adminNavigation } from '../admin/adminNavigation'
import {
  getMaintenanceRecords,
} from '../../services/hospitalResourceService'
import ScheduleMaintenanceModal from './components/ScheduleMaintenanceModal'
import CompleteMaintenanceModal from './components/CompleteMaintenanceModal'
import MaintenanceDetailsModal from './components/MaintenanceDetailsModal'

const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

// Type display mapping helper
const TYPE_LABELS = {
  RoutineInspection: 'Routine Inspection',
  CleaningAndSanitization: 'Cleaning & Sanitization',
  Repair: 'Repair',
  Calibration: 'Calibration',
  EmergencyRepair: 'Emergency Repair',
}

export default function Maintenance() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNavigation : staffNav

  const [records, setRecords] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  // Filters
  const [searchFilter, setSearchFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [targetTypeFilter, setTargetTypeFilter] = useState('')

  // Modals state
  const [showScheduleModal, setShowScheduleModal] = useState(false)
  const [completeTarget, setCompleteTarget] = useState(null)
  const [detailsTarget, setDetailsTarget] = useState(null)

  // Fetch maintenance records
  const fetchRecords = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await getMaintenanceRecords()
      setRecords(Array.isArray(data) ? data : [])
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load maintenance records.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchRecords()
  }, [fetchRecords])

  const handleSuccess = (msg) => {
    setSuccessMessage(msg)
    fetchRecords()
    setTimeout(() => setSuccessMessage(null), 6000)
  }

  const handleResetFilters = () => {
    setSearchFilter('')
    setStatusFilter('')
    setTargetTypeFilter('')
  }

  // Summary KPI Calculations
  const totalCount = records.length
  const scheduledCount = records.filter((r) => r.status === 'Scheduled').length
  const inProgressCount = records.filter((r) => r.status === 'InProgress').length
  const completedCount = records.filter((r) => r.status === 'Completed').length

  // Filtered Records
  const filteredRecords = records.filter((rec) => {
    if (statusFilter && rec.status !== statusFilter) return false
    if (targetTypeFilter && rec.targetType !== targetTypeFilter) return false

    if (searchFilter.trim()) {
      const term = searchFilter.trim().toLowerCase()
      const codeMatch = rec.maintenanceCode?.toLowerCase().includes(term)
      const targetMatch = rec.targetName?.toLowerCase().includes(term)
      const descMatch = rec.description?.toLowerCase().includes(term)
      const typeMatch = rec.type?.toLowerCase().includes(term)
      if (!codeMatch && !targetMatch && !descMatch && !typeMatch) return false
    }

    return true
  })

  // Date formatter
  const formatDateTime = (dateStr) => {
    if (!dateStr) return '—'
    try {
      const d = new Date(dateStr)
      return d.toLocaleString(undefined, {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    } catch {
      return dateStr
    }
  }

  // Status Badge Helper
  const getStatusBadge = (status) => {
    switch (status) {
      case 'Scheduled':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
            Scheduled
          </span>
        )
      case 'InProgress':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-blue-100 text-blue-800 border border-blue-200">
            In Progress
          </span>
        )
      case 'Completed':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
            Completed
          </span>
        )
      case 'Cancelled':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-rose-100 text-rose-800 border border-rose-200">
            Cancelled
          </span>
        )
      default:
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200">
            {status || 'Unknown'}
          </span>
        )
    }
  }

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Resource Maintenance"
      subtitle="Schedule, track, and resolve maintenance logs for beds and clinical equipment."
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
            Maintenance &amp; Inspection Workspace
          </h2>
          <p className="text-xs opacity-70 m-0">
            Manage routine checks, calibrations, sanitizations, and repairs across hospital assets.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={fetchRecords}
            disabled={loading}
            className="secondary-button text-xs px-3 py-2"
          >
            {loading ? 'Refreshing...' : 'Refresh'}
          </button>
          {(role === 'Admin' || role === 'Staff') && (
            <button
              type="button"
              onClick={() => setShowScheduleModal(true)}
              className="primary-button text-xs px-4 py-2"
              style={{ marginTop: 0 }}
            >
              + Schedule Maintenance
            </button>
          )}
        </div>
      </div>

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <article className="stat-card">
          <p>Total Records</p>
          <strong>{totalCount}</strong>
          <span className="text-xs opacity-60">All maintenance logs</span>
        </article>

        <article className="stat-card">
          <p>Scheduled</p>
          <strong style={{ color: '#d97706' }}>{scheduledCount}</strong>
          <span className="text-xs opacity-60">Awaiting start</span>
        </article>

        <article className="stat-card">
          <p>In Progress</p>
          <strong style={{ color: '#1e40af' }}>{inProgressCount}</strong>
          <span className="text-xs opacity-60">Currently underway</span>
        </article>

        <article className="stat-card">
          <p>Completed</p>
          <strong style={{ color: '#0d7a42' }}>{completedCount}</strong>
          <span className="text-xs opacity-60">Successfully serviced</span>
        </article>
      </div>

      {/* Filter & Search Bar */}
      <div
        className="p-4 mb-6 rounded-xl border flex flex-col gap-3"
        style={{
          background: 'var(--color-primary)',
          borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
        }}
      >
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {/* Search */}
          <div>
            <label htmlFor="filter-search-input" className="block text-xs font-semibold mb-1">
              Search Code / Target / Reason
            </label>
            <input
              id="filter-search-input"
              type="text"
              value={searchFilter}
              onChange={(e) => setSearchFilter(e.target.value)}
              placeholder="e.g. MNT-2026 or Ventilator or Bed"
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
            />
          </div>

          {/* Status Filter */}
          <div>
            <label htmlFor="filter-status-select" className="block text-xs font-semibold mb-1">
              Status
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
              <option value="Scheduled">Scheduled</option>
              <option value="InProgress">In Progress</option>
              <option value="Completed">Completed</option>
            </select>
          </div>

          {/* Target Type Filter */}
          <div>
            <label htmlFor="filter-type-select" className="block text-xs font-semibold mb-1">
              Target Resource Type
            </label>
            <select
              id="filter-type-select"
              value={targetTypeFilter}
              onChange={(e) => setTargetTypeFilter(e.target.value)}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Target Types</option>
              <option value="Bed">Hospital Beds</option>
              <option value="MedicalResource">Medical Equipment</option>
            </select>
          </div>
        </div>

        {/* Clear Filters Helper */}
        {(searchFilter || statusFilter || targetTypeFilter) && (
          <div className="flex justify-end pt-1">
            <button
              type="button"
              onClick={handleResetFilters}
              className="text-xs font-medium text-amber-700 hover:underline cursor-pointer"
            >
              Reset Filters
            </button>
          </div>
        )}
      </div>

      {/* Maintenance Table */}
      <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse" style={{ minWidth: '900px' }}>
            <thead>
              <tr
                style={{
                  borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
                  background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))',
                }}
              >
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Maintenance Code
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Target
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Maintenance Type
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Scheduled Start
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
                  <td colSpan="6" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    Loading maintenance records...
                  </td>
                </tr>
              ) : filteredRecords.length === 0 ? (
                <tr>
                  <td colSpan="6" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    No maintenance records found matching current criteria.
                  </td>
                </tr>
              ) : (
                filteredRecords.map((rec) => {
                  const isScheduled = rec.status === 'Scheduled'
                  const isInProgress = rec.status === 'InProgress'

                  return (
                    <tr
                      key={rec.id}
                      style={{
                        borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))',
                      }}
                    >
                      {/* Maintenance Code */}
                      <td className="p-3 text-xs font-mono font-bold" style={{ color: 'var(--color-accent)' }}>
                        {rec.maintenanceCode}
                      </td>

                      {/* Target */}
                      <td className="p-3 text-xs">
                        <div
                          className="text-sm font-semibold leading-snug break-words"
                          style={{ color: 'var(--color-accent)' }}
                        >
                          {rec.targetName}
                        </div>
                        <span className="inline-block mt-1 px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700 border border-slate-200">
                          {rec.targetType === 'Bed' ? 'Hospital Bed' : 'Medical Equipment'}
                        </span>
                      </td>

                      {/* Maintenance Type */}
                      <td className="p-3 text-xs font-medium">
                        {TYPE_LABELS[rec.type] || rec.type}
                      </td>

                      {/* Scheduled Start */}
                      <td className="p-3 text-xs font-mono">
                        {formatDateTime(rec.scheduledStart)}
                      </td>

                      {/* Status */}
                      <td className="p-3 text-xs">
                        {getStatusBadge(rec.status)}
                      </td>

                      {/* Actions */}
                      <td className="p-3 text-xs text-right whitespace-nowrap">
                        <div className="inline-flex items-center gap-1.5 justify-end">
                          {/* InProgress Actions: Complete & Details */}
                          {isInProgress && (role === 'Admin' || role === 'Staff') && (
                            <button
                              type="button"
                              onClick={() => setCompleteTarget(rec)}
                              className="secondary-button text-xs px-2.5 py-1"
                              style={{
                                color: '#0d7a42',
                                borderColor: 'color-mix(in srgb, #0d7a42 40%, transparent)',
                              }}
                              title="Complete maintenance and restore asset to Available"
                            >
                              Complete
                            </button>
                          )}

                          {/* Details (Available for all statuses) */}
                          <button
                            type="button"
                            onClick={() => setDetailsTarget(rec)}
                            className="secondary-button text-xs px-2.5 py-1"
                            title="View maintenance task details"
                          >
                            Details
                          </button>
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

      {/* Modal: Schedule Maintenance */}
      <ScheduleMaintenanceModal
        isOpen={showScheduleModal}
        onClose={() => setShowScheduleModal(false)}
        onSuccess={handleSuccess}
      />

      {/* Modal: Complete Maintenance */}
      <CompleteMaintenanceModal
        isOpen={Boolean(completeTarget)}
        onClose={() => setCompleteTarget(null)}
        record={completeTarget}
        onSuccess={handleSuccess}
      />

      {/* Modal: Maintenance Details */}
      <MaintenanceDetailsModal
        isOpen={Boolean(detailsTarget)}
        onClose={() => setDetailsTarget(null)}
        record={detailsTarget}
      />
    </DashboardLayout>
  )
}
