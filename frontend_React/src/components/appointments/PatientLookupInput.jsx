import { useEffect, useState } from 'react'
import FieldError from '../common/FieldError'
import { searchPatients } from '../../services/userService'

const normalize = (value) => value.trim().replace(/\s+/g, ' ').toLowerCase()

// Patient name with suggestions of existing patients. Picking a suggestion calls onSelect(patient);
// typing a name that is not picked is allowed (the booking form creates a new patient for it).
export default function PatientLookupInput({ value, selectedPatient, onChange, onSelect, onBlur, error, name = 'patientName' }) {
  const [suggestions, setSuggestions] = useState([])
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)
  const [searchError, setSearchError] = useState('')
  const [activeIndex, setActiveIndex] = useState(-1)

  useEffect(() => {
    const query = value.trim()
    if (selectedPatient || query.length < 2) {
      setSuggestions([])
      setLoading(false)
      setSearchError('')
      return undefined
    }

    let cancelled = false
    setLoading(true)
    const timer = window.setTimeout(async () => {
      setSearchError('')
      try {
        const results = await searchPatients(query, 10)
        if (cancelled) return
        const typed = normalize(query)
        // An exact name match goes first so it is picked instead of creating a duplicate patient.
        const list = Array.isArray(results) ? results : []
        setSuggestions([...list.filter((p) => normalize(p.displayName) === typed), ...list.filter((p) => normalize(p.displayName) !== typed)])
        setActiveIndex(-1)
      } catch {
        if (!cancelled) {
          setSuggestions([])
          setSearchError('Patient search is unavailable right now. You can still enter the name.')
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }, 300)

    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [value, selectedPatient])

  const select = (patient) => {
    onSelect(patient)
    setOpen(false)
  }

  const showList = open && !selectedPatient && value.trim().length >= 2
  const typed = normalize(value)

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={name} className="required">Patient Name</label>
      <div className="relative">
        <input
          id={name}
          name={name}
          type="text"
          required
          maxLength={100}
          autoComplete="off"
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={showList}
          aria-controls={`${name}-suggestions`}
          aria-activedescendant={activeIndex >= 0 ? `${name}-option-${activeIndex}` : undefined}
          value={value}
          onChange={(e) => { onChange(e.target.value); setOpen(true) }}
          onFocus={() => setOpen(true)}
          onBlur={() => { window.setTimeout(() => setOpen(false), 100); onBlur?.() }}
          onKeyDown={(e) => {
            if (!showList || !suggestions.length) return
            if (e.key === 'ArrowDown') {
              e.preventDefault()
              setActiveIndex((index) => Math.min(index + 1, suggestions.length - 1))
            } else if (e.key === 'ArrowUp') {
              e.preventDefault()
              setActiveIndex((index) => Math.max(index - 1, 0))
            } else if (e.key === 'Enter' && activeIndex >= 0) {
              e.preventDefault()
              select(suggestions[activeIndex])
            } else if (e.key === 'Escape') {
              setOpen(false)
            }
          }}
          placeholder="Start typing a patient name"
          aria-invalid={error ? 'true' : undefined}
          aria-describedby={error ? `${name}-error` : undefined}
        />
        {showList && (
          <div
            id={`${name}-suggestions`}
            role="listbox"
            aria-label="Matching patients"
            className="absolute z-30 mt-1 w-full max-h-64 overflow-y-auto rounded-lg border shadow-lg"
            style={{ background: 'var(--color-primary)', borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
          >
            {loading ? (
              <div className="px-3 py-2 text-sm" role="status" style={{ opacity: 0.7 }}>Searching patients...</div>
            ) : searchError ? (
              <div className="px-3 py-2 text-sm" role="status" style={{ color: 'var(--color-error)' }}>{searchError}</div>
            ) : suggestions.length ? suggestions.map((patient, index) => (
              <button
                type="button"
                role="option"
                aria-selected={index === activeIndex}
                id={`${name}-option-${index}`}
                key={patient.id}
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => select(patient)}
                className="block w-full px-3 py-2 text-left text-sm"
                style={{ background: index === activeIndex ? 'color-mix(in srgb, var(--color-accent) 15%, var(--color-primary))' : 'transparent', color: 'var(--color-secondary)', border: 0 }}
              >
                <span className="font-medium">{patient.displayName}</span>
                <span className="ml-2 text-xs" style={{ opacity: 0.6 }}>
                  Patient #{patient.id}{normalize(patient.displayName) === typed ? ' · exact match' : ''}
                </span>
              </button>
            )) : (
              <div className="px-3 py-2 text-sm" role="status" style={{ opacity: 0.7 }}>No matching patients. A new patient will be created with this name.</div>
            )}
          </div>
        )}
      </div>
      <FieldError name={name} message={error} />
      <span className="text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 60%, var(--color-primary))' }}>
        {selectedPatient ? 'Existing patient selected.' : 'Start typing to search existing patients, or enter a new name.'}
      </span>
    </div>
  )
}
