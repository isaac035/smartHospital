import { useState, useEffect } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import { listConsultationTypes } from '../../services/consultationTypeService'
import { createSchedule } from '../../services/scheduleService'
import { getAppointments, getAvailableSlots } from '../../services/appointmentService'
import { listDoctors } from '../../services/doctorService'
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

export default function AppointmentCalendar() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav

  const [date, setDate] = useState(getLocalDateString)
  const [doctorId, setDoctorId] = useState(role === 'Doctor' ? user?.id : '')
  
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  const [showGenerate, setShowGenerate] = useState(false)
  const [consultationTypes, setConsultationTypes] = useState([])
  const [genStartTime, setGenStartTime] = useState('08:00')
  const [genEndTime, setGenEndTime] = useState('12:00')
  const [genConsultationTypeId, setGenConsultationTypeId] = useState('')
  const [genLoading, setGenLoading] = useState(false)
  const [scheduleVersion, setScheduleVersion] = useState(0)
  useSignalR({
    SlotUpdated: () => setScheduleVersion(version => version + 1),
    SlotBooked: () => setScheduleVersion(version => version + 1),
    SlotReleased: () => setScheduleVersion(version => version + 1),
  })

  useEffect(() => {
    if (showGenerate && consultationTypes.length === 0) {
      listConsultationTypes().then(setConsultationTypes).catch(console.error)
    }
  }, [showGenerate, consultationTypes.length])

  const handleGenerateSlots = async () => {
    if (!doctorId || !date || !genConsultationTypeId) return;
    try {
      setGenLoading(true);
      const [year, month, dayParts] = date.split('-');
      const localDate = new Date(year, month - 1, dayParts);
      const dayOfWeek = localDate.toLocaleDateString('en-US', { weekday: 'long' });
      await createSchedule({
        doctorId: parseInt(doctorId),
        consultationTypeId: parseInt(genConsultationTypeId),
        dayOfWeek: dayOfWeek,
        specificDate: date,
        startTime: genStartTime + ':00',
        endTime: genEndTime + ':00'
      });
      setShowGenerate(false);
      setScheduleVersion(version => version + 1)
      alert('Slots generated successfully!');
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to generate slots');
    } finally {
      setGenLoading(false);
    }
  }


  useEffect(() => {
    if (role !== 'Doctor') {
      listDoctors().then(docs => {
        setDoctorOptions(docs)
      }).catch(console.error)
    }
  }, [role])


  const [doctorOptions, setDoctorOptions] = useState([])

  useEffect(() => {
    if (!date) return
    if (role !== 'Doctor' && !doctorId) {
      setAppointments([])
      return
    }

    const selectedDoctor = role === 'Doctor'
      ? null
      : doctorOptions.find(doctor => String(doctor.id) === String(doctorId))
    const bookingDoctorId = role === 'Doctor' ? doctorId : selectedDoctor?.userId
    if (!bookingDoctorId) {
      setAppointments([])
      setError(selectedDoctor ? 'This doctor has no linked booking account.' : null)
      setLoading(false)
      return
    }

    const fetchSchedule = async () => {
      try {
        setLoading(true)
        const [appts, freeSlots] = await Promise.all([
          getAppointments({ doctorId: bookingDoctorId, fromDate: date, toDate: date }),
          getAvailableSlots(date, bookingDoctorId, undefined, role === 'Doctor' ? undefined : doctorId)
        ])
        
        const combined = [
          ...freeSlots.filter(s => s.status === 'Available').map(s => ({
            id: 'free_' + s.slotStart,
            start: new Date(s.slotStart),
            end: new Date(s.slotEnd),
            duration: s.durationMinutes,
            status: 'Available'
          })),
          ...appts.filter(a => !['Cancelled', 'NoShow'].includes(a.status)).map(a => {
             const start = new Date(a.scheduledStart);
             return {
               id: 'apt_' + a.id,
               appointmentId: a.id,
               start: start,
               end: new Date(start.getTime() + a.estimatedDurationMinutes * 60000),
               duration: a.estimatedDurationMinutes,
               status: 'Booked',
               realStatus: a.status,
               patientName: a.patientName,
               priority: a.priority,
               referenceNumber: a.referenceNumber
             }
          })
        ].sort((a, b) => a.start - b.start);
        
        setAppointments(combined)
        setError(null)
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to fetch schedule')
      } finally {
        setLoading(false)
      }
    }
    fetchSchedule()
  }, [date, doctorId, doctorOptions, role, scheduleVersion])

  // Helper to generate time slots from 8 AM to 5 PM
  

  return (
    <DashboardLayout role={role} navigation={navigation} title="Daily Schedule" subtitle="Day-at-a-glance view for doctors.">
      
      <div className="mb-6 p-4 rounded-xl border flex flex-wrap items-center gap-6" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'var(--color-primary)' }}>
        <div className="flex flex-col gap-1">
          <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>Date</label>
          <input 
            type="date" 
            value={date} 
            onChange={(e) => setDate(e.target.value)}
            className="border rounded-md px-3 py-1.5 min-w-[150px]"
            style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
          />
        </div>

        {role !== 'Doctor' && (
          <div className="flex flex-col gap-1">
            <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>Doctor</label>
            <select 
              value={doctorId} 
              onChange={(e) => setDoctorId(e.target.value)}
              className="border rounded-md px-3 py-1.5 min-w-[200px]"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            >
              <option value="">-- Select --</option>
              {doctorOptions.map(d => <option key={d.id} value={d.id}>Dr. {d.firstName} {d.lastName}</option>)}
            </select>
          </div>
        )}

        {doctorId && role !== 'Doctor' && (
          <div className="flex items-end">
            <button onClick={() => setShowGenerate(!showGenerate)} className="secondary-button" style={{ marginTop: 0, height: '38px', padding: '0 16px' }}>
              {showGenerate ? 'Cancel' : 'Generate Slots'}
            </button>
          </div>
        )}
      </div>

      {showGenerate && (
        <div className="mb-6 p-4 rounded-xl border bg-white" style={{ borderColor: 'color-mix(in srgb, var(--color-accent) 20%, transparent)' }}>
          <h3 className="text-sm font-bold mb-3" style={{ color: 'var(--color-accent)' }}>Generate Appointment Slots</h3>
          <div className="flex flex-wrap gap-4 items-end">
            <div className="flex flex-col gap-1">
              <label className="text-xs font-bold">Consultation Type</label>
              <select className="border rounded-md px-3 py-1.5" value={genConsultationTypeId} onChange={e => setGenConsultationTypeId(e.target.value)}>
                <option value="">-- Select --</option>
                {consultationTypes.map(c => <option key={c.id} value={c.id}>{c.name} ({c.durationMinutes} min)</option>)}
              </select>
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-bold">Start Time (HH:mm)</label>
              <input type="time" className="border rounded-md px-3 py-1.5" value={genStartTime} onChange={e => setGenStartTime(e.target.value)} />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-bold">End Time (HH:mm)</label>
              <input type="time" className="border rounded-md px-3 py-1.5" value={genEndTime} onChange={e => setGenEndTime(e.target.value)} />
            </div>
            <button className="primary-button" style={{ marginTop: 0, height: '38px' }} onClick={handleGenerateSlots} disabled={genLoading || !genConsultationTypeId}>
              {genLoading ? 'Saving...' : 'Save Slots'}
            </button>
          </div>
        </div>
      )}

      <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
        {(!doctorId && role !== 'Doctor') ? (
          <div className="p-12 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
            Select a doctor and date to view their schedule.
          </div>
        ) : loading ? (
          <div className="p-12 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
            Loading schedule...
          </div>
        ) : error ? (
          <div className="p-12 text-center text-red-600 bg-red-50">{error}</div>
        ) : (
          <div className="flex flex-col gap-2 p-4">
            {appointments.length === 0 ? (
              <div className="text-center py-8 opacity-50">No slots configured for this date.</div>
            ) : appointments.map(slot => (
              <div key={slot.id} className="p-4 rounded-lg border flex justify-between items-center bg-white shadow-sm" style={{ borderColor: slot.status === 'Available' ? '#10b981' : '#f59e0b', borderLeftWidth: '4px' }}>
                <div>
                  <div className="font-bold text-lg">
                    {slot.start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - {slot.end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </div>
                  <div className="text-sm opacity-60">{slot.duration} mins</div>
                </div>
                <div className="flex flex-col items-end gap-1">
                  {slot.status === 'Available' ? (
                    <span className="px-3 py-1 bg-green-100 text-green-800 rounded-full text-xs font-bold">Available</span>
                  ) : (
                    <>
                      <div className="font-bold">{slot.patientName} ({slot.referenceNumber})</div>
                      <div className="flex gap-2 items-center mt-1">
                        <AppointmentStatusBadge status={slot.realStatus} />
                        <PriorityBadge priority={slot.priority} />
                      </div>
                    </>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

    </DashboardLayout>
  )
}
