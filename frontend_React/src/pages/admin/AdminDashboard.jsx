import DashboardLayout from '../../layouts/DashboardLayout'
import DashboardCards from '../DashboardCards'
import { adminNavigation } from './adminNavigation'

const cards = [['Total Patients', '1,248'], ['Total Doctors', '86'], ['Total Staff', '214'], ["Today's Appointments", '42'], ['Pending Items', '12']]

export default function AdminDashboard() { return <DashboardLayout role="Admin" navigation={adminNavigation} title="Dashboard" subtitle="An overview of hospital operations."><DashboardCards cards={cards} /><section className="placeholder-panel"><h2>Administration overview</h2><p>Detailed management modules will be available here as they are implemented.</p></section></DashboardLayout> }
