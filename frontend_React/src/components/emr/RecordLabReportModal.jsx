import { useForm } from 'react-hook-form'
import { recordLabReport } from '../../services/emrService'

export default function RecordLabReportModal({ order, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: {
      resultSummary: order?.report?.resultSummary || '',
      findings: order?.report?.findings || '',
      referenceRange: order?.report?.referenceRange || '',
      doctorRemarks: order?.report?.doctorRemarks || '',
      attachmentUrl: order?.report?.attachmentUrl || '',
    },
  })

  const onSubmit = async (values) => {
    try {
      const payload = {
        resultSummary: values.resultSummary.trim(),
        findings: values.findings.trim(),
        referenceRange: values.referenceRange ? values.referenceRange.trim() : '',
        doctorRemarks: values.doctorRemarks ? values.doctorRemarks.trim() : '',
        attachmentUrl: values.attachmentUrl ? values.attachmentUrl.trim() : '',
        reportDate: new Date().toISOString(),
      }

      await recordLabReport(order.id, payload)
      onSaved()
    } catch (err) {
      setError('root', {
        message: err.response?.data?.message ||
          (err.response?.status === 403
            ? 'Unauthorized: You do not have permission to record results for this order.'
            : 'Failed to record lab report. Please check required fields.'),
      })
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 650px)' }}>
        <h2>Enter Diagnostic Report: {order?.testName}</h2>
        <p className="text-xs text-gray-500 mb-4">
          Order #{order?.orderNumber || order?.id} • Category: {order?.category || 'General'}
        </p>

        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}

          <div>
            <label htmlFor="resultSummary">Result Summary / Impression *</label>
            <input
              id="resultSummary"
              placeholder="e.g. Normal complete blood count, mild leukocytosis"
              {...register('resultSummary', { required: 'Result summary is required.' })}
            />
            {errors.resultSummary && <p className="field-error">{errors.resultSummary.message}</p>}
          </div>

          <div>
            <label htmlFor="findings">Detailed Findings & Values *</label>
            <textarea
              id="findings"
              rows={4}
              className="w-full p-2.5 border rounded-lg bg-white"
              placeholder="e.g. WBC: 11.2 x10^9/L, RBC: 4.8 x10^12/L, Hemoglobin: 14.2 g/dL, Platelets: 250 x10^9/L"
              {...register('findings', { required: 'Findings are required.' })}
            />
            {errors.findings && <p className="field-error">{errors.findings.message}</p>}
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="referenceRange">Reference Range / Normal Values</label>
              <input
                id="referenceRange"
                placeholder="e.g. WBC 4.5-11.0, Hb 13.5-17.5"
                {...register('referenceRange')}
              />
            </div>
            <div>
              <label htmlFor="attachmentUrl">Attachment URL (Optional)</label>
              <input
                id="attachmentUrl"
                type="url"
                placeholder="https://..."
                {...register('attachmentUrl')}
              />
            </div>
          </div>

          <div>
            <label htmlFor="doctorRemarks">Clinical Remarks</label>
            <input
              id="doctorRemarks"
              placeholder="e.g. Consistent with mild bacterial infection, follow up in 1 week"
              {...register('doctorRemarks')}
            />
          </div>

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={isSubmitting}>
              {isSubmitting ? 'Saving Findings...' : 'Save Diagnostic Report'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
