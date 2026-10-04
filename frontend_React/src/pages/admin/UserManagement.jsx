import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation as navigation } from './adminNavigation'
import { listUsers, updateUser, deactivateUser, createDoctorManager } from '../../services/userService'

function UserFormModal({ user, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: {
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      phoneNumber: user?.phoneNumber || '',
    },
  })

  const onSubmit = async (values) => {
    try {
      if (user) await updateUser(user.id, values)
      onSaved()
    } catch (requestError) {
      setError('root', { message: requestError.response?.data?.message || 'Something went wrong. Please try again.' })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true">
    <div className="modal-card">
      <h2>Edit User</h2>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <div className="field-row">
          <div>
            <label htmlFor="firstName">First name</label>
            <input id="firstName" {...register('firstName', { required: 'Required.' })} />
            {errors.firstName && <p className="field-error">{errors.firstName.message}</p>}
          </div>
          <div>
            <label htmlFor="lastName">Last name</label>
            <input id="lastName" {...register('lastName', { required: 'Required.' })} />
            {errors.lastName && <p className="field-error">{errors.lastName.message}</p>}
          </div>
        </div>
        <label htmlFor="phoneNumber">Phone number</label>
        <input id="phoneNumber" {...register('phoneNumber', { required: 'Required.' })} />
        {errors.phoneNumber && <p className="field-error">{errors.phoneNumber.message}</p>}
        
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Save'}</button>
        </div>
      </form>
    </div>
  </div>
}

// Creates a Doctor Manager: an admin who can only manage doctors, availability and leave.
function DoctorManagerFormModal({ onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    defaultValues: { firstName: '', lastName: '', email: '', phoneNumber: '', password: '' },
  })

  const onSubmit = async (values) => {
    try {
      await createDoctorManager(values)
      onSaved()
    } catch (requestError) {
      setError('root', { message: requestError.response?.data?.message || 'Could not create the account. Please try again.' })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true" aria-labelledby="doctor-manager-title">
    <div className="modal-card">
      <h2 id="doctor-manager-title">Create Doctor Manager</h2>
      <p>Can manage doctors, the availability calendar, and leave. No other access.</p>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <div className="field-row">
          <div>
            <label htmlFor="dmFirstName">First name</label>
            <input id="dmFirstName" {...register('firstName', { required: 'Required.' })} />
            {errors.firstName && <p className="field-error">{errors.firstName.message}</p>}
          </div>
          <div>
            <label htmlFor="dmLastName">Last name</label>
            <input id="dmLastName" {...register('lastName', { required: 'Required.' })} />
            {errors.lastName && <p className="field-error">{errors.lastName.message}</p>}
          </div>
        </div>
        <label htmlFor="dmEmail">Email</label>
        <input id="dmEmail" type="email" autoComplete="off" {...register('email', {
          required: 'Required.',
          pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email address.' },
        })} />
        {errors.email && <p className="field-error">{errors.email.message}</p>}
        <label htmlFor="dmPhone">Phone number</label>
        <input id="dmPhone" {...register('phoneNumber')} />
        <label htmlFor="dmPassword">Temporary password</label>
        <input id="dmPassword" type="password" autoComplete="new-password" {...register('password', {
          required: 'Required.',
          minLength: { value: 6, message: 'At least 6 characters.' },
        })} />
        {errors.password && <p className="field-error">{errors.password.message}</p>}
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Creating...' : 'Create account'}</button>
        </div>
      </form>
    </div>
  </div>
}

export default function UserManagement() {
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [activeTab, setActiveTab] = useState('Patients')
  
  const [editingUser, setEditingUser] = useState(null)
  const [showForm, setShowForm] = useState(false)
  const [showDoctorManagerForm, setShowDoctorManagerForm] = useState(false)

  const loadUsers = async () => {
    try {
      setLoading(true)
      const data = await listUsers()
      setUsers(data)
      setError('')
    } catch {
      setError('Unable to load users.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadUsers()
  }, [])

  const handleDeactivate = async (id) => {
    if (!window.confirm('Are you sure you want to delete (deactivate) this user?')) return
    try {
      await deactivateUser(id)
      loadUsers()
    } catch {
      alert('Failed to deactivate user.')
    }
  }

  const closeForm = () => { setShowForm(false); setEditingUser(null) }
  const handleSaved = () => { closeForm(); loadUsers() }

  const displayedUsers = users.filter(u => {
    if (activeTab === 'Patients') return u.role === 'Patient'
    return u.role !== 'Patient' // Admin or Staff or Doctor
  })

  return (
    <DashboardLayout role="Admin" navigation={navigation} title="User Management" subtitle="Manage patients, administrators, and their details.">
      <div className="toolbar" style={{ borderBottom: '1px solid #eee', marginBottom: '16px' }}>
        <div style={{ display: 'flex', gap: '16px' }}>
          <button 
            className={`nav-item ${activeTab === 'Patients' ? 'active' : ''}`} 
            style={{ padding: '8px 16px', background: activeTab === 'Patients' ? '#f0f4f8' : 'none', border: 'none', cursor: 'pointer', fontWeight: activeTab === 'Patients' ? 'bold' : 'normal' }}
            onClick={() => setActiveTab('Patients')}>
            Patients
          </button>
          <button 
            className={`nav-item ${activeTab === 'Admins' ? 'active' : ''}`} 
            style={{ padding: '8px 16px', background: activeTab === 'Admins' ? '#f0f4f8' : 'none', border: 'none', cursor: 'pointer', fontWeight: activeTab === 'Admins' ? 'bold' : 'normal' }}
            onClick={() => setActiveTab('Admins')}>
            Admins
          </button>
        </div>
        <button className="primary-button" onClick={() => setShowDoctorManagerForm(true)}>+ Create Doctor Manager</button>
      </div>

      {error && <p className="form-error" role="alert">{error}</p>}

      <div className="panel table-scroll">
        <table className="data-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>Phone</th>
              <th>Status</th>
              <th>Registered</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {displayedUsers.map((user) => (
              <tr key={user.id}>
                <td>{user.firstName} {user.lastName}</td>
                <td>{user.email}</td>
                <td>{user.role}</td>
                <td>{user.phoneNumber || '-'}</td>
                <td><span className={`badge badge-${user.status.toLowerCase()}`}>{user.status}</span></td>
                <td>{new Date(user.createdAt).toLocaleDateString()}</td>
                <td className="row-actions">
                  <button className="link-button" onClick={() => { setEditingUser(user); setShowForm(true) }}>Edit</button>
                  {user.status !== 'Inactive' && (
                    <button className="link-button danger" onClick={() => handleDeactivate(user.id)}>Delete</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {!loading && displayedUsers.length === 0 && <p className="empty-state">No users found in this category.</p>}
        {loading && <p className="empty-state">Loading users...</p>}
      </div>

      {showForm && <UserFormModal user={editingUser} onClose={closeForm} onSaved={handleSaved} />}
      {showDoctorManagerForm && <DoctorManagerFormModal
        onClose={() => setShowDoctorManagerForm(false)}
        onSaved={() => { setShowDoctorManagerForm(false); setActiveTab('Admins'); loadUsers() }} />}
    </DashboardLayout>
  )
}
