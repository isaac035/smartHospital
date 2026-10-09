export const adminNavigation = [
  { label: 'Dashboard', path: '/admin/dashboard' },
  { label: 'User Management', path: '/admin/users' },
  { label: 'Doctor Management', path: '/admin/doctors' },
  { label: 'Admissions & Resources', path: '/hospital-resources' },
  { label: 'Department Management', path: '/admin/departments' },
  { label: 'Consultation Types', path: '/admin/consultation-types' },
  { label: 'Doctor Availability Calendar', path: '/admin/schedules' },
  { label: 'Leave / Unavailability', path: '/admin/leaves' },
  { 
    label: 'Appointments', 
    children: [
      { label: 'Appointment Management', path: '/admin/appointments' },
      { label: 'Doctor Appointment Management', path: '/admin/doctor-appointments' },
      { label: 'Queue Management', path: '/admin/queue-management' }
    ]
  },
  { label: 'Reports', path: '/admin/reports' },
]
