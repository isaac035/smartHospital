import { useForm } from 'react-hook-form'
import { recordVitalSign } from '../../services/emrService'
import { applyServerErrors, rules } from '../../utils/validators'

export default function RecordVitalsModal({ patientId, medicalRecordId = null, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    mode: 'onTouched',
    defaultValues: {
      patientId: patientId ? Number(patientId) : '',
      temperatureCelsius: '',
      systolicBloodPressure: '',
      diastolicBloodPressure: '',
      heartRateBpm: '',
      respiratoryRateBpm: '',
      oxygenSaturationSpO2: '',
      weightKg: '',
      heightCm: '',
      notes: '',
    },
  })

  const onSubmit = async (values) => {
    try {
      const payload = {
        patientId: Number(values.patientId || patientId),
        medicalRecordId: medicalRecordId ? Number(medicalRecordId) : null,
        temperatureCelsius: values.temperatureCelsius ? Number(values.temperatureCelsius) : null,
        systolicBloodPressure: values.systolicBloodPressure ? Number(values.systolicBloodPressure) : null,
        diastolicBloodPressure: values.diastolicBloodPressure ? Number(values.diastolicBloodPressure) : null,
        heartRateBpm: values.heartRateBpm ? Number(values.heartRateBpm) : null,
        respiratoryRateBpm: values.respiratoryRateBpm ? Number(values.respiratoryRateBpm) : null,
        oxygenSaturationSpO2: values.oxygenSaturationSpO2 ? Number(values.oxygenSaturationSpO2) : null,
        weightKg: values.weightKg ? Number(values.weightKg) : null,
        heightCm: values.heightCm ? Number(values.heightCm) : null,
        notes: values.notes ? values.notes.trim() : '',
      }

      await recordVitalSign(payload)
      onSaved()
    } catch (err) {
      if (err.response?.status === 403) {
        setError('root', { message: err.response?.data?.message || 'Unauthorized: You do not have permission to record vitals for this patient.' })
      } else {
        applyServerErrors(err, setError, { fields: ['temperatureCelsius', 'systolicBloodPressure', 'diastolicBloodPressure', 'heartRateBpm', 'respiratoryRateBpm', 'oxygenSaturationSpO2', 'weightKg', 'heightCm', 'notes'], fallback: 'Failed to record vitals. Please check value ranges.' })
      }
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 600px)' }}>
        <h2>Record Patient Vital Signs</h2>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}

          <div className="field-row">
            <div>
              <label htmlFor="temp">Temperature (°C)</label>
              <input
                id="temp"
                type="number"
                step="0.1"
                placeholder="36.8"
                aria-invalid={errors.temperatureCelsius ? 'true' : undefined}
                {...register('temperatureCelsius', rules.number('Temperature', { min: 30, max: 45, message: 'Temperature must be between 30.0 and 45.0 Celsius.' }))}
              />
              {errors.temperatureCelsius && <p className="field-error">{errors.temperatureCelsius.message}</p>}
            </div>
            <div>
              <label htmlFor="pulse">Heart Rate (bpm)</label>
              <input
                id="pulse"
                type="number"
                placeholder="75"
                aria-invalid={errors.heartRateBpm ? 'true' : undefined}
                {...register('heartRateBpm', rules.number('Heart rate', { min: 30, max: 250, integer: true, message: 'Heart rate must be between 30 and 250 bpm.' }))}
              />
              {errors.heartRateBpm && <p className="field-error">{errors.heartRateBpm.message}</p>}
            </div>
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="sys">Systolic BP (mmHg)</label>
              <input
                id="sys"
                type="number"
                placeholder="120"
                aria-invalid={errors.systolicBloodPressure ? 'true' : undefined}
                {...register('systolicBloodPressure', rules.number('Systolic blood pressure', { min: 50, max: 250, integer: true, message: 'Systolic blood pressure must be between 50 and 250 mmHg.' }))}
              />
              {errors.systolicBloodPressure && <p className="field-error">{errors.systolicBloodPressure.message}</p>}
            </div>
            <div>
              <label htmlFor="dia">Diastolic BP (mmHg)</label>
              <input
                id="dia"
                type="number"
                placeholder="80"
                aria-invalid={errors.diastolicBloodPressure ? 'true' : undefined}
                {...register('diastolicBloodPressure', rules.number('Diastolic blood pressure', { min: 30, max: 150, integer: true, message: 'Diastolic blood pressure must be between 30 and 150 mmHg.' }))}
              />
              {errors.diastolicBloodPressure && <p className="field-error">{errors.diastolicBloodPressure.message}</p>}
            </div>
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="spo2">Oxygen Saturation SpO2 (%)</label>
              <input
                id="spo2"
                type="number"
                step="0.1"
                placeholder="98"
                aria-invalid={errors.oxygenSaturationSpO2 ? 'true' : undefined}
                {...register('oxygenSaturationSpO2', rules.number('SpO2', { min: 50, max: 100, message: 'SpO2 must be between 50.0% and 100.0%.' }))}
              />
              {errors.oxygenSaturationSpO2 && <p className="field-error">{errors.oxygenSaturationSpO2.message}</p>}
            </div>
            <div>
              <label htmlFor="resp">Respiratory Rate (/min)</label>
              <input
                id="resp"
                type="number"
                placeholder="16"
                aria-invalid={errors.respiratoryRateBpm ? 'true' : undefined}
                {...register('respiratoryRateBpm', rules.number('Respiratory rate', { min: 5, max: 60, integer: true, message: 'Respiratory rate must be between 5 and 60 breaths/min.' }))}
              />
              {errors.respiratoryRateBpm && <p className="field-error">{errors.respiratoryRateBpm.message}</p>}
            </div>
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="weight">Weight (kg)</label>
              <input
                id="weight"
                type="number"
                step="0.1"
                placeholder="70"
                aria-invalid={errors.weightKg ? 'true' : undefined}
                {...register('weightKg', rules.number('Weight', { min: 1, max: 500, message: 'Weight must be between 1.0 and 500.0 kg.' }))}
              />
              {errors.weightKg && <p className="field-error">{errors.weightKg.message}</p>}
            </div>
            <div>
              <label htmlFor="height">Height (cm)</label>
              <input
                id="height"
                type="number"
                step="0.1"
                placeholder="175"
                aria-invalid={errors.heightCm ? 'true' : undefined}
                {...register('heightCm', rules.number('Height', { min: 30, max: 300, message: 'Height must be between 30.0 and 300.0 cm.' }))}
              />
              {errors.heightCm && <p className="field-error">{errors.heightCm.message}</p>}
            </div>
          </div>

          <div>
            <label htmlFor="notes">Clinical Notes</label>
            <input
              id="notes"
              placeholder="e.g. Patient resting quietly, cuff size standard adult"
              maxLength={500}
              aria-invalid={errors.notes ? 'true' : undefined}
              {...register('notes', rules.text('Notes', { max: 500 }))}
            />
            {errors.notes && <p className="field-error">{errors.notes.message}</p>}
          </div>

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={isSubmitting}>
              {isSubmitting ? 'Recording...' : 'Save Vitals'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
