import { useState, useEffect, useCallback, useMemo } from 'react'
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

// Generate pagination numbers with ellipsis when needed
function getPageNumbers(current, total) {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1)
  }
  if (current <= 4) {
    return [1, 2, 3, 4, 5, '...', total]
  }
  if (current >= total - 3) {
    return [1, '...', total - 4, total - 3, total - 2, total - 1, total]
  }
  return [1, '...', current - 1, current, current + 1, '...', total]
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
  const [debouncedSearchFilter, setDebouncedSearchFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [targetTypeFilter, setTargetTypeFilter] = useState('')

  // 400ms debounce on search input to prevent rapid recalculations and flicker
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearchFilter(searchFilter)
    }, 400)
    return () => clearTimeout(handler)
  }, [searchFilter])

  // Pagination states - Default 10 records per page
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)

  // Reset pagination to page 1 whenever any filter or search changes
  useEffect(() => {
    setCurrentPage(1)
  }, [statusFilter, targetTypeFilter, debouncedSearchFilter])

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
    setDebouncedSearchFilter('')
    setStatusFilter('')
    setTargetTypeFilter('')
    setCurrentPage(1)
  }

  // Summary KPI Calculations
  const totalCount = records.length
  const scheduledCount = records.filter((r) => r.status === 'Scheduled').length
  const inProgressCount = records.filter((r) => r.status === 'InProgress').length
  const completedCount = records.filter((r) => r.status === 'Completed').length

  // Filtered Records supporting multi-token human-friendly search
  const filteredRecords = records.filter((rec) => {
    if (statusFilter && rec.status !== statusFilter) return false
    if (targetTypeFilter && rec.targetType !== targetTypeFilter) return false

    const query = debouncedSearchFilter.trim().toLowerCase()
    if (query) {
      const tokens = query.split(/\s+/).filter(Boolean)

      // Build composite searchable string containing all human-friendly information
      const searchableFields = [
        rec.maintenanceCode,
        rec.targetName,
        rec.wardName,
        rec.roomNumber ? `Room ${rec.roomNumber}` : '',
        rec.roomNumber,
        rec.bedNumber ? `Bed ${rec.bedNumber}` : '',
        rec.bedNumber,
        rec.medicalResourceName,
        rec.medicalResourceCode,
        rec.locationDescription,
        rec.type,
        TYPE_LABELS[rec.type] || '',
        rec.description,
        rec.resolutionNotes,
      ]

      const searchableText = searchableFields
        .filter(Boolean)
        .join(' ')
        .toLowerCase()

      // Every token in the query must match somewhere in the composite text
      const matchesAllTokens = tokens.every((token) => searchableText.includes(token))
      if (!matchesAllTokens) return false
    }

    return true
  })

  // Pagination calculations applied to currently filtered records dataset
  const filteredTotalCount = filteredRecords.length
  const totalPages = Math.max(1, Math.ceil(filteredTotalCount / pageSize))
  const safePage = Math.min(currentPage, totalPages)

  const startIndex = (safePage - 1) * pageSize
  const endIndex = Math.min(startIndex + pageSize, filteredTotalCount)
  const paginatedRecords = useMemo(() => {
    return filteredRecords.slice(startIndex, endIndex)
  }, [filteredRecords, startIndex, endIndex])

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
              Search Maintenance
            </label>
            <input
              id="filter-search-input"
              type="text"
              value={searchFilter}
              onChange={(e) => setSearchFilter(e.target.value)}
              placeholder="Search by ward, room, bed, resource, type or reason"
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
                paginatedRecords.map((rec) => {
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
                          {rec.targetType === 'Bed'
                            ? (rec.targetName?.startsWith('Bed') ? rec.targetName : `Bed ${rec.bedNumber || rec.targetName}`)
                            : rec.targetName}
                        </div>

                        {rec.targetType === 'Bed' && (rec.wardName || rec.roomNumber) && (
                          <div className="text-xs font-normal mt-0.5 opacity-80" style={{ color: 'var(--color-secondary)' }}>
                            {[rec.wardName, rec.roomNumber ? `Room ${rec.roomNumber}` : null]
                              .filter(Boolean)
                              .join(' • ')}
                          </div>
                        )}

                        {rec.targetType === 'MedicalResource' && (rec.wardName || rec.roomNumber || rec.locationDescription) && (
                          <div className="text-xs font-normal mt-0.5 opacity-80" style={{ color: 'var(--color-secondary)' }}>
                            {[rec.wardName, rec.roomNumber ? `Room ${rec.roomNumber}` : null, rec.locationDescription]
                              .filter(Boolean)
                              .join(' • ')}
                          </div>
                        )}

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

        {/* Pagination & Result Summary Bar */}
        {filteredTotalCount > 0 && (
          <div
            className="flex flex-wrap items-center justify-between gap-3 p-3.5 border-t text-xs transition-opacity duration-150"
            style={{
              borderColor: 'color-mix(in srgb, var(--color-secondary) 12%, var(--color-primary))',
              background: 'color-mix(in srgb, var(--color-accent) 2%, var(--color-primary))',
              opacity: loading && filteredRecords.length > 0 ? 0.65 : 1,
            }}
          >
            <div className="flex items-center gap-3">
              <span className="font-medium opacity-75">
                Showing {startIndex + 1}–{endIndex} of {filteredTotalCount} records
              </span>
              <div className="flex items-center gap-1.5 text-xs">
                <span className="opacity-60">Rows:</span>
                <select
                  value={pageSize}
                  onChange={(e) => {
                    setPageSize(Number(e.target.value))
                    setCurrentPage(1)
                  }}
                  className="px-2 py-1 text-xs border rounded bg-transparent outline-none cursor-pointer"
                  style={{
                    borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                    color: 'var(--color-secondary)',
                  }}
                  aria-label="Rows per page"
                >
                  <option value={10}>10</option>
                  <option value={20}>20</option>
                  <option value={50}>50</option>
                </select>
              </div>
            </div>

            <div className="flex items-center gap-1.5">
              <button
                type="button"
                disabled={safePage <= 1}
                onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                className="secondary-button text-xs px-2.5 py-1 disabled:opacity-40 disabled:cursor-not-allowed"
                style={{ marginTop: 0 }}
              >
                Previous
              </button>

              <div className="flex items-center gap-1">
                {getPageNumbers(safePage, totalPages).map((p, idx) =>
                  p === '...' ? (
                    <span key={`ellipsis-${idx}`} className="px-1.5 py-1 text-xs opacity-50">
                      ...
                    </span>
                  ) : (
                    <button
                      key={`page-${p}`}
                      type="button"
                      onClick={() => setCurrentPage(p)}
                      className="text-xs px-2.5 py-1 rounded font-medium transition-colors cursor-pointer"
                      style={{
                        background:
                          p === safePage
                            ? 'var(--color-accent)'
                            : 'transparent',
                        color:
                          p === safePage
                            ? 'var(--color-primary)'
                            : 'var(--color-secondary)',
                        border:
                          p === safePage
                            ? '1px solid var(--color-accent)'
                            : '1px solid color-mix(in srgb, var(--color-secondary) 20%, var(--color-primary))',
                      }}
                    >
                      {p}
                    </button>
                  )
                )}
              </div>

              <button
                type="button"
                disabled={safePage >= totalPages}
                onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                className="secondary-button text-xs px-2.5 py-1 disabled:opacity-40 disabled:cursor-not-allowed"
                style={{ marginTop: 0 }}
              >
                Next
              </button>
            </div>
          </div>
        )}
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
