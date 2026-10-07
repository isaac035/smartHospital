import { useForm } from 'react-hook-form'
import { recordLabReport } from '../../services/emrService'
import { applyServerErrors, rules } from '../../utils/validators'

export default function RecordLabReportModal({ order, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    mode: 'onTouched',
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
      if (err.response?.status === 403) {
        setError('root', { message: err.response?.data?.message || 'Unauthorized: You do not have permission to record results for this order.' })
      } else {
        applyServerErrors(err, setError, { fields: ['resultSummary', 'findings', 'referenceRange', 'doctorRemarks', 'attachmentUrl'], fallback: 'Failed to record lab report. Please check required fields.' })
      }
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
              maxLength={500}
              aria-invalid={errors.resultSummary ? 'true' : undefined}
              {...register('resultSummary', rules.text('Result summary', { isRequired: true, max: 500 }))}
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
              maxLength={2000}
              aria-invalid={errors.findings ? 'true' : undefined}
              {...register('findings', rules.text('Findings', { isRequired: true, max: 2000 }))}
            />
            {errors.findings && <p className="field-error">{errors.findings.message}</p>}
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="referenceRange">Reference Range / Normal Values</label>
              <input
                id="referenceRange"
                placeholder="e.g. WBC 4.5-11.0, Hb 13.5-17.5"
                maxLength={500}
                aria-invalid={errors.referenceRange ? 'true' : undefined}
                {...register('referenceRange', rules.text('Reference range', { max: 500 }))}
              />
              {errors.referenceRange && <p className="field-error">{errors.referenceRange.message}</p>}
            </div>
            <div>
              <label htmlFor="attachmentUrl">Attachment URL (Optional)</label>
              <input
                id="attachmentUrl"
                type="url"
                placeholder="https://..."
                maxLength={500}
                aria-invalid={errors.attachmentUrl ? 'true' : undefined}
                {...register('attachmentUrl', rules.httpUrl('Attachment URL'))}
              />
              {errors.attachmentUrl && <p className="field-error">{errors.attachmentUrl.message}</p>}
            </div>
          </div>

          <div>
            <label htmlFor="doctorRemarks">Clinical Remarks</label>
            <input
              id="doctorRemarks"
              placeholder="e.g. Consistent with mild bacterial infection, follow up in 1 week"
              maxLength={1000}
              aria-invalid={errors.doctorRemarks ? 'true' : undefined}
              {...register('doctorRemarks', rules.text('Doctor remarks', { max: 1000 }))}
            />
            {errors.doctorRemarks && <p className="field-error">{errors.doctorRemarks.message}</p>}
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
