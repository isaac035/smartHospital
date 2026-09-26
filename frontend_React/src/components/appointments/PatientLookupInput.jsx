export default function PatientLookupInput({ value, onChange }) {
  return (
    <div className="flex flex-col gap-1">
      <label>Patient ID *</label>
      <input 
        type="number" 
        required 
        min="1"
        value={value} 
        onChange={e => onChange(e.target.value)} 
        placeholder="Enter Patient ID (e.g. 1)"
      />
      <span className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 60%, var(--color-primary))' }}>
        // TODO: Replace with shared patient-search component once available
      </span>
    </div>
  )
}
