import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Calendar, dateFnsLocalizer } from 'react-big-calendar'
import format from 'date-fns/format'
import parse from 'date-fns/parse'
import startOfWeek from 'date-fns/startOfWeek'
import getDay from 'date-fns/getDay'
import addDays from 'date-fns/addDays'
import enUS from 'date-fns/locale/en-US'
import 'react-big-calendar/lib/css/react-big-calendar.css'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation as navigation } from './adminNavigation'
import { listDoctors } from '../../services/doctorService'
import { listConsultationTypes } from '../../services/consultationTypeService'
import { createSchedule, listSchedules, removeSchedule } from '../../services/scheduleService'

const DAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']

const locales = { 'en-US': enUS }
const localizer = dateFnsLocalizer({
  format,
  parse,
  startOfWeek: () => startOfWeek(new Date(), { weekStartsOn: 1 }),
  getDay,
  locales,
})

function toTimeString(date) {
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:00`
}

function ScheduleFormModal({ doctorId, consultationTypes, initialSlot, onClose, onSaved }) {
  const defaultDay = initialSlot ? DAY_NAMES[(initialSlot.start.getDay() + 6) % 7] : DAY_NAMES[0]
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: {
      consultationTypeId: consultationTypes[0]?.id || '',
      dayOfWeek: defaultDay,
      startTime: initialSlot ? toTimeString(initialSlot.start).slice(0, 5) : '09:00',
      endTime: initialSlot ? toTimeString(initialSlot.end).slice(0, 5) : '10:00',
    },
  })

  const onSubmit = async (values) => {
    try {
      await createSchedule({
        doctorId: Number(doctorId),
        consultationTypeId: Number(values.consultationTypeId),
        dayOfWeek: values.dayOfWeek,
        startTime: `${values.startTime}:00`,
        endTime: `${values.endTime}:00`,
      })
      onSaved()
    } catch (requestError) {
      setError('root', { message: requestError.response?.data?.message || 'Something went wrong. Please try again.' })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true">
    <div className="modal-card">
      <h2>Add Availability Slot</h2>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <label htmlFor="dayOfWeek">Day of week</label>
        <select id="dayOfWeek" {...register('dayOfWeek', { required: true })}>
          {DAY_NAMES.map((day) => <option key={day} value={day}>{day}</option>)}
        </select>
        <label htmlFor="consultationTypeId">Consultation type</label>
        <select id="consultationTypeId" {...register('consultationTypeId', { required: true })}>
          {consultationTypes.map((type) => <option key={type.id} value={type.id}>{type.name} ({type.durationMinutes} min)</option>)}
        </select>
        <div className="field-row">
          <div><label htmlFor="startTime">Start time</label><input id="startTime" type="time" {...register('startTime', { required: 'Required.' })} /></div>
          <div><label htmlFor="endTime">End time</label><input id="endTime" type="time" {...register('endTime', { required: 'Required.' })} /></div>
        </div>
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Save'}</button>
        </div>
      </form>
    </div>
  </div>
}

export default function AvailabilityCalendar() {
  const [doctors, setDoctors] = useState([])
  const [consultationTypes, setConsultationTypes] = useState([])
  const [doctorId, setDoctorId] = useState('')
  const [schedules, setSchedules] = useState([])
  const [currentDate, setCurrentDate] = useState(new Date())
  const [error, setError] = useState('')
  const [pendingSlot, setPendingSlot] = useState(null)

  useEffect(() => {
    listDoctors().then((data) => { setDoctors(data); if (data[0]) setDoctorId(String(data[0].id)) }).catch(() => {})
    listConsultationTypes().then(setConsultationTypes).catch(() => {})
  }, [])

  const loadSchedules = async () => {
    if (!doctorId) return
    try {
      setSchedules(await listSchedules({ doctorId }))
      setError('')
    } catch {
      setError('Unable to load the availability schedule.')
    }
  }

  useEffect(() => { loadSchedules() }, [doctorId]) // eslint-disable-line react-hooks/exhaustive-deps

  const events = useMemo(() => {
    const weekStart = startOfWeek(currentDate, { weekStartsOn: 1 })
    return schedules.map((schedule) => {
      const dayIndex = DAY_NAMES.indexOf(schedule.dayOfWeek)
      const day = addDays(weekStart, dayIndex)
      const [startHour, startMinute] = schedule.startTime.split(':').map(Number)
      const [endHour, endMinute] = schedule.endTime.split(':').map(Number)
      const start = new Date(day); start.setHours(startHour, startMinute, 0, 0)
      const end = new Date(day); end.setHours(endHour, endMinute, 0, 0)
      return { id: schedule.id, title: `${schedule.consultationTypeName} (${schedule.startTime.slice(0, 5)}–${schedule.endTime.slice(0, 5)})`, start, end }
    })
  }, [schedules, currentDate])

  const handleSelectSlot = (slot) => setPendingSlot(slot)

  const handleSelectEvent = async (event) => {
    if (!window.confirm('Remove this availability slot?')) return
    await removeSchedule(event.id)
    loadSchedules()
  }

  const handleSaved = () => { setPendingSlot(null); loadSchedules() }

  return <DashboardLayout role="Admin" navigation={navigation} title="Doctor Availability Calendar" subtitle="View and manage recurring weekly availability.">
    <div className="toolbar">
      <div className="filter-bar">
        <select value={doctorId} onChange={(event) => setDoctorId(event.target.value)}>
          {doctors.map((doctor) => <option key={doctor.id} value={doctor.id}>Dr. {doctor.firstName} {doctor.lastName}</option>)}
        </select>
      </div>
    </div>

    {error && <p className="form-error" role="alert">{error}</p>}
    {doctors.length === 0 && <p className="empty-state">Add a doctor first to manage availability.</p>}

    {doctors.length > 0 && <div className="panel calendar-page" style={{ padding: 16, height: 640 }}>
      <Calendar
        localizer={localizer}
        events={events}
        defaultView="week"
        views={['week']}
        date={currentDate}
        onNavigate={setCurrentDate}
        selectable
        onSelectSlot={handleSelectSlot}
        onSelectEvent={handleSelectEvent}
        style={{ height: '100%' }}
      />
    </div>}

    {pendingSlot && <ScheduleFormModal doctorId={doctorId} consultationTypes={consultationTypes} initialSlot={pendingSlot} onClose={() => setPendingSlot(null)} onSaved={handleSaved} />}
  </DashboardLayout>
}
