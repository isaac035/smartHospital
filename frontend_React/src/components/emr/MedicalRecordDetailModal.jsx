export default function MedicalRecordDetailModal({ record, onClose }) {
  if (!record) return null

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 750px)' }}>
        <div className="flex justify-between items-center mb-4 pb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <h2 className="m-0 text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Consultation Encounter: {record.recordNumber || `#${record.id}`}
            </h2>
            <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
              Encounter Date: {record.visitDate ? new Date(record.visitDate).toLocaleDateString() : 'N/A'} • Provider: Dr. {record.doctorName || 'Attending'}
            </span>
          </div>
          <button className="secondary-button text-xs px-2 py-1" onClick={onClose}>
            ✕
          </button>
        </div>

        <div className="flex flex-col gap-4 text-sm max-h-[70vh] overflow-y-auto pr-1">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-3 rounded-lg border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
              <span className="text-xs uppercase font-bold text-gray-500 block mb-1">Patient</span>
              <p className="font-bold text-base m-0">{record.patientName || `Patient #${record.patientId}`}</p>
            </div>
            <div className="p-3 rounded-lg border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
              <span className="text-xs uppercase font-bold text-gray-500 block mb-1">Appointment / Encounter</span>
              <p className="font-semibold text-base m-0">
                {record.appointmentId ? `Appointment #${record.appointmentId}` : 'Direct Consultation'}
              </p>
            </div>
          </div>

          <div>
            <label className="font-bold text-gray-700 block mb-1">Chief Complaint</label>
            <div className="p-3 rounded-lg bg-gray-50 font-medium">
              {record.chiefComplaint || 'None specified'}
            </div>
          </div>

          {record.symptoms && (
            <div>
              <label className="font-bold text-gray-700 block mb-1">Reported Symptoms</label>
              <div className="p-3 rounded-lg bg-gray-50">
                {record.symptoms}
              </div>
            </div>
          )}

          {record.examinationNotes && (
            <div>
              <label className="font-bold text-gray-700 block mb-1">Physical Examination Findings</label>
              <div className="p-3 rounded-lg bg-gray-50 whitespace-pre-wrap">
                {record.examinationNotes}
              </div>
            </div>
          )}

          <div className="p-3 rounded-lg border-l-4 border-blue-600 bg-blue-50 text-blue-950">
            <span className="text-xs uppercase font-bold text-blue-800 block mb-1">Clinical Diagnosis</span>
            <div className="text-base font-bold">{record.diagnosis || 'None recorded'}</div>
          </div>

          {record.treatmentPlan && (
            <div className="p-3 rounded-lg border-l-4 border-green-600 bg-green-50 text-green-950">
              <span className="text-xs uppercase font-bold text-green-800 block mb-1">Treatment Plan & Recommendations</span>
              <div className="text-sm font-medium whitespace-pre-wrap">{record.treatmentPlan}</div>
            </div>
          )}

          {record.followUpDate && (
            <div className="p-2 rounded bg-amber-50 text-amber-900 text-xs font-semibold">
              Scheduled Follow-up: {new Date(record.followUpDate).toLocaleDateString()}
            </div>
          )}

          {/* Linked Prescriptions */}
          {record.prescriptions && record.prescriptions.length > 0 && (
            <div>
              <h4 className="font-bold text-sm mb-2" style={{ color: 'var(--color-accent)' }}>
                Prescribed Medications ({record.prescriptions.length})
              </h4>
              <div className="table-responsive">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Medicine</th>
                      <th>Dosage</th>
                      <th>Frequency</th>
                      <th>Duration</th>
                    </tr>
                  </thead>
                  <tbody>
                    {record.prescriptions.flatMap((rx) => rx.items || []).map((item, idx) => (
                      <tr key={idx}>
                        <td><strong>{item.medicineName}</strong></td>
                        <td>{item.dosage}</td>
                        <td>{item.frequency}</td>
                        <td>{item.durationDays} days</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Linked Vitals */}
          {record.vitalSigns && record.vitalSigns.length > 0 && (
            <div>
              <h4 className="font-bold text-sm mb-2" style={{ color: 'var(--color-accent)' }}>
                Recorded Vitals ({record.vitalSigns.length})
              </h4>
              <div className="text-xs flex flex-wrap gap-2">
                {record.vitalSigns.map((v) => (
                  <div key={v.id} className="p-2 rounded border bg-gray-50">
                    <span>Temp: {v.temperatureCelsius ? `${v.temperatureCelsius}°C` : '-'} | </span>
                    <span>BP: {v.systolicBloodPressure && v.diastolicBloodPressure ? `${v.systolicBloodPressure}/${v.diastolicBloodPressure}` : '-'} | </span>
                    <span>Pulse: {v.heartRateBpm ?? '-'} bpm | </span>
                    <span>SpO2: {v.oxygenSaturationSpO2 ? `${v.oxygenSaturationSpO2}%` : '-'}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="modal-actions mt-4 pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <button type="button" className="secondary-button" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  )
}
