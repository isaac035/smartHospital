import { useState, useEffect, useCallback } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import { getAppointments, getQueue, callQueueEntry, markCompleted } from '../../services/appointmentService'
import { useDoctorOptions } from '../../hooks/useDoctorOptions'
import { useSignalR } from '../../hooks/useSignalR'
import { adminNavigation as adminNav } from '../admin/adminNavigation'
import { staffNavigation as staffNav } from '../staff/staffNavigation'
import { doctorNavigation as doctorNav } from '../doctor/doctorNavigation'
import { appointmentManagerNavigation } from './appointmentManagerNavigation'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import { useAuth } from '../../hooks/useAuth'

export default function DoctorAppointmentManagement() {
  const { user } = useAuth()
  const role = user?.role || 'Admin'
  const navigation = role === 'AppointmentManager' ? appointmentManagerNavigation : role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav

  const todayParts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit'
  }).formatToParts(new Date())
  const today = `${todayParts.find(part => part.type === 'year')?.value}-${todayParts.find(part => part.type === 'month')?.value}-${todayParts.find(part => part.type === 'day')?.value}`
  const [date, setDate] = useState(today)
  const [doctorId, setDoctorId] = useState('')
  const doctorOptions = useDoctorOptions(role !== 'Doctor')

  const [appointments, setAppointments] = useState([])
  const [queue, setQueue] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [actionError, setActionError] = useState(null)

  // Doctors are always scoped to their JWT identity. Admin/staff select a real doctor user ID.
  useEffect(() => {
    if (role === 'Doctor') {
      setDoctorId(String(user?.id || ''))
      return
    }
  }, [role, user?.id])

  useEffect(() => {
    if (role === 'Doctor' || doctorId || !doctorOptions.length) return
    const firstDoctor = doctorOptions.find(doctor => doctor.userId != null) || doctorOptions[0]
    setDoctorId(String(firstDoctor.userId ?? `profile:${firstDoctor.id}`))
  }, [doctorId, doctorOptions, role])

  const fetchData = useCallback(async () => {
    if (!doctorId) {
      setAppointments([])
      setQueue([])
      setLoading(false)
      return
    }
    if (doctorId.startsWith('profile:')) {
      setAppointments([])
      setQueue([])
      setError(null)
      setLoading(false)
      return
    }
    setLoading(true)
    setError(null)
    try {
      const [appts, qData] = await Promise.all([
        getAppointments({ doctorId, fromDate: date, toDate: date }),
        getQueue(doctorId, date)
      ])
      setAppointments(Array.isArray(appts) ? appts : [])
      setQueue(qData?.queue || [])
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load data')
    } finally {
      setLoading(false)
    }
  }, [doctorId, date])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  // Real-time updates — when queue changes (via SignalR broadcast from backend), re-fetch
  useSignalR({
    AppointmentCreated: fetchData,
    AppointmentCancelled: fetchData,
    QueueUpdated: fetchData,
    PatientCheckedIn: fetchData,
    ConsultationCompleted: fetchData,
    AppointmentUpdated: fetchData,
  })

  const handleStart = async (queueEntryId) => {
    setActionError(null)
    try {
      await callQueueEntry(queueEntryId)
      fetchData()
    } catch (err) {
      setActionError(err.response?.data?.message || 'Failed to start consultation')
    }
  }

  const handleComplete = async (queueEntryId) => {
    setActionError(null)
    try {
      // This hits POST /api/queues/{id}/complete — backend marks it Completed,
      // records completedAt, then broadcasts ConsultationCompleted + QueueUpdated via SignalR.
      // Queue Management is subscribed to both events and will auto-refresh.
      await markCompleted(queueEntryId)
      fetchData()
    } catch (err) {
      setActionError(err.response?.data?.message || 'Failed to complete consultation')
    }
  }

  // Merge appointment list with live queue state
  const list = appointments
    .map(apt => {
      const qEntry = queue.find(q => q.appointmentId === apt.id)
      return { ...apt, queueEntry: qEntry }
    })
    .sort((a, b) => new Date(a.scheduledStart) - new Date(b.scheduledStart))

  const currentEntry = queue.find(q => q.status === 'InProgress' || q.status === 'Called')

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Doctor Appointment Management"
      subtitle="View and manage a doctor's patient queue for any day."
    >
      {/* Filter bar */}
      <div className="mb-6 p-4 rounded-xl border flex flex-wrap items-center gap-6"
        style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'var(--color-primary)' }}>
        {role !== 'Doctor' && <div className="flex flex-col gap-1">
          <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>Doctor</label>
          <select
            value={doctorId}
            onChange={e => setDoctorId(e.target.value)}
            className="border rounded-md px-3 py-1.5 min-w-[220px]"
            style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
          >
            <option value="">-- Select Doctor --</option>
            {doctorOptions.map(d => (
              <option key={d.id} value={d.userId ?? `profile:${d.id}`}>Dr. {d.firstName} {d.lastName}{d.userId == null ? ' (not linked)' : ''}</option>
            ))}
          </select>
        </div>}
        <div className="flex flex-col gap-1">
          <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>Date</label>
          <input
            type="date"
            value={date}
            onChange={e => setDate(e.target.value)}
            className="border rounded-md px-3 py-1.5 min-w-[160px]"
            style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
          />
        </div>
        {currentEntry && (
          <div className="ml-auto flex items-center gap-2 px-4 py-2 rounded-lg font-bold text-sm"
            style={{ background: 'color-mix(in srgb, var(--color-accent) 10%, white)', color: 'var(--color-accent)', border: '1.5px solid var(--color-accent)' }}>
            🟢 Now Serving: {currentEntry.queueCode || `#${currentEntry.queueNumber}`} — {currentEntry.patientName}
          </div>
        )}
      </div>

      {/* Errors */}
      {error && <div className="mb-4 p-3 bg-red-50 text-red-700 rounded-lg border border-red-200">{error}</div>}
      {actionError && <div className="mb-4 p-3 bg-orange-50 text-orange-700 rounded-lg border border-orange-200">{actionError}</div>}

      {/* Content */}
      {!doctorId ? (
        <div className="p-12 text-center border rounded-xl stat-card opacity-60">Select a doctor to view their queue.</div>
      ) : doctorId.startsWith('profile:') ? (
        <div className="p-12 text-center border rounded-xl stat-card opacity-60">This doctor profile is not linked to an appointment account yet.</div>
      ) : loading ? (
        <div className="p-12 text-center stat-card opacity-60">Loading appointments...</div>
      ) : list.length === 0 ? (
        <div className="p-12 text-center stat-card opacity-60">No appointments for this doctor on the selected date.</div>
      ) : (
        <div className="flex flex-col gap-3">
          {list.map(apt => {
            const q = apt.queueEntry
            const isCurrent = currentEntry && q?.id === currentEntry.id
            const isWaiting = q?.status === 'Waiting'
            const canStart = isWaiting && !currentEntry  // can only start if nothing in-progress
            const canComplete = q?.status === 'InProgress'

            return (
              <div
                key={apt.id}
                className="p-5 rounded-xl border bg-white flex items-center justify-between gap-4"
                style={{
                  borderColor: isCurrent ? 'var(--color-accent)' : 'color-mix(in srgb, var(--color-secondary) 20%, var(--color-primary))',
                  borderWidth: isCurrent ? '2px' : '1px',
                  boxShadow: isCurrent ? '0 0 0 3px color-mix(in srgb, var(--color-accent) 12%, transparent)' : 'none'
                }}
              >
                {/* Queue token */}
                <div className="w-16 h-16 rounded-full flex-shrink-0 flex items-center justify-center font-black text-2xl"
                  style={{
                    background: isCurrent ? 'var(--color-accent)' : 'color-mix(in srgb, var(--color-accent) 8%, var(--color-primary))',
                    color: isCurrent ? 'white' : 'var(--color-accent)'
                  }}>
                  {q?.queueCode || (q?.queueNumber != null ? `#${q.queueNumber}` : '—')}
                </div>

                {/* Main info */}
                <div className="flex-1 min-w-0">
                  <div className="font-bold text-lg leading-tight">{apt.patientName}</div>
                  <div className="text-sm mt-1 flex flex-wrap gap-x-4 gap-y-1 opacity-70">
                    <span>⏰ {new Date(apt.scheduledStart).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', timeZone: 'Asia/Colombo' })}</span>
                    <span>📋 {apt.appointmentType}</span>
                    {q?.estimatedWaitMinutes != null && (
                      <span>⌛ ~{q.estimatedWaitMinutes} min wait</span>
                    )}
                  </div>
                </div>

                {/* Status badges */}
                <div className="flex flex-col items-end gap-1.5 flex-shrink-0">
                  <AppointmentStatusBadge status={apt.status} />
                  <PriorityBadge priority={apt.priority} />
                </div>

                {/* Actions */}
                <div className="flex gap-2 flex-shrink-0">
                  {canStart && (
                    <button
                      className="primary-button text-sm"
                      style={{ marginTop: 0 }}
                      onClick={() => handleStart(q.id)}
                      title="Start this consultation (calls the patient)"
                    >
                      Start Consultation
                    </button>
                  )}
                  {canComplete && (
                    <button
                      className="primary-button text-sm"
                      style={{ marginTop: 0, background: '#059669', borderColor: '#059669' }}
                      onClick={() => handleComplete(q.id)}
                      title="Mark consultation completed. Queue Management will auto-update."
                    >
                      ✓ Complete Consultation
                    </button>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </DashboardLayout>
  )
}
