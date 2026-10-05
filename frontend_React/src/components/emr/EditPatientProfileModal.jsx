import { useForm } from 'react-hook-form'
import { upsertPatientMedicalProfile } from '../../services/emrService'

const bloodGroupMap = {
  Unknown: 0,
  APositive: 1,
  'A+': 1,
  ANegative: 2,
  'A-': 2,
  BPositive: 3,
  'B+': 3,
  BNegative: 4,
  'B-': 4,
  ABPositive: 5,
  'AB+': 5,
  ABNegative: 6,
  'AB-': 6,
  OPositive: 7,
  'O+': 7,
  ONegative: 8,
  'O-': 8,
}

export default function EditPatientProfileModal({ patientId, profile, onClose, onSaved }) {
  const initialDob = profile?.dateOfBirth ? profile.dateOfBirth.slice(0, 10) : ''
  const initialBloodGroup = profile?.bloodGroup ? (bloodGroupMap[profile.bloodGroup] ?? 0) : 0

  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: {
      dateOfBirth: initialDob,
      gender: profile?.gender || 'Male',
      bloodGroup: initialBloodGroup,
      allergies: profile?.allergies || '',
      chronicDiseases: profile?.chronicDiseases || '',
      emergencyContactName: profile?.emergencyContactName || '',
      emergencyContactPhone: profile?.emergencyContactPhone || '',
    },
  })

  const onSubmit = async (values) => {
    try {
      const payload = {
        dateOfBirth: values.dateOfBirth ? new Date(values.dateOfBirth).toISOString() : null,
        gender: values.gender || 'Male',
        bloodGroup: Number(values.bloodGroup),
        allergies: values.allergies ? values.allergies.trim() : '',
        chronicDiseases: values.chronicDiseases ? values.chronicDiseases.trim() : '',
        emergencyContactName: values.emergencyContactName ? values.emergencyContactName.trim() : '',
        emergencyContactPhone: values.emergencyContactPhone ? values.emergencyContactPhone.trim() : '',
      }

      await upsertPatientMedicalProfile(patientId, payload)
      onSaved()
    } catch (err) {
      setError('root', {
        message: err.response?.data?.message ||
          (err.response?.status === 403
            ? 'Unauthorized: You do not have permission to update this profile.'
            : 'Failed to update patient medical profile.'),
      })
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 620px)' }}>
        <h2>{profile ? 'Edit Patient Medical Profile' : 'Initialize Medical Profile'}</h2>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}

          <div className="field-row">
            <div>
              <label htmlFor="dob">Date of Birth</label>
              <input
                id="dob"
                type="date"
                {...register('dateOfBirth')}
              />
            </div>
            <div>
              <label htmlFor="gender">Gender</label>
              <select
                id="gender"
                className="w-full p-2.5 border rounded-lg bg-white"
                {...register('gender')}
              >
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Other">Other</option>
              </select>
            </div>
          </div>

          <div>
            <label htmlFor="bloodGroup">Blood Group</label>
            <select
              id="bloodGroup"
              className="w-full p-2.5 border rounded-lg bg-white"
              {...register('bloodGroup')}
            >
              <option value={0}>Unknown</option>
              <option value={1}>A+ (A Positive)</option>
              <option value={2}>A- (A Negative)</option>
              <option value={3}>B+ (B Positive)</option>
              <option value={4}>B- (B Negative)</option>
              <option value={5}>AB+ (AB Positive)</option>
              <option value={6}>AB- (AB Negative)</option>
              <option value={7}>O+ (O Positive)</option>
              <option value={8}>O- (O Negative)</option>
            </select>
          </div>

          <div>
            <label htmlFor="allergies">Known Allergies (Medications / Food / Environmental)</label>
            <input
              id="allergies"
              placeholder="e.g. Penicillin, Peanuts, Latex (or None)"
              {...register('allergies')}
            />
          </div>

          <div>
            <label htmlFor="chronic">Chronic Illnesses / Ongoing Conditions</label>
            <input
              id="chronic"
              placeholder="e.g. Type 2 Diabetes, Hypertension, Asthma"
              {...register('chronicDiseases')}
            />
          </div>

          <div className="field-row">
            <div>
              <label htmlFor="ecName">Emergency Contact Name</label>
              <input
                id="ecName"
                placeholder="e.g. Jane Doe (Spouse)"
                {...register('emergencyContactName')}
              />
            </div>
            <div>
              <label htmlFor="ecPhone">Emergency Contact Phone</label>
              <input
                id="ecPhone"
                placeholder="e.g. +1 555-0192"
                {...register('emergencyContactPhone')}
              />
            </div>
          </div>

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="primary-button" disabled={isSubmitting}>
              {isSubmitting ? 'Saving Profile...' : 'Save Medical Profile'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
