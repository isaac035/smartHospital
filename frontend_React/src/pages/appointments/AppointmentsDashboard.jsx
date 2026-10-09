import { useState, useEffect, useCallback, useRef } from 'react'
import { Link } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { checkIn, getAppointments, getAvailableSlots, getAppointmentConsultationPeriod } from '../../services/appointmentService'
import { useDoctorOptions } from '../../hooks/useDoctorOptions'
import { useSignalR } from '../../hooks/useSignalR'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import FilterBar from '../../components/appointments/FilterBar'
import { adminNavigation as adminNav } from '../admin/adminNavigation'
import { doctorNavigation as doctorNav } from '../doctor/doctorNavigation'
import { staffNavigation as staffNav } from '../staff/staffNavigation'
import { appointmentManagerNavigation } from './appointmentManagerNavigation'
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
  
  const navigation = role === 'AppointmentManager' ? appointmentManagerNavigation : role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav
  const routeRole = role === 'AppointmentManager' ? 'admin' : role.toLowerCase()
  
  const [appointments, setAppointments] = useState([])
  const doctors = useDoctorOptions()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [checkInLoadingId, setCheckInLoadingId] = useState(null)
  const [checkInNotice, setCheckInNotice] = useState(null)
  
  const [filters, setFilters] = useState(role === 'Doctor' ? { doctorId: user.id } : {})
  const [page, setPage] = useState(1)
  const pageSize = 20
  const [pageInfo, setPageInfo] = useState({ totalCount: 0, totalPages: 0, statusCounts: {}, priorityCounts: {} })
  const [consultationPeriod, setConsultationPeriod] = useState('')
  const [periodLoading, setPeriodLoading] = useState(false)
  const [periodError, setPeriodError] = useState(null)
  const requestSequence = useRef(0)
  const safeDoctors = Array.isArray(doctors)
    ? doctors.filter(doctor => doctor && typeof doctor === 'object')
    : []

  const fetchAppointments = useCallback(async () => {
    const sequence = ++requestSequence.current
    try {
      setLoading(true)
      setError(null)
      if (String(filters.doctorId || '').startsWith('profile:')) {
        setAppointments([])
        setPageInfo({ totalCount: 0, totalPages: 0, statusCounts: {}, priorityCounts: {} })
        setLoading(false)
        return
      }
      const data = await getAppointments({ ...filters, page, pageSize })
      if (sequence !== requestSequence.current) return
      if (!data || !Array.isArray(data.appointments)) {
        throw new Error('The appointments response was invalid. Please try again.')
      }
      setAppointments(data.appointments.filter(appointment => appointment && typeof appointment === 'object'))
      setPageInfo({
        totalCount: data.totalCount || 0,
        totalPages: data.totalPages || 0,
        statusCounts: data.statusCounts && typeof data.statusCounts === 'object' ? data.statusCounts : {},
        priorityCounts: data.priorityCounts && typeof data.priorityCounts === 'object' ? data.priorityCounts : {},
      })
    } catch (err) {
      if (sequence === requestSequence.current) {
        setError(err.response?.data?.message || err.message || 'Failed to load appointments')
        setAppointments([])
        setPageInfo({ totalCount: 0, totalPages: 0, statusCounts: {}, priorityCounts: {} })
      }
    } finally {
      if (sequence === requestSequence.current) setLoading(false)
    }
  }, [filters, page])

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

  const refreshForAppointmentEvent = useCallback(event => {
    if (filters.doctorId && event?.doctorId != null && String(event.doctorId) !== String(filters.doctorId)) return
    fetchAppointments()
  }, [filters.doctorId, fetchAppointments])

  useSignalR({
    AppointmentCreated: refreshForAppointmentEvent,
    AppointmentUpdated: refreshForAppointmentEvent,
    AppointmentCancelled: refreshForAppointmentEvent,
    ConsultationCompleted: refreshForAppointmentEvent,
    PatientCheckedIn: refreshForAppointmentEvent,
  })

  const selectedDoctor = safeDoctors.find(doctor => String(doctor.userId ?? `profile:${doctor.id}`) === String(filters.doctorId))

  useEffect(() => {
    if (!filters.doctorId) {
      setConsultationPeriod('')
      setPeriodError(null)
      setPeriodLoading(false)
      return
    }
    if (String(filters.doctorId).startsWith('profile:')) {
      setConsultationPeriod('Doctor has no linked appointment account')
      setPeriodError(null)
      setPeriodLoading(false)
      return
    }
    if (filters.fromDate && filters.toDate && filters.fromDate !== filters.toDate) {
      setConsultationPeriod('Varies by day within the selected date range')
      setPeriodError(null)
      setPeriodLoading(false)
      return
    }

    const date = filters.fromDate || filters.toDate || hospitalDateKey()
    let active = true
    setPeriodLoading(true)
    setPeriodError(null)
    const slotsRequest = role === 'AppointmentManager'
      ? getAppointmentConsultationPeriod(date, Number(filters.doctorId))
      : getAvailableSlots(date, Number(filters.doctorId))
    slotsRequest
      .then(slots => {
        if (!active) return
        if (!Array.isArray(slots)) {
          setConsultationPeriod('')
          setPeriodError('Could not load the consultation period')
          return
        }
        if (!slots.length) {
          setConsultationPeriod('No consultation period configured for this date')
          return
        }
        const ordered = [...slots].sort((a, b) => new Date(a.slotStart) - new Date(b.slotStart))
        const formatTime = value => new Date(value).toLocaleTimeString('en-US', {
          hour: 'numeric', minute: '2-digit', timeZone: 'Asia/Colombo'
        })
        setConsultationPeriod(`${formatTime(ordered[0].slotStart)}–${formatTime(ordered[ordered.length - 1].slotEnd)}`)
      })
      .catch(err => {
        if (active) setPeriodError(err.response?.data?.message || 'Could not load the consultation period')
      })
      .finally(() => {
        if (active) setPeriodLoading(false)
      })
    return () => { active = false }
  }, [filters.doctorId, filters.fromDate, filters.toDate, role])

  const handleFilterChange = nextFilters => {
    setFilters(nextFilters)
    setPage(1)
  }

  const clearFilters = () => {
    setFilters({})
    setPage(1)
  }

  const formatCounts = counts => Object.entries(counts)
    .filter(([, count]) => count > 0)
    .map(([name, count]) => `${count} ${name.replace(/([a-z])([A-Z])/g, '$1 $2')}`)
    .join(' · ') || 'None'

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

