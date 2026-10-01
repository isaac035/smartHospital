import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation } from './adminNavigation'
import { useAuth } from '../../hooks/useAuth'
import { getPatientLabOrders, getPatientMedicalProfile } from '../../services/emrService'
import LabOrdersList from '../../components/emr/LabOrdersList'
import RecordLabReportModal from '../../components/emr/RecordLabReportModal'
import NewLabOrderModal from '../../components/emr/NewLabOrderModal'

export default function AdminReportsPage() {
  const { user } = useAuth()

  const [searchParams, setSearchParams] = useSearchParams()

  const initialPatientId = searchParams.get('patientId') || ''
  const [inputPatientId, setInputPatientId] = useState(initialPatientId)
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
    if (trimmed) {
      setSearchParams({ patientId: trimmed })
      setActivePatientId(trimmed)
    }
  }

  const handleClear = () => {
    setInputPatientId('')
    setActivePatientId('')
    setSearchParams({})
    setLabOrders([])
    setProfile(null)
    setError(null)
    setUnauthorized(false)
    setStatusFilter('ALL')
    setPriorityFilter('ALL')
  }

  const filteredOrders = labOrders.filter((order) => {
    const matchStatus = statusFilter === 'ALL' || order.status?.toUpperCase() === statusFilter
    const matchPriority = priorityFilter === 'ALL' || order.priority?.toUpperCase() === priorityFilter
    return matchStatus && matchPriority
  })

  return (
    <DashboardLayout
      role={user?.role || 'Admin'}
      navigation={adminNavigation}
      title="Laboratory & Diagnostic Reports"
      subtitle="Search by patient ID to review ordered investigations and examination findings."
    >
      {/* Patient Search Card */}
      <div className="panel mb-6" style={{ padding: 20 }}>
        <form onSubmit={handleSearchSubmit} className="flex flex-wrap items-center justify-between gap-4" style={{ margin: 0 }}>
          <div className="flex items-center gap-3 flex-1 min-w-[260px]">
            <label htmlFor="searchPatientIdAdmin" className="font-bold text-sm whitespace-nowrap" style={{ color: 'var(--color-accent)' }}>
              Patient Lookup:
            </label>
            <input
              id="searchPatientIdAdmin"
              type="number"
              placeholder="Enter Patient ID (e.g. 1, 2, 10)..."
              value={inputPatientId}
              onChange={(e) => setInputPatientId(e.target.value)}
              style={{ maxWidth: 280 }}
            />
            <button type="submit" className="primary-button" style={{ marginTop: 0 }}>
              Load Reports
            </button>
            {activePatientId && (
              <button type="button" className="secondary-button" style={{ marginTop: 0 }} onClick={handleClear}>
                Clear
              </button>
            )}
          </div>

          {activePatientId && (
            <button
              type="button"
              className="primary-button text-xs px-3 py-1.5"
              style={{ marginTop: 0 }}
              onClick={() => setShowNewOrderModal(true)}
            >
              + Order Lab Test
            </button>
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
            You do not have authorization to view lab reports for Patient #{activePatientId}.
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

      {/* Empty State – no patient selected */}
      {!activePatientId && (
        <div className="panel text-center py-16 px-6" style={{ background: 'var(--color-primary)' }}>
          <div className="brand-mark mx-auto mb-4" style={{ width: 50, height: 50, fontSize: '2rem' }}>
            +
          </div>
          <h2 className="text-xl font-bold mb-2" style={{ color: 'var(--color-accent)' }}>
            Laboratory &amp; Diagnostic Reports
          </h2>
          <p className="text-sm text-gray-500 max-w-md mx-auto">
            Enter a Patient ID above to review all ordered lab investigations, pathology and radiology findings, and recorded diagnostic reports for that patient.
          </p>
        </div>
      )}

      {/* Active Patient Content */}
      {activePatientId && !unauthorized && (
        <div>
          {/* Patient Header Banner */}
          <div className="patient-banner mb-6">
            <div className="flex flex-wrap justify-between items-center gap-4">
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>
                    {profile?.patientName || `Patient #${activePatientId}`}
                  </h3>
                  {profile?.bloodGroup && (
                    <span className="badge badge-primary">{profile.bloodGroup}</span>
                  )}
                  <span className="text-xs px-2 py-0.5 rounded bg-gray-100 font-semibold">ID: #{activePatientId}</span>
                </div>
                <div className="text-xs text-gray-600 mt-1 flex gap-4">
                  {profile?.gender && <span>Gender: <strong>{profile.gender}</strong></span>}
                  {profile?.allergies && (
                    <span>Allergies: <strong className="text-red-600">{profile.allergies}</strong></span>
                  )}
                </div>
              </div>

              {/* Filters */}
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
              <p className="placeholder-text font-semibold">
                Loading diagnostic orders for Patient #{activePatientId}...
              </p>
            </div>
          ) : (
            <LabOrdersList
              labOrders={filteredOrders}
              onNewOrder={() => setShowNewOrderModal(true)}
              onRecordReport={(order) => setSelectedOrderForReport(order)}
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
