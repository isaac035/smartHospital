import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation } from './adminNavigation'
import { clinicalCareManagerNavigation } from './clinicalCareManagerNavigation'
import { useAuth } from '../../hooks/useAuth'
import { getPatientLabOrders, getPatientMedicalProfile, getAiMedicalReports, getAiMedicalReport, generateAiMedicalReport } from '../../services/emrService'
import LabOrdersList from '../../components/emr/LabOrdersList'
import RecordLabReportModal from '../../components/emr/RecordLabReportModal'
import NewLabOrderModal from '../../components/emr/NewLabOrderModal'
import { searchPatients } from '../../services/userService'

export default function AdminReportsPage() {
  const { user } = useAuth()

  const [searchParams, setSearchParams] = useSearchParams()

  const initialPatientId = searchParams.get('patientId') || ''
  const [inputPatientId, setInputPatientId] = useState(initialPatientId)
  const [activePatientId, setActivePatientId] = useState(initialPatientId)
  const [selectedPatient, setSelectedPatient] = useState(null)
  const [suggestions, setSuggestions] = useState([])
  const [suggestionsOpen, setSuggestionsOpen] = useState(false)
  const [suggestionsActive, setSuggestionsActive] = useState(false)
  const [suggestionsLoading, setSuggestionsLoading] = useState(false)
  const [suggestionError, setSuggestionError] = useState('')
  const [activeSuggestionIndex, setActiveSuggestionIndex] = useState(-1)
  const [lookupError, setLookupError] = useState('')

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
  const [aiReports, setAiReports] = useState([])
  const [aiReportsLoading, setAiReportsLoading] = useState(false)
  const [aiReportError, setAiReportError] = useState('')
  const [aiReportActionLoading, setAiReportActionLoading] = useState(false)
  const [selectedAiReport, setSelectedAiReport] = useState(null)

  const dismissSuggestions = () => {
    setSuggestionsActive(false)
    setSuggestionsOpen(false)
    setSuggestions([])
    setActiveSuggestionIndex(-1)
  }

  const selectPatient = (patient) => {
    setSelectedPatient(patient)
    setInputPatientId(patient.displayName)
    setLookupError('')
    dismissSuggestions()
    const patientId = String(patient.id)
    setSearchParams({ patientId })
    setActivePatientId(patientId)
  }

  useEffect(() => {
    const pId = searchParams.get('patientId')
    if (pId && pId !== activePatientId) {
      setActivePatientId(pId)
      setInputPatientId(pId)
      setSelectedPatient(null)
      setLookupError('')
      dismissSuggestions()
    }
  }, [searchParams]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    const query = inputPatientId.trim()
    if (!suggestionsActive || selectedPatient || !query || /^\d+$/.test(query)) {
      setSuggestions([])
      setSuggestionsOpen(false)
      setSuggestionsLoading(false)
      setSuggestionError('')
      return undefined
    }

    let cancelled = false
    const timer = window.setTimeout(async () => {
      setSuggestionsLoading(true)
      setSuggestionError('')
      try {
        const results = await searchPatients(query, 10)
        if (!cancelled) {
          setSuggestions(Array.isArray(results) ? results : [])
          setSuggestionsOpen(true)
          setActiveSuggestionIndex(-1)
        }
      } catch {
        if (!cancelled) {
          setSuggestions([])
          setSuggestionsOpen(true)
          setSuggestionError('Patient name search is unavailable right now.')
        }
      } finally {
        if (!cancelled) setSuggestionsLoading(false)
      }
    }, 250)

    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [inputPatientId, selectedPatient, suggestionsActive])

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

  useEffect(() => {
    if (!activePatientId) {
      setAiReports([])
      setSelectedAiReport(null)
      return
    }
    let cancelled = false
    setAiReportsLoading(true)
    setAiReportError('')
    getAiMedicalReports(Number(activePatientId))
      .then((reports) => { if (!cancelled) setAiReports(Array.isArray(reports) ? reports : []) })
      .catch((err) => { if (!cancelled) setAiReportError(err.response?.data?.message || 'Could not load AI medical reports.') })
      .finally(() => { if (!cancelled) setAiReportsLoading(false) })
    return () => { cancelled = true }
  }, [activePatientId])

  // A patient may create reports in Flutter while this page is open in another tab.
  // Refresh when the admin returns to the tab and whenever this route is mounted.
  useEffect(() => {
    if (!activePatientId) return undefined
    let cancelled = false
    const refresh = () => {
      if (document.visibilityState !== 'visible') return
      getAiMedicalReports(Number(activePatientId))
        .then((reports) => { if (!cancelled) setAiReports(Array.isArray(reports) ? reports : []) })
        .catch((err) => { if (!cancelled) setAiReportError(err.response?.data?.message || 'Could not load AI medical reports.') })
    }
    window.addEventListener('focus', refresh)
    document.addEventListener('visibilitychange', refresh)
    return () => {
      cancelled = true
      window.removeEventListener('focus', refresh)
      document.removeEventListener('visibilitychange', refresh)
    }
  }, [activePatientId])

  const refreshAiReports = async () => {
    if (!activePatientId) return
    try {
      setAiReportsLoading(true)
      setAiReportError('')
      const reports = await getAiMedicalReports(Number(activePatientId))
      setAiReports(Array.isArray(reports) ? reports : [])
    } catch (err) {
      setAiReportError(err.response?.data?.message || 'Could not load AI medical reports.')
    } finally {
      setAiReportsLoading(false)
    }
  }

  const handleGenerateAiReport = async () => {
    try {
      setAiReportActionLoading(true)
      setAiReportError('')
      const report = await generateAiMedicalReport(Number(activePatientId))
      setSelectedAiReport(report)
      const reports = await getAiMedicalReports(Number(activePatientId))
      setAiReports(Array.isArray(reports) ? reports : [])
    } catch (err) {
      setAiReportError(err.response?.data?.message || 'Could not generate the medical report.')
    } finally {
      setAiReportActionLoading(false)
    }
  }

  const renderReportValue = (value) => {
    if (value == null || value === '') return <span className="text-gray-500">Not recorded</span>
    if (Array.isArray(value)) {
      if (!value.length) return <span className="text-gray-500">None recorded</span>
      return <ul className="list-disc pl-5 space-y-1">{value.map((item, index) => <li key={index}>{renderReportValue(item)}</li>)}</ul>
    }
    if (typeof value === 'object') {
      return <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-2">{Object.entries(value).map(([key, child]) => (
        <div key={key} className="min-w-0"><dt className="text-xs font-semibold text-gray-500">{key.replace(/([A-Z])/g, ' $1').replace(/^./, (s) => s.toUpperCase())}</dt><dd className="text-sm text-gray-800 break-words">{renderReportValue(child)}</dd></div>
      ))}</dl>
    }
    return String(value)
  }

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
    const patientId = selectedPatient?.id ?? (/^\d+$/.test(trimmed) ? trimmed : null)
    if (patientId) {
      const id = String(patientId)
      setLookupError('')
      dismissSuggestions()
      setSearchParams({ patientId: id })
      setActivePatientId(id)
    } else if (trimmed) {
      setLookupError('Choose a patient from the suggestions or enter a patient ID.')
    }
  }

  const handleClear = () => {
    setInputPatientId('')
    setSelectedPatient(null)
    setLookupError('')
    dismissSuggestions()
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
      navigation={user?.role === 'ClinicalCareManager' ? clinicalCareManagerNavigation : adminNavigation}
      title="Laboratory & Diagnostic Reports"
      subtitle="Search by patient name or ID to review ordered investigations and examination findings."
    >
      {/* Patient Search Card */}
      <div className="panel mb-6" style={{ padding: 20 }}>
        <form onSubmit={handleSearchSubmit} className="flex flex-wrap items-center justify-between gap-4" style={{ margin: 0 }}>
          <div className="flex items-start gap-3 flex-1 min-w-[260px]">
            <label htmlFor="searchPatientIdAdmin" className="font-bold text-sm whitespace-nowrap pt-2" style={{ color: 'var(--color-accent)' }}>
              Patient Lookup:
            </label>
            <div className="relative flex-1 max-w-sm">
              <input
                id="searchPatientIdAdmin"
                maxLength={100}
                type="text"
                inputMode="search"
                role="combobox"
                aria-autocomplete="list"
                aria-expanded={suggestionsOpen}
                aria-controls="admin-patient-suggestions"
                aria-activedescendant={activeSuggestionIndex >= 0 ? `patient-suggestion-${activeSuggestionIndex}` : undefined}
                autoComplete="off"
                placeholder="Enter a patient name or ID..."
                value={inputPatientId}
                onFocus={() => {
                  if (inputPatientId.trim() && !selectedPatient) setSuggestionsActive(true)
                }}
                onBlur={() => window.setTimeout(dismissSuggestions, 100)}
                onChange={(e) => {
                  const value = e.target.value
                  setInputPatientId(value)
                  setSelectedPatient(null)
                  setLookupError('')
                  if (value.trim()) setSuggestionsActive(true)
                  else dismissSuggestions()
                }}
                onKeyDown={(e) => {
                  if (e.key === 'Escape') {
                    dismissSuggestions()
                  } else if (e.key === 'ArrowDown' && suggestions.length) {
                    e.preventDefault()
                    setSuggestionsOpen(true)
                    setActiveSuggestionIndex((index) => Math.min(index + 1, suggestions.length - 1))
                  } else if (e.key === 'ArrowUp' && suggestions.length) {
                    e.preventDefault()
                    setActiveSuggestionIndex((index) => Math.max(index <= 0 ? suggestions.length - 1 : index - 1, 0))
                  } else if (e.key === 'Enter' && suggestionsOpen && suggestions.length) {
                    e.preventDefault()
                    selectPatient(suggestions[activeSuggestionIndex >= 0 ? activeSuggestionIndex : 0])
                  }
                }}
                style={{ maxWidth: 380, width: '100%' }}
              />
              {suggestionsOpen && (suggestionsLoading || suggestionError || suggestions.length > 0 || inputPatientId.trim()) && (
                <div
                  id="admin-patient-suggestions"
                  role="listbox"
                  aria-label="Matching patients"
                  className="absolute z-30 mt-1 w-full max-h-64 overflow-y-auto rounded-lg border bg-white shadow-lg"
                  style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 20%, white)' }}
                >
                  {suggestionsLoading ? (
                    <div className="px-3 py-2 text-sm text-gray-500" role="status">Searching patients...</div>
                  ) : suggestionError ? (
                    <div className="px-3 py-2 text-sm text-red-700" role="status">{suggestionError}</div>
                  ) : suggestions.length ? suggestions.map((patient, index) => (
                    <button
                      type="button"
                      role="option"
                      aria-selected={index === activeSuggestionIndex}
                      id={`patient-suggestion-${index}`}
                      key={patient.id}
                      onMouseDown={(e) => e.preventDefault()}
                      onClick={() => selectPatient(patient)}
                      className="block w-full px-3 py-2 text-left text-sm hover:bg-slate-100 focus:bg-slate-100"
                    >
                      <span className="font-medium">{patient.displayName}</span>
                      <span className="ml-2 text-xs text-gray-500">ID: #{patient.id}</span>
                    </button>
                  )) : (
                    <div className="px-3 py-2 text-sm text-gray-500" role="status">No matching patients.</div>
                  )}
                </div>
              )}
            </div>
            <button type="submit" className="primary-button" style={{ marginTop: 0 }}>
              Load Reports
            </button>
            {activePatientId && (
              <button type="button" className="secondary-button" style={{ marginTop: 0 }} onClick={handleClear}>
                Clear
              </button>
            )}
          </div>
          {lookupError && <p className="w-full m-0 text-sm text-red-700" role="alert">{lookupError}</p>}

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
            Enter a patient name or ID above to review all ordered lab investigations, pathology and radiology findings, and recorded diagnostic reports for that patient.
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

          <section className="panel mt-6 p-5" aria-labelledby="ai-medical-reports-title">
            <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
              <div>
                <h2 id="ai-medical-reports-title" className="m-0 text-lg font-bold" style={{ color: 'var(--color-accent)' }}>AI Medical Reports</h2>
                <p className="mt-1 mb-0 text-sm text-gray-600">Structured summaries of recorded patient and encounter data.</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <button type="button" className="link-button" style={{ marginTop: 0 }} disabled={aiReportsLoading} onClick={refreshAiReports}>
                  {aiReportsLoading ? 'Refreshing…' : 'Refresh reports'}
                </button>
                <button type="button" className="primary-button" style={{ marginTop: 0 }} disabled={aiReportActionLoading} onClick={handleGenerateAiReport}>
                  {aiReportActionLoading ? 'Generating…' : 'Generate report'}
                </button>
              </div>
            </div>
            {aiReportError && <p className="form-error" role="alert">{aiReportError}</p>}
            {aiReportsLoading ? <p className="text-sm text-gray-500" role="status">Loading medical reports…</p> : aiReports.length === 0 ? (
              <p className="text-sm text-gray-500">No AI medical reports have been generated for this patient.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm"><thead><tr className="text-left border-b"><th className="py-2 pr-3">Generated</th><th className="py-2 pr-3">Appointment</th><th className="py-2 pr-3">Doctor</th><th className="py-2 pr-3">Priority</th><th className="py-2"> </th></tr></thead>
                  <tbody>{aiReports.map((report) => <tr key={report.reportId} className="border-b last:border-0"><td className="py-2 pr-3">{new Date(report.createdAt).toLocaleString()}</td><td className="py-2 pr-3">{report.appointmentType || (report.appointmentId ? `#${report.appointmentId}` : 'Patient history')}</td><td className="py-2 pr-3">{report.doctorName || '—'}</td><td className="py-2 pr-3">{report.priority || '—'}</td><td className="py-2 text-right"><button type="button" className="link-button" onClick={async () => {
                    try { const detail = await getAiMedicalReport(report.reportId); setSelectedAiReport(detail) }
                    catch (err) { setAiReportError(err.response?.data?.message || 'Could not load this report.') }
                  }}>View</button></td></tr>)}</tbody>
                </table>
              </div>
            )}
            {selectedAiReport?.content && (
              <div className="mt-5 rounded-xl border border-slate-200 bg-slate-50 p-4" role="region" aria-label="AI medical report detail">
                <div className="flex justify-between items-center mb-3"><h3 className="m-0 font-bold">Medical Report · Version {selectedAiReport.versionNumber}</h3><button className="link-button" onClick={() => setSelectedAiReport(null)}>Close</button></div>
                {Object.entries(selectedAiReport.content).map(([section, value]) => <section key={section} className="mb-4 last:mb-0 rounded-lg bg-white border p-3"><h4 className="mt-0 mb-2 font-semibold">{section === 'recordedData' ? 'Recorded Data' : section === 'aiSummary' ? 'AI Summary' : section === 'aiRecommendations' ? 'AI Recommendations (for clinical review)' : section.replace(/([A-Z])/g, ' $1').replace(/^./, (s) => s.toUpperCase())}</h4>{renderReportValue(value)}</section>)}
              </div>
            )}
          </section>
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
