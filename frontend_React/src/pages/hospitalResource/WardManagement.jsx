import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function WardManagement() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Ward Management"
      subtitle="Configure hospital wards, capacity, and departments."
    >
      <ResourceNavigation />
      <section className="placeholder-panel">
        <h2>Ward Management Workspace</h2>
        <p>Ward configuration, capacity management, and building block assignments will be available here in the next phase.</p>
      </section>
    </DashboardLayout>
  )
}
