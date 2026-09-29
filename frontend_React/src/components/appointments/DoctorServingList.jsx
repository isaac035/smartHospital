import { useState, useEffect } from 'react'
import { getAppointments, getQueue, callQueueEntry, markCompleted } from '../../services/appointmentService'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'

export default function DoctorServingList({ user }) {
  const [appointments, setAppointments] = useState([])
  const [queue, setQueue] = useState([])
  const [loading, setLoading] = useState(true)

  const fetchData = async () => {
    try {
      setLoading(true)
      const today = new Date().toISOString().split('T')[0]
      const [appts, qData] = await Promise.all([
        getAppointments({ doctorId: user.id, fromDate: today, toDate: today }),
        getQueue(user.id)
      ])
      setAppointments(appts)
      setQueue(qData?.queue || [])
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchData()
  }, [user.id])

  useSignalR({
    QueueUpdated: fetchData,
    PatientCheckedIn: fetchData,
    ConsultationCompleted: fetchData,
    AppointmentUpdated: fetchData,
    AppointmentCancelled: fetchData
  });


  if (loading) return <div className="p-12 text-center">Loading your serving list...</div>

  // Merge appointments with queue data
  const list = appointments.map(apt => {
    const qEntry = queue.find(q => q.appointmentId === apt.id)
    return { ...apt, queueEntry: qEntry }
  }).sort((a, b) => {
    // Sort by queue position if available, else by appointment time
    if (a.queueEntry && b.queueEntry) return a.queueEntry.queuePosition - b.queueEntry.queuePosition
    if (a.queueEntry) return -1
    if (b.queueEntry) return 1
    return new Date(a.scheduledStart) - new Date(b.scheduledStart)
  })

  const handleStart = async (queueId) => {
    try { await callQueueEntry(queueId); fetchData(); } 
    catch (err) { alert('Failed to start'); }
  }
  const handleComplete = async (queueId) => {
    try { await markCompleted(queueId); fetchData(); } 
    catch (err) { alert('Failed to complete'); }
  }

  return (
    <div className="flex flex-col gap-4">
      {list.length === 0 ? (
        <div className="p-8 text-center border rounded-xl bg-white opacity-50">No appointments scheduled for today.</div>
      ) : list.map(apt => {
        const q = apt.queueEntry
        const isCurrent = q?.status === 'InProgress' || q?.status === 'Called'
        
        return (
          <div key={apt.id} className="p-4 rounded-xl border bg-white flex justify-between items-center" style={{ borderColor: isCurrent ? 'var(--color-accent)' : '#e5e7eb', borderWidth: isCurrent ? '2px' : '1px' }}>
            <div className="flex items-center gap-6">
              <div className="w-16 h-16 rounded-full flex items-center justify-center font-bold text-2xl" style={{ background: isCurrent ? 'var(--color-accent)' : '#f3f4f6', color: isCurrent ? 'white' : '#9ca3af' }}>
                {q ? q.queueNumber : '-'}
              </div>
              <div>
                <div className="font-bold text-lg">{apt.patientName}</div>
                <div className="text-sm opacity-60 flex gap-4 mt-1">
                  <span>{new Date(apt.scheduledStart).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>
                  <span>{apt.appointmentType}</span>
                </div>
              </div>
            </div>
            
            <div className="flex items-center gap-4">
              <div className="flex flex-col gap-1 items-end mr-4">
                <AppointmentStatusBadge status={apt.status} />
                <PriorityBadge priority={apt.priority} />
              </div>
              
              {q?.status === 'Waiting' && (
                <button className="primary-button" style={{ marginTop: 0 }} onClick={() => handleStart(q.id)}>
                  Start
                </button>
              )}
              {isCurrent && (
                <button className="primary-button" style={{ marginTop: 0 }} onClick={() => handleComplete(q.id)}>
                  Complete
                </button>
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}
