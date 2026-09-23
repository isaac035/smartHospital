export default function VitalsSummaryTable({ vitals = [] }) {
  if (vitals.length === 0) {
    return <p className="placeholder-text">No vital signs recorded yet.</p>
  }

  return (
    <div className="table-responsive">
      <table className="data-table">
        <thead>
          <tr>
            <th>Date & Time</th>
            <th>Temp (°C)</th>
            <th>BP (mmHg)</th>
            <th>Pulse (bpm)</th>
            <th>SpO2 (%)</th>
            <th>Weight (kg)</th>
            <th>BMI</th>
            <th>Recorded By</th>
          </tr>
        </thead>
        <tbody>
          {vitals.map((v) => (
            <tr key={v.id}>
              <td>{new Date(v.recordedAt).toLocaleString()}</td>
              <td>{v.temperatureCelsius ? `${v.temperatureCelsius}°C` : '-'}</td>
              <td>{v.systolicBloodPressure && v.diastolicBloodPressure ? `${v.systolicBloodPressure}/${v.diastolicBloodPressure}` : '-'}</td>
              <td>{v.heartRateBpm ?? '-'}</td>
              <td>{v.oxygenSaturationSpO2 ? `${v.oxygenSaturationSpO2}%` : '-'}</td>
              <td>{v.weightKg ?? '-'}</td>
              <td>{v.bmi ?? '-'}</td>
              <td>{v.recordedByUserName || 'Staff'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
