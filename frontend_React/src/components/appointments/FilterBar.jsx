export default function FilterBar({ filters, onFilterChange }) {
  const handleChange = (e) => {
    onFilterChange({
      ...filters,
      [e.target.name]: e.target.value
    })
  }

  return (
    <div className="flex flex-wrap gap-4 mb-6 p-4 rounded-xl border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'var(--color-primary)' }}>
      <div className="flex flex-col gap-1">
        <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>Status</label>
        <select 
          name="status" 
          value={filters.status || ''} 
          onChange={handleChange}
          className="border rounded-md px-3 py-1.5 text-sm"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
        >
          <option value="">All Statuses</option>
          <option value="1">Scheduled</option>
          <option value="2">Confirmed</option>
          <option value="3">Checked In</option>
          <option value="4">In Progress</option>
          <option value="5">Completed</option>
          <option value="6">Cancelled</option>
          <option value="7">No Show</option>
          <option value="8">Rescheduled</option>
        </select>
      </div>
      
      <div className="flex flex-col gap-1">
        <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>Priority</label>
        <select 
          name="priority" 
          value={filters.priority || ''} 
          onChange={handleChange}
          className="border rounded-md px-3 py-1.5 text-sm"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
        >
          <option value="">All Priorities</option>
          <option value="1">Normal</option>
          <option value="2">Urgent</option>
          <option value="3">Emergency</option>
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>From Date</label>
        <input 
          type="date" 
          name="fromDate" 
          value={filters.fromDate || ''} 
          onChange={handleChange}
          className="border rounded-md px-3 py-1.5 text-sm"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs uppercase tracking-wider font-bold" style={{ color: 'var(--color-accent)' }}>To Date</label>
        <input 
          type="date" 
          name="toDate" 
          value={filters.toDate || ''} 
          onChange={handleChange}
          className="border rounded-md px-3 py-1.5 text-sm"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
        />
      </div>
    </div>
  )
}
