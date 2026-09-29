import { useForm } from 'react-hook-form'
import { createLabOrder } from '../../services/emrService'

export default function NewLabOrderModal({ patientId, medicalRecordId = null, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: {
      patientId: patientId ? Number(patientId) : '',
      testName: '',
      category: 'Hematology',
      priority: 1, // Routine
      clinicalNotes: '',
    },
  })

  const onSubmit = async (values) => {
    try {
      const payload = {
        patientId: Number(values.patientId || patientId),
        medicalRecordId: medicalRecordId ? Number(medicalRecordId) : null,
        testName: values.testName.trim(),
        category: values.category || 'General',
        priority: Number(values.priority),
        clinicalNotes: values.clinicalNotes ? values.clinicalNotes.trim() : '',
      }

      await createLabOrder(payload)
      onSaved()
    } catch (err) {
      setError('root', {
        message: err.response?.data?.message ||
          (err.response?.status === 403
            ? 'Unauthorized: You do not have permission to order tests for this patient.'
            : 'Failed to create lab order. Please check all fields.'),
      })
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 600px)' }}>
        <h2>Order Diagnostic / Laboratory Test</h2>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}

          <div>
            <label htmlFor="testName">Test / Investigation Name *</label>
            <input
              id="testName"
              placeholder="e.g. Complete Blood Count (CBC), Lipid Panel, Chest X-Ray"
              {...register('testName', { required: 'Test name is required.' })}
            />
            {errors.testName && <p className="field-error">{errors.testName.message}</p>}
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="category">Category</label>
              <select
                id="category"
                className="w-full p-2.5 border rounded-lg bg-white"
                {...register('category')}
              >
                <option value="Hematology">Hematology</option>
                <option value="Biochemistry">Biochemistry</option>
                <option value="Microbiology">Microbiology</option>
                <option value="Radiology">Radiology / Imaging</option>
                <option value="Pathology">Pathology</option>
                <option value="Cardiology">Cardiology (ECG/Echo)</option>
                <option value="General">General / Routine</option>
              </select>
            </div>
            <div>
              <label htmlFor="priority">Priority Level *</label>
              <select
                id="priority"
                className="w-full p-2.5 border rounded-lg bg-white"
                {...register('priority', { required: true })}
              >
                <option value={1}>Routine</option>
                <option value={2}>Urgent</option>
                <option value={3}>Stat (Emergency)</option>
              </select>
            </div>
          </div>

          <div>
            <label htmlFor="clinicalNotes">Clinical Indication / Reason for Test</label>
            <input
              id="clinicalNotes"
              placeholder="e.g. Suspected anemia, rule out infection, pre-operative workup"
              {...register('clinicalNotes')}
            />
          </div>

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={isSubmitting}>
              {isSubmitting ? 'Placing Order...' : 'Submit Lab Order'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
