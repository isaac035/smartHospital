import { useForm } from 'react-hook-form'
import { createMedicalRecord } from '../../services/emrService'

export default function NewMedicalRecordModal({ patientId, appointmentId = null, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
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
      setError('root', {
        message: err.response?.data?.message || (err.response?.status === 403 ? 'Unauthorized: You do not have permission to create this record.' : 'Failed to create medical record. Please verify all fields.'),
      })
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
                {...register('patientId', { required: 'Patient ID is required.' })}
              />
              {errors.patientId && <p className="field-error">{errors.patientId.message}</p>}
            </div>
            <div>
              <label htmlFor="appointmentId">Appointment ID (Optional)</label>
              <input
                id="appointmentId"
                type="number"
                {...register('appointmentId')}
              />
            </div>
          </div>

          <div>
            <label htmlFor="chiefComplaint">Chief Complaint *</label>
            <input
              id="chiefComplaint"
              placeholder="e.g. Severe headache, persistent fever for 3 days"
              {...register('chiefComplaint', { required: 'Chief complaint is required.' })}
            />
            {errors.chiefComplaint && <p className="field-error">{errors.chiefComplaint.message}</p>}
          </div>

          <div>
            <label htmlFor="symptoms">Symptoms</label>
            <input
              id="symptoms"
              placeholder="e.g. Nausea, photophobia, body aches"
              {...register('symptoms')}
            />
          </div>

          <div>
            <label htmlFor="examinationNotes">Physical Examination Notes</label>
            <input
              id="examinationNotes"
              placeholder="e.g. BP 120/80, chest clear, no focal neurological deficits"
              {...register('examinationNotes')}
            />
          </div>

          <div>
            <label htmlFor="diagnosis">Primary Diagnosis *</label>
            <input
              id="diagnosis"
              placeholder="e.g. Acute Migraine / Tension Headache"
              {...register('diagnosis', { required: 'Primary diagnosis is required.' })}
            />
            {errors.diagnosis && <p className="field-error">{errors.diagnosis.message}</p>}
          </div>

          <div>
            <label htmlFor="treatmentPlan">Treatment Plan & Recommendations</label>
            <input
              id="treatmentPlan"
              placeholder="e.g. Analgesics, hydration, rest, return if symptoms worsen"
              {...register('treatmentPlan')}
            />
          </div>

          <div>
            <label htmlFor="followUpDate">Follow-up Date (Optional)</label>
            <input
              id="followUpDate"
              type="date"
              {...register('followUpDate')}
            />
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
