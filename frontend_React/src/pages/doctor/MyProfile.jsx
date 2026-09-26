import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import DashboardLayout from '../../layouts/DashboardLayout'
import { doctorNavigation as navigation } from './doctorNavigation'
import { getMyDoctorProfile, updateMyDoctorProfile } from '../../services/doctorService'

export default function MyProfile() {
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editing, setEditing] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')

  const { register, handleSubmit, reset, formState: { errors, isSubmitting }, setError: setFormError } = useForm({
    defaultValues: { phoneNumber: '', bio: '' },
  })

  const loadProfile = async () => {
    try {
      setLoading(true)
      const data = await getMyDoctorProfile()
      setProfile(data)
      reset({ phoneNumber: data.phoneNumber, bio: data.bio })
      setError('')
    } catch (requestError) {
      setError(requestError.response?.status === 404
        ? 'No doctor profile is linked to your account yet. Please contact an administrator.'
        : 'Unable to load your profile.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { loadProfile() }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const onSubmit = async (values) => {
    setSuccessMessage('')
    try {
      const updated = await updateMyDoctorProfile(values)
      setProfile(updated)
      setEditing(false)
      setSuccessMessage('Profile updated successfully.')
    } catch (requestError) {
      setFormError('root', { message: requestError.response?.data?.message || 'Something went wrong. Please try again.' })
    }
  }

  return <DashboardLayout role="Doctor" navigation={navigation} title="My Profile" subtitle="View and update your contact details.">
    {error && <p className="form-error" role="alert">{error}</p>}
    {successMessage && !editing && <p className="form-success" role="status">{successMessage}</p>}

    {!loading && profile && !editing && <div className="panel" style={{ padding: 24 }}>
      <div className="field-row">
        <div><label>Name</label><p>Dr. {profile.firstName} {profile.lastName}</p></div>
        <div><label>Status</label><p><span className={`badge badge-${profile.status.toLowerCase()}`}>{profile.status}</span></p></div>
      </div>
      <div className="field-row">
        <div><label>Department</label><p>{profile.departmentName}</p></div>
        <div><label>Specialization</label><p>{profile.specialization}</p></div>
      </div>
      <div className="field-row">
        <div><label>License number</label><p>{profile.licenseNumber}</p></div>
        <div><label>Years of experience</label><p>{profile.yearsOfExperience} yrs</p></div>
      </div>
      <div className="field-row">
        <div><label>Email</label><p>{profile.email}</p></div>
        <div><label>Phone number</label><p>{profile.phoneNumber}</p></div>
      </div>
      <label>Bio</label>
      <p>{profile.bio || '—'}</p>
      <button className="primary-button" onClick={() => setEditing(true)}>Edit contact details</button>
    </div>}

    {editing && <div className="panel" style={{ padding: 24 }}>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <label htmlFor="phoneNumber">Phone number</label>
        <input id="phoneNumber" {...register('phoneNumber', { required: 'Phone number is required.' })} />
        {errors.phoneNumber && <p className="field-error">{errors.phoneNumber.message}</p>}
        <label htmlFor="bio">Bio</label>
        <input id="bio" {...register('bio')} />
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={() => { setEditing(false); reset({ phoneNumber: profile.phoneNumber, bio: profile.bio }) }}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Save'}</button>
        </div>
      </form>
    </div>}

    <p className="page-subtitle" style={{ marginTop: 16 }}>Department, specialization, license and experience are managed by hospital administration. Contact an administrator to update them.</p>
  </DashboardLayout>
}
