import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { addSlotsToSchedule, listSchedules } from '../../services/scheduleService'
import { getAppointments, getAvailableSlots } from '../../services/appointmentService'
import { listDoctors, getMyDoctorProfile } from '../../services/doctorService'
import { useSignalR } from '../../hooks/useSignalR'
import { adminNavigation as adminNav } from '../admin/adminNavigation'
import { doctorNavigation as doctorNav } from '../doctor/doctorNavigation'
import { staffNavigation as staffNav } from '../staff/staffNavigation'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import { useAuth } from '../../hooks/useAuth'

function getLocalDateString(date = new Date()) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function minutesFromTime(value) {
  const [hours, minutes] = value.split(':').map(Number)
  return hours * 60 + minutes
}

function timeFromMinutes(value) {
  const hours = Math.floor(value / 60)
  const minutes = value % 60
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
}

function slotCounts(schedule, appointments, availableSlots) {
  const duration = Number(schedule.slotDurationMinutes) || 0
  if (duration <= 0) return { total: 0, booked: 0, available: 0 }
  const scheduleStart = minutesFromTime(schedule.startTime)
  const end = minutesFromTime(schedule.endTime)
  const total = Math.max(0, Math.floor((end - scheduleStart) / duration))
  let booked = 0
  for (let index = 0; index < total; index += 1) {
    const slotStart = scheduleStart + index * duration
    const slotEnd = slotStart + duration
    if (appointments.some((appointment) => {
      if (['Cancelled', 'Rescheduled', 'NoShow'].includes(appointment.status)) return false
      const localStart = new Date(appointment.scheduledStart)
      const appointmentStart = localStart.getHours() * 60 + localStart.getMinutes()
      const appointmentEnd = appointmentStart + (Number(appointment.estimatedDurationMinutes) || duration)
      return appointmentStart < slotEnd && appointmentEnd > slotStart
    })) booked += 1
  }
  const available = availableSlots.filter((slot) => {
    const start = new Date(slot.slotStart)
    const minute = start.getHours() * 60 + start.getMinutes()
    return Number(slot.durationMinutes) === duration && minute >= scheduleStart && minute + duration <= end
  }).length
  return { total, booked, available }
}

