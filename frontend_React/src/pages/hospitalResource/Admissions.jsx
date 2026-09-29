import { useState, useEffect, useCallback, useMemo } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'
import { getAdmissions, getAllWards } from '../../services/hospitalResourceService'
import AdmitPatientModal from './components/AdmitPatientModal'
import AllocateBedModal from './components/AllocateBedModal'
import TransferPatientModal from './components/TransferPatientModal'
import DischargePatientModal from './components/DischargePatientModal'
import AdmissionDetailsModal from './components/AdmissionDetailsModal'
import EditAdmissionModal from './components/EditAdmissionModal'
import EditIconButton from './components/EditIconButton'
import { adminNavigation } from '../admin/adminNavigation'

const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

// Ranking for status sorting: 1. Admitted, 2. Discharged, 3. Cancelled
const getStatusRank = (status) => {
  const s = String(status || '').toLowerCase()
  if (s === 'admitted') return 1
  if (s === 'discharged') return 2
  if (s === 'cancelled') return 3
  return 4
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

export default function Admissions() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNavigation : staffNav

  // Data states:
  // Concept A: Full admissions dataset (used ONLY for overall KPI totals)
  const [fullAdmissions, setFullAdmissions] = useState([])
  // Concept B: Filtered admissions dataset (used for table rows, result count, and pagination)
  const [tableAdmissions, setTableAdmissions] = useState([])
  const [wards, setWards] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  // Filter states - Default to "1" (Admitted / Active)
  const [patientSearch, setPatientSearch] = useState('')
  const [debouncedPatientSearch, setDebouncedPatientSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('1')
  const [wardFilter, setWardFilter] = useState('')
  const [priorityFilter, setPriorityFilter] = useState('')

  // Debounce patient search by 400ms to prevent table flicker on every keystroke
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedPatientSearch((prev) => {
        if (prev !== patientSearch) {
          setCurrentPage(1)
          return patientSearch
        }
        return prev
      })
    }, 400)

    return () => clearTimeout(timer)
  }, [patientSearch])

  // Pagination states - Default 10 records per page
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)

  // Modal states
  const [showAdmitModal, setShowAdmitModal] = useState(false)
  const [allocateTarget, setAllocateTarget] = useState(null)
  const [transferTarget, setTransferTarget] = useState(null)
  const [dischargeTarget, setDischargeTarget] = useState(null)
  const [detailsTarget, setDetailsTarget] = useState(null)
  const [editTarget, setEditTarget] = useState(null)

  // Helper: Fetch full admissions dataset (unfiltered) for overall KPI totals
  const fetchFullAdmissions = useCallback(async () => {
    let all = []
    let pageNum = 1
    let hasMore = true
    while (hasMore) {
      const pageData = await getAdmissions({ page: pageNum, pageSize: 100 })
      if (Array.isArray(pageData) && pageData.length > 0) {
        all = all.concat(pageData)
        if (pageData.length < 100 || pageNum >= 20) {
          hasMore = false
        } else {
          pageNum++
        }
      } else {
        hasMore = false
      }
    }
    return all
  }, [])

  // Helper: Fetch filtered admissions matching active query filters for the table
  const fetchFilteredAdmissions = useCallback(async () => {
    const params = {}
    if (debouncedPatientSearch.trim()) params.patientSearch = debouncedPatientSearch.trim()
    if (statusFilter) params.status = parseInt(statusFilter, 10)
    if (wardFilter) params.wardId = parseInt(wardFilter, 10)
    if (priorityFilter) params.priority = parseInt(priorityFilter, 10)

    let all = []
    let pageNum = 1
    let hasMore = true
    while (hasMore) {
      const pageData = await getAdmissions({ ...params, page: pageNum, pageSize: 100 })
      if (Array.isArray(pageData) && pageData.length > 0) {
        all = all.concat(pageData)
        if (pageData.length < 100 || pageNum >= 20) {
          hasMore = false
        } else {
          pageNum++
        }
      } else {
        hasMore = false
      }
    }
    return all
  }, [debouncedPatientSearch, statusFilter, wardFilter, priorityFilter])

  // Initial load for full admissions dataset and wards (runs once on mount)
  useEffect(() => {
    let isCurrent = true
    const loadInitial = async () => {
      try {
        const [fullData, wardsData] = await Promise.all([
          fetchFullAdmissions(),
          getAllWards({ isActive: true }).catch(() => []),
        ])
        if (isCurrent) {
          setFullAdmissions(Array.isArray(fullData) ? fullData : [])
          if (Array.isArray(wardsData) && wardsData.length > 0) {
            setWards(wardsData)
          }
        }
      } catch (err) {
        if (isCurrent) {
          setError(err.response?.data?.message || 'Failed to load admissions summary.')
        }
      }
    }

    loadInitial()

    return () => {
      isCurrent = false
    }
  }, [fetchFullAdmissions])

  // Filtered admissions loader (runs on mount and whenever any table filter changes)
  useEffect(() => {
    let isCurrent = true
    const loadFiltered = async () => {
      try {
        setLoading(true)
        setError(null)
        const data = await fetchFilteredAdmissions()
        if (isCurrent) {
          setTableAdmissions(Array.isArray(data) ? data : [])
        }
      } catch (err) {
        if (isCurrent) {
          setError(err.response?.data?.message || 'Failed to load inpatient admissions.')
        }
      } finally {
        if (isCurrent) {
          setLoading(false)
        }
      }
    }

    loadFiltered()

    return () => {
      isCurrent = false
    }
  }, [fetchFilteredAdmissions])

  // Refresh both full admissions (KPIs) and filtered admissions (table)
  const handleRefresh = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const [fullData, filteredData, wardsData] = await Promise.all([
        fetchFullAdmissions(),
        fetchFilteredAdmissions(),
        wards.length === 0 ? getAllWards({ isActive: true }).catch(() => []) : Promise.resolve(wards),
      ])
      setFullAdmissions(Array.isArray(fullData) ? fullData : [])
      setTableAdmissions(Array.isArray(filteredData) ? filteredData : [])
      if (Array.isArray(wardsData) && wardsData.length > 0 && wards.length === 0) {
        setWards(wardsData)
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to refresh inpatient admissions.')
    } finally {
      setLoading(false)
    }
  }, [fetchFullAdmissions, fetchFilteredAdmissions, wards])

  const handleResetFilters = () => {
    setPatientSearch('')
    setDebouncedPatientSearch('')
    setStatusFilter('1')
    setWardFilter('')
    setPriorityFilter('')
    setCurrentPage(1)
  }

  const handleSuccessFeedback = (msg) => {
    setSuccessMessage(msg)
    handleRefresh()
    setTimeout(() => {
      setSuccessMessage(null)
    }, 5000)
  }

  // Summary Metrics (Calculated strictly from the full, unfiltered admissions dataset)
  const totalAdmissionsCount = fullAdmissions.length
  const activeAdmissionsCount = fullAdmissions.filter(
    (a) => a.status?.toLowerCase() === 'admitted'
  ).length
  const dischargedAdmissionsCount = fullAdmissions.filter(
    (a) => a.status?.toLowerCase() === 'discharged'
  ).length

  // Sort admissions:
  // For "All Statuses": 1. Admitted, 2. Discharged, 3. Cancelled.
  // Within each status group: newest AdmissionDate first.
  const sortedAdmissions = useMemo(() => {
    return [...tableAdmissions].sort((a, b) => {
      const rankA = getStatusRank(a.status)
      const rankB = getStatusRank(b.status)
      if (rankA !== rankB) {
        return rankA - rankB
      }
      const dateA = a.admissionDate ? new Date(a.admissionDate).getTime() : 0
      const dateB = b.admissionDate ? new Date(b.admissionDate).getTime() : 0
      return dateB - dateA
    })
  }, [tableAdmissions])

  // Pagination calculations applied to currently filtered and sorted dataset
  const totalCount = sortedAdmissions.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const safePage = Math.min(currentPage, totalPages)

  const startIndex = (safePage - 1) * pageSize
  const endIndex = Math.min(startIndex + pageSize, totalCount)
  const paginatedAdmissions = useMemo(() => {
    return sortedAdmissions.slice(startIndex, endIndex)
  }, [sortedAdmissions, startIndex, endIndex])

  const formatDate = (dateStr) => {
    if (!dateStr) return '—'
    try {
      const d = new Date(dateStr)
      return d.toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
      })
    } catch {
      return dateStr
    }
  }

  const renderPriorityBadge = (priority) => {
    const p = (priority || '').toLowerCase()
    if (p === 'emergency') {
      return (
        <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-red-100 text-red-800 border border-red-200">
          Emergency
        </span>
      )
    }
    if (p === 'urgent') {
      return (
        <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
          Urgent
        </span>
      )
    }
    return (
      <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200">
        {priority || 'Normal'}
      </span>
    )
  }

  const renderStatusBadge = (status) => {
    const s = (status || '').toLowerCase()
    if (s === 'admitted') {
      return (
        <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
          Admitted
        </span>
      )
    }
    if (s === 'discharged') {
      return (
        <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-700 border border-gray-300">
          Discharged
        </span>
      )
    }
    return (
      <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-red-50 text-red-700 border border-red-200">
        {status || 'Cancelled'}
      </span>
    )
  }

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Admissions"
      subtitle="Manage patient admissions, bed allocation, transfers, and discharge."
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
            Inpatient Admissions Directory
          </h2>
          <p className="text-xs opacity-70 m-0">
            Track active inpatient stays, ward assignments, and bed occupancy.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleRefresh}
            disabled={loading}
            className="secondary-button text-xs px-3 py-2"
          >
            {loading ? 'Refreshing...' : 'Refresh'}
          </button>
          <button
            type="button"
            onClick={() => setShowAdmitModal(true)}
            className="primary-button text-xs px-4 py-2"
            style={{ marginTop: 0 }}
          >
            + Admit Patient
          </button>
        </div>
      </div>

      {/* Summary KPI Cards (Overall totals from full dataset) */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <article className="stat-card">
          <p>Total Admissions</p>
          <strong>{totalAdmissionsCount}</strong>
          <span className="text-xs opacity-60">All admissions</span>
        </article>

        <article className="stat-card">
          <p>Active Admissions</p>
          <strong style={{ color: '#0d7a42' }}>{activeAdmissionsCount}</strong>
          <span className="text-xs opacity-60">Currently admitted</span>
        </article>

        <article className="stat-card">
          <p>Discharged Admissions</p>
          <strong style={{ color: '#4b5563' }}>{dischargedAdmissionsCount}</strong>
          <span className="text-xs opacity-60">Completed stays</span>
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
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          <div>
            <label htmlFor="filter-patient-search" className="block text-xs font-semibold mb-1">
              Search Patient (Name / Email / Phone)
            </label>
            <input
              id="filter-patient-search"
              type="text"
              value={patientSearch}
              onChange={(e) => setPatientSearch(e.target.value)}
              placeholder="e.g. John, john@mail.com"
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            />
          </div>

          <div>
            <label htmlFor="filter-status" className="block text-xs font-semibold mb-1">
              Admission Status
            </label>
            <select
              id="filter-status"
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value)
                setCurrentPage(1)
              }}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Statuses</option>
              <option value="1">Admitted (Active)</option>
              <option value="2">Discharged</option>
              <option value="3">Cancelled</option>
            </select>
          </div>

          <div>
            <label htmlFor="filter-ward" className="block text-xs font-semibold mb-1">
              Ward
            </label>
            <select
              id="filter-ward"
              value={wardFilter}
              onChange={(e) => {
                setWardFilter(e.target.value)
                setCurrentPage(1)
              }}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Wards</option>
              {wards.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="filter-priority" className="block text-xs font-semibold mb-1">
              Priority
            </label>
            <select
              id="filter-priority"
              value={priorityFilter}
              onChange={(e) => {
                setPriorityFilter(e.target.value)
                setCurrentPage(1)
              }}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Priorities</option>
              <option value="1">Normal</option>
              <option value="2">Urgent</option>
              <option value="3">Emergency</option>
            </select>
          </div>
        </div>

        {(patientSearch || statusFilter !== '1' || wardFilter || priorityFilter) && (
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

      {/* Admissions Table */}
      <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="overflow-x-auto">
          <table
            className="w-full text-left border-collapse transition-opacity duration-150"
            style={{
              minWidth: '850px',
              opacity: loading && tableAdmissions.length > 0 ? 0.65 : 1,
            }}
          >
            <thead>
              <tr
                style={{
                  borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
                  background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))',
                }}
              >
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Admission Number
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Patient
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Admission Date
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Priority
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Status
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Ward
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Room
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Bed
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider text-right" style={{ color: 'var(--color-accent)' }}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {loading && tableAdmissions.length === 0 ? (
                <tr>
                  <td colSpan="9" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    Loading admissions data...
                  </td>
                </tr>
              ) : paginatedAdmissions.length === 0 ? (
                <tr>
                  <td colSpan="9" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    No admissions found for the selected filters.
                  </td>
                </tr>
              ) : (
                paginatedAdmissions.map((admission) => {
                  const isActive = admission.status?.toLowerCase() === 'admitted'
                  const hasBed = Boolean(admission.activeBedId)

                  return (
                    <tr
                      key={admission.id}
                      style={{
                        borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))',
                      }}
                    >
                      <td className="p-3 text-xs font-mono font-medium" style={{ color: 'var(--color-accent)' }}>
                        {admission.admissionNumber}
                      </td>

                      <td className="p-3 text-xs">
                        <div className="font-semibold text-sm">
                          {admission.patientName || `Patient #${admission.patientId}`}
                        </div>
                        {admission.patientPhone && (
                          <div className="opacity-60 text-xs">{admission.patientPhone}</div>
                        )}
                      </td>

                      <td className="p-3 text-xs">
                        {formatDate(admission.admissionDate)}
                      </td>

                      <td className="p-3 text-xs">
                        {renderPriorityBadge(admission.priority)}
                      </td>

                      <td className="p-3 text-xs">
                        {renderStatusBadge(admission.status)}
                      </td>

                      <td className="p-3 text-xs">
                        {admission.activeWardName || '—'}
                      </td>

                      <td className="p-3 text-xs">
                        {admission.activeRoomNumber || '—'}
                      </td>

                      <td className="p-3 text-xs">
                        {admission.activeBedNumber ? (
                          <span className="font-semibold text-emerald-700">
                            Bed {admission.activeBedNumber}
                          </span>
                        ) : (
                          <span className="italic opacity-60">No bed</span>
                        )}
                      </td>

                      <td className="p-3 text-xs text-right whitespace-nowrap">
                        <div className="inline-flex items-center gap-1.5 justify-end">
                          <button
                            type="button"
                            onClick={() => setDetailsTarget(admission)}
                            className="secondary-button text-xs px-2 py-1"
                            title="View full admission details"
                          >
                            Details
                          </button>

                          {isActive && (
                            <EditIconButton
                              onClick={() => setEditTarget(admission)}
                              title="Edit"
                              aria-label="Edit Admission"
                            />
                          )}

                          {isActive && !hasBed && (role === 'Admin' || role === 'Staff') && (
                            <button
                              type="button"
                              onClick={() => setAllocateTarget(admission)}
                              className="primary-button text-xs px-2 py-1"
                              style={{ marginTop: 0 }}
                              title="Allocate bed to patient"
                            >
                              Allocate Bed
                            </button>
                          )}

                          {isActive && hasBed && (role === 'Admin' || role === 'Staff') && (
                            <button
                              type="button"
                              onClick={() => setTransferTarget(admission)}
                              className="secondary-button text-xs px-2 py-1"
                              style={{ borderColor: '#d97706', color: '#b45309' }}
                              title="Transfer patient to another bed"
                            >
                              Transfer
                            </button>
                          )}

                          {isActive && (
                            <button
                              type="button"
                              onClick={() => setDischargeTarget(admission)}
                              className="secondary-button text-xs px-2 py-1"
                              style={{ borderColor: '#b91c1c', color: '#b91c1c' }}
                              title="Discharge patient and free bed"
                            >
                              Discharge
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

        {/* Pagination & Result Summary Bar */}
        {totalCount > 0 && (
          <div
            className="flex flex-wrap items-center justify-between gap-3 p-3.5 border-t text-xs transition-opacity duration-150"
            style={{
              borderColor: 'color-mix(in srgb, var(--color-secondary) 12%, var(--color-primary))',
              background: 'color-mix(in srgb, var(--color-accent) 2%, var(--color-primary))',
              opacity: loading && tableAdmissions.length > 0 ? 0.65 : 1,
            }}
          >
            <div className="flex items-center gap-3">
              <span className="font-medium opacity-75">
                Showing {startIndex + 1}–{endIndex} of {totalCount} admissions
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
                      className="text-xs px-2.5 py-1 rounded font-medium transition-colors"
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

      {/* Modal Dialogs */}
      <AdmitPatientModal
        isOpen={showAdmitModal}
        onClose={() => setShowAdmitModal(false)}
        onSuccess={handleSuccessFeedback}
      />

      <AllocateBedModal
        isOpen={Boolean(allocateTarget)}
        onClose={() => setAllocateTarget(null)}
        admission={allocateTarget}
        onSuccess={handleSuccessFeedback}
      />

      <TransferPatientModal
        isOpen={Boolean(transferTarget)}
        onClose={() => setTransferTarget(null)}
        admission={transferTarget}
        onSuccess={handleSuccessFeedback}
      />

      <DischargePatientModal
        isOpen={Boolean(dischargeTarget)}
        onClose={() => setDischargeTarget(null)}
        admission={dischargeTarget}
        onSuccess={handleSuccessFeedback}
      />

      <AdmissionDetailsModal
        isOpen={Boolean(detailsTarget)}
        onClose={() => setDetailsTarget(null)}
        admission={detailsTarget}
      />

      <EditAdmissionModal
        isOpen={Boolean(editTarget)}
        onClose={() => setEditTarget(null)}
        admission={editTarget}
        onSuccess={handleSuccessFeedback}
      />
    </DashboardLayout>
  )
}
