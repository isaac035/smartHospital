import { useEffect, useState } from 'react'
import { listDoctors } from '../services/doctorService'
import { getAppointmentDoctorOptions } from '../services/appointmentService'
import { useAuth } from './useAuth'

// Keep admin doctor selectors current when another page creates or activates a doctor.
export function useDoctorOptions(enabled = true) {
  const { user } = useAuth()
  const [doctors, setDoctors] = useState([])

  useEffect(() => {
    if (!enabled) {
      setDoctors([])
      return undefined
    }

    let active = true
    let requestInFlight = false
    const refresh = async () => {
      if (requestInFlight) return
      requestInFlight = true
      try {
        const result = user?.role === 'AppointmentManager'
          ? await getAppointmentDoctorOptions()
          : await listDoctors()
        if (active) setDoctors(Array.isArray(result) ? result : [])
      } catch {
        // Keep the last successful options visible during a transient API failure.
      } finally {
        requestInFlight = false
      }
    }
    const onFocus = () => {
      if (document.visibilityState === 'visible') refresh()
    }

    refresh()
    const interval = window.setInterval(refresh, 30000)
    window.addEventListener('focus', onFocus)
    document.addEventListener('visibilitychange', onFocus)
    return () => {
      active = false
      window.clearInterval(interval)
      window.removeEventListener('focus', onFocus)
      document.removeEventListener('visibilitychange', onFocus)
    }
  }, [enabled, user?.role])

  return doctors
}
