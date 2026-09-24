export default function PriorityBadge({ priority }) {
  // Using Tailwind utility classes for semantic colors since index.css
  // only defines --color-primary, --color-secondary, --color-accent
  
  let styles = 'bg-gray-100 text-gray-800'
  
  if (priority === 'Emergency' || priority === 3) {
    styles = 'bg-red-100 text-red-800 border border-red-200 font-bold'
  } else if (priority === 'Urgent' || priority === 2) {
    styles = 'bg-orange-100 text-orange-800 border border-orange-200'
  } else if (priority === 'Normal' || priority === 1) {
    styles = 'bg-green-100 text-green-800 border border-green-200'
  }

  const label = priority === 3 ? 'Emergency' : priority === 2 ? 'Urgent' : priority === 1 ? 'Normal' : priority

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${styles}`}>
      {label}
    </span>
  )
}