export default function AppointmentCalendar() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav
  const [date, setDate] = useState(getLocalDateString)
  const [doctorId, setDoctorId] = useState('')
  const [bookingDoctorId, setBookingDoctorId] = useState('')
  const [doctorOptions, setDoctorOptions] = useState([])
  const [schedules, setSchedules] = useState([])
  const [appointments, setAppointments] = useState([])
  const [calendarSlots, setCalendarSlots] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [scheduleVersion, setScheduleVersion] = useState(0)
  const [addingTo, setAddingTo] = useState(null)
  const [additionalCount, setAdditionalCount] = useState(1)
  const [saving, setSaving] = useState(false)
  const [actionError, setActionError] = useState('')

  useSignalR({
    SlotUpdated: () => setScheduleVersion((version) => version + 1),
    SlotBooked: () => setScheduleVersion((version) => version + 1),
    SlotReleased: () => setScheduleVersion((version) => version + 1),
  })

  useEffect(() => {
    let cancelled = false
    if (role === 'Doctor') {
      getMyDoctorProfile().then((profile) => {
        if (!cancelled) {
          setDoctorId(String(profile.id))
          setBookingDoctorId(String(profile.userId || user?.userId || user?.id || ''))
        }
      }).catch(() => { if (!cancelled) setError('Unable to load your doctor profile.') })
    } else {
      listDoctors().then((doctors) => {
        if (cancelled) return
        setDoctorOptions(doctors)
        if (!doctorId && doctors.length) setDoctorId(String(doctors[0].id))
      }).catch(() => { if (!cancelled) setError('Unable to load doctors.') })
    }
    return () => { cancelled = true }
  // The selected doctor is initialized once; subsequent selection changes are user driven.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [role])

  useEffect(() => {
    const selectedDoctor = doctorOptions.find((doctor) => String(doctor.id) === String(doctorId))
    if (role !== 'Doctor') setBookingDoctorId(String(selectedDoctor?.userId || ''))
  }, [doctorId, doctorOptions, role])

  const loadDay = useCallback(async () => {
    if (!date || !doctorId || !bookingDoctorId) {
      setSchedules([])
      setAppointments([])
      setCalendarSlots([])
      setLoading(false)
      return
    }
    try {
      setLoading(true)
      setError(null)
      const [scheduleRows, appointmentRows, freeSlots] = await Promise.all([
        listSchedules({ doctorId, specificDate: date }),
        getAppointments({ doctorId: bookingDoctorId, fromDate: date, toDate: date, page: 1, pageSize: 100 }),
        getAvailableSlots(date, bookingDoctorId, undefined, doctorId),
      ])
      const dateSpecific = scheduleRows.filter((schedule) => schedule.specificDate === date)
      setSchedules(dateSpecific.length
        ? dateSpecific
        : scheduleRows.filter((schedule) => !schedule.specificDate))
      setAppointments((appointmentRows.appointments || []).filter((appointment) => !['Cancelled', 'Rescheduled', 'NoShow', 'No Show'].includes(appointment.status)))
      setCalendarSlots(freeSlots.filter((slot) => slot.status === 'Available'))
    } catch (loadError) {
      setError(loadError.response?.data?.message || 'Failed to load the schedule for this date.')
      setSchedules([])
      setAppointments([])
      setCalendarSlots([])
    } finally {
      setLoading(false)
    }
  }, [date, doctorId, bookingDoctorId])

  useEffect(() => { loadDay() }, [loadDay, scheduleVersion])

  const visibleAppointments = useMemo(() => appointments.map((appointment) => {
    const start = new Date(appointment.scheduledStart)
    return {
      id: `apt_${appointment.id}`,
      appointmentId: appointment.id,
      start,
      end: new Date(start.getTime() + appointment.estimatedDurationMinutes * 60000),
      duration: appointment.estimatedDurationMinutes,
      status: 'Booked',
      realStatus: appointment.status,
      patientName: appointment.patientName,
      priority: appointment.priority,
      referenceNumber: appointment.referenceNumber,
    }
  }), [appointments])

  const visibleSlots = useMemo(() => [
    ...calendarSlots.map((slot) => ({
      id: `free_${slot.slotStart}`,
      start: new Date(slot.slotStart),
      end: new Date(slot.slotEnd),
      duration: slot.durationMinutes,
      status: 'Available',
    })),
    ...visibleAppointments,
  ].sort((left, right) => left.start - right.start), [calendarSlots, visibleAppointments])

  const handleAddSlots = async (schedule) => {
    const count = Number(additionalCount)
    if (!Number.isInteger(count) || count < 1 || count > 100) {
      setActionError('Choose between 1 and 100 additional slots.')
      return
    }
    try {
      setSaving(true)
      setActionError('')
      await addSlotsToSchedule(schedule.id, { date, additionalSlotCount: count })
      setAddingTo(null)
      setAdditionalCount(1)
      setScheduleVersion((version) => version + 1)
    } catch (requestError) {
      setActionError(requestError.response?.data?.message || 'Unable to add slots to this schedule.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <DashboardLayout role={role} navigation={navigation} title="Appointment Slots" subtitle="View the doctor's configured sessions and manage slots for one date.">
      <div className="mb-6 p-4 rounded-xl border flex flex-wrap items-center gap-6" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'var(--color-primary)' }}>
        <div className="flex flex-col gap-1">
          <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>Date</label>
          <input type="date" value={date} onChange={(event) => setDate(event.target.value)} className="border rounded-md px-3 py-1.5 min-w-[150px]" />
        </div>
        {role !== 'Doctor' && <div className="flex flex-col gap-1">
          <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>Doctor</label>
          <select value={doctorId} onChange={(event) => setDoctorId(event.target.value)} className="border rounded-md px-3 py-1.5 min-w-[200px]">
            {doctorOptions.map((doctor) => <option key={doctor.id} value={doctor.id}>Dr. {doctor.firstName} {doctor.lastName}</option>)}
          </select>
        </div>}
      </div>

      {loading ? <div className="stat-card p-12 text-center">Loading schedule...</div>
        : error ? <div className="stat-card p-12 text-center text-red-600 bg-red-50" role="alert">{error}</div>
          : <>
            {schedules.length === 0 ? <div className="stat-card p-8 text-center">
              <p className="mb-3">No schedule configured for this date.</p>
              {role === 'Admin' || role === 'Staff'
                ? <Link className="primary-button inline-flex" to="/admin/schedules">Open Doctor Availability Calendar</Link>
                : <p className="opacity-70">Please contact the hospital to configure your availability.</p>}
            </div> : <section className="grid gap-4 mb-6" aria-label="Configured consultation schedules">
              {schedules.map((schedule) => {
                const counts = slotCounts(schedule, appointments, calendarSlots)
                const previewMinutes = minutesFromTime(schedule.endTime) + (Number(schedule.slotDurationMinutes) || 0) * Number(additionalCount || 0)
                const crossesMidnight = previewMinutes >= 24 * 60
                const previewEnd = crossesMidnight ? null : timeFromMinutes(previewMinutes)
                return <article className="stat-card p-4" key={schedule.id}>
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <h2 className="font-bold text-lg">{schedule.consultationTypeName}</h2>
                      <p>{schedule.startTime.slice(0, 5)}–{schedule.endTime.slice(0, 5)} · {schedule.slotDurationMinutes} min per slot</p>
                    </div>
                    <div className="flex flex-wrap gap-4 text-sm" aria-label="Slot counts">
                      <span><strong>{counts.total}</strong> total</span>
                      <span><strong>{counts.booked}</strong> booked</span>
                      <span><strong>{counts.available}</strong> available</span>
                    </div>
                  </div>
                  {role !== 'Doctor' && (addingTo === schedule.id ? <div className="mt-4 border-t pt-4">
                    <label className="block text-sm font-semibold mb-1" htmlFor={`additional-count-${schedule.id}`}>Number of additional slots</label>
                    <div className="flex flex-wrap items-center gap-3">
                      <input id={`additional-count-${schedule.id}`} type="number" min="1" max="100" value={additionalCount} onChange={(event) => setAdditionalCount(event.target.value)} className="border rounded-md px-3 py-2 w-28" />
                      <span className="text-sm">Preview: {schedule.endTime.slice(0, 5)} → {previewEnd || 'past midnight'} ({counts.total} → {counts.total + Number(additionalCount || 0)} total slots)</span>
                      <button type="button" className="primary-button" onClick={() => handleAddSlots(schedule)} disabled={saving || crossesMidnight}>{saving ? 'Adding...' : 'Confirm Add Slots'}</button>
                      <button type="button" className="secondary-button" onClick={() => { setAddingTo(null); setActionError('') }} disabled={saving}>Cancel</button>
                    </div>
                    {crossesMidnight && <p className="form-error mt-2" role="alert">Added slots must end before midnight.</p>}
                    {actionError && <p className="form-error mt-2" role="alert">{actionError}</p>}
                  </div> : <button type="button" className="secondary-button mt-4" onClick={() => { setAddingTo(schedule.id); setActionError('') }}>Add Slots</button>)}
                </article>
              })}
            </section>}

            <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
              {visibleSlots.length === 0
                ? <div className="text-center p-8 opacity-70">No slots configured for this date.</div>
                : <div className="flex flex-col gap-2 p-4">
                  {visibleSlots.map((slot) => <div key={slot.id} className="p-4 rounded-lg border flex justify-between items-center bg-white shadow-sm" style={{ borderColor: slot.status === 'Available' ? '#10b981' : '#f59e0b', borderLeftWidth: '4px' }}>
                    <div>
                      <div className="font-bold text-lg">{slot.start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - {slot.end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
                      <div className="text-sm opacity-60">{slot.duration} mins</div>
                    </div>
                    <div className="flex flex-col items-end gap-1">
                      {slot.status === 'Available' ? <span className="px-3 py-1 bg-green-100 text-green-800 rounded-full text-xs font-bold">Available</span> : <>
                        <div className="font-bold">{slot.patientName} ({slot.referenceNumber})</div>
                        <div className="flex gap-2 items-center mt-1"><AppointmentStatusBadge status={slot.realStatus} /><PriorityBadge priority={slot.priority} /></div>
                      </>}
                    </div>
                  </div>)}
                </div>}
            </div>
          </>}
    </DashboardLayout>
  )
}
