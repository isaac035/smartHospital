export default function AppointmentStatusBadge({ status }) {
  let styles = 'bg-gray-100 text-gray-800 border-gray-200'
  
  switch(status) {
    case 'Scheduled':
    case 1:
      styles = 'bg-blue-100 text-blue-800 border-blue-200'
      break
    case 'Confirmed':
    case 2:
      styles = 'bg-indigo-100 text-indigo-800 border-indigo-200'
      break
    case 'CheckedIn':
    case 3:
      styles = 'bg-purple-100 text-purple-800 border-purple-200'
      break
    case 'InProgress':
    case 4:
      styles = 'bg-yellow-100 text-yellow-800 border-yellow-200'
      break
    case 'Completed':
    case 5:
      styles = 'bg-green-100 text-green-800 border-green-200'
      break
    case 'Cancelled':
    case 6:
      styles = 'bg-red-100 text-red-800 border-red-200'
      break
    case 'NoShow':
    case 7:
      styles = 'bg-gray-200 text-gray-600 border-gray-300'
      break
    case 'Rescheduled':
    case 8:
      styles = 'bg-orange-100 text-orange-800 border-orange-200'
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
