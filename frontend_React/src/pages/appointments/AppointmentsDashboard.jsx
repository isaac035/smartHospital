import { useState, useEffect, useCallback } from 'react'
import { Link } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { checkIn, getAppointments } from '../../services/appointmentService'
import { useSignalR } from '../../hooks/useSignalR'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import FilterBar from '../../components/appointments/FilterBar'
import { adminNavigation as adminNav } from '../admin/adminNavigation'
import { doctorNavigation as doctorNav } from '../doctor/doctorNavigation'
import { staffNavigation as staffNav } from '../staff/staffNavigation'
import { useAuth } from '../../hooks/useAuth'
import DoctorServingList from '../../components/appointments/DoctorServingList'

function hospitalDateKey(value = new Date()) {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit'
  }).formatToParts(value)
  const part = type => parts.find(item => item.type === type)?.value || ''
  return `${part('year')}-${part('month')}-${part('day')}`
}


export default function AppointmentsDashboard() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  
  const navigation = role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav
  
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [checkInLoadingId, setCheckInLoadingId] = useState(null)
  const [checkInNotice, setCheckInNotice] = useState(null)
  
  const [filters, setFilters] = useState(role === 'Doctor' ? { doctorId: user.id } : {})
  

  const fetchAppointments = useCallback(async () => {
    try {
      setLoading(true)
      const data = await getAppointments(filters)
      setAppointments(data)
      setError(null)
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load appointments')
    } finally {
      setLoading(false)
    }
  }, [filters])

  useEffect(() => {
    fetchAppointments()
  }, [fetchAppointments])

  const handleCheckIn = async appointment => {
    setCheckInLoadingId(appointment.id)
    setCheckInNotice(null)
    try {
      const entry = await checkIn(appointment.id)
      const position = entry.queuePosition > 0 ? ` · Position ${entry.queuePosition}` : ''
      setCheckInNotice({
        type: 'success',
        text: `${appointment.patientName} checked in · ${entry.queueCode}${position}`
      })
      await fetchAppointments()
    } catch (err) {
      setCheckInNotice({
        type: 'error',
        text: err.response?.data?.message || 'Check-in failed. Please try again.'
      })
    } finally {
      setCheckInLoadingId(null)
    }
  }

  useSignalR({
    AppointmentCreated: fetchAppointments,
    AppointmentUpdated: fetchAppointments,
    AppointmentCancelled: fetchAppointments,
    ConsultationCompleted: fetchAppointments,
    PatientCheckedIn: fetchAppointments,
    QueueUpdated: fetchAppointments
  })

  return (
    <DashboardLayout 
      role={role} 
      navigation={navigation} 
      title={role === 'Doctor' ? 'My Appointments' : 'Appointments'} 
      subtitle="View and manage appointments."
    >
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
          Appointment List
        </h2>
        
        {/* Only Staff/Admin can book appointments for patients */}
        {(role === 'Staff' || role === 'Admin') && (
          <Link to={`/${role.toLowerCase()}/appointments/book`} className="primary-button" style={{ textDecoration: 'none', marginTop: 0 }}>
            + Book Appointment
          </Link>
        )}
      </div>

      
      {role === 'Doctor' ? (
        <DoctorServingList user={user} />
      ) : (
        <>
          <FilterBar filters={filters} onFilterChange={setFilters} />


      {error && <div className="form-error">{error}</div>}
      {checkInNotice && (
        <div className={`mb-4 p-3 rounded-lg border ${checkInNotice.type === 'success' ? 'bg-green-50 text-green-800 border-green-200' : 'bg-red-50 text-red-700 border-red-200'}`}>
          {checkInNotice.text}
        </div>
      )}

      <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr style={{ borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))' }}>
                <th className="p-4 font-semibold text-sm">Ref Number</th>
                <th className="p-4 font-semibold text-sm">Patient</th>
                {role !== 'Doctor' && <th className="p-4 font-semibold text-sm">Doctor</th>}
                <th className="p-4 font-semibold text-sm">Date & Time</th>
                <th className="p-4 font-semibold text-sm">Priority</th>
                <th className="p-4 font-semibold text-sm">Status</th>
                <th className="p-4 font-semibold text-sm text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="7" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    Loading appointments...
                  </td>
                </tr>
              ) : appointments.length === 0 ? (
                <tr>
                  <td colSpan="7" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    No appointments found matching your filters.
                  </td>
                </tr>
              ) : (
                appointments.map(apt => (
                  <tr key={apt.id} style={{ borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))' }}>
                    <td className="p-4 font-medium" style={{ color: 'var(--color-accent)' }}>{apt.referenceNumber}</td>
                    <td className="p-4">
                      <div>{apt.patientName}</div>
                    </td>
                    {role !== 'Doctor' && (
                      <td className="p-4">
                        <div className="text-sm">{apt.doctorName || '-'}</div>
                      </td>
                    )}
                    <td className="p-4">
                      <div className="text-sm">{new Date(apt.scheduledStart).toLocaleString()}</div>
                      <div className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 68%, var(--color-primary))' }}>
                        {apt.estimatedDurationMinutes} mins
                      </div>
                    </td>
                    <td className="p-4">
                      <PriorityBadge priority={apt.priority} />
                    </td>
                    <td className="p-4">
                      <AppointmentStatusBadge status={apt.status} />
                    </td>
                    <td className="p-4 text-right">
                      <div className="flex justify-end items-center gap-3">
                        {['Staff', 'Admin'].includes(role) &&
                          ['Scheduled', 'Confirmed'].includes(apt.status) &&
                          hospitalDateKey(new Date(apt.scheduledStart)) === hospitalDateKey() && (
                            <button
                              className="primary-button text-sm"
                              style={{ marginTop: 0 }}
                              disabled={checkInLoadingId === apt.id}
                              onClick={() => handleCheckIn(apt)}
                            >
                              {checkInLoadingId === apt.id ? 'Checking in...' : 'Check In'}
                            </button>
                          )}
                        <Link
                          to={`/${role.toLowerCase()}/appointments/${apt.id}`}
                          className="text-sm font-semibold hover:underline"
                          style={{ color: 'var(--color-accent)' }}
                        >
                          View Details
                        </Link>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
            </>
      )}
    </DashboardLayout>
  )
}

