import { useState, useEffect } from 'react'
import { getAvailableSlots } from '../../services/appointmentService'

export default function SlotPicker({ doctorId, departmentId, date, onSlotSelect, selectedSlot, durationMinutes = 30 }) {
  const [slots, setSlots] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (!date) return
    if (!doctorId && !departmentId) return

    const fetchSlots = async () => {
      try {
        setLoading(true)
        const data = await getAvailableSlots(date, doctorId, departmentId)
        
        // Filter by duration if needed, though backend handles the basic chunks
        setSlots(data)
        setError(null)
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to fetch slots')
      } finally {
        setLoading(false)
      }
    }
    
    fetchSlots()
  }, [date, doctorId, departmentId])

  if (!date || (!doctorId && !departmentId)) {
    return <div className="p-4 text-sm text-center border rounded-md" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', color: 'color-mix(in srgb, var(--color-secondary) 60%, var(--color-primary))' }}>
      Select a date and a doctor/department to see available slots.
    </div>
  }

  if (loading) {
    return <div className="p-4 text-sm text-center border rounded-md" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>Loading slots...</div>
  }

  if (error) {
    return <div className="p-4 text-sm text-center border rounded-md text-red-600 bg-red-50 border-red-200">{error}</div>
  }

  if (slots.length === 0) {
    return <div className="p-4 text-sm text-center border rounded-md" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
      No available slots found for this date.
    </div>
  }

  return (
    <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2 mt-2">
      {slots.map((slot, idx) => {
        const d = new Date(slot.slotStart)
        const isSelected = selectedSlot === slot.slotStart
        
        return (
          <button
            key={idx}
            type="button"
            onClick={() => onSlotSelect(slot.slotStart)}
            className={`py-2 px-1 text-sm rounded-md border text-center transition-colors ${
              isSelected 
                ? 'border-transparent font-bold' 
                : 'hover:bg-gray-50'
            }`}
            style={{
              borderColor: isSelected ? 'transparent' : 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
              background: isSelected ? 'var(--color-accent)' : 'var(--color-primary)',
              color: isSelected ? 'var(--color-primary)' : 'var(--color-secondary)'
            }}
          >
            {d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </button>
        )
      })}
    </div>
  )
}
