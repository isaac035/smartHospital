import { useState } from 'react'
import DashboardLayout from '../../../layouts/DashboardLayout'

const navigation = ['Dashboard', 'My Appointments', 'My Patients', 'Medical Records', 'Prescriptions', 'Lab Reports']

export default function DoctorMedicalRecordsPage() {
  const [patientId, setPatientId] = useState('')
  const [records] = useState([])

  return (
    <DashboardLayout
      role="Doctor"
      navigation={navigation}
      title="Medical Records"
      subtitle="Search and review clinical consultation history and patient records."
    >
      <div className="card mb-4">
        <h3>Patient Search</h3>
        <div className="form-group flex gap-2 mt-2">
          <input
            type="number"
            className="form-control"
            placeholder="Enter Patient ID..."
            value={patientId}
            onChange={(e) => setPatientId(e.target.value)}
          />
          <button className="btn btn-primary" onClick={() => {}}>Search Records</button>
        </div>
      </div>

      <div className="card">
        <h3>Consultation History</h3>
        {records.length === 0 ? (
          <p className="placeholder-text">Enter a patient ID to view consultation encounters.</p>
        ) : (
          <p>Displaying consultation records.</p>
        )}
      </div>
    </DashboardLayout>
  )
}
