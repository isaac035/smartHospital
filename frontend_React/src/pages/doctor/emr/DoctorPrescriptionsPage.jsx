import { useState } from 'react'
import DashboardLayout from '../../../layouts/DashboardLayout'

const navigation = ['Dashboard', 'My Appointments', 'My Patients', 'Medical Records', 'Prescriptions', 'Lab Reports']

export default function DoctorPrescriptionsPage() {
  const [patientId, setPatientId] = useState('')

  return (
    <DashboardLayout
      role="Doctor"
      navigation={navigation}
      title="Prescriptions"
      subtitle="Issue and manage patient medications and prescriptions."
    >
      <div className="card mb-4">
        <h3>Patient Prescriptions Search</h3>
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
        <h3>Active Prescriptions</h3>
        <p className="placeholder-text">Select a patient to review or author prescriptions.</p>
      </div>
    </DashboardLayout>
  )
}
