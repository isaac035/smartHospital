export default function PrescriptionList({ prescriptions = [] }) {
  if (prescriptions.length === 0) {
    return <p className="placeholder-text">No prescriptions found.</p>
  }

  return (
    <div className="prescription-stack">
      {prescriptions.map((rx) => (
        <div key={rx.id} className="card mb-3">
          <div className="card-header">
            <div>
              <strong>{rx.prescriptionNumber}</strong>
              <span className="text-muted ml-2">Issued: {new Date(rx.issueDate).toLocaleDateString()}</span>
            </div>
            <span className={`badge ${rx.status === 'Active' ? 'badge-success' : 'badge-secondary'}`}>
              {rx.status}
            </span>
          </div>
          {rx.generalInstructions && (
            <p className="instruction-text mt-2"><em>Instructions: {rx.generalInstructions}</em></p>
          )}
          <table className="data-table mt-2">
            <thead>
              <tr>
                <th>Medicine</th>
                <th>Dosage</th>
                <th>Route</th>
                <th>Frequency</th>
                <th>Duration</th>
                <th>Instructions</th>
              </tr>
            </thead>
            <tbody>
              {rx.items?.map((item, index) => (
                <tr key={item.id || index}>
                  <td><strong>{item.medicineName}</strong></td>
                  <td>{item.dosage}</td>
                  <td>{item.route}</td>
                  <td>{item.frequency}</td>
                  <td>{item.durationDays} days</td>
                  <td>{item.specialInstructions || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
    </div>
  )
}
