export default function PrescriptionList({ prescriptions = [], onNewPrescription }) {
  if (prescriptions.length === 0) {
    return (
      <div className="panel" style={{ padding: 24 }}>
        <div className="flex justify-between items-center mb-3">
          <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>Prescriptions</h3>
          {onNewPrescription && (
            <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onNewPrescription}>
              + New Prescription
            </button>
          )}
        </div>
        <p className="placeholder-text text-sm">No prescriptions found for this patient.</p>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex justify-between items-center">
        <div>
          <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>
            Patient Prescriptions ({prescriptions.length})
          </h3>
          <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
            Current medications and clinical prescription orders
          </span>
        </div>
        {onNewPrescription && (
          <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onNewPrescription}>
            + New Prescription
          </button>
        )}
      </div>

      <div className="prescription-stack flex flex-col gap-4">
        {prescriptions.map((rx) => {
          const isActive = rx.status === 'Active'
          return (
            <div key={rx.id} className="panel" style={{ padding: 20 }}>
              <div className="flex flex-wrap justify-between items-center gap-2 pb-3 mb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <div className="flex items-center gap-3">
                  <strong className="text-base font-bold" style={{ color: 'var(--color-accent)' }}>
                    {rx.prescriptionNumber || `Rx #${rx.id}`}
                  </strong>
                  <span className={`badge ${isActive ? 'badge-success' : 'badge-secondary'}`}>
                    {rx.status || 'Active'}
                  </span>
                </div>
                <div className="text-xs flex gap-4" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
                  <span>Issued: {new Date(rx.issueDate).toLocaleDateString()}</span>
                  {rx.expiryDate && <span>Expires: {new Date(rx.expiryDate).toLocaleDateString()}</span>}
                  {rx.doctorName && <span>Doctor: Dr. {rx.doctorName}</span>}
                </div>
              </div>

              {rx.generalInstructions && (
                <div className="p-3 mb-3 rounded-lg text-sm" style={{ background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))' }}>
                  <strong>General Instructions:</strong> <em>{rx.generalInstructions}</em>
                </div>
              )}

              <div className="table-responsive">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Medicine Name</th>
                      <th>Dosage</th>
                      <th>Route</th>
                      <th>Frequency</th>
                      <th>Duration</th>
                      <th>Special Instructions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rx.items?.map((item, index) => (
                      <tr key={item.id || index}>
                        <td><strong>{item.medicineName}</strong></td>
                        <td>{item.dosage}</td>
                        <td>{item.route || 'Oral'}</td>
                        <td>{item.frequency}</td>
                        <td>{item.durationDays} days</td>
                        <td className="text-xs">{item.specialInstructions || '-'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
