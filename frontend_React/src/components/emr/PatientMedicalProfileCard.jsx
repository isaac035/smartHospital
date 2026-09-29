export default function PatientMedicalProfileCard({ profile, onEdit }) {
  if (!profile) {
    return (
      <div className="panel" style={{ padding: 24 }}>
        <div className="flex justify-between items-center mb-3">
          <h3 className="font-bold text-lg" style={{ color: 'var(--color-accent)' }}>Patient Medical Profile</h3>
          {onEdit && (
            <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onEdit}>
              + Create Medical Profile
            </button>
          )}
        </div>
        <p className="placeholder-text text-sm">No medical profile recorded for this patient yet.</p>
      </div>
    )
  }

  const hasAllergies = profile.allergies && profile.allergies.toLowerCase() !== 'none' && profile.allergies.toLowerCase() !== 'none reported'

  return (
    <div className="panel" style={{ padding: 24 }}>
      <div className="flex justify-between items-center mb-4 pb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
        <div className="flex items-center gap-3">
          <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>Medical Profile</h3>
          <span className="badge badge-primary">{profile.bloodGroup || 'Blood: Unknown'}</span>
        </div>
        {onEdit && (
          <button className="secondary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onEdit}>
            Edit Profile
          </button>
        )}
      </div>

      {hasAllergies && (
        <div className="allergy-alert mb-4">
          <span className="text-lg font-bold">⚠️ Allergy Alert:</span>
          <span>{profile.allergies}</span>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
        <div>
          <span className="block font-semibold" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>Date of Birth</span>
          <span className="font-medium">{profile.dateOfBirth ? new Date(profile.dateOfBirth).toLocaleDateString() : 'N/A'}</span>
        </div>
        <div>
          <span className="block font-semibold" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>Gender</span>
          <span className="font-medium">{profile.gender || 'N/A'}</span>
        </div>
        <div>
          <span className="block font-semibold" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>Blood Group</span>
          <span className="font-medium">{profile.bloodGroup || 'Unknown'}</span>
        </div>
        <div>
          <span className="block font-semibold" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>Documented Allergies</span>
          <span className={hasAllergies ? 'text-red-600 font-bold' : 'font-medium'}>
            {profile.allergies || 'None reported'}
          </span>
        </div>
        <div>
          <span className="block font-semibold" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>Chronic Conditions</span>
          <span className="font-medium">{profile.chronicDiseases || 'None reported'}</span>
        </div>
        <div>
          <span className="block font-semibold" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>Emergency Contact</span>
          <span className="font-medium">
            {profile.emergencyContactName ? `${profile.emergencyContactName} (${profile.emergencyContactPhone || 'No phone'})` : 'N/A'}
          </span>
        </div>
      </div>
    </div>
  )
}
