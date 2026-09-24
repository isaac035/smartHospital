import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { bookAppointment } from '../../services/appointmentService'
import SlotPicker from '../../components/appointments/SlotPicker'
import PatientLookupInput from '../../components/appointments/PatientLookupInput'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function BookAppointment() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [patientId, setPatientId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  // Hardcoding options since Doctor Management is another module's responsibility
  const doctorOptions = [
    { id: 2, name: 'Dr. Bob' },
    { id: 10, name: 'Dr. Smith' }
  ]

  const [appointmentType, setAppointmentType] = useState('1') // General
  const [priority, setPriority] = useState('1') // Normal
  const [date, setDate] = useState('')
  const [selectedSlot, setSelectedSlot] = useState('')
  const [duration, setDuration] = useState('30')
  const [notes, setNotes] = useState('')

  const handleSubmit = async (e) => {
    e.preventDefault()
    setFormError(null)

    if (!selectedSlot) {
      setFormError('Please select an available time slot.')
      return
    }

    try {
      setSubmitting(true)
      const result = await bookAppointment({
        patientId: parseInt(patientId),
        doctorId: parseInt(doctorId),
        appointmentType: parseInt(appointmentType),
        scheduledStart: selectedSlot,
        estimatedDurationMinutes: parseInt(duration),
        priority: parseInt(priority),
        notes: notes
      })
      navigate(`/${role.toLowerCase()}/appointments/${result.id}`)
    } catch (err) {
      setFormError(err.response?.data?.message || 'Failed to book appointment')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <DashboardLayout role={role} navigation={navigation} title="Book Appointment" subtitle="Create a new appointment for a patient.">
      <div className="login-card" style={{ width: '100%', maxWidth: '800px', margin: '0 auto' }}>
        
        {formError && <div className="form-error mb-4">{formError}</div>}

        <form onSubmit={handleSubmit} className="grid gap-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <PatientLookupInput value={patientId} onChange={setPatientId} />
            
            <div className="flex flex-col gap-1">
              <label>Doctor *</label>
              <select required value={doctorId} onChange={e => { setDoctorId(e.target.value); setSelectedSlot('') }}>
                <option value="">Select a Doctor</option>
                {doctorOptions.map(d => (
                  <option key={d.id} value={d.id}>{d.name}</option>
                ))}
              </select>
            </div>
            
            <div className="flex flex-col gap-1">
              <label>Type *</label>
              <select required value={appointmentType} onChange={e => setAppointmentType(e.target.value)}>
                <option value="1">General</option>
                <option value="2">FollowUp</option>
                <option value="3">Consultation</option>
                <option value="4">Checkup</option>
                <option value="5">Vaccination</option>
                <option value="6">Procedure</option>
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label>Priority *</label>
              <select required value={priority} onChange={e => setPriority(e.target.value)}>
                <option value="1">Normal</option>
                <option value="2">Urgent</option>
                <option value="3">Emergency</option>
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label>Estimated Duration (mins) *</label>
              <select required value={duration} onChange={e => setDuration(e.target.value)}>
                <option value="15">15 mins</option>
                <option value="30">30 mins</option>
                <option value="45">45 mins</option>
                <option value="60">60 mins</option>
              </select>
            </div>
          </div>

          <div className="border-t pt-6" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
            <h3 className="font-bold mb-4" style={{ color: 'var(--color-accent)' }}>Schedule</h3>
            
            <div className="flex flex-col gap-1 mb-4" style={{ maxWidth: '300px' }}>
              <label>Date *</label>
              <input 
                type="date" 
                required
                value={date}
                min={new Date().toISOString().split('T')[0]}
                onChange={(e) => {
                  setDate(e.target.value)
                  setSelectedSlot('')
                }}
              />
            </div>

            <div className="flex flex-col gap-1">
              <label>Time Slot *</label>
              <SlotPicker 
                doctorId={doctorId}
                date={date}
                selectedSlot={selectedSlot}
                onSlotSelect={setSelectedSlot}
                durationMinutes={parseInt(duration)}
              />
            </div>
          </div>

          <div className="flex flex-col gap-1">
            <label>Notes</label>
            <textarea 
              rows="3"
              value={notes}
              onChange={e => setNotes(e.target.value)}
              className="border rounded-md px-3 py-2"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))', outline: 'none' }}
              placeholder="Any additional details..."
            />
          </div>

          <div className="flex gap-2 justify-end mt-4">
            <button type="button" className="secondary-button" onClick={() => navigate(-1)} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className="primary-button" style={{ marginTop: 0 }} disabled={submitting}>
              {submitting ? 'Booking...' : 'Book Appointment'}
            </button>
          </div>
        </form>
      </div>
    </DashboardLayout>
  )
}
