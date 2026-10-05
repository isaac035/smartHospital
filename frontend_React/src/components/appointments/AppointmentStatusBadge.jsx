export default function AppointmentStatusBadge({ status }) {
  let styles = 'bg-gray-800 text-gray-200 border-gray-700'
  
  switch(status) {
    case 'Scheduled':
    case 1:
      styles = 'bg-blue-900 text-blue-200 border-blue-700'
      break
    case 'Confirmed':
    case 2:
      styles = 'bg-indigo-900 text-indigo-200 border-indigo-700'
      break
    case 'CheckedIn':
    case 3:
      styles = 'bg-purple-900 text-purple-200 border-purple-700'
      break
    case 'InProgress':
    case 4:
      styles = 'bg-yellow-900 text-yellow-200 border-yellow-700'
      break
    case 'Completed':
    case 5:
      styles = 'bg-green-900 text-green-200 border-green-700'
      break
    case 'Cancelled':
    case 6:
      styles = 'bg-red-900 text-red-200 border-red-700'
      break
    case 'NoShow':
    case 7:
      styles = 'bg-gray-700 text-gray-300 border-gray-600'
      break
    case 'Rescheduled':
    case 8:
      styles = 'bg-orange-900 text-orange-200 border-orange-700'
      break
  }

  const label = typeof status === 'string' 
    ? status.replace(/([A-Z])/g, ' $1').trim() // "CheckedIn" -> "Checked In"
    : status

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${styles}`}>
      {label}
    </span>
  )
}
