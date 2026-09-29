export default function VitalsSummaryTable({ vitals = [], onRecordVitals }) {
  if (vitals.length === 0) {
    return (
      <div className="panel" style={{ padding: 24 }}>
        <div className="flex justify-between items-center mb-3">
          <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>Vital Signs</h3>
          {onRecordVitals && (
            <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onRecordVitals}>
              + Record Vital Signs
            </button>
          )}
        </div>
        <p className="placeholder-text text-sm">No vital signs recorded yet for this patient.</p>
      </div>
    )
  }

  const latest = vitals[0] // vitals usually ordered descending by recorded date

  const isTempAbnormal = latest?.temperatureCelsius && (latest.temperatureCelsius >= 38.0 || latest.temperatureCelsius <= 35.5)
  const isBPAbnormal = latest?.systolicBloodPressure && latest?.diastolicBloodPressure && (latest.systolicBloodPressure >= 140 || latest.diastolicBloodPressure >= 90)
  const isPulseAbnormal = latest?.heartRateBpm && (latest.heartRateBpm > 100 || latest.heartRateBpm < 60)
  const isSpO2Abnormal = latest?.oxygenSaturationSpO2 && latest.oxygenSaturationSpO2 < 95

  return (
    <div className="panel" style={{ padding: 24 }}>
      <div className="flex justify-between items-center mb-4 pb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
        <div>
          <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>Vital Signs History</h3>
          <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
            Latest recorded: {latest ? new Date(latest.recordedAt).toLocaleString() : 'N/A'}
          </span>
        </div>
        {onRecordVitals && (
          <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onRecordVitals}>
            + Record New Vitals
          </button>
        )}
      </div>

      {latest && (
        <div className="vitals-grid mb-6">
          <div className={`vitals-card ${isTempAbnormal ? 'abnormal' : ''}`}>
            <div className="vitals-label">Temperature</div>
            <div className="vitals-value">{latest.temperatureCelsius ? `${latest.temperatureCelsius}°C` : '--'}</div>
            <div className="text-xs">{isTempAbnormal ? 'Fever / Alert' : 'Normal: 36.5–37.5'}</div>
          </div>
          <div className={`vitals-card ${isBPAbnormal ? 'abnormal' : ''}`}>
            <div className="vitals-label">Blood Pressure</div>
            <div className="vitals-value">
              {latest.systolicBloodPressure && latest.diastolicBloodPressure
                ? `${latest.systolicBloodPressure}/${latest.diastolicBloodPressure}`
                : '--'}
            </div>
            <div className="text-xs">{isBPAbnormal ? 'High BP' : 'mmHg'}</div>
          </div>
          <div className={`vitals-card ${isPulseAbnormal ? 'abnormal' : ''}`}>
            <div className="vitals-label">Heart Rate</div>
            <div className="vitals-value">{latest.heartRateBpm ? `${latest.heartRateBpm} bpm` : '--'}</div>
            <div className="text-xs">{isPulseAbnormal ? 'Elevated / Low' : 'Normal: 60–100'}</div>
          </div>
          <div className={`vitals-card ${isSpO2Abnormal ? 'abnormal' : ''}`}>
            <div className="vitals-label">SpO2</div>
            <div className="vitals-value">{latest.oxygenSaturationSpO2 ? `${latest.oxygenSaturationSpO2}%` : '--'}</div>
            <div className="text-xs">{isSpO2Abnormal ? 'Low Oxygen' : 'Normal: ≥95%'}</div>
          </div>
          <div className="vitals-card">
            <div className="vitals-label">Weight & BMI</div>
            <div className="vitals-value">{latest.weightKg ? `${latest.weightKg} kg` : '--'}</div>
            <div className="text-xs">{latest.bmi ? `BMI: ${latest.bmi}` : '--'}</div>
          </div>
        </div>
      )}

      <div className="table-responsive">
        <table className="data-table">
          <thead>
            <tr>
              <th>Date & Time</th>
              <th>Temp</th>
              <th>BP (mmHg)</th>
              <th>Pulse</th>
              <th>Resp Rate</th>
              <th>SpO2</th>
              <th>Weight</th>
              <th>BMI</th>
              <th>Recorded By</th>
              <th>Notes</th>
            </tr>
          </thead>
          <tbody>
            {vitals.map((v) => (
              <tr key={v.id}>
                <td>{new Date(v.recordedAt).toLocaleString()}</td>
                <td>{v.temperatureCelsius ? `${v.temperatureCelsius}°C` : '-'}</td>
                <td>{v.systolicBloodPressure && v.diastolicBloodPressure ? `${v.systolicBloodPressure}/${v.diastolicBloodPressure}` : '-'}</td>
                <td>{v.heartRateBpm ? `${v.heartRateBpm} bpm` : '-'}</td>
                <td>{v.respiratoryRateBpm ? `${v.respiratoryRateBpm}/min` : '-'}</td>
                <td>{v.oxygenSaturationSpO2 ? `${v.oxygenSaturationSpO2}%` : '-'}</td>
                <td>{v.weightKg ? `${v.weightKg} kg` : '-'}</td>
                <td>{v.bmi ?? '-'}</td>
                <td>{v.recordedByUserName || 'Staff'}</td>
                <td className="text-xs max-w-xs truncate" title={v.notes}>{v.notes || '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
