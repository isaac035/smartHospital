export default function DiagnosisTreatmentCard({
  diagnoses = [],
  treatmentPlans = [],
  primaryDiagnosis = '',
  primaryTreatmentPlan = '',
  onAddDiagnosis,
  onAddTreatmentPlan,
}) {
  const hasDiagnoses = diagnoses.length > 0 || !!primaryDiagnosis
  const hasPlans = treatmentPlans.length > 0 || !!primaryTreatmentPlan

  return (
    <div className="flex flex-col gap-6">
      {/* Diagnoses Section */}
      <div className="panel" style={{ padding: 24 }}>
        <div className="flex justify-between items-center mb-4 pb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>
              Clinical Diagnoses
            </h3>
            <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
              Documented clinical impressions, ICD codes, and diagnostic assessments
            </span>
          </div>
          {onAddDiagnosis && (
            <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onAddDiagnosis}>
              + Add Diagnosis
            </button>
          )}
        </div>

        {primaryDiagnosis && (
          <div className="p-3 mb-4 rounded-lg border-l-4 border-blue-600" style={{ background: 'color-mix(in srgb, var(--color-accent) 5%, var(--color-primary))' }}>
            <span className="text-xs uppercase font-bold text-blue-900 block mb-1">Primary Diagnosis</span>
            <div className="text-base font-bold" style={{ color: 'var(--color-accent)' }}>{primaryDiagnosis}</div>
          </div>
        )}

        {!hasDiagnoses ? (
          <p className="placeholder-text text-sm">No specific clinical diagnoses documented yet.</p>
        ) : diagnoses.length > 0 ? (
          <div className="table-responsive">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Description</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>Severity</th>
                  <th>Diagnosed At</th>
                  <th>Doctor</th>
                </tr>
              </thead>
              <tbody>
                {diagnoses.map((d, index) => (
                  <tr key={d.id || index}>
                    <td><strong>{d.code || 'Clinical'}</strong></td>
                    <td>
                      <div>
                        <strong>{d.description}</strong>
                        {d.notes && <div className="text-xs text-gray-500">{d.notes}</div>}
                      </div>
                    </td>
                    <td><span className="badge badge-info">{d.type || 'Primary'}</span></td>
                    <td>
                      <span className={`badge ${d.status === 'Active' ? 'badge-success' : 'badge-secondary'}`}>
                        {d.status || 'Active'}
                      </span>
                    </td>
                    <td>{d.severity || 'Moderate'}</td>
                    <td>{d.diagnosedAt ? new Date(d.diagnosedAt).toLocaleDateString() : 'N/A'}</td>
                    <td>{d.doctorName ? `Dr. ${d.doctorName}` : 'Attending'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </div>

      {/* Treatment Plans Section */}
      <div className="panel" style={{ padding: 24 }}>
        <div className="flex justify-between items-center mb-4 pb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>
              Treatment Plans & Goals
            </h3>
            <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
              Therapeutic interventions, medical goals, and management strategies
            </span>
          </div>
          {onAddTreatmentPlan && (
            <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onAddTreatmentPlan}>
              + Add Treatment Plan
            </button>
          )}
        </div>

        {primaryTreatmentPlan && (
          <div className="p-3 mb-4 rounded-lg border-l-4 border-green-600" style={{ background: 'color-mix(in srgb, #0f7a3d 6%, var(--color-primary))' }}>
            <span className="text-xs uppercase font-bold text-green-900 block mb-1">Primary Consultation Plan</span>
            <div className="text-sm font-semibold">{primaryTreatmentPlan}</div>
          </div>
        )}

        {!hasPlans ? (
          <p className="placeholder-text text-sm">No structured treatment plans recorded yet.</p>
        ) : treatmentPlans.length > 0 ? (
          <div className="flex flex-col gap-3">
            {treatmentPlans.map((tp, idx) => (
              <div key={tp.id || idx} className="p-4 rounded-lg border" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <div className="flex justify-between items-center mb-2">
                  <div className="flex items-center gap-2">
                    <strong className="text-base" style={{ color: 'var(--color-accent)' }}>{tp.title || 'Therapeutic Plan'}</strong>
                    <span className="badge badge-info">{tp.category || 'General'}</span>
                  </div>
                  <span className={`badge ${tp.status === 'Active' ? 'badge-success' : 'badge-secondary'}`}>
                    {tp.status || 'Active'}
                  </span>
                </div>
                <p className="text-sm my-2">{tp.description}</p>
                {tp.goals && (
                  <div className="text-xs text-gray-700 bg-gray-50 p-2 rounded mb-2">
                    <strong>Goals:</strong> {tp.goals}
                  </div>
                )}
                <div className="flex flex-wrap gap-4 text-xs text-gray-500 pt-2 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))' }}>
                  {tp.startDate && <span>Started: {new Date(tp.startDate).toLocaleDateString()}</span>}
                  {tp.targetDate && <span>Target Review: {new Date(tp.targetDate).toLocaleDateString()}</span>}
                  {tp.doctorName && <span>Doctor: Dr. {tp.doctorName}</span>}
                </div>
              </div>
            ))}
          </div>
        ) : null}
      </div>
    </div>
  )
}
