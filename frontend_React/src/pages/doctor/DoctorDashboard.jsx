import DashboardLayout from '../../layouts/DashboardLayout'
import { doctorNavigation } from './doctorNavigation'

export default function DoctorDashboard() {
  return <DashboardLayout role="Doctor" navigation={doctorNavigation} title="My Dashboard" subtitle="Your clinical work at a glance.">
    <p>Use the navigation to open your appointments, schedule, and clinical records.</p>
  </DashboardLayout>
}
