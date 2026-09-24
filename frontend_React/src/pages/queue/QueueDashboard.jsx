import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { getQueue, callQueueEntry, markNoShow, markCompleted } from '../../services/appointmentService'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']
const doctorNav = ['Dashboard', 'My Appointments', 'My Patients', 'Medical Records', 'Prescriptions', 'Lab Reports']

export default function QueueDashboard() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav

  const [doctorId, setDoctorId] = useState(role === 'Doctor' ? user?.id : '')
  const [queue, setQueue] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  
  // Dummy list of doctors for Staff/Admin to select from
  const doctorOptions = [
    { id: 2, name: 'Dr. Bob' },
    { id: 10, name: 'Dr. Smith' }
  ]

  useEffect(() => {
    let intervalId
    if (doctorId) {
      fetchQueue()
      // Auto-refresh every 10 seconds to simulate real-time
      intervalId = setInterval(fetchQueue, 10000)
    } else {
      setQueue([])
    }
    return () => clearInterval(intervalId)
  }, [doctorId])

  const fetchQueue = async () => {
    try {
      setLoading(true)
      const data = await getQueue(doctorId)
      setQueue(data)
      setError(null)
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to fetch queue')
    } finally {
      setLoading(false)
    }
  }

  const handleCall = async (id) => {
    try {
      await callQueueEntry(id)
      fetchQueue()
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to call patient')
    }
  }

  const handleNoShow = async (id) => {
    try {
      await markNoShow(id)
      fetchQueue()
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to mark no-show')
    }
  }

  const handleComplete = async (id) => {
    try {
      await markCompleted(id)
      fetchQueue()
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to complete')
    }
  }

  const currentServing = queue.find(q => q.status === 'InProgress' || q.status === 'Called')
  const waitingList = queue.filter(q => q.status === 'Waiting')

  return (
    <DashboardLayout role={role} navigation={navigation} title="Queue Management" subtitle="Live view of doctor queues.">
      
      {role !== 'Doctor' && (
        <div className="mb-6 p-4 rounded-xl border flex items-center gap-4" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'var(--color-primary)' }}>
          <label className="font-bold" style={{ color: 'var(--color-accent)' }}>Select Doctor Queue:</label>
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

      {!doctorId ? (
        <div className="p-12 text-center border rounded-xl" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'var(--color-primary)' }}>
          Please select a doctor to view their queue.
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          
          {/* Now Serving Panel */}
          <div className="lg:col-span-1">
            <div className="stat-card" style={{ padding: '30px', textAlign: 'center', height: '100%' }}>
              <h2 className="text-xl font-bold mb-6" style={{ color: 'var(--color-accent)' }}>Now Serving</h2>
              
              {currentServing ? (
                <div>
                  <div className="text-sm font-bold text-gray-500 uppercase tracking-widest mb-2">Queue No</div>
                  <div className="text-6xl font-black mb-4" style={{ color: 'var(--color-accent)' }}>
                    {currentServing.queueNumber}
                  </div>
                  <div className="text-lg font-bold mb-2">{currentServing.patientName}</div>
                  <PriorityBadge priority={currentServing.priority} />
                  
                  <div className="mt-8 flex gap-2 justify-center flex-wrap">
                    <button className="primary-button text-sm w-full" onClick={() => handleComplete(currentServing.id)}>
                      Mark Completed
                    </button>
                    <button className="secondary-button text-sm w-full" onClick={() => handleNoShow(currentServing.id)}>
                      No Show
                    </button>
                  </div>
                </div>
              ) : (
                <div className="py-12" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                  No patient currently being served.
                </div>
              )}
            </div>
          </div>

          {/* Waiting List */}
          <div className="lg:col-span-2">
            <div className="stat-card" style={{ padding: 0, overflow: 'hidden', height: '100%', display: 'flex', flexDirection: 'column' }}>
              <div className="p-4 border-b flex justify-between items-center" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))' }}>
                <h2 className="font-bold" style={{ color: 'var(--color-accent)' }}>Waiting List ({waitingList.length})</h2>
                <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 60%, var(--color-primary))' }}>
                  Auto-refreshes every 10s
                </span>
              </div>
              
              <div className="flex-1 overflow-auto p-4">
                {error && <div className="form-error mb-4">{error}</div>}
                
                {waitingList.length === 0 ? (
                  <div className="text-center py-12" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    {loading ? 'Loading...' : 'Queue is empty.'}
                  </div>
                ) : (
                  <div className="flex flex-col gap-3">
                    {waitingList.map((entry, idx) => (
                      <div key={entry.id} className="p-4 rounded-lg border flex items-center justify-between" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                        <div className="flex items-center gap-4">
                          <div className="w-12 h-12 rounded-full flex items-center justify-center font-bold text-lg" style={{ background: 'color-mix(in srgb, var(--color-accent) 10%, var(--color-primary))', color: 'var(--color-accent)' }}>
                            {entry.queueNumber}
                          </div>
                          <div>
                            <div className="font-bold">{entry.patientName}</div>
                            <div className="text-sm flex gap-3 mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
                              <span>Wait: ~{entry.estimatedWaitMinutes} min</span>
                              <span>Pos: {idx + 1}</span>
                            </div>
                          </div>
                        </div>
                        <div className="flex items-center gap-3">
                          <PriorityBadge priority={entry.priority} />
                          <button 
                            className="primary-button text-xs px-3 py-1.5" 
                            style={{ marginTop: 0 }}
                            onClick={() => handleCall(entry.id)}
                            disabled={currentServing != null}
                            title={currentServing ? "Complete current patient first" : "Call next"}
                          >
                            Call
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </DashboardLayout>
  )
}
