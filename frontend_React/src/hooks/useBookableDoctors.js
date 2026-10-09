import { useEffect, useState } from 'react'
import { listDoctors } from '../services/doctorService'
import { getAppointmentDoctorOptions } from '../services/appointmentService'
import { useAuth } from './useAuth'

// Doctors that the Appointment/Queue module can use. Appointments reference the
// doctor's User id (not the Doctor profile id), so only active doctors with a
// linked account are listed, keyed by userId.
// Appointment Managers read the active-doctor options endpoint (they cannot list doctors directly).
export function useBookableDoctors() {
  const { user } = useAuth()
  const [doctors, setDoctors] = useState([])

  useEffect(() => {
    let cancelled = false
    const isAppointmentManager = user?.role === 'AppointmentManager'
    const request = isAppointmentManager ? getAppointmentDoctorOptions() : listDoctors()
    request
      .then((data) => {
        if (cancelled) return
        setDoctors(
          data
            .filter((d) => d.userId != null && (isAppointmentManager || d.status === 'Active'))
            .map((d) => ({ id: d.userId, name: `Dr. ${d.firstName} ${d.lastName}` }))
        )
      })
      .catch(() => {
        if (!cancelled) setDoctors([])
      })
    return () => {
      cancelled = true
    }
  }, [user?.role])

  return doctors
}
