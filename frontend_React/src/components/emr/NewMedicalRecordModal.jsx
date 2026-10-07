import { useForm } from 'react-hook-form'
import { createMedicalRecord } from '../../services/emrService'
import { applyServerErrors, rules } from '../../utils/validators'

export default function NewMedicalRecordModal({ patientId, appointmentId = null, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    mode: 'onTouched',
    defaultValues: {
      patientId: patientId ? Number(patientId) : '',
      appointmentId: appointmentId ? Number(appointmentId) : '',
      chiefComplaint: '',
      symptoms: '',
      examinationNotes: '',
      diagnosis: '',
      treatmentPlan: '',
      followUpDate: '',
    },
  })

  const onSubmit = async (values) => {
    try {
      const payload = {
        patientId: Number(values.patientId || patientId),
        appointmentId: values.appointmentId ? Number(values.appointmentId) : null,
        chiefComplaint: values.chiefComplaint.trim(),
        symptoms: values.symptoms ? values.symptoms.trim() : '',
        examinationNotes: values.examinationNotes ? values.examinationNotes.trim() : '',
        diagnosis: values.diagnosis.trim(),
        treatmentPlan: values.treatmentPlan ? values.treatmentPlan.trim() : '',
        followUpDate: values.followUpDate ? new Date(values.followUpDate).toISOString() : null,
      }

      await createMedicalRecord(payload)
      onSaved()
    } catch (err) {
      if (err.response?.status === 403) {
        setError('root', { message: err.response?.data?.message || 'Unauthorized: You do not have permission to create this record.' })
      } else {
        applyServerErrors(err, setError, { fields: ['patientId', 'appointmentId', 'chiefComplaint', 'symptoms', 'examinationNotes', 'diagnosis', 'treatmentPlan', 'followUpDate'], conflicts: [{ match: /follow-up/i, field: 'followUpDate' }, { match: /appointment/i, field: 'appointmentId' }], fallback: 'Failed to create medical record. Please verify all fields.' })
      }
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 680px)' }}>
        <h2>New Clinical Consultation Encounter</h2>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}

          <div className="field-row">
            <div>
              <label htmlFor="patientId">Patient ID *</label>
              <input
                id="patientId"
                type="number"
                readOnly={!!patientId}
                {...register('patientId', rules.number('Patient ID', { isRequired: true, min: 1, max: 2147483647, integer: true }))}
              />
              {errors.patientId && <p className="field-error">{errors.patientId.message}</p>}
            </div>
            <div>
              <label htmlFor="appointmentId">Appointment ID (Optional)</label>
              <input
                id="appointmentId"
                type="number"
                aria-invalid={errors.appointmentId ? 'true' : undefined}
                {...register('appointmentId', rules.number('Appointment ID', { min: 1, max: 2147483647, integer: true }))}
              />
              {errors.appointmentId && <p className="field-error">{errors.appointmentId.message}</p>}
            </div>
          </div>

          <div>
            <label htmlFor="chiefComplaint">Chief Complaint *</label>
            <input
              id="chiefComplaint"
              placeholder="e.g. Severe headache, persistent fever for 3 days"
              maxLength={500}
              aria-invalid={errors.chiefComplaint ? 'true' : undefined}
              {...register('chiefComplaint', rules.text('Chief complaint', { isRequired: true, max: 500 }))}
            />
            {errors.chiefComplaint && <p className="field-error">{errors.chiefComplaint.message}</p>}
          </div>

          <div>
            <label htmlFor="symptoms">Symptoms</label>
            <input
              id="symptoms"
              placeholder="e.g. Nausea, photophobia, body aches"
              maxLength={1000}
              aria-invalid={errors.symptoms ? 'true' : undefined}
              {...register('symptoms', rules.text('Symptoms', { max: 1000 }))}
            />
            {errors.symptoms && <p className="field-error">{errors.symptoms.message}</p>}
          </div>

          <div>
            <label htmlFor="examinationNotes">Physical Examination Notes</label>
            <input
              id="examinationNotes"
              placeholder="e.g. BP 120/80, chest clear, no focal neurological deficits"
              maxLength={2000}
              aria-invalid={errors.examinationNotes ? 'true' : undefined}
              {...register('examinationNotes', rules.text('Examination notes', { max: 2000 }))}
            />
            {errors.examinationNotes && <p className="field-error">{errors.examinationNotes.message}</p>}
          </div>

          <div>
            <label htmlFor="diagnosis">Primary Diagnosis *</label>
            <input
              id="diagnosis"
              placeholder="e.g. Acute Migraine / Tension Headache"
              maxLength={500}
              aria-invalid={errors.diagnosis ? 'true' : undefined}
              {...register('diagnosis', rules.text('Diagnosis', { isRequired: true, max: 500 }))}
            />
            {errors.diagnosis && <p className="field-error">{errors.diagnosis.message}</p>}
          </div>

          <div>
            <label htmlFor="treatmentPlan">Treatment Plan & Recommendations</label>
            <input
              id="treatmentPlan"
              placeholder="e.g. Analgesics, hydration, rest, return if symptoms worsen"
              maxLength={2000}
              aria-invalid={errors.treatmentPlan ? 'true' : undefined}
              {...register('treatmentPlan', rules.text('Treatment plan', { max: 2000 }))}
            />
            {errors.treatmentPlan && <p className="field-error">{errors.treatmentPlan.message}</p>}
          </div>

          <div>
            <label htmlFor="followUpDate">Follow-up Date (Optional)</label>
            <input
              id="followUpDate"
              type="date"
              aria-invalid={errors.followUpDate ? 'true' : undefined}
              {...register('followUpDate', rules.futureDate('Follow-up date', 5))}
            />
            {errors.followUpDate && <p className="field-error">{errors.followUpDate.message}</p>}
          </div>

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={isSubmitting}>
              {isSubmitting ? 'Saving Encounter...' : 'Save Consultation Record'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
