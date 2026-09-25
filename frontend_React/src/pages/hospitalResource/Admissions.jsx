import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function Admissions() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Inpatient Admissions"
      subtitle="Manage patient admissions, transfers, and discharge workflows."
    >
      <ResourceNavigation />
      <section className="placeholder-panel">
        <h2>Admissions Workspace</h2>
        <p>Inpatient admission registration, admitting doctor assignment, bed allocation, and discharge summaries will be available here in the next phase.</p>
      </section>
    </DashboardLayout>
  )
}
