export default function PriorityBadge({ priority }) {
  // Using Tailwind utility classes for semantic colors since index.css
  // only defines --color-primary, --color-secondary, --color-accent
  
  let styles = 'bg-gray-800 text-gray-200 border-gray-700'
  
  if (priority === 'Emergency' || priority === 3) {
    styles = 'bg-red-900 text-red-100 border border-red-700 font-bold shadow-[0_0_8px_rgba(220,38,38,0.5)]'
  } else if (priority === 'Urgent' || priority === 2) {
    styles = 'bg-orange-900 text-orange-200 border border-orange-700'
  } else if (priority === 'Normal' || priority === 1) {
    styles = 'bg-green-900 text-green-200 border border-green-700'
  }

  const label = priority === 3 ? 'Emergency' : priority === 2 ? 'Urgent' : priority === 1 ? 'Normal' : priority

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${styles}`}>
      {label}
    </span>
  )
}
