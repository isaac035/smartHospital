import { useForm } from 'react-hook-form'
import { addMedicalRecordTreatmentPlan } from '../../services/emrService'

export default function AddTreatmentPlanModal({ recordId, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: {
      title: '',
      category: 1, // General
      description: '',
      goals: '',
      interventions: '',
      status: 1, // Active
      targetDate: '',
    },
  })

  const onSubmit = async (values) => {
    try {
      const payload = {
        title: values.title.trim(),
        category: Number(values.category),
        description: values.description.trim(),
        goals: values.goals ? values.goals.trim() : '',
        interventions: values.interventions ? values.interventions.trim() : '',
        status: Number(values.status),
        startDate: new Date().toISOString(),
        targetDate: values.targetDate ? new Date(values.targetDate).toISOString() : null,
      }

      await addMedicalRecordTreatmentPlan(recordId, payload)
      onSaved()
    } catch (err) {
      setError('root', {
        message: err.response?.data?.message ||
          (err.response?.status === 403
            ? 'Unauthorized: You do not have permission to add treatment plans to this record.'
            : 'Failed to add treatment plan. Please check all fields.'),
      })
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 620px)' }}>
        <h2>Add Treatment Plan</h2>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}

          <div>
            <label htmlFor="title">Plan Title *</label>
            <input
              id="title"
              placeholder="e.g. Hypertension Management Protocol, Post-Operative Rehab"
              {...register('title', { required: 'Plan title is required.' })}
            />
            {errors.title && <p className="field-error">{errors.title.message}</p>}
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="category">Category *</label>
              <select
                id="category"
                className="w-full p-2.5 border rounded-lg bg-white"
                {...register('category', { required: true })}
              >
                <option value={1}>General</option>
                <option value={2}>Pharmacological</option>
                <option value={3}>Lifestyle Modification</option>
                <option value={4}>Surgical / Procedural</option>
                <option value={5}>Rehabilitative</option>
                <option value={6}>Dietary</option>
                <option value={7}>Monitoring</option>
              </select>
            </div>
            <div>
              <label htmlFor="status">Status *</label>
              <select
                id="status"
                className="w-full p-2.5 border rounded-lg bg-white"
                {...register('status', { required: true })}
              >
                <option value={1}>Active</option>
                <option value={2}>In Progress</option>
                <option value={3}>Completed</option>
                <option value={4}>Revised</option>
                <option value={5}>Discontinued</option>
                <option value={6}>On Hold</option>
              </select>
            </div>
          </div>

          <div>
            <label htmlFor="desc">Plan Description *</label>
            <textarea
              id="desc"
              rows={3}
              className="w-full p-2.5 border rounded-lg bg-white"
              placeholder="e.g. Initiate low-dose ACE inhibitor, recommend DASH diet, daily BP log"
              {...register('description', { required: 'Description is required.' })}
            />
            {errors.description && <p className="field-error">{errors.description.message}</p>}
          </div>

          <div>
            <label htmlFor="goals">Therapeutic Goals</label>
            <input
              id="goals"
              placeholder="e.g. Target BP < 130/80 mmHg within 4 weeks"
              {...register('goals')}
            />
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="interventions">Interventions</label>
              <input
                id="interventions"
                placeholder="e.g. Dietary counseling, weekly check-in"
                {...register('interventions')}
              />
            </div>
            <div>
              <label htmlFor="targetDate">Target Review Date</label>
              <input
                id="targetDate"
                type="date"
                {...register('targetDate')}
              />
            </div>
          </div>

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={isSubmitting}>
              {isSubmitting ? 'Saving Plan...' : 'Save Treatment Plan'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
