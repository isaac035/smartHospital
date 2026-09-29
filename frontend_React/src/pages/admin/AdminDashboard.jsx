import { useCallback, useEffect, useRef, useState } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import DashboardCards from '../DashboardCards'
import { adminNavigation } from './adminNavigation'
import { getDashboardSummary } from '../../services/dashboardService'
import { useSignalR } from '../../hooks/useSignalR'

const cardRoutes = {
  totalPatients: '/admin/users',
  totalDoctors: '/admin/doctors',
  totalStaff: '/admin/users',
  todaysAppointments: '/admin/appointments',
}

const cardLabels = [
  ['totalPatients', 'Total Patients'],
  ['totalDoctors', 'Total Doctors'],
  ['totalStaff', 'Total Staff'],
  ['todaysAppointments', "Today's Appointments"],
]

export default function AdminDashboard() {
  const [summary, setSummary] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const summaryRef = useRef(null)

  const refreshSummary = useCallback(async ({ showLoading = false } = {}) => {
    if (showLoading || !summaryRef.current) setLoading(true)
    try {
      const nextSummary = await getDashboardSummary()
      summaryRef.current = nextSummary
      setSummary(nextSummary)
      setError(false)
    } catch {
      summaryRef.current = null
      setSummary(null)
      setError(true)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    refreshSummary({ showLoading: true })
    const interval = window.setInterval(() => refreshSummary(), 45000)
    const refreshOnFocus = () => {
      if (document.visibilityState === 'visible') refreshSummary()
    }
    window.addEventListener('focus', refreshOnFocus)
    document.addEventListener('visibilitychange', refreshOnFocus)
    return () => {
      window.clearInterval(interval)
      window.removeEventListener('focus', refreshOnFocus)
      document.removeEventListener('visibilitychange', refreshOnFocus)
    }
  }, [refreshSummary])

  useSignalR({
    AppointmentCreated: () => refreshSummary(),
    AppointmentUpdated: () => refreshSummary(),
    AppointmentCancelled: () => refreshSummary(),
    ConsultationCompleted: () => refreshSummary(),
  })

  const cards = cardLabels.map(([key, label]) => ({
    label,
    value: summary?.[key] ?? 0,
    to: cardRoutes[key],
  }))

  return <DashboardLayout role="Admin" navigation={adminNavigation} title="Dashboard" subtitle="An overview of hospital operations.">
    <DashboardCards cards={cards} loading={loading} error={error} onRetry={() => refreshSummary({ showLoading: true })} />
  </DashboardLayout>
}
