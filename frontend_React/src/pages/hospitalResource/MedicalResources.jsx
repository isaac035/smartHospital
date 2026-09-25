import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function MedicalResources() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Medical Resources"
      subtitle="Track medical equipment, ventilators, monitors, and devices."
    >
      <ResourceNavigation />
      <section className="placeholder-panel">
        <h2>Medical Resources Workspace</h2>
        <p>Resource registry, serial numbers, categories, manufacturer details, and location tracking will be available here in the next phase.</p>
      </section>
    </DashboardLayout>
  )
}
