import { useState, useEffect, useCallback } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'
import { getAdmissions, getAllWards } from '../../services/hospitalResourceService'
import AdmitPatientModal from './components/AdmitPatientModal'
import AllocateBedModal from './components/AllocateBedModal'
import TransferPatientModal from './components/TransferPatientModal'
import DischargePatientModal from './components/DischargePatientModal'
import AdmissionDetailsModal from './components/AdmissionDetailsModal'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function Admissions() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  // Data states
  const [admissions, setAdmissions] = useState([])
  const [wards, setWards] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  // Filter states
  const [patientSearch, setPatientSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [wardFilter, setWardFilter] = useState('')
  const [priorityFilter, setPriorityFilter] = useState('')

  // Modal states
  const [showAdmitModal, setShowAdmitModal] = useState(false)
  const [allocateTarget, setAllocateTarget] = useState(null)
  const [transferTarget, setTransferTarget] = useState(null)
  const [dischargeTarget, setDischargeTarget] = useState(null)
  const [detailsTarget, setDetailsTarget] = useState(null)

  const fetchAdmissionsData = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)

      const params = {
        pageSize: 50,
      }
      if (patientSearch.trim()) params.patientSearch = patientSearch.trim()
      if (statusFilter) params.status = parseInt(statusFilter, 10)
      if (wardFilter) params.wardId = parseInt(wardFilter, 10)
      if (priorityFilter) params.priority = parseInt(priorityFilter, 10)

      const [admissionsData, wardsData] = await Promise.all([
        getAdmissions(params),
        wards.length === 0 ? getAllWards({ isActive: true }).catch(() => []) : Promise.resolve(wards),
      ])

      setAdmissions(Array.isArray(admissionsData) ? admissionsData : [])
      if (Array.isArray(wardsData) && wardsData.length > 0 && wards.length === 0) {
        setWards(wardsData)
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load inpatient admissions.')
    } finally {
      setLoading(false)
    }
  }, [patientSearch, statusFilter, wardFilter, priorityFilter, wards])

  useEffect(() => {
    fetchAdmissionsData()
  }, [fetchAdmissionsData])

  const handleResetFilters = () => {
    setPatientSearch('')
    setStatusFilter('')
    setWardFilter('')
    setPriorityFilter('')
  }

  const handleSuccessFeedback = (msg) => {
    setSuccessMessage(msg)
    fetchAdmissionsData()
    setTimeout(() => {
      setSuccessMessage(null)
    }, 5000)
  }

  // Summary Metrics (Derived from currently loaded admissions list)
  const totalLoaded = admissions.length
  const activeCount = admissions.filter((a) => a.status?.toLowerCase() === 'admitted').length
  const dischargedCount = admissions.filter((a) => a.status?.toLowerCase() === 'discharged').length

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
            onClick={fetchAdmissionsData}
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

      {/* Summary KPI Cards (Carefully labeled as loaded dataset) */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <article className="stat-card">
          <p>Total Admissions</p>
          <strong>{totalLoaded}</strong>
          <span className="text-xs opacity-60">Loaded in current view</span>
        </article>

        <article className="stat-card">
          <p>Active Admissions</p>
          <strong style={{ color: '#0d7a42' }}>{activeCount}</strong>
          <span className="text-xs opacity-60">Currently admitted in view</span>
        </article>

        <article className="stat-card">
          <p>Discharged Admissions</p>
          <strong style={{ color: '#4b5563' }}>{dischargedCount}</strong>
          <span className="text-xs opacity-60">Completed stays in view</span>
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
              onChange={(e) => setStatusFilter(e.target.value)}
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
              onChange={(e) => setPriorityFilter(e.target.value)}
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

        {(patientSearch || statusFilter || wardFilter || priorityFilter) && (
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
          <table className="w-full text-left border-collapse" style={{ minWidth: '850px' }}>
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
              {loading ? (
                <tr>
                  <td colSpan="9" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    Loading admissions data...
                  </td>
                </tr>
              ) : admissions.length === 0 ? (
                <tr>
                  <td colSpan="9" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    No admissions found matching the criteria.
                  </td>
                </tr>
              ) : (
                admissions.map((admission) => {
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
    </DashboardLayout>
  )
}
