import DashboardLayout from '../../layouts/DashboardLayout'
import DashboardCards from '../DashboardCards'
import { doctorNavigation } from './doctorNavigation'

const cards = [["Today's Appointments", '8'], ['My Patients', '34'], ['Pending Reports', '5'], ['Upcoming Appointments', '12']]

export default function DoctorDashboard() { return <DashboardLayout role="Doctor" navigation={doctorNavigation} title="My Dashboard" subtitle="Your clinical work at a glance."><DashboardCards cards={cards} /><section className="placeholder-panel"><h2>Clinical workspace</h2><p>Your appointments, patient records, and reports will appear here as those modules are added.</p></section></DashboardLayout> }
