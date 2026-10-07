import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import DashboardLayout from '../../../layouts/DashboardLayout'
import { doctorNavigation } from '../doctorNavigation'
import { useAuth } from '../../../hooks/useAuth'
import {
  getPatientMedicalProfile,
  getPatientMedicalRecords,
  getPatientVitals,
  getPatientPrescriptions,
  getPatientLabOrders,
  getPatientTimeline,
} from '../../../services/emrService'

// Existing & New Clinical Components
import PatientMedicalProfileCard from '../../../components/emr/PatientMedicalProfileCard'
import VitalsSummaryTable from '../../../components/emr/VitalsSummaryTable'
import PrescriptionList from '../../../components/emr/PrescriptionList'
import LabOrdersList from '../../../components/emr/LabOrdersList'
import ClinicalHistoryTimeline from '../../../components/emr/ClinicalHistoryTimeline'
import DiagnosisTreatmentCard from '../../../components/emr/DiagnosisTreatmentCard'
import PatientHeaderBanner from '../../../components/emr/PatientHeaderBanner'

// Modals
import ClinicalVisitWorkflowModal from '../../../components/emr/ClinicalVisitWorkflowModal'
import NewMedicalRecordModal from '../../../components/emr/NewMedicalRecordModal'
import MedicalRecordDetailModal from '../../../components/emr/MedicalRecordDetailModal'
import RecordVitalsModal from '../../../components/emr/RecordVitalsModal'
import NewPrescriptionModal from '../../../components/emr/NewPrescriptionModal'
import NewLabOrderModal from '../../../components/emr/NewLabOrderModal'
import RecordLabReportModal from '../../../components/emr/RecordLabReportModal'
import EditPatientProfileModal from '../../../components/emr/EditPatientProfileModal'
import AddDiagnosisModal from '../../../components/emr/AddDiagnosisModal'
import AddTreatmentPlanModal from '../../../components/emr/AddTreatmentPlanModal'
import FieldError from '../../../components/common/FieldError'

const patientIdMessage = (value) => {
  const trimmed = String(value ?? '').trim()
  if (!trimmed) return 'Patient ID is required.'
  return /^[0-9]+$/.test(trimmed) && Number(trimmed) > 0 && Number(trimmed) <= 2147483647 ? '' : 'Patient ID must be a positive whole number.'
}

const TABS = [
  { id: 'overview', label: 'Overview & Profile' },
  { id: 'records', label: 'Consultation Records' },
  { id: 'timeline', label: 'Clinical History' },
  { id: 'diagnosis-treatment', label: 'Diagnoses & Treatment Plans' },
  { id: 'vitals', label: 'Vital Signs' },
  { id: 'prescriptions', label: 'Prescriptions' },
  { id: 'lab-orders', label: 'Lab Orders & Reports' },
]

