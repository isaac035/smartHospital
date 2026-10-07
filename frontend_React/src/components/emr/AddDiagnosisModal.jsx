import { useForm } from 'react-hook-form'
import { addMedicalRecordDiagnosis } from '../../services/emrService'
import { applyServerErrors, rules } from '../../utils/validators'

export default function AddDiagnosisModal({ recordId, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    mode: 'onTouched',
    defaultValues: {
      code: '',
      description: '',
      type: 1, // Primary
      status: 1, // Active
      severity: 2, // Moderate
      notes: '',
    },
  })

  const onSubmit = async (values) => {
    try {
      const payload = {
        code: values.code ? values.code.trim() : null,
        description: values.description.trim(),
        type: Number(values.type),
        status: Number(values.status),
        severity: Number(values.severity),
        notes: values.notes ? values.notes.trim() : '',
        diagnosedAt: new Date().toISOString(),
      }

      await addMedicalRecordDiagnosis(recordId, payload)
      onSaved()
    } catch (err) {
      if (err.response?.status === 403) {
        setError('root', { message: err.response?.data?.message || 'Unauthorized: You do not have permission to add diagnoses to this record.' })
      } else {
        applyServerErrors(err, setError, { fields: ['code', 'description', 'type', 'status', 'severity', 'notes'], fallback: 'Failed to add diagnosis. Please check fields.' })
      }
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 580px)' }}>
        <h2>Add Clinical Diagnosis</h2>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}

          <div className="field-row">
            <div>
              <label htmlFor="code">ICD / Clinical Code (Optional)</label>
              <input
                id="code"
                placeholder="e.g. I10, E11.9, J06.9"
                maxLength={50}
                aria-invalid={errors.code ? 'true' : undefined}
                {...register('code', rules.text('Clinical code', { max: 50 }))}
              />
              {errors.code && <p className="field-error">{errors.code.message}</p>}
            </div>
            <div>
              <label htmlFor="type">Diagnosis Type *</label>
              <select
                id="type"
                className="w-full p-2.5 border rounded-lg bg-white"
                {...register('type', { required: true })}
              >
                <option value={1}>Primary</option>
                <option value={2}>Secondary</option>
                <option value={3}>Differential</option>
                <option value={4}>Provisional</option>
                <option value={5}>Confirmed</option>
              </select>
            </div>
          </div>

          <div>
            <label htmlFor="desc">Diagnosis Description *</label>
            <input
              id="desc"
              placeholder="e.g. Essential (primary) hypertension, Type 2 diabetes mellitus"
              maxLength={500}
              aria-invalid={errors.description ? 'true' : undefined}
              {...register('description', rules.text('Diagnosis description', { isRequired: true, max: 500 }))}
            />
            {errors.description && <p className="field-error">{errors.description.message}</p>}
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="status">Status *</label>
              <select
                id="status"
                className="w-full p-2.5 border rounded-lg bg-white"
                {...register('status', { required: true })}
              >
                <option value={1}>Active</option>
                <option value={2}>Resolved</option>
                <option value={3}>Chronic</option>
                <option value={4}>Inactive</option>
                <option value={5}>Relapsed</option>
              </select>
            </div>
            <div>
              <label htmlFor="severity">Severity *</label>
              <select
                id="severity"
                className="w-full p-2.5 border rounded-lg bg-white"
                {...register('severity', { required: true })}
              >
                <option value={1}>Mild</option>
                <option value={2}>Moderate</option>
                <option value={3}>Severe</option>
                <option value={4}>Critical</option>
              </select>
            </div>
          </div>

          <div>
            <label htmlFor="notes">Clinical Notes</label>
            <input
              id="notes"
              placeholder="e.g. Onset 2 weeks ago, patient reports intermittent spikes"
              maxLength={2000}
              aria-invalid={errors.notes ? 'true' : undefined}
              {...register('notes', rules.text('Clinical notes', { max: 2000 }))}
            />
            {errors.notes && <p className="field-error">{errors.notes.message}</p>}
          </div>

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={isSubmitting}>
              {isSubmitting ? 'Adding...' : 'Add Diagnosis'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
