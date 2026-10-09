import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation } from './adminNavigation'
import { doctorManagerNavigation } from './doctorManagerNavigation'
import { useAuth } from '../../hooks/useAuth'
import { createDepartment, deactivateDepartment, activateDepartment, listDepartments, updateDepartment } from '../../services/departmentService'
import ToggleActiveButton from '../../components/common/ToggleActiveButton'
import { applyServerErrors, rules } from '../../utils/validators'

function DepartmentFormModal({ department, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    mode: 'onTouched',
    defaultValues: { name: department?.name || '', description: department?.description || '' },
  })

  const onSubmit = async (values) => {
    try {
      if (department) await updateDepartment(department.id, values)
      else await createDepartment(values)
      onSaved()
    } catch (requestError) {
      applyServerErrors(requestError, setError, { fields: ['name', 'description'], conflicts: [{ match: /name/i, field: 'name' }] })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true">
    <div className="modal-card">
      <h2>{department ? 'Edit Department' : 'Add Department'}</h2>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <label htmlFor="name" className="required">Name</label>
        <input id="name" maxLength={100} aria-invalid={errors.name ? 'true' : undefined} {...register('name', rules.text('Name', { isRequired: true, max: 100 }))} />
        {errors.name && <p className="field-error">{errors.name.message}</p>}
        <label htmlFor="description">Description</label>
        <input id="description" maxLength={500} aria-invalid={errors.description ? 'true' : undefined} {...register('description', rules.text('Description', { max: 500 }))} />
        {errors.description && <p className="field-error">{errors.description.message}</p>}
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Save'}</button>
        </div>
      </form>
    </div>
  </div>
}

export default function DepartmentManagement() {
  const { user } = useAuth()
  const role = user?.role === 'DoctorManager' ? 'DoctorManager' : 'Admin'
  const navigation = role === 'DoctorManager' ? doctorManagerNavigation : adminNavigation
  const [departments, setDepartments] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editingDepartment, setEditingDepartment] = useState(null)
  const [showForm, setShowForm] = useState(false)

  const loadDepartments = async () => {
    try {
      setLoading(true)
      setDepartments(await listDepartments())
      setError('')
    } catch {
      setError('Unable to load departments.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { loadDepartments() }, [])

  const handleDeactivate = async (id) => {
    if (!window.confirm('Deactivate this department?')) return
    await deactivateDepartment(id)
    loadDepartments()
  }

  const closeForm = () => { setShowForm(false); setEditingDepartment(null) }
  const handleSaved = () => { closeForm(); loadDepartments() }

  return <DashboardLayout role={role} navigation={navigation} title="Department Management" subtitle="Manage hospital departments.">
    <div className="toolbar">
      <div className="filter-bar" />
      <button className="primary-button" style={{ marginTop: 0 }} onClick={() => setShowForm(true)}>+ Add Department</button>
    </div>

    {error && <p className="form-error" role="alert">{error}</p>}

    <div className="panel table-scroll">
      <table className="data-table">
        <thead>
          <tr><th>Name</th><th>Description</th><th>Status</th><th></th></tr>
        </thead>
        <tbody>
          {departments.map((department) => <tr key={department.id}>
            <td>{department.name}</td>
            <td>{department.description || '—'}</td>
            <td><span className={`badge badge-${(department.status || 'inactive').toLowerCase()}`}>{department.status || 'Inactive'}</span></td>
            <td className="row-actions">
              <button className="link-button" onClick={() => { setEditingDepartment(department); setShowForm(true) }}>Edit</button>
              <ToggleActiveButton 
                  isActive={department.status === 'Active'}
                  resourceName="Department"
                  identifier={department.name}
                  onToggle={async () => {
                    if (department.status === 'Active') {
                      await deactivateDepartment(department.id)
                    } else {
                      await activateDepartment(department.id)
                    }
                    loadDepartments()
                  }}
                />
            </td>
          </tr>)}
        </tbody>
      </table>
      {!loading && departments.length === 0 && <p className="empty-state">No departments yet.</p>}
    </div>

    {showForm && <DepartmentFormModal department={editingDepartment} onClose={closeForm} onSaved={handleSaved} />}
  </DashboardLayout>
}
