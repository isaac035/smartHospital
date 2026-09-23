export default function PatientMedicalProfileCard({ profile }) {
  if (!profile) {
    return (
      <div className="card">
        <h3>Patient Medical Profile</h3>
        <p className="placeholder-text">No medical profile recorded for this patient.</p>
      </div>
    )
  }

  return (
    <div className="card">
      <div className="card-header">
        <h3>Patient Medical Profile</h3>
        <span className="badge badge-primary">{profile.bloodGroup || 'Blood: Unknown'}</span>
      </div>
      <div className="grid grid-2 gap-4 mt-3">
        <div>
          <span className="label">Date of Birth:</span>
          <span>{profile.dateOfBirth ? new Date(profile.dateOfBirth).toLocaleDateString() : 'N/A'}</span>
        </div>
        <div>
          <span className="label">Gender:</span>
          <span>{profile.gender || 'N/A'}</span>
        </div>
        <div>
          <span className="label">Allergies:</span>
          <span className="text-danger">{profile.allergies || 'None reported'}</span>
        </div>
        <div>
          <span className="label">Chronic Illnesses:</span>
          <span>{profile.chronicDiseases || 'None reported'}</span>
        </div>
        <div>
          <span className="label">Emergency Contact:</span>
          <span>{profile.emergencyContactName ? `${profile.emergencyContactName} (${profile.emergencyContactPhone})` : 'N/A'}</span>
        </div>
      </div>
    </div>
  )
}
