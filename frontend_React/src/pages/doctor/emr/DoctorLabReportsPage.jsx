import { useState } from 'react'
import DashboardLayout from '../../../layouts/DashboardLayout'

const navigation = ['Dashboard', 'My Appointments', 'My Patients', 'Medical Records', 'Prescriptions', 'Lab Reports']

export default function DoctorLabReportsPage() {
  const [patientId, setPatientId] = useState('')

  return (
    <DashboardLayout
      role="Doctor"
      navigation={navigation}
      title="Lab Reports"
      subtitle="Order laboratory tests and inspect diagnostic findings."
    >
      <div className="card mb-4">
        <h3>Patient Diagnostics Search</h3>
        <div className="form-group flex gap-2 mt-2">
          <input
            type="number"
            className="form-control"
            placeholder="Enter Patient ID..."
            value={patientId}
            onChange={(e) => setPatientId(e.target.value)}
          />
          <button className="btn btn-primary" onClick={() => {}}>Lookup</button>
        </div>
      </div>

      <div className="card">
        <h3>Diagnostic Orders & Reports</h3>
        <p className="placeholder-text">Select a patient to track diagnostic test orders and results.</p>
      </div>
    </DashboardLayout>
  )
}
