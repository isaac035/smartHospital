import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { doctorNavigation } from './doctorNavigation'
import { getAppointments } from '../../services/appointmentService'
import { useAuth } from '../../hooks/useAuth'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'

export default function DoctorDashboard() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const [searchPatientId, setSearchPatientId] = useState('')
  const [todayAppointments, setTodayAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    fetchDashboardData()
  }, [])

  const fetchDashboardData = async () => {
    try {
      setLoading(true)
      setError(null)
      // Fetch appointments for today
      const todayStr = new Date().toISOString().slice(0, 10)
      const data = await getAppointments({
        fromDate: todayStr,
        toDate: todayStr,
      })
      setTodayAppointments(Array.isArray(data) ? data : [])
    } catch (err) {
      if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Unauthorized: Unable to load clinical dashboard. Please sign in again.')
      } else {
        setError(err.response?.data?.message || 'Failed to load today\'s appointments and clinical queue.')
      }
    } finally {
      setLoading(false)
    }
  }

  const handlePatientSearch = (e) => {
    e.preventDefault()
    if (searchPatientId.trim()) {
      navigate(`/doctor/medical-records?patientId=${encodeURIComponent(searchPatientId.trim())}`)
    }
  }

  const statCards = [
    { label: "Today's Appointments", value: todayAppointments.length.toString(), sub: 'Scheduled encounters' },
    {
      label: 'Checked In / In Queue',
      value: todayAppointments.filter((a) => a.status === 'CheckedIn' || a.status === 'InProgress').length.toString(),
      sub: 'Patients ready for review',
    },
    {
      label: 'Completed Today',
      value: todayAppointments.filter((a) => a.status === 'Completed').length.toString(),
      sub: 'Consultations finished',
    },
    {
      label: 'Pending Follow-ups',
      value: todayAppointments.filter((a) => a.priority === 'Urgent' || a.priority === 'Emergency').length.toString(),
      sub: 'High priority cases',
    },
  ]

  return (
    <DashboardLayout
      role="Doctor"
      navigation={doctorNavigation}
      title="Clinical Dashboard"
      subtitle={`Welcome Dr. ${user?.firstName || ''} ${user?.lastName || ''}. Clinical workstation and patient care management.`}
    >
      {/* Quick Stat Summary */}
      <div className="dashboard-cards mb-6">
        {statCards.map((c, i) => (
          <div key={i} className="stat-card">
            <span>{c.label}</span>
            <strong>{loading ? '...' : c.value}</strong>
            <p>{c.sub}</p>
          </div>
        ))}
      </div>

      {/* Patient Lookup & Clinical Quick Launch */}
      <div className="panel mb-6" style={{ padding: 24 }}>
        <div className="flex flex-wrap justify-between items-center gap-4">
          <div>
            <h2 className="text-lg font-bold m-0" style={{ color: 'var(--color-accent)' }}>
              Patient EMR Quick Search
            </h2>
            <p className="text-xs text-gray-500 m-0 mt-1">
              Search by Patient ID to open their clinical chart, vitals, prescriptions, or diagnostics.
            </p>
          </div>

          <form onSubmit={handlePatientSearch} className="flex gap-2 items-center flex-wrap" style={{ margin: 0 }}>
            <input
              type="number"
              placeholder="Enter Patient ID..."
              style={{ width: 220 }}
              value={searchPatientId}
              onChange={(e) => setSearchPatientId(e.target.value)}
            />
            <button type="submit" className="primary-button" style={{ marginTop: 0 }}>
              Open Clinical Chart
            </button>
          </form>
        </div>
      </div>

      {/* Clinical Workflow Modules Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div
          className="panel p-5 cursor-pointer hover:shadow-md transition-shadow"
          onClick={() => navigate('/doctor/medical-records')}
        >
          <div className="flex items-center gap-3 mb-2">
            <span className="badge badge-primary">EMR</span>
            <h3 className="font-bold text-base m-0" style={{ color: 'var(--color-accent)' }}>
              Medical Records
            </h3>
          </div>
          <p className="text-xs text-gray-600 m-0 mb-3">
            Consultation encounters, clinical diagnoses, treatment plans, and complete medical timelines.
          </p>
          <span className="text-xs font-semibold text-blue-800 hover:underline">
            Launch Medical Records &rarr;
          </span>
        </div>

        <div
          className="panel p-5 cursor-pointer hover:shadow-md transition-shadow"
          onClick={() => navigate('/doctor/prescriptions')}
        >
          <div className="flex items-center gap-3 mb-2">
            <span className="badge badge-success">Rx</span>
            <h3 className="font-bold text-base m-0" style={{ color: 'var(--color-accent)' }}>
              Prescriptions
            </h3>
          </div>
          <p className="text-xs text-gray-600 m-0 mb-3">
            Issue medications, manage dosages, route administration, and track active prescriptions.
          </p>
          <span className="text-xs font-semibold text-green-800 hover:underline">
            Manage Prescriptions &rarr;
          </span>
        </div>

        <div
          className="panel p-5 cursor-pointer hover:shadow-md transition-shadow"
          onClick={() => navigate('/doctor/lab-reports')}
        >
          <div className="flex items-center gap-3 mb-2">
            <span className="badge badge-warning">Lab</span>
            <h3 className="font-bold text-base m-0" style={{ color: 'var(--color-accent)' }}>
              Diagnostic & Lab Reports
            </h3>
          </div>
          <p className="text-xs text-gray-600 m-0 mb-3">
            Order laboratory and imaging investigations, review findings, and record diagnostic reports.
          </p>
          <span className="text-xs font-semibold text-amber-800 hover:underline">
            View Diagnostic Orders &rarr;
          </span>
        </div>
      </div>

      {/* Today's Appointments & Patient Queue */}
      <div className="panel" style={{ padding: 24 }}>
        <div className="flex justify-between items-center mb-4 pb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <h2 className="text-lg font-bold m-0" style={{ color: 'var(--color-accent)' }}>
              Today's Clinical Encounters
            </h2>
            <span className="text-xs text-gray-500">
              Patients scheduled for consultation today
            </span>
          </div>
          <button className="secondary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={fetchDashboardData}>
            Refresh
          </button>
        </div>

        {error && (
          <div className="form-error mb-4 flex justify-between items-center" role="alert">
            <span>{error}</span>
            <button type="button" className="link-button text-xs" onClick={fetchDashboardData}>
              Retry
            </button>
          </div>
        )}

        {loading ? (
          <p className="placeholder-text py-8 text-center">Loading today's clinical encounters...</p>
        ) : todayAppointments.length === 0 ? (
          <div className="text-center py-10 text-gray-500">
            <p className="font-semibold text-base mb-1">No appointments scheduled for today.</p>
            <p className="text-xs">Use the search box above to look up any patient's medical records directly.</p>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Patient</th>
                  <th>Reference #</th>
                  <th>Priority</th>
                  <th>Status</th>
                  <th>Reason / Notes</th>
                  <th>Clinical Actions</th>
                </tr>
              </thead>
              <tbody>
                {todayAppointments.map((apt) => (
                  <tr key={apt.id}>
                    <td>
                      <strong>
                        {apt.scheduledStart
                          ? new Date(apt.scheduledStart).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
                          : 'TBD'}
                      </strong>
                    </td>
                    <td>
                      <div>
                        <strong>{apt.patientName || `Patient #${apt.patientId}`}</strong>
                        <div className="text-xs text-gray-500">ID: #{apt.patientId}</div>
                      </div>
                    </td>
                    <td>{apt.referenceNumber}</td>
                    <td>
                      <PriorityBadge priority={apt.priority} />
                    </td>
                    <td>
                      <AppointmentStatusBadge status={apt.status} />
                    </td>
                    <td className="text-xs max-w-xs truncate" title={apt.reason}>
                      {apt.reason || '-'}
                    </td>
                    <td>
                      <div className="flex gap-2">
                        <button
                          type="button"
                          className="primary-button text-xs px-2.5 py-1"
                          style={{ marginTop: 0 }}
                          onClick={() => navigate(`/doctor/medical-records?patientId=${apt.patientId}&appointmentId=${apt.id}`)}
                        >
                          Open EMR
                        </button>
                        <button
                          type="button"
                          className="secondary-button text-xs px-2.5 py-1"
                          style={{ marginTop: 0 }}
                          onClick={() => navigate(`/doctor/prescriptions?patientId=${apt.patientId}`)}
                        >
                          Prescribe
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </DashboardLayout>
  )
}
