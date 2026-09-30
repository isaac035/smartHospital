import { useCallback, useEffect, useRef, useState } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import DashboardCards from '../DashboardCards'
import { adminNavigation } from './adminNavigation'
import { getDashboardSummary } from '../../services/dashboardService'
import { getOccupancyOverview, getAdmissions } from '../../services/hospitalResourceService'
import { useSignalR } from '../../hooks/useSignalR'

const coreCardRoutes = {
  totalPatients: '/admin/users',
  totalDoctors: '/admin/doctors',
  totalStaff: '/admin/users',
  todaysAppointments: '/admin/appointments',
}

const coreCardLabels = [
  ['totalPatients', 'Total Patients'],
  ['totalDoctors', 'Total Doctors'],
  ['totalStaff', 'Total Staff'],
  ['todaysAppointments', "Today's Appointments"],
]

const resourceCardRoutes = {
  activeAdmissions: '/hospital-resources/admissions',
  activeWards: '/hospital-resources/wards',
  totalBeds: '/hospital-resources/beds',
  totalMedicalResources: '/hospital-resources/medical-resources',
}

const resourceCardLabels = [
  ['activeAdmissions', 'Active Admissions'],
  ['activeWards', 'Active Wards'],
  ['totalBeds', 'Total Beds'],
  ['totalMedicalResources', 'Total Medical Resources'],
]

export default function AdminDashboard() {
  const [summary, setSummary] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const summaryRef = useRef(null)

  const refreshSummary = useCallback(async ({ showLoading = false } = {}) => {
    if (showLoading || !summaryRef.current) setLoading(true)
    try {
      const [dashData, occData, admissionsData] = await Promise.all([
        getDashboardSummary(),
        getOccupancyOverview(),
        getAdmissions({ status: 1, pageSize: 100 }),
      ])

      let activeAdmissionsCount = Array.isArray(admissionsData)
        ? admissionsData.filter((a) => a.status?.toLowerCase() === 'admitted').length
        : 0

      // If there are more than 100 admitted patients, fetch subsequent pages
      if (Array.isArray(admissionsData) && admissionsData.length === 100) {
        let pageNum = 2
        let hasMore = true
        while (hasMore) {
          const nextPage = await getAdmissions({ page: pageNum, pageSize: 100, status: 1 })
          if (Array.isArray(nextPage) && nextPage.length > 0) {
            activeAdmissionsCount += nextPage.filter((a) => a.status?.toLowerCase() === 'admitted').length
            if (nextPage.length < 100 || pageNum >= 20) {
              hasMore = false
            } else {
              pageNum++
            }
          } else {
            hasMore = false
          }
        }
      }

      const nextSummary = {
        // Core dashboard metrics
        totalPatients: dashData?.totalPatients ?? 0,
        totalDoctors: dashData?.totalDoctors ?? 0,
        totalStaff: dashData?.totalStaff ?? 0,
        todaysAppointments: dashData?.todaysAppointments ?? 0,

        // Resource module metrics (consistent with Resource Dashboard)
        activeAdmissions: activeAdmissionsCount,
        activeWards: occData?.activeWards ?? occData?.ActiveWards ?? 0,
        totalBeds: occData?.totalBeds ?? occData?.TotalBeds ?? 0,
        totalMedicalResources: occData?.totalMedicalResources ?? occData?.TotalMedicalResources ?? 0,
      }

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

  const coreCards = coreCardLabels.map(([key, label]) => ({
    label,
    value: summary?.[key] ?? 0,
    to: coreCardRoutes[key],
  }))

  const resourceCards = resourceCardLabels.map(([key, label]) => ({
    label,
    value: summary?.[key] ?? 0,
    to: resourceCardRoutes[key],
  }))

  return (
    <DashboardLayout
      role="Admin"
      navigation={adminNavigation}
      title="Dashboard"
      subtitle="An overview of hospital operations."
    >
      <div style={{ display: 'grid', gap: '20px' }}>
        <DashboardCards
          cards={coreCards}
          loading={loading}
          error={error}
          onRetry={() => refreshSummary({ showLoading: true })}
        />
        <DashboardCards
          cards={resourceCards}
          loading={loading}
          error={error}
          onRetry={() => refreshSummary({ showLoading: true })}
        />
      </div>
    </DashboardLayout>
  )
}
