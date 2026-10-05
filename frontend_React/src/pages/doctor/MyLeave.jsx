import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import DashboardLayout from '../../layouts/DashboardLayout'
import { doctorNavigation as navigation } from './doctorNavigation'
import { getMyDoctorProfile } from '../../services/doctorService'
import { cancelLeave, createLeave, listLeaves } from '../../services/leaveService'

function LeaveFormModal({ doctorId, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: { startDate: '', endDate: '', reason: '' },
  })

  const onSubmit = async (values) => {
    try {
      await createLeave({ ...values, doctorId })
      onSaved()
    } catch (requestError) {
      setError('root', { message: requestError.response?.data?.message || 'Something went wrong. Please try again.' })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true">
    <div className="modal-card">
      <h2>Request Leave / Unavailability</h2>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <div className="field-row">
          <div><label htmlFor="startDate">Start date</label><input id="startDate" type="date" {...register('startDate', { required: 'Required.' })} />{errors.startDate && <p className="field-error">{errors.startDate.message}</p>}</div>
          <div><label htmlFor="endDate">End date</label><input id="endDate" type="date" {...register('endDate', { required: 'Required.' })} />{errors.endDate && <p className="field-error">{errors.endDate.message}</p>}</div>
        </div>
        <label htmlFor="reason">Reason</label>
        <input id="reason" {...register('reason')} />
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Save'}</button>
        </div>
      </form>
    </div>
  </div>
}

export default function MyLeave() {
  const [doctorId, setDoctorId] = useState(null)
  const [leaves, setLeaves] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)

  const loadLeaves = async (id) => {
    try {
      setLoading(true)
      setLeaves(await listLeaves({ doctorId: id }))
      setError('')
    } catch {
      setError('Unable to load your leave records.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    const init = async () => {
      try {
        const profile = await getMyDoctorProfile()
        setDoctorId(profile.id)
        await loadLeaves(profile.id)
      } catch (requestError) {
        setError(requestError.response?.status === 404
          ? 'No doctor profile is linked to your account yet. Please contact an administrator.'
          : 'Unable to load your profile.')
        setLoading(false)
      }
    }
    init()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleCancel = async (id) => {
    if (!window.confirm('Cancel this leave record?')) return
    await cancelLeave(id)
    loadLeaves(doctorId)
  }

  const handleSaved = () => { setShowForm(false); loadLeaves(doctorId) }

  return <DashboardLayout role="Doctor" navigation={navigation} title="My Leave / Unavailability" subtitle="Record and manage your own leave and temporary unavailability.">
    <div className="toolbar">
      <div className="filter-bar" />
      {doctorId && <button className="primary-button" style={{ marginTop: 0 }} onClick={() => setShowForm(true)}>+ Request Leave</button>}
    </div>

    {error && <p className="form-error" role="alert">{error}</p>}

    {!error && <div className="panel table-scroll">
      <table className="data-table">
        <thead>
          <tr><th>Start date</th><th>End date</th><th>Reason</th><th>Status</th><th></th></tr>
        </thead>
        <tbody>
          {leaves.map((leave) => <tr key={leave.id}>
            <td>{leave.startDate}</td>
            <td>{leave.endDate}</td>
            <td>{leave.reason || '—'}</td>
            <td><span className={`badge badge-${leave.status.toLowerCase()}`}>{leave.status}</span></td>
            <td className="row-actions">
              {leave.status !== 'Cancelled' && <button className="link-button danger" onClick={() => handleCancel(leave.id)}>Cancel</button>}
            </td>
          </tr>)}
        </tbody>
      </table>
      {!loading && leaves.length === 0 && <p className="empty-state">No leave records yet.</p>}
    </div>}

    {showForm && <LeaveFormModal doctorId={doctorId} onClose={() => setShowForm(false)} onSaved={handleSaved} />}
  </DashboardLayout>
}
