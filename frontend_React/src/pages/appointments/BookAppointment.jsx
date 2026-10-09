import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { bookAppointment } from '../../services/appointmentService'
import { createWalkInPatient } from '../../services/userService'
import SlotPicker from '../../components/appointments/SlotPicker'
import PatientLookupInput from '../../components/appointments/PatientLookupInput'
import { useAuth } from '../../hooks/useAuth'
import { useBookableDoctors } from '../../hooks/useBookableDoctors'
import { useFormValidation } from '../../hooks/useFormValidation'
import FieldError from '../../components/common/FieldError'
import { first, notBeforeToday, number, personName, required, selection, text } from '../../utils/validators'

const BOOKING_SCHEMA = {
  // A picked suggestion is an existing patient; a typed name must be a valid new patient name.
  patientName: (value, values) => values.selectedPatientId ? undefined : personName(value, 'Patient name'),
  doctorId: (value) => selection(value, 'a doctor'),
  appointmentType: (value) => selection(value, 'an appointment type'),
  priority: (value) => selection(value, 'a priority'),
  duration: (value) => number(value, 'Duration', { isRequired: true, min: 10, max: 480, integer: true, message: 'Duration must be between 10 and 480 minutes.' }),
  date: (value) => first(required(value, 'Date'), notBeforeToday(value, 'Appointment date cannot be in the past.')),
  selectedSlot: (value) => (value ? undefined : 'Please select an available time slot.'),
  notes: (value) => text(value, 'Notes', { max: 1000 }),
}

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function BookAppointment() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [patientName, setPatientName] = useState('')
  const [selectedPatient, setSelectedPatient] = useState(null)
  const [doctorId, setDoctorId] = useState('')
  const doctorOptions = useBookableDoctors()

  const [appointmentType, setAppointmentType] = useState('1') // General
  const [priority, setPriority] = useState('1') // Normal
  const [date, setDate] = useState('')
  const [selectedSlot, setSelectedSlot] = useState('')
  const [duration, setDuration] = useState('30')
  const [notes, setNotes] = useState('')
  const formValues = { patientName, selectedPatientId: selectedPatient?.id ?? null, doctorId, appointmentType, priority, duration, date, selectedSlot, notes }
  const validation = useFormValidation(BOOKING_SCHEMA, formValues)
  const touch = (name) => () => validation.touch(name, formValues)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setFormError(null)

    if (!validation.validateAll(formValues, e.currentTarget)) return

    try {
      setSubmitting(true)
      let patient = selectedPatient
      if (!patient) {
        // Typed name not picked from suggestions: create the patient once, then keep it selected for retries.
        patient = await createWalkInPatient(patientName.trim())
        setSelectedPatient(patient)
        setPatientName(patient.displayName)
      }
      const result = await bookAppointment({
        patientId: patient.id,
        doctorId: parseInt(doctorId),
        appointmentType: parseInt(appointmentType),
        scheduledStart: selectedSlot,
        estimatedDurationMinutes: parseInt(duration),
        priority: parseInt(priority),
        notes: notes
      })
      navigate(`/${role.toLowerCase()}/appointments/${result.id}`)
    } catch (err) {
      setFormError(validation.applyServerErrors(err, {
        conflicts: [
          { match: /slot|future|capacity|scheduled|available/i, field: 'selectedSlot' },
          { match: /patient/i, field: 'patientName' },
          { match: /doctor/i, field: 'doctorId' },
        ],
        rename: { fullName: 'patientName' },
        fallback: 'Failed to book appointment',
      }) || null)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <DashboardLayout role={role} navigation={navigation} title="Book Appointment" subtitle="Create a new appointment for a patient.">
      <div className="login-card" style={{ width: '100%', maxWidth: '800px', margin: '0 auto' }}>

        {formError && <div className="form-error mb-4">{formError}</div>}

        <form onSubmit={handleSubmit} noValidate className="grid gap-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <PatientLookupInput
              value={patientName}
              selectedPatient={selectedPatient}
              onChange={(text) => { setPatientName(text); setSelectedPatient(null) }}
              onSelect={(patient) => { setSelectedPatient(patient); setPatientName(patient.displayName) }}
              onBlur={touch('patientName')}
              error={validation.errorFor('patientName')}
            />

            <div className="flex flex-col gap-1">
              <label htmlFor="doctorId" className="required">Doctor</label>
              <select id="doctorId" name="doctorId" required value={doctorId} onBlur={touch('doctorId')} {...validation.fieldProps('doctorId')} onChange={e => { setDoctorId(e.target.value); setSelectedSlot('') }}>
                <option value="">Select a Doctor</option>
                {doctorOptions.map(d => (
                  <option key={d.id} value={d.id}>{d.name}</option>
                ))}
              </select>
              <FieldError name="doctorId" message={validation.errorFor('doctorId')} />
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="appointmentType" className="required">Type</label>
              <select id="appointmentType" name="appointmentType" required value={appointmentType} onBlur={touch('appointmentType')} {...validation.fieldProps('appointmentType')} onChange={e => setAppointmentType(e.target.value)}>
                <option value="1">General</option>
                <option value="2">FollowUp</option>
                <option value="3">Consultation</option>
                <option value="4">Checkup</option>
                <option value="5">Vaccination</option>
                <option value="6">Procedure</option>
              </select>
              <FieldError name="appointmentType" message={validation.errorFor('appointmentType')} />
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="priority" className="required">Priority</label>
              <select id="priority" name="priority" required value={priority} onBlur={touch('priority')} {...validation.fieldProps('priority')} onChange={e => setPriority(e.target.value)}>
                <option value="1">Normal</option>
                <option value="2">Urgent</option>
                <option value="3">Emergency</option>
              </select>
              <FieldError name="priority" message={validation.errorFor('priority')} />
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="duration" className="required">Estimated Duration (mins)</label>
              <select id="duration" name="duration" required value={duration} onBlur={touch('duration')} {...validation.fieldProps('duration')} onChange={e => setDuration(e.target.value)}>
                <option value="15">15 mins</option>
                <option value="30">30 mins</option>
                <option value="45">45 mins</option>
                <option value="60">60 mins</option>
              </select>
              <FieldError name="duration" message={validation.errorFor('duration')} />
            </div>
          </div>

          <div className="border-t pt-6" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
            <h3 className="font-bold mb-4" style={{ color: 'var(--color-accent)' }}>Schedule</h3>

            <div className="flex flex-col gap-1 mb-4" style={{ maxWidth: '300px' }}>
              <label htmlFor="date" className="required">Date</label>
              <input
                id="date"
                name="date"
                type="date"
                required
                onBlur={touch('date')}
                {...validation.fieldProps('date')}
                value={date}
                min={new Date().toISOString().split('T')[0]}
                onChange={(e) => {
                  setDate(e.target.value)
                  setSelectedSlot('')
                }}
              />
              <FieldError name="date" message={validation.errorFor('date')} />
            </div>

            <div className="flex flex-col gap-1">
              <label className="required">Time Slot</label>
              <SlotPicker
                doctorId={doctorId}
                date={date}
                selectedSlot={selectedSlot}
                onSlotSelect={setSelectedSlot}
                durationMinutes={parseInt(duration)}
              />
              <FieldError name="selectedSlot" message={validation.errorFor('selectedSlot')} />
            </div>
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="notes">Notes</label>
            <textarea
              id="notes"
              name="notes"
              rows="3"
              maxLength={1000}
              onBlur={touch('notes')}
              {...validation.fieldProps('notes')}
              value={notes}
              onChange={e => setNotes(e.target.value)}
              className="border rounded-md px-3 py-2"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))', outline: 'none' }}
              placeholder="Any additional details..."
            />
            <FieldError name="notes" message={validation.errorFor('notes')} />
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
