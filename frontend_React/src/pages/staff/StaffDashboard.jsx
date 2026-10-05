import DashboardLayout from '../../layouts/DashboardLayout'

const navigation = [
  { label: 'Dashboard', path: '/staff/dashboard' },
  { label: 'Patients' },
  { label: 'Appointments', path: '/staff/appointments' },
  { label: 'Queue Management', path: '/staff/queue-management' },
  { label: 'Resource Management', path: '/hospital-resources' },
]

export default function StaffDashboard() {
  return <DashboardLayout role="Staff" navigation={navigation} title="Staff Dashboard" subtitle="Today’s front-desk and operational activity.">
    <p>Use the navigation to open appointments, queue management, and hospital resources.</p>
  </DashboardLayout>
}