//filter bar    
      {role === 'Doctor' ? (
        <DoctorServingList user={user} />
      ) : (
        <>
          <FilterBar filters={filters} doctors={safeDoctors} onFilterChange={handleFilterChange} onClear={clearFilters} />

      {selectedDoctor && (
        <div className="stat-card mb-6" style={{ padding: '16px 20px' }}>
          <div className="font-bold text-base">Dr. {selectedDoctor.firstName} {selectedDoctor.lastName}</div>
          <div className="text-sm text-gray-600">{selectedDoctor.departmentName} · {selectedDoctor.specialization}</div>
          <div className="text-sm mt-2">
            <span className="font-semibold">Consultation period: </span>
            {periodLoading ? 'Loading…' : periodError || consultationPeriod}
          </div>
          <div className="text-sm mt-1"><span className="font-semibold">Appointments by status: </span>{formatCounts(pageInfo.statusCounts)}</div>
          <div className="text-sm mt-1"><span className="font-semibold">Appointments by priority: </span>{formatCounts(pageInfo.priorityCounts)}</div>
        </div>
      )}


      {error && (
        <div className="form-error flex items-center justify-between gap-3" role="alert">
          <span>{error}</span>
          <button type="button" className="secondary-button" onClick={fetchAppointments}>Retry</button>
        </div>
      )}
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
              ) : error ? (
                <tr>
                  <td colSpan="7" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    Appointments could not be loaded.
                  </td>
                </tr>
              ) : appointments.length === 0 ? (
                <tr>
                  <td colSpan="7" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                    {filters.doctorId
                      ? (filters.status || filters.priority || filters.fromDate || filters.toDate || filters.departmentId || filters.patientId)
                        ? 'No appointments found for this doctor with the selected filters'
                        : 'No appointments found for this doctor'
                      : 'No appointments found matching your filters.'}
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
                      {apt.priorityNeedsReview && <div className="text-xs mt-1 text-gray-500">Patient requested</div>}
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
                          to={`/${routeRole}/appointments/${apt.id}`}
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
      {!loading && pageInfo.totalCount > 0 && (
        <div className="flex items-center justify-between px-2 py-4 text-sm">
          <span>Showing {((page - 1) * pageSize) + 1}–{Math.min(page * pageSize, pageInfo.totalCount)} of {pageInfo.totalCount}</span>
          <div className="flex gap-2">
            <button className="secondary-button" disabled={page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous</button>
            <span className="px-2 py-2">Page {page} of {pageInfo.totalPages}</span>
            <button className="secondary-button" disabled={page >= pageInfo.totalPages} onClick={() => setPage(current => current + 1)}>Next</button>
          </div>
        </div>
      )}
            </>
      )}
    </DashboardLayout>
  )
}

