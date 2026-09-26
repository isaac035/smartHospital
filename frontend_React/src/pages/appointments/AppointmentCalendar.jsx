import { useState, useEffect } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import { getAppointments } from '../../services/appointmentService'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']
const doctorNav = ['Dashboard', 'My Appointments', 'My Patients', 'Medical Records', 'Prescriptions', 'Lab Reports']

export default function AppointmentCalendar() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav

  const [date, setDate] = useState(new Date().toISOString().split('T')[0])
  const [doctorId, setDoctorId] = useState(role === 'Doctor' ? user?.id : '')
  
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  const doctorOptions = [
    { id: 2, name: 'Dr. Bob' },
    { id: 10, name: 'Dr. Smith' }
  ]

  useEffect(() => {
    if (!date) return
    if (role !== 'Doctor' && !doctorId) {
      setAppointments([])
      return
    }

    const fetchSchedule = async () => {
      try {
        setLoading(true)
        // Pass both fromDate and toDate as the same day to get a daily view
        const data = await getAppointments({
          doctorId,
          fromDate: date,
          toDate: date
        })
        setAppointments(data)
        setError(null)
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to fetch schedule')
      } finally {
        setLoading(false)
      }
    }
    
    fetchSchedule()
  }, [date, doctorId])

  // Helper to generate time slots from 8 AM to 5 PM
  const timeSlots = Array.from({ length: 19 }, (_, i) => {
    const hour = Math.floor(i / 2) + 8
    const min = i % 2 === 0 ? '00' : '30'
    return `${hour.toString().padStart(2, '0')}:${min}`
  })

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
              {doctorOptions.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </div>
        )}
      </div>

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
          <div className="flex flex-col divide-y" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))' }}>
            {timeSlots.map(timeStr => {
              // Very simple mapping to match appointments roughly into the 30min slots
              const [h, m] = timeStr.split(':')
              const slotTime = new Date(date)
              slotTime.setHours(parseInt(h), parseInt(m), 0, 0)
              
              const slotEnd = new Date(slotTime)
              slotEnd.setMinutes(slotEnd.getMinutes() + 30)

              // Find appointments overlapping this 30-min block
              const overlapping = appointments.filter(a => {
                const aStart = new Date(a.scheduledStart)
                const aEnd = new Date(aStart)
                aEnd.setMinutes(aEnd.getMinutes() + a.estimatedDurationMinutes)
                
                return (aStart < slotEnd && aEnd > slotTime)
              })

              return (
                <div key={timeStr} className="flex flex-col sm:flex-row min-h-[80px]">
                  <div className="w-24 p-4 font-bold text-sm border-r flex items-start justify-end" style={{ color: 'var(--color-accent)', borderColor: 'color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))', background: 'color-mix(in srgb, var(--color-accent) 2%, var(--color-primary))' }}>
                    {timeStr}
                  </div>
                  <div className="flex-1 p-2 flex flex-col gap-2">
                    {overlapping.length > 0 ? (
                      overlapping.map(apt => {
                        // Avoid rendering same appointment multiple times if it spans multiple slots?
                        // For a simple view, we'll just render a card in every slot it overlaps.
                        return (
                          <div key={apt.id} className="p-3 rounded border text-sm flex justify-between items-center bg-white shadow-sm" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 20%, var(--color-primary))' }}>
                            <div>
                              <div className="font-bold">{apt.patientName}</div>
                              <div className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 60%, var(--color-primary))' }}>
                                Ref: {apt.referenceNumber} • {apt.estimatedDurationMinutes} mins
                              </div>
                            </div>
                            <div className="flex flex-col items-end gap-1">
                              <AppointmentStatusBadge status={apt.status} />
                              <PriorityBadge priority={apt.priority} />
                            </div>
                          </div>
                        )
                      })
                    ) : (
                      <div className="w-full h-full flex items-center p-2 text-sm opacity-30">
                        -- Free --
                      </div>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </div>

    </DashboardLayout>
  )
}
