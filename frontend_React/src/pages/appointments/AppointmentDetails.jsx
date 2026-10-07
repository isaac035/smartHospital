import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { getAppointmentById, getAppointmentHistory, cancelAppointment, confirmAppointment, confirmEmergency, checkIn, updateAppointmentPriority } from '../../services/appointmentService'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import { useAuth } from '../../hooks/useAuth'
import { useSignalR } from '../../hooks/useSignalR'
import { appointmentManagerNavigation } from './appointmentManagerNavigation'
import FieldError from '../../components/common/FieldError'
import { parseServerErrors, text } from '../../utils/validators'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']
const doctorNav = ['Dashboard', 'My Appointments', 'My Patients', 'Medical Records', 'Prescriptions', 'Lab Reports']

export default function AppointmentDetails() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'AppointmentManager' ? appointmentManagerNavigation : role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav
  const routeRole = role === 'AppointmentManager' ? 'admin' : role.toLowerCase()

  const [appointment, setAppointment] = useState(null)
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [cancelReason, setCancelReason] = useState('')
  const [showCancelModal, setShowCancelModal] = useState(false)
  const [cancelReasonError, setCancelReasonError] = useState('')
  const validateCancelReason = (value) => text(value, 'Cancellation reason', { isRequired: true, max: 500 }) || ''
  const [priorityDraft, setPriorityDraft] = useState(1)
  const [prioritySaving, setPrioritySaving] = useState(false)

  const fetchData = useCallback(async () => {
    try {
      setLoading(true)
      const data = await getAppointmentById(id)
      setAppointment(data)
      setPriorityDraft(({ Normal: 1, Urgent: 2, Emergency: 3 })[data.priority] || 1)

      // Fetch history if not a doctor (or if doctor is allowed, backend will enforce)
      try {
        const histData = await getAppointmentHistory(id)
        setHistory(histData)
      } catch {
        // Ignore if forbidden
      }

      setError(null)
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load appointment details')
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  useSignalR({
    AppointmentUpdated: (updated) => {
      if (!updated?.id || String(updated.id) === String(id)) fetchData()
    },
    AppointmentCancelled: (updated) => {
      if (!updated?.id || String(updated.id) === String(id)) fetchData()
    },
  })

  const handleCancel = async (e) => {
    e.preventDefault()
    const reasonError = validateCancelReason(cancelReason)
    setCancelReasonError(reasonError)
    if (reasonError) {
      document.getElementById('cancelReason')?.focus()
      return
    }
    try {
      await cancelAppointment(id, cancelReason)
      setShowCancelModal(false)
      setCancelReason('')
      fetchData() // Refresh
    } catch (err) {
      const { fieldErrors, message } = parseServerErrors(err, { fields: ['reason'], fallback: 'Failed to cancel appointment' })
      if (fieldErrors.reason) setCancelReasonError(fieldErrors.reason)
      if (message) alert(message)
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

  const handleConfirm = async () => {
    try {
      const updated = await confirmAppointment(id)
      setAppointment(updated)
      const updatedHistory = await getAppointmentHistory(id)
      setHistory(updatedHistory)
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to confirm appointment')
      // The appointment may have changed while this details view was open.
      fetchData()
    }
  }

  const handlePrioritySave = async () => {
    try {
      setPrioritySaving(true)
      const updated = await updateAppointmentPriority(id, priorityDraft)
      setAppointment(updated)
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to update appointment priority')
      fetchData()
    } finally {
      setPrioritySaving(false)
    }
  }


  const handleDoctorComplete = async () => {
    try {
      const qStatus = await getQueue(appointment.doctorId);
      const queueEntries = qStatus.queue || [];
      const entry = queueEntries.find(q => q.appointmentId === appointment.id);
      if (!entry) {
        alert('Could not find active queue entry for this appointment.');
        return;
      }
      await markCompleted(entry.id);
      fetchData();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to complete appointment');
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
  const canCancel = ['Staff', 'Admin', 'AppointmentManager'].includes(role) && !isTerminal
  const canCheckIn = (role === 'Staff' || role === 'Admin') && ['Scheduled', 'Confirmed'].includes(appointment.status)
  const canConfirm = ['Staff', 'Admin', 'AppointmentManager'].includes(role) && appointment.status === 'Scheduled'
  const needsEmergencyConfirm = ['Staff', 'Admin', 'AppointmentManager'].includes(role) && appointment.priority === 'Emergency' && !appointment.priorityNeedsReview && !appointment.emergencyConfirmed && appointment.status === 'Scheduled'

  return (
    <DashboardLayout role={role} navigation={navigation} title={`Appointment ${appointment.referenceNumber}`} subtitle="Detailed view and actions">

      <div className="flex justify-between items-center mb-6">
        <button className="text-sm font-semibold hover:underline" style={{ color: 'var(--color-accent)' }} onClick={() => navigate(-1)}>
          &larr; Back to List
        </button>
        <div className="flex gap-2">

          {role === 'Doctor' && appointment.status === 'InProgress' && (
            <button className="primary-button" style={{ marginTop: 0 }} onClick={handleDoctorComplete}>
              Mark Completed
            </button>
          )}

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
          {['Staff', 'Admin', 'AppointmentManager'].includes(role) && !isTerminal && (
            <Link to={`/${routeRole}/appointments/${id}/reschedule`} className="secondary-button text-center flex items-center justify-center" style={{ textDecoration: 'none' }}>
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
                {canConfirm && (
                  <button className="primary-button" style={{ marginTop: 0 }} onClick={handleConfirm}>
                    Confirm
                  </button>
                )}
              </div>
              {(role === 'Staff' || role === 'Admin' || role === 'AppointmentManager') && (
                <div className="flex items-center gap-2 mt-2">
                  <label htmlFor="appointment-priority" className="text-sm">Change priority</label>
                  <select
                    id="appointment-priority"
                    className="border rounded px-2 py-1 text-sm"
                    value={priorityDraft}
                    disabled={prioritySaving}
                    onChange={(event) => setPriorityDraft(Number(event.target.value))}
                  >
                    <option value={1}>Normal</option>
                    <option value={2}>Urgent</option>
                    <option value={3}>Emergency</option>
                  </select>
                  {(appointment.priorityNeedsReview || priorityDraft !== ({ Normal: 1, Urgent: 2, Emergency: 3 })[appointment.priority]) && (
                    <button className="secondary-button" disabled={prioritySaving} onClick={handlePrioritySave}>
                      {prioritySaving ? 'Saving…' : appointment.priorityNeedsReview ? 'Approve priority' : 'Save'}
                    </button>
                  )}
                  {appointment.priorityNeedsReview && (
                    <span className="text-xs text-gray-500">Patient requested priority; it does not affect queue order until approved.</span>
                  )}
                </div>
              )}
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

        {appointment.reservedResources?.length > 0 && (
          <div className="stat-card" style={{ padding: '24px' }}>
            <h2 className="text-lg font-bold mb-4" style={{ color: 'var(--color-accent)' }}>Reserved Resources</h2>
            <div className="grid gap-4">
              {appointment.reservedResources.map((resource) => (
                <div key={resource.id} className="border rounded p-3">
                  <div className="font-semibold">{resource.name}{resource.kind === 'equipment' && resource.code ? ` · ${resource.code}` : ''}</div>
                  <div className="text-sm">{resource.type} · {resource.status}</div>
                  <div className="text-sm text-gray-600">{resource.location}</div>
                </div>
              ))}
            </div>
          </div>
        )}

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
            <form onSubmit={handleCancel} noValidate>
              <div className="flex flex-col gap-1">
                <label htmlFor="cancelReason" className="required">Cancellation Reason</label>
                <textarea
                  id="cancelReason"
                  name="reason"
                  required
                  rows="3"
                  maxLength={500}
                  value={cancelReason}
                  aria-invalid={cancelReasonError ? 'true' : undefined}
                  aria-describedby={cancelReasonError ? 'cancelReason-error' : undefined}
                  onBlur={() => setCancelReasonError(validateCancelReason(cancelReason))}
                  onChange={e => { setCancelReason(e.target.value); if (cancelReasonError) setCancelReasonError(validateCancelReason(e.target.value)) }}
                  className="border rounded-md px-3 py-2"
                  style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))', outline: 'none' }}
                  placeholder="e.g. Patient requested cancellation"
                />
                <FieldError name="cancelReason" message={cancelReasonError} />
              </div>
              <div className="flex gap-2 mt-4 justify-end">
                <button type="button" className="secondary-button" onClick={() => { setShowCancelModal(false); setCancelReasonError('') }}>Back</button>
                <button type="submit" className="primary-button" style={{ marginTop: 0, backgroundColor: '#b91c1c', borderColor: '#b91c1c' }}>Confirm Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </DashboardLayout>
  )
}
