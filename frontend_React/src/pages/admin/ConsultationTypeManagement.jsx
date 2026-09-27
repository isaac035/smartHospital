import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation as navigation } from './adminNavigation'
import { createConsultationType, deactivateConsultationType, listConsultationTypes, updateConsultationType } from '../../services/consultationTypeService'

function ConsultationTypeFormModal({ consultationType, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: {
      name: consultationType?.name || '',
      durationMinutes: consultationType?.durationMinutes ?? 30,
      description: consultationType?.description || '',
    },
  })

  const onSubmit = async (values) => {
    const payload = { ...values, durationMinutes: Number(values.durationMinutes) }
    try {
      if (consultationType) await updateConsultationType(consultationType.id, payload)
      else await createConsultationType(payload)
      onSaved()
    } catch (requestError) {
      setError('root', { message: requestError.response?.data?.message || 'Something went wrong. Please try again.' })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true">
    <div className="modal-card">
      <h2>{consultationType ? 'Edit Consultation Type' : 'Add Consultation Type'}</h2>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <label htmlFor="name">Name</label>
        <input id="name" {...register('name', { required: 'Name is required.' })} />
        {errors.name && <p className="field-error">{errors.name.message}</p>}
        <label htmlFor="durationMinutes">Session duration (minutes)</label>
        <input id="durationMinutes" type="number" min="1" max="480" {...register('durationMinutes', { required: 'Duration is required.', min: 1, max: 480 })} />
        {errors.durationMinutes && <p className="field-error">Enter a duration between 1 and 480 minutes.</p>}
        <label htmlFor="description">Description</label>
        <input id="description" {...register('description')} />
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Save'}</button>
        </div>
      </form>
    </div>
  </div>
}

export default function ConsultationTypeManagement() {
  const [consultationTypes, setConsultationTypes] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editingType, setEditingType] = useState(null)
  const [showForm, setShowForm] = useState(false)

  const loadConsultationTypes = async () => {
    try {
      setLoading(true)
      setConsultationTypes(await listConsultationTypes())
      setError('')
    } catch {
      setError('Unable to load consultation types.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { loadConsultationTypes() }, [])

  const handleDeactivate = async (id) => {
    if (!window.confirm('Deactivate this consultation type? Doctors will no longer be able to add new availability sessions for it.')) return
    await deactivateConsultationType(id)
    loadConsultationTypes()
  }

  const closeForm = () => { setShowForm(false); setEditingType(null) }
  const handleSaved = () => { closeForm(); loadConsultationTypes() }

  return <DashboardLayout role="Admin" navigation={navigation} title="Consultation Types" subtitle="Define the session types and durations doctors can be scheduled for.">
    <div className="toolbar">
      <div className="filter-bar" />
      <button className="primary-button" style={{ marginTop: 0 }} onClick={() => setShowForm(true)}>+ Add Consultation Type</button>
    </div>

    {error && <p className="form-error" role="alert">{error}</p>}

    <div className="panel table-scroll">
      <table className="data-table">
        <thead>
          <tr><th>Name</th><th>Duration</th><th>Description</th><th>Status</th><th></th></tr>
        </thead>
        <tbody>
          {consultationTypes.map((type) => <tr key={type.id}>
            <td>{type.name}</td>
            <td>{type.durationMinutes} min</td>
            <td>{type.description || '—'}</td>
            <td><span className={`badge badge-${type.status.toLowerCase()}`}>{type.status}</span></td>
            <td className="row-actions">
              <button className="link-button" onClick={() => { setEditingType(type); setShowForm(true) }}>Edit</button>
              {type.status === 'Active' && <button className="link-button danger" onClick={() => handleDeactivate(type.id)}>Deactivate</button>}
            </td>
          </tr>)}
        </tbody>
      </table>
      {!loading && consultationTypes.length === 0 && <p className="empty-state">No consultation types yet.</p>}
    </div>

    {showForm && <ConsultationTypeFormModal consultationType={editingType} onClose={closeForm} onSaved={handleSaved} />}
  </DashboardLayout>
}
