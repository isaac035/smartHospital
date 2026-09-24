import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { getAppointmentById, rescheduleAppointment } from '../../services/appointmentService'
import SlotPicker from '../../components/appointments/SlotPicker'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function RescheduleAppointment() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav // Doctors can't reschedule

  const [appointment, setAppointment] = useState(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const [formError, setFormError] = useState(null)

  const [date, setDate] = useState('')
  const [selectedSlot, setSelectedSlot] = useState('')
  const [reason, setReason] = useState('')

  useEffect(() => {
    fetchData()
  }, [id])

  const fetchData = async () => {
    try {
      setLoading(true)
      const data = await getAppointmentById(id)
      setAppointment(data)
      
      // Default date to today or existing start
      const existing = new Date(data.scheduledStart)
      if (existing > new Date()) {
        setDate(existing.toISOString().split('T')[0])
      } else {
        setDate(new Date().toISOString().split('T')[0])
      }
    } catch (err) {
      setError('Failed to load appointment details')
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setFormError(null)

    if (!selectedSlot) {
      setFormError('Please select a new time slot.')
      return
    }

    try {
      setSubmitting(true)
      await rescheduleAppointment(id, {
        newScheduledStart: selectedSlot,
        newEstimatedDurationMinutes: appointment.estimatedDurationMinutes,
        reason: reason
      })
      navigate(`/${role.toLowerCase()}/appointments/${id}`)
    } catch (err) {
      setFormError(err.response?.data?.message || 'Failed to reschedule appointment')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <DashboardLayout role={role} navigation={navigation} title="Reschedule" subtitle="..."><div className="p-8 text-center">Loading...</div></DashboardLayout>
  if (error || !appointment) return <DashboardLayout role={role} navigation={navigation} title="Reschedule" subtitle="..."><div className="form-error">{error}</div></DashboardLayout>

  return (
    <DashboardLayout role={role} navigation={navigation} title="Reschedule Appointment" subtitle={`Ref: ${appointment.referenceNumber}`}>
      <div className="login-card" style={{ width: '100%', maxWidth: '800px', margin: '0 auto' }}>
        <button type="button" className="text-sm font-semibold hover:underline mb-4 inline-block" style={{ color: 'var(--color-accent)', background: 'transparent', border: 0, padding: 0 }} onClick={() => navigate(-1)}>
          &larr; Back to Details
        </button>

        <div className="mb-6 p-4 rounded-md border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))' }}>
          <h3 className="font-bold mb-2" style={{ color: 'var(--color-accent)' }}>Current Appointment Info</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div><span className="font-bold">Patient:</span> {appointment.patientName}</div>
            <div><span className="font-bold">Doctor:</span> {appointment.doctorName}</div>
            <div><span className="font-bold">Current Date:</span> {new Date(appointment.scheduledStart).toLocaleString()}</div>
            <div><span className="font-bold">Duration:</span> {appointment.estimatedDurationMinutes} mins</div>
          </div>
        </div>

        {formError && <div className="form-error mb-4">{formError}</div>}

        <form onSubmit={handleSubmit} className="grid gap-6">
          <div className="flex flex-col gap-1">
            <label>Select New Date *</label>
            <input 
              type="date" 
              required
              value={date}
              min={new Date().toISOString().split('T')[0]}
              onChange={(e) => {
                setDate(e.target.value)
                setSelectedSlot('') // reset slot on date change
              }}
            />
          </div>

          <div className="flex flex-col gap-1">
            <label>Select New Time Slot *</label>
            <SlotPicker 
              doctorId={appointment.doctorId}
              date={date}
              selectedSlot={selectedSlot}
              onSlotSelect={setSelectedSlot}
              durationMinutes={appointment.estimatedDurationMinutes}
            />
          </div>

          <div className="flex flex-col gap-1 mt-2">
            <label>Reason for Rescheduling *</label>
            <textarea 
              required
              rows="2"
              value={reason}
              onChange={e => setReason(e.target.value)}
              className="border rounded-md px-3 py-2"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))', outline: 'none' }}
              placeholder="e.g. Patient requested a different day"
            />
          </div>

          <div className="flex gap-2 justify-end mt-4">
            <button type="button" className="secondary-button" onClick={() => navigate(-1)} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="primary-button" style={{ marginTop: 0 }} disabled={submitting}>
              {submitting ? 'Saving...' : 'Confirm Reschedule'}
            </button>
          </div>
        </form>
      </div>
    </DashboardLayout>
  )
}
