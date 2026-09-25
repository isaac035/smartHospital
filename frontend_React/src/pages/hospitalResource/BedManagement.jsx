import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function BedManagement() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Bed Management"
      subtitle="Monitor bed statuses, room allocation, and availability."
    >
      <ResourceNavigation />
      <section className="placeholder-panel">
        <h2>Bed Management Workspace</h2>
        <p>Physical bed allocation, status management (Available, Occupied, Maintenance, Reserved), and room tracking will be available here in the next phase.</p>
      </section>
    </DashboardLayout>
  )
}
