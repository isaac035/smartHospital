export default function PatientHeaderBanner({
  patientId,
  profile,
  patientName = '',
  onStartWorkflow,
  onNewRecord,
  onRecordVitals,
  onPrescribe,
  onOrderLab,
  onEditProfile,
  onSwitchPatient,
}) {
  const displayName = patientName || profile?.patientName || `Patient #${patientId}`
  const hasAllergies = profile?.allergies && profile.allergies.toLowerCase() !== 'none' && profile.allergies.toLowerCase() !== 'none reported'

  // Calculate age if dateOfBirth exists
  let age = null
  if (profile?.dateOfBirth) {
    const dob = new Date(profile.dateOfBirth)
    const diffMs = Date.now() - dob.getTime()
    const ageDate = new Date(diffMs)
    age = Math.abs(ageDate.getUTCFullYear() - 1970)
  }

  return (
    <div className="patient-banner mb-6">
      <div className="flex flex-wrap justify-between items-start gap-4">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <span className="user-avatar" style={{ width: 44, height: 44, fontSize: '1.1rem' }}>
              {displayName?.[0] || 'P'}
            </span>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="patient-banner-title">{displayName}</h2>
                <span className="badge badge-primary">{profile?.bloodGroup || 'Blood: Unknown'}</span>
                <span className="text-xs px-2 py-0.5 rounded bg-gray-100 text-gray-700 font-semibold">
                  ID: #{patientId}
                </span>
              </div>
              <div className="flex flex-wrap gap-4 text-xs mt-1" style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}>
                {profile?.gender && <span>Gender: <strong>{profile.gender}</strong></span>}
                {age !== null && <span>Age: <strong>{age} yrs</strong> ({new Date(profile.dateOfBirth).toLocaleDateString()})</span>}
                {profile?.emergencyContactName && (
                  <span>Emergency: <strong>{profile.emergencyContactName}</strong> ({profile.emergencyContactPhone || 'No tel'})</span>
                )}
              </div>
            </div>
          </div>
        </div>

        <div className="flex flex-wrap gap-2 items-center">
          {onStartWorkflow && (
            <button
              className="primary-button text-xs px-3.5 py-1.5"
              style={{ marginTop: 0, backgroundColor: '#0f7a3d', borderColor: '#0f7a3d' }}
              onClick={onStartWorkflow}
              title="Step-by-step guided clinical visit from medical record to follow-up"
            >
              ▶ Start Guided Clinical Visit
            </button>
          )}
          {onNewRecord && (
            <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onNewRecord}>
              + New Consultation
            </button>
          )}
          {onRecordVitals && (
            <button className="secondary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onRecordVitals}>
              + Vitals
            </button>
          )}
          {onPrescribe && (
            <button className="secondary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onPrescribe}>
              + Prescribe
            </button>
          )}
          {onOrderLab && (
            <button className="secondary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onOrderLab}>
              + Order Lab
            </button>
          )}
          {onEditProfile && (
            <button className="link-button text-xs" onClick={onEditProfile}>
              Edit Profile
            </button>
          )}
          {onSwitchPatient && (
            <button className="link-button text-xs" onClick={onSwitchPatient}>
              Change Patient
            </button>
          )}
        </div>
      </div>

      {hasAllergies && (
        <div className="allergy-alert mt-4">
          <span className="text-sm">⚠️ <strong>Critical Clinical Alert:</strong> Patient has documented allergies: <u>{profile.allergies}</u></span>
        </div>
      )}
    </div>
  )
}
