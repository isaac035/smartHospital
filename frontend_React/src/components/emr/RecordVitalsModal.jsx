import { useForm } from 'react-hook-form'
import { recordVitalSign } from '../../services/emrService'

export default function RecordVitalsModal({ patientId, medicalRecordId = null, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
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
      setError('root', {
        message: err.response?.data?.message || (err.response?.status === 403 ? 'Unauthorized: You do not have permission to record vitals for this patient.' : 'Failed to record vitals. Please check value ranges.'),
      })
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
                {...register('temperatureCelsius', {
                  min: { value: 30, message: 'Min 30°C' },
                  max: { value: 45, message: 'Max 45°C' },
                })}
              />
              {errors.temperatureCelsius && <p className="field-error">{errors.temperatureCelsius.message}</p>}
            </div>
            <div>
              <label htmlFor="pulse">Heart Rate (bpm)</label>
              <input
                id="pulse"
                type="number"
                placeholder="75"
                {...register('heartRateBpm', {
                  min: { value: 30, message: 'Min 30 bpm' },
                  max: { value: 250, message: 'Max 250 bpm' },
                })}
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
                {...register('systolicBloodPressure', {
                  min: { value: 50, message: 'Min 50' },
                  max: { value: 250, message: 'Max 250' },
                })}
              />
              {errors.systolicBloodPressure && <p className="field-error">{errors.systolicBloodPressure.message}</p>}
            </div>
            <div>
              <label htmlFor="dia">Diastolic BP (mmHg)</label>
              <input
                id="dia"
                type="number"
                placeholder="80"
                {...register('diastolicBloodPressure', {
                  min: { value: 30, message: 'Min 30' },
                  max: { value: 150, message: 'Max 150' },
                })}
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
                {...register('oxygenSaturationSpO2', {
                  min: { value: 50, message: 'Min 50%' },
                  max: { value: 100, message: 'Max 100%' },
                })}
              />
              {errors.oxygenSaturationSpO2 && <p className="field-error">{errors.oxygenSaturationSpO2.message}</p>}
            </div>
            <div>
              <label htmlFor="resp">Respiratory Rate (/min)</label>
              <input
                id="resp"
                type="number"
                placeholder="16"
                {...register('respiratoryRateBpm', {
                  min: { value: 5, message: 'Min 5' },
                  max: { value: 60, message: 'Max 60' },
                })}
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
                {...register('weightKg', {
                  min: { value: 1, message: 'Min 1 kg' },
                  max: { value: 500, message: 'Max 500 kg' },
                })}
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
                {...register('heightCm', {
                  min: { value: 30, message: 'Min 30 cm' },
                  max: { value: 300, message: 'Max 300 cm' },
                })}
              />
              {errors.heightCm && <p className="field-error">{errors.heightCm.message}</p>}
            </div>
          </div>

          <div>
            <label htmlFor="notes">Clinical Notes</label>
            <input
              id="notes"
              placeholder="e.g. Patient resting quietly, cuff size standard adult"
              {...register('notes')}
            />
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
