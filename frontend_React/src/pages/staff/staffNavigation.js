export const staffNavigation = [
  { label: 'Dashboard', path: '/staff/dashboard' },
  { label: 'Patients' },
  { 
    label: 'Appointments', 
    children: [
      { label: 'Appointment Management', path: '/staff/appointments' },
      { label: 'Queue Management', path: '/staff/queue-management' }
    ]
  },
  { label: 'Resource Management', path: '/hospital-resources' },
]
