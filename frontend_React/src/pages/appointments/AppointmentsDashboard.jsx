import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import DashboardLayout from '../../layouts/DashboardLayout'
import { getAppointments } from '../../services/appointmentService'
import AppointmentStatusBadge from '../../components/appointments/AppointmentStatusBadge'
import PriorityBadge from '../../components/appointments/PriorityBadge'
import FilterBar from '../../components/appointments/FilterBar'
import { useAuth } from '../../hooks/useAuth'

// Navigation lists for different roles
const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']
const doctorNav = ['Dashboard', 'My Appointments', 'My Patients', 'Medical Records', 'Prescriptions', 'Lab Reports']

export default function AppointmentsDashboard() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  
  const navigation = role === 'Admin' ? adminNav : role === 'Doctor' ? doctorNav : staffNav
  
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [filters, setFilters] = useState({})

  useEffect(() => {
    fetchAppointments()
  }, [filters])

  const fetchAppointments = async () => {
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
  }

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

      <FilterBar filters={filters} onFilterChange={setFilters} />

      {error && <div className="form-error">{error}</div>}

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
                      <Link 
                        to={`/${role.toLowerCase()}/appointments/${apt.id}`}
                        className="text-sm font-semibold hover:underline"
                        style={{ color: 'var(--color-accent)' }}
                      >
                        View Details
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </DashboardLayout>
  )
}
