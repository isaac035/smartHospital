import { useState, useEffect, useCallback, useMemo } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'
import { adminNavigation } from '../admin/adminNavigation'
import { resourceAdminNavigation } from './resourceAdminNavigation'
import {
  getMedicalResources,
  getAllWards,
} from '../../services/hospitalResourceService'
import MedicalResourceFormModal, {
  RESOURCE_CATEGORIES,
  RESOURCE_STATUSES,
} from './components/MedicalResourceFormModal'
import AssignResourceModal from './components/AssignResourceModal'
import DeactivateResourceModal from './components/DeactivateResourceModal'
import EditIconButton from './components/EditIconButton'

const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

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

export default function MedicalResources() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'ResourceAdmin' ? resourceAdminNavigation : role === 'Admin' ? adminNavigation : staffNav

  // Data states:
  // Concept A: Full resources dataset (used ONLY for overall KPI calculations)
  const [fullResources, setFullResources] = useState([])
  // Concept B: Filtered resources dataset (used for table rows, result count, and search/filter display)
  const [resources, setResources] = useState([])
  const [wards, setWards] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  // Filters
  const [searchFilter, setSearchFilter] = useState('')
  const [debouncedSearchFilter, setDebouncedSearchFilter] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [wardFilter, setWardFilter] = useState('')

  // Debounce search input by ~400ms to avoid visible refresh/flicker while typing
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchFilter(searchFilter)
    }, 400)

    return () => clearTimeout(timer)
  }, [searchFilter])

  // Pagination states - Default 10 records per page
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)

  // Reset pagination to page 1 whenever any filter or search changes
  useEffect(() => {
    setCurrentPage(1)
  }, [categoryFilter, statusFilter, wardFilter, debouncedSearchFilter])

  // Modals state
  const [showAddModal, setShowAddModal] = useState(false)
  const [editTarget, setEditTarget] = useState(null)
  const [assignTarget, setAssignTarget] = useState(null)
  const [deactivateTarget, setDeactivateTarget] = useState(null)

  // Helper: Fetch complete unfiltered medical resources dataset for KPI metrics
  const fetchFullResources = useCallback(async () => {
    let all = []
    let pageNum = 1
    let hasMore = true
    while (hasMore) {
      const pageData = await getMedicalResources({ page: pageNum, pageSize: 100 })
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

  // Helper: Fetch filtered medical resources matching active query filters for table display
  const fetchFilteredResources = useCallback(async () => {
    const params = { pageSize: 100 }
    if (categoryFilter) params.category = parseInt(categoryFilter, 10)
    if (statusFilter) params.status = parseInt(statusFilter, 10)
    if (wardFilter) params.wardId = parseInt(wardFilter, 10)
    if (debouncedSearchFilter.trim()) params.search = debouncedSearchFilter.trim()

    let all = []
    let pageNum = 1
    let hasMore = true
    while (hasMore) {
      const pageData = await getMedicalResources({ ...params, page: pageNum, pageSize: 100 })
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
  }, [categoryFilter, statusFilter, wardFilter, debouncedSearchFilter])

  // Initial load for full resources dataset (KPIs) and wards
  useEffect(() => {
    let isCurrent = true
    const loadInitial = async () => {
      try {
        const [fullData, wardsData] = await Promise.all([
          fetchFullResources(),
          getAllWards().catch(() => []),
        ])
        if (isCurrent) {
          setFullResources(Array.isArray(fullData) ? fullData : [])
          if (Array.isArray(wardsData) && wardsData.length > 0) {
            setWards(wardsData)
          }
        }
      } catch (err) {
        if (isCurrent) {
          setError(err.response?.data?.message || 'Failed to load medical resources summary.')
        }
      }
    }

    loadInitial()

    return () => {
      isCurrent = false
    }
  }, [fetchFullResources])

  // Filtered resources loader (runs on mount and whenever any table filter or debounced search changes)
  useEffect(() => {
    let isCurrent = true
    const loadFiltered = async () => {
      try {
        setLoading(true)
        setError(null)
        const data = await fetchFilteredResources()
        if (isCurrent) {
          setResources(Array.isArray(data) ? data : [])
        }
      } catch (err) {
        if (isCurrent) {
          setError(err.response?.data?.message || 'Failed to load medical resources.')
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
  }, [fetchFilteredResources])

  // Refresh both full resources (KPIs) and filtered resources (table)
  const handleRefresh = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const [fullData, filteredData, wardsData] = await Promise.all([
        fetchFullResources(),
        fetchFilteredResources(),
        getAllWards().catch(() => wards),
      ])
      setFullResources(Array.isArray(fullData) ? fullData : [])
      setResources(Array.isArray(filteredData) ? filteredData : [])
      if (Array.isArray(wardsData) && wardsData.length > 0) {
        setWards(wardsData)
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to refresh medical resources.')
    } finally {
      setLoading(false)
    }
  }, [fetchFullResources, fetchFilteredResources, wards])

  const handleSuccess = (msg) => {
    setSuccessMessage(msg)
    handleRefresh()
    setTimeout(() => setSuccessMessage(null), 5000)
  }

  const handleResetFilters = () => {
    setSearchFilter('')
    setDebouncedSearchFilter('')
    setCategoryFilter('')
    setStatusFilter('')
    setWardFilter('')
    setCurrentPage(1)
  }

  // Summary KPI Calculations (calculated strictly from complete, unfiltered resources dataset)
  const totalResources = fullResources.length
  const availableResources = fullResources.filter(
    (r) => (r.status === 'Available' || r.status?.toLowerCase() === 'available') && r.isActive !== false
  ).length
  const inUseResources = fullResources.filter(
    (r) => r.status === 'InUse' || r.status === 'In Use' || r.status?.toLowerCase() === 'inuse'
  ).length
  const maintenanceResources = fullResources.filter(
    (r) =>
      r.status === 'Maintenance' ||
      r.status === 'OutOfService' ||
      r.status === 'Out of Service' ||
      r.status?.toLowerCase() === 'maintenance' ||
      r.status?.toLowerCase() === 'outofservice'
  ).length

  // Pagination calculations applied to currently filtered medical resources dataset
  const totalCount = resources.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const safePage = Math.min(currentPage, totalPages)

  const startIndex = (safePage - 1) * pageSize
  const endIndex = Math.min(startIndex + pageSize, totalCount)
  const paginatedResources = useMemo(() => {
    return resources.slice(startIndex, endIndex)
  }, [resources, startIndex, endIndex])

  // Status badge styling helper
  const getStatusBadge = (status) => {
    switch (status) {
      case 'Available':
        return (
          <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
            Available
          </span>
        )
      case 'InUse':
        return (
          <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-blue-100 text-blue-800 border border-blue-200">
            In Use
          </span>
        )
      case 'Maintenance':
        return (
          <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
            Maintenance
          </span>
        )
      case 'OutOfService':
        return (
          <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-rose-100 text-rose-800 border border-rose-200">
            Out of Service
          </span>
        )
      default:
        return (
          <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200">
            {status || 'Unknown'}
          </span>
        )
    }
  }

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Medical Resources"
      subtitle="Track medical equipment, ventilators, monitors, and devices."
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
            Hospital Equipment &amp; Resource Registry
          </h2>
          <p className="text-xs opacity-70 m-0">
            Manage physical devices, ventilators, monitors, and clinical location assignments.
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
          {(role === 'Admin' || role === 'Staff' || role === 'ResourceAdmin') && (
            <button
              type="button"
              onClick={() => setShowAddModal(true)}
              className="primary-button text-xs px-4 py-2"
              style={{ marginTop: 0 }}
            >
              + Add Resource
            </button>
          )}
        </div>
      </div>

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <article className="stat-card">
          <p>Total Resources</p>
          <strong>{totalResources}</strong>
          <span className="text-xs opacity-60">Registered equipment</span>
        </article>

        <article className="stat-card">
          <p>Available</p>
          <strong style={{ color: '#0d7a42' }}>{availableResources}</strong>
          <span className="text-xs opacity-60">Ready for clinical deployment</span>
        </article>

        <article className="stat-card">
          <p>In Use</p>
          <strong style={{ color: '#1e40af' }}>{inUseResources}</strong>
          <span className="text-xs opacity-60">Active in patient care</span>
        </article>

        <article className="stat-card">
          <p>Maintenance / Out of Service</p>
          <strong style={{ color: '#d97706' }}>{maintenanceResources}</strong>
          <span className="text-xs opacity-60">Under service or offline</span>
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
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          {/* Search */}
          <div>
            <label htmlFor="filter-search-input" className="block text-xs font-semibold mb-1">
              Search Code / Name / Location
            </label>
            <input
              id="filter-search-input"
              type="text"
              value={searchFilter}
              onChange={(e) => setSearchFilter(e.target.value)}
              placeholder="e.g. RES-001 or Ventilator"
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              }}
            />
          </div>

          {/* Category Filter */}
          <div>
            <label htmlFor="filter-category-select" className="block text-xs font-semibold mb-1">
              Category
            </label>
            <select
              id="filter-category-select"
              value={categoryFilter}
              onChange={(e) => setCategoryFilter(e.target.value)}
              className="w-full px-3 py-1.5 text-xs border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value="">All Categories</option>
              {RESOURCE_CATEGORIES.map((cat) => (
                <option key={cat.value} value={cat.value}>
                  {cat.label}
                </option>
              ))}
            </select>
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
              {RESOURCE_STATUSES.map((st) => (
                <option key={st.value} value={st.value}>
                  {st.label}
                </option>
              ))}
            </select>
          </div>

          {/* Ward Filter */}
          <div>
            <label htmlFor="filter-ward-select" className="block text-xs font-semibold mb-1">
              Assigned Ward
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
        </div>

        {/* Clear Filters Helper */}
        {(searchFilter || categoryFilter || statusFilter || wardFilter) && (
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

      {/* Resources Table */}
      <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="overflow-x-auto">
          <table
            className="w-full text-left border-collapse transition-opacity duration-150"
            style={{
              minWidth: '950px',
              opacity: loading && resources.length > 0 ? 0.65 : 1,
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
                  Resource Code
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Resource Name
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Serial Number
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Category
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Status
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Assigned Location
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                  Active
                </th>
                <th className="p-3 text-xs font-bold uppercase tracking-wider text-right" style={{ color: 'var(--color-accent)' }}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {loading && resources.length === 0 ? (
                <tr>
                  <td colSpan="8" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    Loading medical resources...
                  </td>
                </tr>
              ) : resources.length === 0 ? (
                <tr>
                  <td colSpan="8" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    No medical resources found matching current criteria.
                  </td>
                </tr>
              ) : (
                paginatedResources.map((res) => {
                  const isInUse = res.status === 'InUse'

                  return (
                    <tr
                      key={res.id}
                      style={{
                        borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))',
                      }}
                    >
                      {/* Resource Code */}
                      <td className="p-3 text-xs font-mono font-bold" style={{ color: 'var(--color-accent)' }}>
                        {res.resourceCode}
                      </td>

                      {/* Resource Name */}
                      <td className="p-3 text-xs font-semibold" style={{ color: 'var(--color-accent)' }}>
                        {res.name}
                      </td>

                      {/* Serial Number if available */}
                      <td className="p-3 text-xs font-mono">
                        {res.serialNumber ? (
                          <span>{res.serialNumber}</span>
                        ) : (
                          <span className="opacity-40">—</span>
                        )}
                      </td>

                      {/* Category */}
                      <td className="p-3 text-xs">
                        <span className="inline-block px-2.5 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700 border border-slate-200">
                          {res.category}
                        </span>
                      </td>

                      {/* Operational Status */}
                      <td className="p-3 text-xs">
                        {getStatusBadge(res.status)}
                      </td>

                      {/* Location */}
                      <td className="p-3 text-xs">
                        {res.wardName ? (
                          <div>
                            <span className="font-semibold" style={{ color: 'var(--color-accent)' }}>
                              {res.wardName}
                            </span>
                            {res.roomNumber && (
                              <span className="font-mono text-xs ml-1">· Room {res.roomNumber}</span>
                            )}
                            {res.bedNumber && (
                              <span className="font-mono text-xs ml-1 font-semibold text-blue-700">· {res.bedNumber}</span>
                            )}
                            <div className="text-xs opacity-60 mt-0.5">{res.locationDescription}</div>
                          </div>
                        ) : (
                          <div>
                            <span className="inline-block px-2 py-0.5 rounded text-xs font-medium bg-amber-50 text-amber-800 border border-amber-200">
                              Unassigned (Storage)
                            </span>
                            <div className="text-xs opacity-60 mt-0.5">{res.locationDescription}</div>
                          </div>
                        )}
                      </td>

                      {/* Active Status */}
                      <td className="p-3 text-xs">
                        {res.isActive ? (
                          <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
                            Active
                          </span>
                        ) : (
                          <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-600 border border-gray-300">
                            Inactive
                          </span>
                        )}
                      </td>

                      {/* Actions */}
                      <td className="p-3 text-xs text-right whitespace-nowrap">
                        <div className="inline-flex items-center gap-1.5 justify-end">
                          {(role === 'Admin' || role === 'Staff' || role === 'ResourceAdmin') && (
                            <>
                              <button
                                type="button"
                                onClick={() => setAssignTarget(res)}
                                className="secondary-button text-xs px-2.5 py-1"
                                title="Assign or relocate equipment location"
                              >
                                Assign
                              </button>
                              <EditIconButton
                                onClick={() => setEditTarget(res)}
                                title="Edit"
                                aria-label="Edit Medical Resource"
                              />
                            </>
                          )}

                          {(role === 'Admin' || role === 'ResourceAdmin') && res.isActive && (
                            <button
                              type="button"
                              onClick={() => setDeactivateTarget(res)}
                              disabled={isInUse}
                              className="secondary-button text-xs px-2.5 py-1"
                              style={{
                                borderColor: '#dc2626',
                                color: '#dc2626',
                                opacity: isInUse ? 0.5 : 1,
                                cursor: isInUse ? 'not-allowed' : 'pointer',
                              }}
                              title={
                                isInUse
                                  ? 'Cannot deactivate resource while In Use'
                                  : 'Deactivate resource'
                              }
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

        {/* Pagination & Result Summary Bar */}
        {totalCount > 0 && (
          <div
            className="flex flex-wrap items-center justify-between gap-3 p-3.5 border-t text-xs transition-opacity duration-150"
            style={{
              borderColor: 'color-mix(in srgb, var(--color-secondary) 12%, var(--color-primary))',
              background: 'color-mix(in srgb, var(--color-accent) 2%, var(--color-primary))',
              opacity: loading && resources.length > 0 ? 0.65 : 1,
            }}
          >
            <div className="flex items-center gap-3">
              <span className="font-medium opacity-75">
                Showing {startIndex + 1}–{endIndex} of {totalCount} resources
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

      {/* Modal: Add Medical Resource */}
      <MedicalResourceFormModal
        isOpen={showAddModal}
        onClose={() => setShowAddModal(false)}
        resource={null}
        onSuccess={handleSuccess}
      />

      {/* Modal: Edit Medical Resource */}
      <MedicalResourceFormModal
        isOpen={Boolean(editTarget)}
        onClose={() => setEditTarget(null)}
        resource={editTarget}
        onSuccess={handleSuccess}
      />

      {/* Modal: Assign / Relocate Resource */}
      <AssignResourceModal
        isOpen={Boolean(assignTarget)}
        onClose={() => setAssignTarget(null)}
        resource={assignTarget}
        wards={wards}
        onSuccess={handleSuccess}
      />

      {/* Modal: Deactivate Resource (Admin Only) */}
      <DeactivateResourceModal
        isOpen={Boolean(deactivateTarget)}
        onClose={() => setDeactivateTarget(null)}
        resource={deactivateTarget}
        onSuccess={handleSuccess}
      />
    </DashboardLayout>
  )
}
