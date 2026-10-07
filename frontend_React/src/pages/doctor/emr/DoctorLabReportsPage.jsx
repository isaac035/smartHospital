import { useState, useEffect } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import DashboardLayout from '../../../layouts/DashboardLayout'
import { doctorNavigation } from '../doctorNavigation'
import { useAuth } from '../../../hooks/useAuth'
import { getPatientLabOrders, getPatientMedicalProfile } from '../../../services/emrService'
import LabOrdersList from '../../../components/emr/LabOrdersList'
import NewLabOrderModal from '../../../components/emr/NewLabOrderModal'
import RecordLabReportModal from '../../../components/emr/RecordLabReportModal'
import FieldError from '../../../components/common/FieldError'

const patientIdMessage = (value) => {
  const trimmed = String(value ?? '').trim()
  if (!trimmed) return 'Patient ID is required.'
  return /^[0-9]+$/.test(trimmed) && Number(trimmed) > 0 && Number(trimmed) <= 2147483647 ? '' : 'Patient ID must be a positive whole number.'
}

export default function DoctorLabReportsPage() {
  const { user } = useAuth()
  const isDoctorOrAdmin = user?.role === 'Doctor' || user?.role === 'Admin'
  const isStaff = user?.role === 'Staff'

  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const initialPatientId = searchParams.get('patientId') || ''
  const [inputPatientId, setInputPatientId] = useState(initialPatientId)
  const [patientIdError, setPatientIdError] = useState('')
  const [activePatientId, setActivePatientId] = useState(initialPatientId)

  const [profile, setProfile] = useState(null)
  const [labOrders, setLabOrders] = useState([])
  const [statusFilter, setStatusFilter] = useState('ALL')
  const [priorityFilter, setPriorityFilter] = useState('ALL')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [unauthorized, setUnauthorized] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')

  const [showNewOrderModal, setShowNewOrderModal] = useState(false)
  const [selectedOrderForReport, setSelectedOrderForReport] = useState(null)

  useEffect(() => {
    const pId = searchParams.get('patientId')
    if (pId && pId !== activePatientId) {
      setActivePatientId(pId)
      setInputPatientId(pId)
    }
  }, [searchParams]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (activePatientId) {
      fetchLabOrders(activePatientId)
    } else {
      setLabOrders([])
      setProfile(null)
      setError(null)
      setUnauthorized(false)
    }
  }, [activePatientId]) // eslint-disable-line react-hooks/exhaustive-deps

  const triggerSuccess = (msg) => {
    setSuccessMessage(msg)
    setTimeout(() => setSuccessMessage(''), 4500)
  }

  const fetchLabOrders = async (patientId) => {
    try {
      setLoading(true)
      setError(null)
      setUnauthorized(false)

      const id = Number(patientId)

      const [ordersRes, profileRes] = await Promise.allSettled([
        getPatientLabOrders(id),
        getPatientMedicalProfile(id),
      ])

      if (ordersRes.status === 'fulfilled') {
        setLabOrders(Array.isArray(ordersRes.value) ? ordersRes.value : [])
      } else {
        if (ordersRes.reason?.response?.status === 403) setUnauthorized(true)
        else setError(ordersRes.reason?.response?.data?.message || 'Failed to load laboratory orders.')
        setLabOrders([])
      }

      if (profileRes.status === 'fulfilled') {
        setProfile(profileRes.value)
      } else {
        setProfile(null)
      }
    } catch (err) {
      if (err.response?.status === 401 || err.response?.status === 403) {
        setUnauthorized(true)
      } else {
        setError(err.response?.data?.message || 'Failed to load laboratory orders.')
      }
    } finally {
      setLoading(false)
    }
  }

  const handleSearchSubmit = (e) => {
    e.preventDefault()
    const trimmed = inputPatientId.trim()
    const message = patientIdMessage(trimmed)
    setPatientIdError(message)
    if (message) {
      document.getElementById('searchPatientId')?.focus()
      return
    }
    if (trimmed) {
      setSearchParams({ patientId: trimmed })
      setActivePatientId(trimmed)
    }
  }

  const handleClear = () => {
    setInputPatientId('')
    setPatientIdError('')
    setActivePatientId('')
    setSearchParams({})
  }

  const filteredOrders = labOrders.filter((order) => {
    const matchStatus = statusFilter === 'ALL' || order.status?.toUpperCase() === statusFilter
    const matchPriority = priorityFilter === 'ALL' || order.priority?.toUpperCase() === priorityFilter
    return matchStatus && matchPriority
  })

  return (
    <DashboardLayout
      role={user?.role || 'Doctor'}
      navigation={doctorNavigation}
      title="Laboratory & Diagnostic Reports"
      subtitle="Order laboratory tests, inspect pathology/radiology findings, and manage clinical diagnostics."
    >
      {/* Patient Search Card */}
      <div className="panel mb-6" style={{ padding: 20 }}>
        <form onSubmit={handleSearchSubmit} noValidate className="flex flex-wrap items-center justify-between gap-4" style={{ margin: 0 }}>
          <div className="flex items-center gap-3 flex-1 min-w-[260px]">
            <label htmlFor="searchPatientId" className="font-bold text-sm whitespace-nowrap" style={{ color: 'var(--color-accent)' }}>
              Patient Lookup:
            </label>
            <input
              id="searchPatientId"
              type="number"
              placeholder="Enter Patient ID..."
              min="1"
              step="1"
              value={inputPatientId}
              aria-invalid={patientIdError ? 'true' : undefined}
              aria-describedby={patientIdError ? 'searchPatientId-error' : undefined}
              onChange={(e) => { setInputPatientId(e.target.value); if (patientIdError) setPatientIdError(patientIdMessage(e.target.value)) }}
              style={{ maxWidth: 260 }}
            />
            <button type="submit" className="primary-button" style={{ marginTop: 0 }}>
              Lookup
            </button>
            <FieldError name="searchPatientId" message={patientIdError} className="w-full" />
            {activePatientId && (
              <button type="button" className="secondary-button" style={{ marginTop: 0 }} onClick={handleClear}>
                Clear
              </button>
            )}
          </div>

          {activePatientId && (
            <div className="flex gap-2">
              <button
                type="button"
                className="secondary-button text-xs px-3 py-1.5"
                style={{ marginTop: 0 }}
                onClick={() => navigate(`/doctor/medical-records?patientId=${activePatientId}`)}
              >
                View Full EMR
              </button>
              {isDoctorOrAdmin && (
                <button
                  type="button"
                  className="primary-button text-xs px-3 py-1.5"
                  style={{ marginTop: 0 }}
                  onClick={() => setShowNewOrderModal(true)}
                >
                  + Order Lab Test
                </button>
              )}
            </div>
          )}
        </form>
      </div>

      {/* Success Notification */}
      {successMessage && (
        <div className="p-3.5 mb-6 rounded-lg bg-green-50 border border-green-300 text-green-900 text-sm font-semibold flex justify-between items-center" role="status">
          <div className="flex items-center gap-2">
            <span>✓</span>
            <span>{successMessage}</span>
          </div>
          <button type="button" className="link-button text-xs" onClick={() => setSuccessMessage('')}>
            ✕
          </button>
        </div>
      )}

      {/* Unauthorized State */}
      {unauthorized && (
        <div className="status-card mb-6" style={{ padding: 30, textAlign: 'center', background: '#fff' }}>
          <h2 className="text-red-700 font-bold mb-2">Access Forbidden (403)</h2>
          <p className="text-sm text-gray-600 mb-4">
            You do not have authorization to view or order lab reports for Patient #{activePatientId}.
          </p>
          <button className="secondary-button text-sm" onClick={handleClear}>
            Search Another Patient
          </button>
        </div>
      )}

      {/* Error State */}
      {error && !unauthorized && (
        <div className="form-error mb-6 flex justify-between items-center" role="alert">
          <span>{error}</span>
          <button type="button" className="link-button text-xs" onClick={() => fetchLabOrders(activePatientId)}>
            Retry
          </button>
        </div>
      )}

      {/* Initial Empty State */}
      {!activePatientId && (
        <div className="panel text-center py-16 px-6">
          <h3 className="text-xl font-bold mb-2" style={{ color: 'var(--color-accent)' }}>
            Diagnostic & Laboratory Workstation
          </h3>
          <p className="text-sm text-gray-500 max-w-md mx-auto">
            Please enter a Patient ID above to review ordered investigations, examine report findings, and request new diagnostic tests.
          </p>
        </div>
      )}

      {/* Patient Active Content */}
      {activePatientId && !unauthorized && (
        <div>
          {/* Patient Header Summary */}
          <div className="patient-banner mb-6">
            <div className="flex flex-wrap justify-between items-center gap-4">
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>
                    {profile?.patientName || `Patient #${activePatientId}`}
                  </h3>
                  <span className="badge badge-primary">{profile?.bloodGroup || 'Blood: Unknown'}</span>
                  <span className="text-xs px-2 py-0.5 rounded bg-gray-100 font-semibold">ID: #{activePatientId}</span>
                </div>
                <div className="text-xs text-gray-600 mt-1 flex gap-4">
                  {profile?.gender && <span>Gender: <strong>{profile.gender}</strong></span>}
                  {profile?.allergies && <span>Allergies: <strong className="text-red-600">{profile.allergies}</strong></span>}
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <div className="flex items-center gap-1.5">
                  <label className="text-xs font-bold text-gray-600">Status:</label>
                  <select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    className="p-1.5 text-xs border rounded-lg bg-white"
                  >
                    <option value="ALL">All ({labOrders.length})</option>
                    <option value="PENDING">Pending</option>
                    <option value="INPROGRESS">In Progress</option>
                    <option value="COMPLETED">Completed</option>
                  </select>
                </div>

                <div className="flex items-center gap-1.5">
                  <label className="text-xs font-bold text-gray-600">Priority:</label>
                  <select
                    value={priorityFilter}
                    onChange={(e) => setPriorityFilter(e.target.value)}
                    className="p-1.5 text-xs border rounded-lg bg-white"
                  >
                    <option value="ALL">All Priorities</option>
                    <option value="ROUTINE">Routine</option>
                    <option value="URGENT">Urgent</option>
                    <option value="STAT">Stat</option>
                  </select>
                </div>
              </div>
            </div>
          </div>

          {loading ? (
            <div className="panel py-10 text-center">
              <p className="placeholder-text font-semibold">Loading diagnostic orders for Patient #{activePatientId}...</p>
            </div>
          ) : (
            <LabOrdersList
              labOrders={filteredOrders}
              onNewOrder={isDoctorOrAdmin ? () => setShowNewOrderModal(true) : undefined}
              onRecordReport={isDoctorOrAdmin || isStaff ? (order) => setSelectedOrderForReport(order) : undefined}
            />
          )}
        </div>
      )}

      {/* New Lab Order Modal */}
      {showNewOrderModal && (
        <NewLabOrderModal
          patientId={activePatientId}
          onClose={() => setShowNewOrderModal(false)}
          onSaved={() => {
            setShowNewOrderModal(false)
            triggerSuccess('Laboratory order placed successfully.')
            fetchLabOrders(activePatientId)
          }}
        />
      )}

      {/* Record Lab Report Modal */}
      {selectedOrderForReport && (
        <RecordLabReportModal
          order={selectedOrderForReport}
          onClose={() => setSelectedOrderForReport(null)}
          onSaved={() => {
            setSelectedOrderForReport(null)
            triggerSuccess('Diagnostic report recorded successfully.')
            fetchLabOrders(activePatientId)
          }}
        />
      )}
    </DashboardLayout>
  )
}
