import DashboardLayout from '../../layouts/DashboardLayout'
import DashboardCards from '../DashboardCards'

const cards = [["Today's Appointments", '42'], ['Waiting Patients', '16'], ['Queue Status', 'Active'], ['Available Resources', '9']]
const navigation = [
  { label: 'Dashboard', path: '/staff/dashboard' },
  { label: 'Patients' },
  { label: 'Appointments' },
  { label: 'Queue Management' },
  { label: 'Resources' },
]

export default function StaffDashboard() { return <DashboardLayout role="Staff" navigation={navigation} title="Staff Dashboard" subtitle="Today’s front-desk and operational activity."><DashboardCards cards={cards} /><section className="placeholder-panel"><h2>Operations workspace</h2><p>Patient, appointment, queue, and resource tools will be added in their respective modules.</p></section></DashboardLayout> }