export default function DoctorMedicalRecordsPage() {
  const { user } = useAuth()
  const isDoctorOrAdmin = user?.role === 'Doctor' || user?.role === 'Admin'
  const isStaff = user?.role === 'Staff'

  const [searchParams, setSearchParams] = useSearchParams()
  const initialPatientId = searchParams.get('patientId') || ''
  const appointmentId = searchParams.get('appointmentId') || null

  const [inputPatientId, setInputPatientId] = useState(initialPatientId)
  const [patientIdError, setPatientIdError] = useState('')
  const [activePatientId, setActivePatientId] = useState(initialPatientId)
  const [activeTab, setActiveTab] = useState('overview')

  // Patient Clinical State
  const [profile, setProfile] = useState(null)
  const [records, setRecords] = useState([])
  const [vitals, setVitals] = useState([])
  const [prescriptions, setPrescriptions] = useState([])
  const [labOrders, setLabOrders] = useState([])
  const [timelineEvents, setTimelineEvents] = useState([])

  // Loading, Success & Error States
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [unauthorized, setUnauthorized] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')

  // Modals Visibility State
  const [showWorkflowModal, setShowWorkflowModal] = useState(false)
  const [showNewRecordModal, setShowNewRecordModal] = useState(false)
  const [selectedRecordDetail, setSelectedRecordDetail] = useState(null)
  const [showRecordVitalsModal, setShowRecordVitalsModal] = useState(false)
  const [showNewPrescriptionModal, setShowNewPrescriptionModal] = useState(false)
  const [showNewLabOrderModal, setShowNewLabOrderModal] = useState(false)
  const [selectedOrderForReport, setSelectedOrderForReport] = useState(null)
  const [showEditProfileModal, setShowEditProfileModal] = useState(false)
  const [diagnosisRecordId, setDiagnosisRecordId] = useState(null)
  const [treatmentPlanRecordId, setTreatmentPlanRecordId] = useState(null)

  useEffect(() => {
    const pId = searchParams.get('patientId')
    if (pId && pId !== activePatientId) {
      setActivePatientId(pId)
      setInputPatientId(pId)
    }
  }, [searchParams]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (activePatientId) {
      loadPatientClinicalData(activePatientId)
    } else {
      resetPatientState()
    }
  }, [activePatientId]) // eslint-disable-line react-hooks/exhaustive-deps

  const triggerSuccess = (msg) => {
    setSuccessMessage(msg)
    setTimeout(() => {
      setSuccessMessage('')
    }, 4500)
  }

  const resetPatientState = () => {
    setProfile(null)
    setRecords([])
    setVitals([])
    setPrescriptions([])
    setLabOrders([])
    setTimelineEvents([])
    setError(null)
    setUnauthorized(false)
  }

  const loadPatientClinicalData = async (patientId) => {
    try {
      setLoading(true)
      setError(null)
      setUnauthorized(false)

      const id = Number(patientId)

      // Fetch all patient EMR data in parallel
      const [
        profileRes,
        recordsRes,
        vitalsRes,
        prescriptionsRes,
        labOrdersRes,
        timelineRes,
      ] = await Promise.allSettled([
        getPatientMedicalProfile(id),
        getPatientMedicalRecords(id),
        getPatientVitals(id),
        getPatientPrescriptions(id),
        getPatientLabOrders(id),
        getPatientTimeline(id),
      ])

      // Profile: 404 is allowed (no profile yet)
      if (profileRes.status === 'fulfilled') {
        setProfile(profileRes.value)
      } else {
        if (profileRes.reason?.response?.status === 403) setUnauthorized(true)
        setProfile(null)
      }

      // Medical Records
      if (recordsRes.status === 'fulfilled') {
        setRecords(Array.isArray(recordsRes.value) ? recordsRes.value : [])
      } else {
        if (recordsRes.reason?.response?.status === 403) setUnauthorized(true)
        setRecords([])
      }

      // Vitals
      if (vitalsRes.status === 'fulfilled') {
        setVitals(Array.isArray(vitalsRes.value) ? vitalsRes.value : [])
      } else {
        if (vitalsRes.reason?.response?.status === 403) setUnauthorized(true)
        setVitals([])
      }

      // Prescriptions
      if (prescriptionsRes.status === 'fulfilled') {
        setPrescriptions(Array.isArray(prescriptionsRes.value) ? prescriptionsRes.value : [])
      } else {
        if (prescriptionsRes.reason?.response?.status === 403) setUnauthorized(true)
        setPrescriptions([])
      }

      // Lab Orders
      if (labOrdersRes.status === 'fulfilled') {
        setLabOrders(Array.isArray(labOrdersRes.value) ? labOrdersRes.value : [])
      } else {
        if (labOrdersRes.reason?.response?.status === 403) setUnauthorized(true)
        setLabOrders([])
      }

      // Clinical Timeline
      if (timelineRes.status === 'fulfilled') {
        setTimelineEvents(timelineRes.value?.events || [])
      } else {
        if (timelineRes.reason?.response?.status === 403) setUnauthorized(true)
        setTimelineEvents([])
      }
    } catch (err) {
      if (err.response?.status === 401 || err.response?.status === 403) {
        setUnauthorized(true)
      } else {
        setError(err.response?.data?.message || 'Failed to retrieve patient clinical records.')
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

  const handleClearPatient = () => {
    setInputPatientId('')
    setPatientIdError('')
    setActivePatientId('')
    setSearchParams({})
    resetPatientState()
  }

  // Aggregate all diagnoses and treatment plans across records
  const allDiagnoses = records.flatMap((r) => r.diagnoses || [])
  const allTreatmentPlans = records.flatMap((r) => r.treatmentPlans || [])
  const latestRecord = records[0]

  return (
    <DashboardLayout
      role={user?.role || 'Doctor'}
      navigation={doctorNavigation}
      title="Electronic Medical Records (EMR)"
      subtitle="Comprehensive clinical chart, patient consultations, vitals, prescriptions, and diagnostic timeline."
    >
      {/* Patient Search & Selection Bar */}
      <div className="panel mb-6" style={{ padding: 20 }}>
        <form onSubmit={handleSearchSubmit} noValidate className="flex flex-wrap items-center justify-between gap-4" style={{ margin: 0 }}>
          <div className="flex items-center gap-3 flex-1 min-w-[260px]">
            <label htmlFor="searchPatientId" className="font-bold text-sm whitespace-nowrap" style={{ color: 'var(--color-accent)' }}>
              Patient Lookup:
            </label>
            <input
              id="searchPatientId"
              type="number"
              placeholder="Enter Patient ID (e.g. 1, 2, 10)..."
              min="1"
              step="1"
              value={inputPatientId}
              aria-invalid={patientIdError ? 'true' : undefined}
              aria-describedby={patientIdError ? 'searchPatientId-error' : undefined}
              onChange={(e) => { setInputPatientId(e.target.value); if (patientIdError) setPatientIdError(patientIdMessage(e.target.value)) }}
              style={{ maxWidth: 280 }}
            />
            <button type="submit" className="primary-button" style={{ marginTop: 0 }}>
              Load Chart
            </button>
            <FieldError name="searchPatientId" message={patientIdError} className="w-full" />
            {activePatientId && (
              <button type="button" className="secondary-button" style={{ marginTop: 0 }} onClick={handleClearPatient}>
                Clear
              </button>
            )}
          </div>

          {activePatientId && isDoctorOrAdmin && (
            <div className="flex gap-2">
              <button
                type="button"
                className="primary-button text-xs px-3.5 py-1.5"
                style={{ marginTop: 0, backgroundColor: '#0f7a3d', borderColor: '#0f7a3d' }}
                onClick={() => setShowWorkflowModal(true)}
              >
                ▶ Start Guided Clinical Visit
              </button>
              <button
                type="button"
                className="secondary-button text-xs px-3 py-1.5"
                style={{ marginTop: 0 }}
                onClick={() => setShowNewRecordModal(true)}
              >
                + New Consultation
              </button>
            </div>
          )}
        </form>
      </div>

      {/* Success Notification Alert */}
      {successMessage && (
        <div className="p-3.5 mb-6 rounded-lg bg-green-50 border border-green-300 text-green-900 text-sm font-semibold flex justify-between items-center" role="status">
          <div className="flex items-center gap-2">
            <span className="text-base font-bold">✓</span>
            <span>{successMessage}</span>
          </div>
          <button type="button" className="link-button text-xs" onClick={() => setSuccessMessage('')}>
            ✕
          </button>
        </div>
      )}

      {/* Unauthorized Alert */}
      {unauthorized && (
        <div className="status-card mb-6" style={{ padding: 30, textAlign: 'center', background: '#fff' }}>
          <h2 className="text-red-700 font-bold mb-2">Access Restricted (403 Forbidden)</h2>
          <p className="text-sm text-gray-600 mb-4">
            You do not have authorization to view clinical records for Patient #{activePatientId}.
          </p>
          <button className="secondary-button text-sm" onClick={handleClearPatient}>
            Search Another Patient
          </button>
        </div>
      )}

      {/* Generic API Error */}
      {error && !unauthorized && (
        <div className="form-error mb-6 flex justify-between items-center" role="alert">
          <span>{error}</span>
          <button type="button" className="link-button text-xs" onClick={() => loadPatientClinicalData(activePatientId)}>
            Retry
          </button>
        </div>
      )}

      {/* Initial Empty State (No Patient Selected) */}
      {!activePatientId && (
        <div className="panel text-center py-16 px-6" style={{ background: 'var(--color-primary)' }}>
          <div className="brand-mark mx-auto mb-4" style={{ width: 50, height: 50, fontSize: '2rem' }}>
            +
          </div>
          <h2 className="text-xl font-bold mb-2" style={{ color: 'var(--color-accent)' }}>
            Select a Patient to Begin Clinical Consultation
          </h2>
          <p className="text-sm text-gray-500 max-w-md mx-auto mb-6">
            Enter a Patient ID above or select a patient from your appointment schedule to view their medical profile, vitals history, active prescriptions, lab investigations, and clinical timeline.
          </p>
        </div>
      )}

      {/* Active Patient Clinical Workspace */}
      {activePatientId && !unauthorized && (
        <div>
          {/* Patient Header Banner */}
          <PatientHeaderBanner
            patientId={activePatientId}
            profile={profile}
            patientName={records[0]?.patientName}
            onStartWorkflow={isDoctorOrAdmin ? () => setShowWorkflowModal(true) : undefined}
            onNewRecord={isDoctorOrAdmin ? () => setShowNewRecordModal(true) : undefined}
            onRecordVitals={isDoctorOrAdmin || isStaff ? () => setShowRecordVitalsModal(true) : undefined}
            onPrescribe={isDoctorOrAdmin ? () => setShowNewPrescriptionModal(true) : undefined}
            onOrderLab={isDoctorOrAdmin ? () => setShowNewLabOrderModal(true) : undefined}
            onEditProfile={isDoctorOrAdmin || isStaff ? () => setShowEditProfileModal(true) : undefined}
            onSwitchPatient={handleClearPatient}
          />

          {/* Clinical Navigation Tabs */}
          <nav className="clinical-tabs" aria-label="Clinical record sections">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                className={`clinical-tab-button ${activeTab === tab.id ? 'active' : ''}`}
                onClick={() => setActiveTab(tab.id)}
              >
                {tab.label}
                {tab.id === 'records' && records.length > 0 && ` (${records.length})`}
                {tab.id === 'vitals' && vitals.length > 0 && ` (${vitals.length})`}
                {tab.id === 'prescriptions' && prescriptions.length > 0 && ` (${prescriptions.length})`}
                {tab.id === 'lab-orders' && labOrders.length > 0 && ` (${labOrders.length})`}
              </button>
            ))}
          </nav>

          {/* Loading Indicator */}
          {loading && (
            <div className="panel py-10 text-center mb-6">
              <p className="placeholder-text font-semibold">Loading clinical data for Patient #{activePatientId}...</p>
            </div>
          )}

          {/* TAB 1: Overview & Profile */}
          {!loading && activeTab === 'overview' && (
            <div className="flex flex-col gap-6">
              <PatientMedicalProfileCard
                profile={profile}
                onEdit={isDoctorOrAdmin || isStaff ? () => setShowEditProfileModal(true) : undefined}
              />

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Recent Consultation Card */}
                <div className="panel" style={{ padding: 24 }}>
                  <div className="flex justify-between items-center mb-3 pb-2 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                    <h3 className="font-bold text-base m-0" style={{ color: 'var(--color-accent)' }}>
                      Latest Consultation Encounter
                    </h3>
                    {latestRecord && (
                      <button
                        type="button"
                        className="link-button text-xs"
                        onClick={() => setSelectedRecordDetail(latestRecord)}
                      >
                        View Full Encounter
                      </button>
                    )}
                  </div>

                  {latestRecord ? (
                    <div className="text-sm flex flex-col gap-2">
                      <div className="flex justify-between text-xs text-gray-500">
                        <span>Date: {new Date(latestRecord.visitDate).toLocaleDateString()}</span>
                        <span>Doctor: Dr. {latestRecord.doctorName}</span>
                      </div>
                      <div>
                        <strong className="block text-xs uppercase text-gray-600">Chief Complaint:</strong>
                        <p className="m-0 font-medium">{latestRecord.chiefComplaint}</p>
                      </div>
                      <div>
                        <strong className="block text-xs uppercase text-gray-600">Primary Diagnosis:</strong>
                        <p className="m-0 font-semibold text-blue-900">{latestRecord.diagnosis}</p>
                      </div>
                      {latestRecord.treatmentPlan && (
                        <div>
                          <strong className="block text-xs uppercase text-gray-600">Plan:</strong>
                          <p className="m-0 text-gray-700 truncate">{latestRecord.treatmentPlan}</p>
                        </div>
                      )}
                    </div>
                  ) : (
                    <p className="placeholder-text text-sm">No consultation encounters recorded yet.</p>
                  )}
                </div>

                {/* Quick Diagnostics & Rx Summary */}
                <div className="panel" style={{ padding: 24 }}>
                  <div className="flex justify-between items-center mb-3 pb-2 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                    <h3 className="font-bold text-base m-0" style={{ color: 'var(--color-accent)' }}>
                      Active Care Summary
                    </h3>
                  </div>

                  <div className="grid grid-cols-2 gap-4 text-center my-2">
                    <div className="p-3 rounded-lg bg-blue-50">
                      <span className="text-xs uppercase font-bold text-blue-900">Active Prescriptions</span>
                      <div className="text-2xl font-bold text-blue-950 mt-1">{prescriptions.length}</div>
                    </div>
                    <div className="p-3 rounded-lg bg-amber-50">
                      <span className="text-xs uppercase font-bold text-amber-900">Lab Orders</span>
                      <div className="text-2xl font-bold text-amber-950 mt-1">{labOrders.length}</div>
                    </div>
                  </div>

                  <div className="flex gap-2 mt-4">
                    <button
                      type="button"
                      className="secondary-button text-xs w-full"
                      onClick={() => setActiveTab('prescriptions')}
                    >
                      Review Medications
                    </button>
                    <button
                      type="button"
                      className="secondary-button text-xs w-full"
                      onClick={() => setActiveTab('lab-orders')}
                    >
                      Review Lab Tests
                    </button>
                  </div>
                </div>
              </div>

              {/* Vitals Snapshot */}
              <VitalsSummaryTable
                vitals={vitals}
                onRecordVitals={isDoctorOrAdmin || isStaff ? () => setShowRecordVitalsModal(true) : undefined}
              />
            </div>
          )}

          {/* TAB 2: Consultation Records */}
          {!loading && activeTab === 'records' && (
            <div className="flex flex-col gap-4">
              <div className="flex justify-between items-center">
                <div>
                  <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>
                    Patient Consultation Records ({records.length})
                  </h3>
                  <span className="text-xs text-gray-500">
                    Clinical consultations, doctor notes, diagnoses, and encounter outcomes
                  </span>
                </div>
                {isDoctorOrAdmin && (
                  <div className="flex gap-2">
                    <button
                      type="button"
                      className="primary-button text-xs px-3 py-1.5"
                      style={{ marginTop: 0, backgroundColor: '#0f7a3d', borderColor: '#0f7a3d' }}
                      onClick={() => setShowWorkflowModal(true)}
                    >
                      ▶ Guided Clinical Visit
                    </button>
                    <button
                      type="button"
                      className="secondary-button text-xs px-3 py-1.5"
                      style={{ marginTop: 0 }}
                      onClick={() => setShowNewRecordModal(true)}
                    >
                      + Standard Record
                    </button>
                  </div>
                )}
              </div>

              {records.length === 0 ? (
                <div className="panel py-12 text-center">
                  <p className="placeholder-text text-sm mb-4">No consultation encounters recorded for this patient.</p>
                  {isDoctorOrAdmin && (
                    <button
                      type="button"
                      className="primary-button text-xs"
                      onClick={() => setShowWorkflowModal(true)}
                    >
                      ▶ Start Clinical Visit Workflow
                    </button>
                  )}
                </div>
              ) : (
                <div className="panel overflow-hidden">
                  <div className="table-responsive">
                    <table className="data-table">
                      <thead>
                        <tr>
                          <th>Record #</th>
                          <th>Visit Date</th>
                          <th>Attending Doctor</th>
                          <th>Chief Complaint</th>
                          <th>Primary Diagnosis</th>
                          <th>Follow-Up</th>
                          <th>Clinical Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {records.map((rec) => (
                          <tr key={rec.id}>
                            <td><strong>{rec.recordNumber || `#${rec.id}`}</strong></td>
                            <td>{rec.visitDate ? new Date(rec.visitDate).toLocaleDateString() : 'N/A'}</td>
                            <td>Dr. {rec.doctorName || 'Attending'}</td>
                            <td>
                              <span className="max-w-xs truncate block" title={rec.chiefComplaint}>
                                {rec.chiefComplaint}
                              </span>
                            </td>
                            <td>
                              <span className="font-semibold text-blue-900 block max-w-xs truncate" title={rec.diagnosis}>
                                {rec.diagnosis}
                              </span>
                            </td>
                            <td>
                              {rec.followUpDate ? (
                                <span className="badge badge-warning">
                                  {new Date(rec.followUpDate).toLocaleDateString()}
                                </span>
                              ) : (
                                <span className="text-gray-400">-</span>
                              )}
                            </td>
                            <td>
                              <div className="flex gap-2">
                                <button
                                  type="button"
                                  className="link-button"
                                  onClick={() => setSelectedRecordDetail(rec)}
                                >
                                  View Details
                                </button>
                                {isDoctorOrAdmin && (
                                  <>
                                    <button
                                      type="button"
                                      className="link-button"
                                      onClick={() => setDiagnosisRecordId(rec.id)}
                                    >
                                      + Diagnosis
                                    </button>
                                    <button
                                      type="button"
                                      className="link-button"
                                      onClick={() => setTreatmentPlanRecordId(rec.id)}
                                    >
                                      + Plan
                                    </button>
                                  </>
                                )}
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* TAB 3: Clinical History Timeline */}
          {!loading && activeTab === 'timeline' && (
            <ClinicalHistoryTimeline
              events={timelineEvents}
              loading={loading}
            />
          )}

          {/* TAB 4: Diagnoses & Treatment Plans */}
          {!loading && activeTab === 'diagnosis-treatment' && (
            <DiagnosisTreatmentCard
              diagnoses={allDiagnoses}
              treatmentPlans={allTreatmentPlans}
              primaryDiagnosis={latestRecord?.diagnosis}
              primaryTreatmentPlan={latestRecord?.treatmentPlan}
              onAddDiagnosis={isDoctorOrAdmin && latestRecord ? () => setDiagnosisRecordId(latestRecord.id) : undefined}
              onAddTreatmentPlan={isDoctorOrAdmin && latestRecord ? () => setTreatmentPlanRecordId(latestRecord.id) : undefined}
            />
          )}

          {/* TAB 5: Vital Signs */}
          {!loading && activeTab === 'vitals' && (
            <VitalsSummaryTable
              vitals={vitals}
              onRecordVitals={isDoctorOrAdmin || isStaff ? () => setShowRecordVitalsModal(true) : undefined}
            />
          )}

          {/* TAB 6: Prescriptions */}
          {!loading && activeTab === 'prescriptions' && (
            <PrescriptionList
              prescriptions={prescriptions}
              onNewPrescription={isDoctorOrAdmin ? () => setShowNewPrescriptionModal(true) : undefined}
            />
          )}

          {/* TAB 7: Lab Orders & Reports */}
          {!loading && activeTab === 'lab-orders' && (
            <LabOrdersList
              labOrders={labOrders}
              onNewOrder={isDoctorOrAdmin ? () => setShowNewLabOrderModal(true) : undefined}
              onRecordReport={isDoctorOrAdmin || isStaff ? (order) => setSelectedOrderForReport(order) : undefined}
            />
          )}
        </div>
      )}

      {/* --- CLINICAL MODALS --- */}

      {/* 0. Clinical Visit Workflow Wizard Modal */}
      {showWorkflowModal && (
        <ClinicalVisitWorkflowModal
          patientId={activePatientId}
          patientName={records[0]?.patientName || profile?.patientName}
          appointmentId={appointmentId}
          onClose={() => setShowWorkflowModal(false)}
          onCompleted={() => {
            setShowWorkflowModal(false)
            triggerSuccess('Clinical visit successfully completed and recorded to patient history.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}

      {/* 1. New Medical Record Modal */}
      {showNewRecordModal && (
        <NewMedicalRecordModal
          patientId={activePatientId}
          appointmentId={appointmentId}
          onClose={() => setShowNewRecordModal(false)}
          onSaved={() => {
            setShowNewRecordModal(false)
            triggerSuccess('Consultation encounter created successfully.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}

      {/* 2. Medical Record Detail Inspection Modal */}
      {selectedRecordDetail && (
        <MedicalRecordDetailModal
          record={selectedRecordDetail}
          onClose={() => setSelectedRecordDetail(null)}
        />
      )}

      {/* 3. Record Vitals Modal */}
      {showRecordVitalsModal && (
        <RecordVitalsModal
          patientId={activePatientId}
          medicalRecordId={latestRecord?.id}
          onClose={() => setShowRecordVitalsModal(false)}
          onSaved={() => {
            setShowRecordVitalsModal(false)
            triggerSuccess('Vital signs logged successfully.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}

      {/* 4. New Prescription Modal */}
      {showNewPrescriptionModal && (
        <NewPrescriptionModal
          patientId={activePatientId}
          medicalRecordId={latestRecord?.id}
          onClose={() => setShowNewPrescriptionModal(false)}
          onSaved={() => {
            setShowNewPrescriptionModal(false)
            triggerSuccess('Prescription issued successfully.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}

      {/* 5. New Lab Order Modal */}
      {showNewLabOrderModal && (
        <NewLabOrderModal
          patientId={activePatientId}
          medicalRecordId={latestRecord?.id}
          onClose={() => setShowNewLabOrderModal(false)}
          onSaved={() => {
            setShowNewLabOrderModal(false)
            triggerSuccess('Laboratory order submitted successfully.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}

      {/* 6. Record Lab Report Modal */}
      {selectedOrderForReport && (
        <RecordLabReportModal
          order={selectedOrderForReport}
          onClose={() => setSelectedOrderForReport(null)}
          onSaved={() => {
            setSelectedOrderForReport(null)
            triggerSuccess('Diagnostic report recorded successfully.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}

      {/* 7. Edit Patient Medical Profile Modal */}
      {showEditProfileModal && (
        <EditPatientProfileModal
          patientId={activePatientId}
          profile={profile}
          onClose={() => setShowEditProfileModal(false)}
          onSaved={() => {
            setShowEditProfileModal(false)
            triggerSuccess('Patient medical profile updated successfully.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}

      {/* 8. Add Diagnosis to Record Modal */}
      {diagnosisRecordId && (
        <AddDiagnosisModal
          recordId={diagnosisRecordId}
          onClose={() => setDiagnosisRecordId(null)}
          onSaved={() => {
            setDiagnosisRecordId(null)
            triggerSuccess('Diagnosis attached to medical record.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}

      {/* 9. Add Treatment Plan to Record Modal */}
      {treatmentPlanRecordId && (
        <AddTreatmentPlanModal
          recordId={treatmentPlanRecordId}
          onClose={() => setTreatmentPlanRecordId(null)}
          onSaved={() => {
            setTreatmentPlanRecordId(null)
            triggerSuccess('Treatment plan recorded successfully.')
            loadPatientClinicalData(activePatientId)
          }}
        />
      )}
    </DashboardLayout>
  )
}
