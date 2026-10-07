import { useState, useEffect } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import DashboardLayout from '../../../layouts/DashboardLayout'
import { doctorNavigation } from '../doctorNavigation'
import { useAuth } from '../../../hooks/useAuth'
import { getPatientPrescriptions, getPatientMedicalProfile } from '../../../services/emrService'
import PrescriptionList from '../../../components/emr/PrescriptionList'
import NewPrescriptionModal from '../../../components/emr/NewPrescriptionModal'
import FieldError from '../../../components/common/FieldError'

const patientIdMessage = (value) => {
  const trimmed = String(value ?? '').trim()
  if (!trimmed) return 'Patient ID is required.'
  return /^[0-9]+$/.test(trimmed) && Number(trimmed) > 0 && Number(trimmed) <= 2147483647 ? '' : 'Patient ID must be a positive whole number.'
}

export default function DoctorPrescriptionsPage() {
  const { user } = useAuth()
  const isDoctorOrAdmin = user?.role === 'Doctor' || user?.role === 'Admin'

  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const initialPatientId = searchParams.get('patientId') || ''
  const [inputPatientId, setInputPatientId] = useState(initialPatientId)
  const [patientIdError, setPatientIdError] = useState('')
  const [activePatientId, setActivePatientId] = useState(initialPatientId)

  const [profile, setProfile] = useState(null)
  const [prescriptions, setPrescriptions] = useState([])
  const [statusFilter, setStatusFilter] = useState('ALL')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [unauthorized, setUnauthorized] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')
  const [showNewModal, setShowNewModal] = useState(false)

  useEffect(() => {
    const pId = searchParams.get('patientId')
    if (pId && pId !== activePatientId) {
      setActivePatientId(pId)
      setInputPatientId(pId)
    }
  }, [searchParams]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (activePatientId) {
      fetchPrescriptions(activePatientId)
    } else {
      setPrescriptions([])
      setProfile(null)
      setError(null)
      setUnauthorized(false)
    }
  }, [activePatientId]) // eslint-disable-line react-hooks/exhaustive-deps

  const triggerSuccess = (msg) => {
    setSuccessMessage(msg)
    setTimeout(() => setSuccessMessage(''), 4500)
  }

  const fetchPrescriptions = async (patientId) => {
    try {
      setLoading(true)
      setError(null)
      setUnauthorized(false)

      const id = Number(patientId)

      const [rxRes, profileRes] = await Promise.allSettled([
        getPatientPrescriptions(id),
        getPatientMedicalProfile(id),
      ])

      if (rxRes.status === 'fulfilled') {
        setPrescriptions(Array.isArray(rxRes.value) ? rxRes.value : [])
      } else {
        if (rxRes.reason?.response?.status === 403) setUnauthorized(true)
        else setError(rxRes.reason?.response?.data?.message || 'Failed to load prescriptions.')
        setPrescriptions([])
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
        setError(err.response?.data?.message || 'Failed to load prescriptions.')
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

  const filteredPrescriptions = statusFilter === 'ALL'
    ? prescriptions
    : prescriptions.filter((rx) => rx.status?.toUpperCase() === statusFilter)

  const hasAllergies = profile?.allergies && profile.allergies.toLowerCase() !== 'none' && profile.allergies.toLowerCase() !== 'none reported'

  return (
    <DashboardLayout
      role={user?.role || 'Doctor'}
      navigation={doctorNavigation}
      title="Prescriptions Management"
      subtitle="Issue and review patient medications, dosage schedules, and drug safety."
    >
      {/* Patient Lookup Card */}
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
                  onClick={() => setShowNewModal(true)}
                >
                  + Issue Prescription
                </button>
              )}
            </div>
          )}
        </form>
      </div>

      {/* Success Banner */}
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
            You do not have authorization to view or issue prescriptions for Patient #{activePatientId}.
          </p>
          <button className="secondary-button text-sm" onClick={handleClear}>
            Search Another Patient
          </button>
        </div>
      )}

      {/* API Error State */}
      {error && !unauthorized && (
        <div className="form-error mb-6 flex justify-between items-center" role="alert">
          <span>{error}</span>
          <button type="button" className="link-button text-xs" onClick={() => fetchPrescriptions(activePatientId)}>
            Retry
          </button>
        </div>
      )}

      {/* Initial Empty State */}
      {!activePatientId && (
        <div className="panel text-center py-16 px-6">
          <h3 className="text-xl font-bold mb-2" style={{ color: 'var(--color-accent)' }}>
            Patient Prescriptions Workspace
          </h3>
          <p className="text-sm text-gray-500 max-w-md mx-auto">
            Please enter a Patient ID above to view existing medication history and author new prescriptions.
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
                  {profile?.chronicDiseases && <span>Chronic: <strong>{profile.chronicDiseases}</strong></span>}
                </div>
              </div>

              <div className="flex items-center gap-2">
                <label className="text-xs font-bold text-gray-600">Filter Status:</label>
                <select
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value)}
                  className="p-1.5 text-xs border rounded-lg bg-white"
                >
                  <option value="ALL">All Statuses ({prescriptions.length})</option>
                  <option value="ACTIVE">Active Only</option>
                  <option value="COMPLETED">Completed</option>
                  <option value="CANCELLED">Cancelled</option>
                </select>
              </div>
            </div>

            {hasAllergies && (
              <div className="allergy-alert mt-4">
                <span>⚠️ <strong>Allergy Alert:</strong> Documented allergies: <u>{profile.allergies}</u></span>
              </div>
            )}
          </div>

          {loading ? (
            <div className="panel py-10 text-center">
              <p className="placeholder-text font-semibold">Loading prescriptions for Patient #{activePatientId}...</p>
            </div>
          ) : (
            <PrescriptionList
              prescriptions={filteredPrescriptions}
              onNewPrescription={isDoctorOrAdmin ? () => setShowNewModal(true) : undefined}
            />
          )}
        </div>
      )}

      {/* New Prescription Modal */}
      {showNewModal && (
        <NewPrescriptionModal
          patientId={activePatientId}
          onClose={() => setShowNewModal(false)}
          onSaved={() => {
            setShowNewModal(false)
            triggerSuccess('Prescription issued successfully.')
            fetchPrescriptions(activePatientId)
          }}
        />
      )}
    </DashboardLayout>
  )
}
