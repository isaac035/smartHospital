import FieldError from '../common/FieldError'

export default function PatientLookupInput({ value, onChange, onBlur, error, name = 'patientId' }) {
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={name} className="required">Patient ID</label>
      <input 
        id={name}
        name={name}
        type="number" 
        required 
        min="1"
        step="1"
        value={value} 
        onChange={e => onChange(e.target.value)} 
        onBlur={onBlur}
        placeholder="Enter Patient ID (e.g. 1)"
        aria-invalid={error ? 'true' : undefined}
        aria-describedby={error ? `${name}-error` : undefined}
      />
      <FieldError name={name} message={error} />
      <span className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 60%, var(--color-primary))' }}>
        // TODO: Replace with shared patient-search component once available
      </span>
    </div>
  )
}
