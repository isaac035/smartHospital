export default function AdmissionDetailsModal({ isOpen, onClose, admission }) {
  if (!isOpen || !admission) return null

  const formatDate = (dateStr) => {
    if (!dateStr) return '—'
    try {
      const d = new Date(dateStr)
      return d.toLocaleString()
    } catch {
      return dateStr
    }
  }

  const getPriorityBadge = (priority) => {
    switch (priority) {
      case 'Emergency':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-red-100 text-red-800 border border-red-200">
            Emergency
          </span>
        )
      case 'Urgent':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
            Urgent
          </span>
        )
      default:
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200">
            {priority || 'Normal'}
          </span>
        )
    }
  }

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Admitted':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
            Admitted
          </span>
        )
      case 'Discharged':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-700 border border-gray-300">
            Discharged
          </span>
        )
      case 'Cancelled':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-red-50 text-red-700 border border-red-200">
            Cancelled
          </span>
        )
      default:
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-700 border border-gray-200">
            {status || 'Unknown'}
          </span>
        )
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 overflow-y-auto">
      <div
        className="w-full max-w-2xl my-8 p-6 rounded-2xl shadow-2xl relative"
        style={{
          background: 'var(--color-primary)',
          border: '1px solid color-mix(in srgb, var(--color-secondary) 18%, var(--color-primary))',
        }}
      >
        <div className="flex items-center justify-between pb-4 mb-4 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Admission Details
            </h2>
            <p className="text-xs font-mono mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
              {admission.admissionNumber}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-2xl leading-none font-bold"
            aria-label="Close"
          >
            &times;
          </button>
        </div>

        <div className="flex flex-col gap-4 text-sm">
          {/* Status & Priority Ribbon */}
          <div className="flex items-center gap-3 p-3 rounded-xl border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 12%, var(--color-primary))', background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))' }}>
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold opacity-70">Status:</span>
              {getStatusBadge(admission.status)}
            </div>
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold opacity-70">Priority:</span>
              {getPriorityBadge(admission.priority)}
            </div>
            <div className="ml-auto text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
              Admission ID: #{admission.id}
            </div>
          </div>

          {/* Grid: Patient & Doctor */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-4 rounded-xl border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
              <h3 className="text-xs font-bold uppercase tracking-wider mb-2" style={{ color: 'var(--color-accent)' }}>
                Patient Information
              </h3>
              <div className="flex flex-col gap-1.5 text-xs">
                <div>
                  <span className="font-semibold block opacity-70">Name:</span>
                  <span className="text-sm font-medium">{admission.patientName || `Patient #${admission.patientId}`}</span>
                </div>
                <div>
                  <span className="font-semibold block opacity-70">Patient User ID:</span>
                  <span>#{admission.patientId}</span>
                </div>
                {admission.patientEmail && (
                  <div>
                    <span className="font-semibold block opacity-70">Email:</span>
                    <span>{admission.patientEmail}</span>
                  </div>
                )}
                {admission.patientPhone && (
                  <div>
                    <span className="font-semibold block opacity-70">Phone:</span>
                    <span>{admission.patientPhone}</span>
                  </div>
                )}
              </div>
            </div>

            <div className="p-4 rounded-xl border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
              <h3 className="text-xs font-bold uppercase tracking-wider mb-2" style={{ color: 'var(--color-accent)' }}>
                Admitting Doctor & Stay Timeline
              </h3>
              <div className="flex flex-col gap-1.5 text-xs">
                <div>
                  <span className="font-semibold block opacity-70">Admitting Doctor:</span>
                  <span className="text-sm font-medium">
                    {admission.admittingDoctorName || (admission.admittingDoctorId ? `Doctor #${admission.admittingDoctorId}` : 'Not Assigned')}
                  </span>
                </div>
                <div>
                  <span className="font-semibold block opacity-70">Admission Date & Time:</span>
                  <span>{formatDate(admission.admissionDate)}</span>
                </div>
                <div>
                  <span className="font-semibold block opacity-70">Discharge Date & Time:</span>
                  <span>{formatDate(admission.dischargeDate)}</span>
                </div>
              </div>
            </div>
          </div>

          {/* Current Bed Allocation */}
          <div className="p-4 rounded-xl border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
            <h3 className="text-xs font-bold uppercase tracking-wider mb-2" style={{ color: 'var(--color-accent)' }}>
              Current Bed Location
            </h3>
            {admission.activeBedNumber ? (
              <div className="grid grid-cols-3 gap-2 text-xs">
                <div>
                  <span className="font-semibold block opacity-70">Ward:</span>
                  <span className="text-sm font-medium">{admission.activeWardName || '—'}</span>
                </div>
                <div>
                  <span className="font-semibold block opacity-70">Room:</span>
                  <span className="text-sm font-medium">{admission.activeRoomNumber || '—'}</span>
                </div>
                <div>
                  <span className="font-semibold block opacity-70">Bed Number:</span>
                  <span className="text-sm font-semibold text-emerald-700">Bed {admission.activeBedNumber}</span>
                </div>
              </div>
            ) : (
              <div className="text-xs opacity-70 italic">
                No active bed allocation currently assigned to this patient.
              </div>
            )}
          </div>

          {/* Clinical Details */}
          <div className="p-4 rounded-xl border flex flex-col gap-3" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
            <div>
              <span className="text-xs font-bold uppercase tracking-wider block opacity-70 mb-1">
                Reason for Admission
              </span>
              <p className="text-xs m-0 whitespace-pre-wrap">{admission.reasonForAdmission || '—'}</p>
            </div>

            {admission.diagnosis && (
              <div>
                <span className="text-xs font-bold uppercase tracking-wider block opacity-70 mb-1">
                  Diagnosis
                </span>
                <p className="text-xs m-0 whitespace-pre-wrap">{admission.diagnosis}</p>
              </div>
            )}

            {admission.dischargeSummary && (
              <div className="pt-2 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))' }}>
                <span className="text-xs font-bold uppercase tracking-wider block text-slate-700 mb-1">
                  Discharge Summary
                </span>
                <p className="text-xs m-0 whitespace-pre-wrap font-medium">{admission.dischargeSummary}</p>
              </div>
            )}
          </div>
        </div>

        <div className="flex justify-end mt-5 pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <button
            type="button"
            onClick={onClose}
            className="secondary-button text-sm px-5 py-2"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  )
}
