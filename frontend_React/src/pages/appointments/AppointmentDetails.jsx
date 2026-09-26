import { useState, useEffect } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { getAppointmentById, getAppointmentHistory, cancelAppointment, confirmEmergency, checkIn } from '../../services/appointmentService'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']
const doctorNav = ['Dashboard', 'My Appointments', 'My Patients', 'Medical Records', 'Prescriptions', 'Lab Reports']

export default function AppointmentDetails() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav

  const [appointment, setAppointment] = useState(null)
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [cancelReason, setCancelReason] = useState('')
  const [showCancelModal, setShowCancelModal] = useState(false)

  useEffect(() => {
    fetchData()
  }, [id])

  const fetchData = async () => {
    try {
      setLoading(true)
      const data = await getAppointmentById(id)
      setAppointment(data)
      
      // Fetch history if not a doctor (or if doctor is allowed, backend will enforce)
      try {
        const histData = await getAppointmentHistory(id)
        setHistory(histData)
      } catch (e) {
        // Ignore if forbidden
      }
      
      setError(null)
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load appointment details')
    } finally {
      setLoading(false)
    }
  }

  const handleCancel = async (e) => {
    e.preventDefault()
    try {
      await cancelAppointment(id, cancelReason)
      setShowCancelModal(false)
      fetchData() // Refresh
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to cancel appointment')
    }
  }

  const handleConfirmEmergency = async () => {
    try {
      await confirmEmergency(id)
      fetchData()
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to confirm emergency')
    }
  }

  const handleCheckIn = async () => {
    try {
      await checkIn(id)
      fetchData()
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to check in')
    }
  }

  if (loading) return (
    <DashboardLayout role={role} navigation={navigation} title="Appointment Details" subtitle="...">
      <div className="p-8 text-center">Loading...</div>
    </DashboardLayout>
  )

  if (error || !appointment) return (
    <DashboardLayout role={role} navigation={navigation} title="Appointment Details" subtitle="...">
      <div className="form-error">{error || 'Appointment not found'}</div>
      <button className="secondary-button mt-4" onClick={() => navigate(-1)}>Go Back</button>
    </DashboardLayout>
  )

  const isTerminal = ['Completed', 'Cancelled', 'NoShow', 'Rescheduled'].includes(appointment.status)
  const canCancel = (role === 'Staff' || role === 'Admin') && !isTerminal
  const canCheckIn = (role === 'Staff' || role === 'Admin') && appointment.status === 'Confirmed'
  const needsEmergencyConfirm = (role === 'Staff' || role === 'Admin') && appointment.priority === 'Emergency' && !appointment.emergencyConfirmed && appointment.status === 'Scheduled'

  return (
    <DashboardLayout role={role} navigation={navigation} title={`Appointment ${appointment.referenceNumber}`} subtitle="Detailed view and actions">
      
      <div className="flex justify-between items-center mb-6">
        <button className="text-sm font-semibold hover:underline" style={{ color: 'var(--color-accent)' }} onClick={() => navigate(-1)}>
          &larr; Back to List
        </button>
        <div className="flex gap-2">
          {canCheckIn && (
            <button className="primary-button" style={{ marginTop: 0 }} onClick={handleCheckIn}>
              Check In Patient
            </button>
          )}
          {needsEmergencyConfirm && (
            <button className="primary-button" style={{ marginTop: 0, backgroundColor: '#b91c1c', borderColor: '#b91c1c' }} onClick={handleConfirmEmergency}>
              Confirm Emergency
            </button>
          )}
          {canCancel && (
            <button className="secondary-button" onClick={() => setShowCancelModal(true)}>
              Cancel Appointment
            </button>
          )}
          {(role === 'Staff' || role === 'Admin') && !isTerminal && (
            <Link to={`/${role.toLowerCase()}/appointments/${id}/reschedule`} className="secondary-button text-center flex items-center justify-center" style={{ textDecoration: 'none' }}>
              Reschedule
            </Link>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="stat-card" style={{ padding: '24px' }}>
          <h2 className="text-lg font-bold mb-4" style={{ color: 'var(--color-accent)' }}>Details</h2>
          <div className="grid gap-4">
            <div>
              <div className="text-sm font-bold" style={{ color: 'var(--color-accent)' }}>Patient</div>
              <div>{appointment.patientName}</div>
            </div>
            <div>
              <div className="text-sm font-bold" style={{ color: 'var(--color-accent)' }}>Doctor</div>
              <div>{appointment.doctorName || 'Unassigned'}</div>
            </div>
            <div>
              <div className="text-sm font-bold" style={{ color: 'var(--color-accent)' }}>Department</div>
              <div>{appointment.departmentName || '-'}</div>
            </div>
            <div>
              <div className="text-sm font-bold" style={{ color: 'var(--color-accent)' }}>Date & Time</div>
              <div>{new Date(appointment.scheduledStart).toLocaleString()} ({appointment.estimatedDurationMinutes} min)</div>
            </div>
            <div>
              <div className="text-sm font-bold" style={{ color: 'var(--color-accent)' }}>Type</div>
              <div>{appointment.appointmentType.replace(/([A-Z])/g, ' $1').trim()}</div>
            </div>
            <div>
              <div className="text-sm font-bold" style={{ color: 'var(--color-accent)' }}>Status & Priority</div>
              <div className="flex gap-2 mt-1">
                <AppointmentStatusBadge status={appointment.status} />
                <PriorityBadge priority={appointment.priority} />
              </div>
            </div>
            {appointment.notes && (
              <div>
                <div className="text-sm font-bold" style={{ color: 'var(--color-accent)' }}>Notes</div>
                <div className="p-3 mt-1 bg-gray-50 rounded border text-sm" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                  {appointment.notes}
                </div>
              </div>
            )}
            {appointment.cancelledReason && (
              <div>
                <div className="text-sm font-bold text-red-700">Cancellation Reason</div>
                <div className="p-3 mt-1 bg-red-50 text-red-800 rounded border border-red-200 text-sm">
                  {appointment.cancelledReason}
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="stat-card" style={{ padding: '24px' }}>
          <h2 className="text-lg font-bold mb-4" style={{ color: 'var(--color-accent)' }}>Status History</h2>
          {history.length === 0 ? (
            <p>No history available.</p>
          ) : (
            <div className="flex flex-col gap-4 relative">
              <div className="absolute left-3 top-2 bottom-2 w-0.5" style={{ background: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}></div>
              {history.map(h => (
                <div key={h.id} className="relative pl-8">
                  <div className="absolute left-1.5 top-1.5 w-3.5 h-3.5 rounded-full" style={{ background: 'var(--color-accent)', border: '2px solid var(--color-primary)' }}></div>
                  <div className="text-sm font-bold">{h.newStatus}</div>
                  <div className="text-xs mt-0.5" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
                    {new Date(h.changedAt).toLocaleString()} • By {h.changedByName}
                  </div>
                  {h.reason && <div className="text-sm mt-1">{h.reason}</div>}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {showCancelModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="login-card" style={{ width: '400px' }}>
            <h2 className="text-xl font-bold mb-4" style={{ color: 'var(--color-accent)' }}>Cancel Appointment</h2>
            <form onSubmit={handleCancel}>
              <div className="flex flex-col gap-1">
                <label>Cancellation Reason *</label>
                <textarea 
                  required
                  rows="3"
                  value={cancelReason}
                  onChange={e => setCancelReason(e.target.value)}
                  className="border rounded-md px-3 py-2"
                  style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))', outline: 'none' }}
                  placeholder="e.g. Patient requested cancellation"
                />
              </div>
              <div className="flex gap-2 mt-4 justify-end">
                <button type="button" className="secondary-button" onClick={() => setShowCancelModal(false)}>Back</button>
                <button type="submit" className="primary-button" style={{ marginTop: 0, backgroundColor: '#b91c1c', borderColor: '#b91c1c' }}>Confirm Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </DashboardLayout>
  )
}
