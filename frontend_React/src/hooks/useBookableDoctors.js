import { useEffect, useState } from 'react'
import { listDoctors } from '../services/doctorService'

// Doctors that the Appointment/Queue module can use. Appointments reference the
// doctor's User id (not the Doctor profile id), so only active doctors with a
// linked account are listed, keyed by userId.
export function useBookableDoctors() {
  const [doctors, setDoctors] = useState([])

  useEffect(() => {
    let cancelled = false
    listDoctors()
      .then((data) => {
        if (cancelled) return
        setDoctors(
          data
            .filter((d) => d.userId != null && d.status === 'Active')
            .map((d) => ({ id: d.userId, name: `Dr. ${d.firstName} ${d.lastName}` }))
        )
      })
      .catch(() => {
        if (!cancelled) setDoctors([])
      })
    return () => {
      cancelled = true
    }
  }, [])

  return doctors
}
