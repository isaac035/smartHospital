import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation as navigation } from './adminNavigation'
import { listDoctors } from '../../services/doctorService'
import { cancelLeave, createLeave, listLeaves, updateLeave } from '../../services/leaveService'

function LeaveFormModal({ doctors, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: { doctorId: doctors[0]?.id || '', startDate: '', endDate: '', reason: '' },
  })

  const onSubmit = async (values) => {
    try {
      await createLeave({ ...values, doctorId: Number(values.doctorId) })
      onSaved()
    } catch (requestError) {
      setError('root', { message: requestError.response?.data?.message || 'Something went wrong. Please try again.' })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true">
    <div className="modal-card">
      <h2>Record Leave / Unavailability</h2>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <label htmlFor="doctorId">Doctor</label>
        <select id="doctorId" {...register('doctorId', { required: true })}>
          {doctors.map((doctor) => <option key={doctor.id} value={doctor.id}>Dr. {doctor.firstName} {doctor.lastName}</option>)}
        </select>
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

export default function LeaveManagement() {
  const [leaves, setLeaves] = useState([])
  const [doctors, setDoctors] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)

  const loadLeaves = async () => {
    try {
      setLoading(true)
      setLeaves(await listLeaves())
      setError('')
    } catch {
      setError('Unable to load leave records.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadLeaves()
    listDoctors().then(setDoctors).catch(() => {})
  }, [])

  const handleCancel = async (id) => {
    if (!window.confirm('Cancel this leave record?')) return
    await cancelLeave(id)
    loadLeaves()
  }

  const handleDecision = async (leave, status) => {
    await updateLeave(leave.id, { startDate: leave.startDate, endDate: leave.endDate, reason: leave.reason, status })
    loadLeaves()
  }

  const handleSaved = () => { setShowForm(false); loadLeaves() }

  return <DashboardLayout role="Admin" navigation={navigation} title="Leave / Unavailability" subtitle="Record and manage doctor leave and temporary unavailability.">
    <div className="toolbar">
      <div className="filter-bar" />
      <button className="primary-button" style={{ marginTop: 0 }} onClick={() => setShowForm(true)}>+ Record Leave</button>
    </div>

    {error && <p className="form-error" role="alert">{error}</p>}

    <div className="panel table-scroll">
      <table className="data-table">
        <thead>
          <tr><th>Doctor</th><th>Start date</th><th>End date</th><th>Reason</th><th>Status</th><th></th></tr>
        </thead>
        <tbody>
          {leaves.map((leave) => <tr key={leave.id}>
            <td>Dr. {leave.doctorName}</td>
            <td>{leave.startDate}</td>
            <td>{leave.endDate}</td>
            <td>{leave.reason || '—'}</td>
            <td><span className={`badge badge-${leave.status.toLowerCase()}`}>{leave.status}</span></td>
            <td className="row-actions">
              {leave.status === 'Pending' && <button className="link-button" onClick={() => handleDecision(leave, 'Approved')}>Approve</button>}
              {leave.status === 'Pending' && <button className="link-button danger" onClick={() => handleDecision(leave, 'Rejected')}>Reject</button>}
              {leave.status !== 'Cancelled' && <button className="link-button danger" onClick={() => handleCancel(leave.id)}>Cancel</button>}
            </td>
          </tr>)}
        </tbody>
      </table>
      {!loading && leaves.length === 0 && <p className="empty-state">No leave records yet.</p>}
    </div>

    {showForm && <LeaveFormModal doctors={doctors} onClose={() => setShowForm(false)} onSaved={handleSaved} />}
  </DashboardLayout>
}
