export default function ClinicalHistoryTimeline({ events = [], loading = false }) {
  if (loading) {
    return (
      <div className="panel" style={{ padding: 28, textAlign: 'center' }}>
        <p className="placeholder-text">Loading clinical history timeline...</p>
      </div>
    )
  }

  if (events.length === 0) {
    return (
      <div className="panel" style={{ padding: 28, textAlign: 'center' }}>
        <h3 className="font-bold text-lg mb-2" style={{ color: 'var(--color-accent)' }}>Clinical History Timeline</h3>
        <p className="placeholder-text text-sm">No timeline events or past encounters recorded for this patient.</p>
      </div>
    )
  }

  const getBadgeClass = (eventType) => {
    switch (eventType?.toLowerCase()) {
      case 'consultation':
      case 'medicalrecord':
        return 'badge-primary'
      case 'vitalsign':
      case 'vitals':
        return 'badge-info'
      case 'prescription':
        return 'badge-success'
      case 'laborder':
      case 'labreport':
        return 'badge-warning'
      default:
        return 'badge-secondary'
    }
  }

  return (
    <div className="panel" style={{ padding: 24 }}>
      <div className="flex justify-between items-center mb-6 pb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
        <div>
          <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>
            Clinical History Timeline ({events.length} Events)
          </h3>
          <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
            Complete chronological clinical journey across all hospital encounters
          </span>
        </div>
      </div>

      <div className="timeline-container">
        {events.map((evt, idx) => {
          const badgeClass = getBadgeClass(evt.eventType)
          const formattedDate = evt.eventDate ? new Date(evt.eventDate).toLocaleString() : 'Date N/A'

          return (
            <div key={idx} className="timeline-item">
              <div className="timeline-dot" />
              <div className="timeline-card">
                <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
                  <div className="flex items-center gap-2">
                    <span className={`badge ${badgeClass}`}>
                      {evt.eventType || 'Clinical Event'}
                    </span>
                    {evt.category && (
                      <span className="text-xs font-semibold px-2 py-0.5 rounded bg-gray-100 text-gray-700">
                        {evt.category}
                      </span>
                    )}
                    {evt.status && (
                      <span className="text-xs text-gray-500">
                        Status: <strong>{evt.status}</strong>
                      </span>
                    )}
                  </div>
                  <span className="text-xs font-medium" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
                    {formattedDate}
                  </span>
                </div>

                <div className="text-sm font-semibold mb-1" style={{ color: 'var(--color-secondary)' }}>
                  {evt.summary || 'Encounter record'}
                </div>

                <div className="flex justify-between items-center text-xs mt-2 pt-2 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))', color: 'color-mix(in srgb, var(--color-secondary) 60%, var(--color-primary))' }}>
                  <span>Provider: {evt.doctorName ? `Dr. ${evt.doctorName}` : 'Hospital Staff'}</span>
                  {evt.sourceRecordId ? (
                    <span className="text-gray-400">Ref #{evt.sourceRecordId}</span>
                  ) : null}
                </div>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
