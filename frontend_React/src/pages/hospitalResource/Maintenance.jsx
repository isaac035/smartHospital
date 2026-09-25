import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function Maintenance() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Resource Maintenance"
      subtitle="Schedule and track maintenance logs for beds and clinical equipment."
    >
      <ResourceNavigation />
      <section className="placeholder-panel">
        <h2>Maintenance Workspace</h2>
        <p>Preventive maintenance scheduling, breakdown logs, repair notes, and resolution workflows will be available here in the next phase.</p>
      </section>
    </DashboardLayout>
  )
}
