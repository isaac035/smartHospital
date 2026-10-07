import ToggleActiveButton from '../../components/common/ToggleActiveButton'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation } from './adminNavigation'
import { doctorManagerNavigation } from './doctorManagerNavigation'
import { useAuth } from '../../hooks/useAuth'
import { listDepartments } from '../../services/departmentService'
import { listConsultationTypes } from '../../services/consultationTypeService'
import { applyServerErrors, rules } from '../../utils/validators'
import { activateDoctor, createDoctor, createDoctorLogin, deactivateDoctor, deleteDoctorPermanently, listDoctors, updateDoctor } from '../../services/doctorService'

function DoctorFormModal({ doctor, departments, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
    mode: 'onTouched',
    defaultValues: {
      firstName: doctor?.firstName || '',
      lastName: doctor?.lastName || '',
      email: doctor?.email || '',
      phoneNumber: doctor?.phoneNumber || '',
      departmentId: doctor?.departmentId || departments[0]?.id || '',
      specialization: doctor?.specialization || '',
      licenseNumber: doctor?.licenseNumber || '',
      yearsOfExperience: doctor?.yearsOfExperience ?? 0,
      bio: doctor?.bio || '',
    },
  })

  const onSubmit = async (values) => {
    const payload = { ...values, departmentId: Number(values.departmentId), yearsOfExperience: Number(values.yearsOfExperience) }
    try {
      const result = doctor ? await updateDoctor(doctor.id, payload) : await createDoctor(payload)
      onSaved(doctor ? null : result)
    } catch (requestError) {
      applyServerErrors(requestError, setError, {
        fields: ['firstName', 'lastName', 'email', 'phoneNumber', 'departmentId', 'specialization', 'licenseNumber', 'yearsOfExperience', 'bio'],
        conflicts: [{ match: /email|account|user/i, field: 'email' }, { match: /licen[cs]e/i, field: 'licenseNumber' }, { match: /department/i, field: 'departmentId' }],
      })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true">
    <div className="modal-card">
      <h2>{doctor ? 'Edit Doctor' : 'Add Doctor'}</h2>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <div className="field-row">
          <div><label htmlFor="firstName" className="required">First name</label><input id="firstName" maxLength={100} aria-invalid={errors.firstName ? 'true' : undefined} {...register('firstName', rules.personName('First name'))} />{errors.firstName && <p className="field-error">{errors.firstName.message}</p>}</div>
          <div><label htmlFor="lastName" className="required">Last name</label><input id="lastName" maxLength={100} aria-invalid={errors.lastName ? 'true' : undefined} {...register('lastName', rules.personName('Last name'))} />{errors.lastName && <p className="field-error">{errors.lastName.message}</p>}</div>
        </div>
        <label htmlFor="email" className="required">Email</label>
        <input id="email" type="email" maxLength={255} aria-invalid={errors.email ? 'true' : undefined} {...register('email', rules.email())} />
        {errors.email && <p className="field-error">{errors.email.message}</p>}
        <label htmlFor="phoneNumber" className="required">Phone number</label>
        <input id="phoneNumber" type="tel" inputMode="tel" maxLength={20} aria-invalid={errors.phoneNumber ? 'true' : undefined} {...register('phoneNumber', rules.phone())} />
        {errors.phoneNumber && <p className="field-error">{errors.phoneNumber.message}</p>}
        <label htmlFor="departmentId" className="required">Department</label>
        <select id="departmentId" aria-invalid={errors.departmentId ? 'true' : undefined} {...register('departmentId', rules.selection('a department'))}>
          {departments.length === 0 && <option value="">No departments available</option>}
          {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
        </select>
        {errors.departmentId && <p className="field-error">{errors.departmentId.message}</p>}
        <div className="field-row">
          <div><label htmlFor="specialization" className="required">Specialization</label><input id="specialization" maxLength={150} aria-invalid={errors.specialization ? 'true' : undefined} {...register('specialization', rules.text('Specialization', { isRequired: true, max: 150 }))} />{errors.specialization && <p className="field-error">{errors.specialization.message}</p>}</div>
          <div><label htmlFor="licenseNumber" className="required">License number</label><input id="licenseNumber" maxLength={100} aria-invalid={errors.licenseNumber ? 'true' : undefined} {...register('licenseNumber', rules.text('License number', { isRequired: true, max: 100 }))} />{errors.licenseNumber && <p className="field-error">{errors.licenseNumber.message}</p>}</div>
        </div>
        <label htmlFor="yearsOfExperience" className="required">Years of experience</label>
        <input id="yearsOfExperience" type="number" min="0" max="80" step="1" aria-invalid={errors.yearsOfExperience ? 'true' : undefined} {...register('yearsOfExperience', rules.number('Years of experience', { isRequired: true, min: 0, max: 80, integer: true, unit: 'years' }))} />
        {errors.yearsOfExperience && <p className="field-error">{errors.yearsOfExperience.message}</p>}
        <label htmlFor="bio">Bio</label>
        <input id="bio" maxLength={2000} aria-invalid={errors.bio ? 'true' : undefined} {...register('bio', rules.text('Bio', { max: 2000 }))} />
        {errors.bio && <p className="field-error">{errors.bio.message}</p>}
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Save'}</button>
        </div>
      </form>
    </div>
  </div>
}

export default function DoctorManagement() {
  const { user } = useAuth()
  const role = user?.role === 'DoctorManager' ? 'DoctorManager' : 'Admin'
  const navigation = role === 'DoctorManager' ? doctorManagerNavigation : adminNavigation
  const [doctors, setDoctors] = useState([])
  const [departments, setDepartments] = useState([])
  const [consultationTypes, setConsultationTypes] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editingDoctor, setEditingDoctor] = useState(null)
  const [showForm, setShowForm] = useState(false)
  const [statusTarget, setStatusTarget] = useState(null)
  const [statusBusy, setStatusBusy] = useState(false)
  const [statusMessage, setStatusMessage] = useState(null)
  const [deleteTarget, setDeleteTarget] = useState(null)
  const [deleteBusy, setDeleteBusy] = useState(false)
  const [createdAccount, setCreatedAccount] = useState(null)
  const [accountTarget, setAccountTarget] = useState(null)
  const [accountBusy, setAccountBusy] = useState(false)
  const [accountError, setAccountError] = useState('')

  const [searchTerm, setSearchTerm] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [minExperience, setMinExperience] = useState('')
  const [consultationTypeId, setConsultationTypeId] = useState('')

  const loadDoctors = async () => {
    try {
      setLoading(true)
      const filters = {}
      filters.includeInactive = true
      if (searchTerm.trim()) filters.searchTerm = searchTerm.trim()
      if (departmentId) filters.departmentId = departmentId
      if (minExperience) filters.minExperience = minExperience
      if (consultationTypeId) filters.consultationTypeId = consultationTypeId
      setDoctors(await listDoctors(filters))
      setError('')
    } catch {
      setError('Unable to load doctors.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    listDepartments().then(setDepartments).catch(() => {})
    listConsultationTypes().then(setConsultationTypes).catch(() => {})
  }, [])

  useEffect(() => {
    const timeout = setTimeout(loadDoctors, 300)
    return () => clearTimeout(timeout)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm, departmentId, minExperience, consultationTypeId])

  const handleStatusChange = async () => {
    if (!statusTarget || statusBusy) return
    const { doctor, action } = statusTarget
    setStatusBusy(true)
    setStatusMessage(null)
    try {
      if (action === 'activate') await activateDoctor(doctor.id)
      else await deactivateDoctor(doctor.id)
      const status = action === 'activate' ? 'Active' : 'Inactive'
      setDoctors((current) => current.map((item) => item.id === doctor.id ? { ...item, status } : item))
      setStatusMessage({ type: 'success', text: `Dr. ${doctor.firstName} ${doctor.lastName} has been ${action === 'activate' ? 'activated' : 'deactivated'}.` })
      setStatusTarget(null)
      loadDoctors()
    } catch (requestError) {
      setStatusMessage({ type: 'error', text: requestError.response?.data?.message || `Failed to ${action} doctor.` })
    } finally {
      setStatusBusy(false)
    }
  }

  const handleCreateLogin = async () => {
    if (!accountTarget || accountBusy) return
    setAccountBusy(true)
    setAccountError('')
    try {
      const result = await createDoctorLogin(accountTarget.id)
      setAccountTarget(null)
      setCreatedAccount(result)
      await loadDoctors()
    } catch (requestError) {
      setAccountError(requestError.response?.data?.message || 'Unable to create this doctor login.')
    } finally {
      setAccountBusy(false)
    }
  }

  const handleDeleteDoctor = async () => {
    if (!deleteTarget || deleteBusy) return
    setDeleteBusy(true)
    setStatusMessage(null)
    try {
      await deleteDoctorPermanently(deleteTarget.id)
      setDoctors((current) => current.filter((doctor) => doctor.id !== deleteTarget.id))
      setStatusMessage({ type: 'success', text: `Dr. ${deleteTarget.firstName} ${deleteTarget.lastName} was permanently deleted.` })
      setDeleteTarget(null)
    } catch (requestError) {
      setStatusMessage({ type: 'error', text: requestError.response?.data?.message || 'Failed to delete doctor. Please try again.' })
    } finally {
      setDeleteBusy(false)
    }
  }

  const closeForm = () => { setShowForm(false); setEditingDoctor(null) }
  const handleSaved = (account) => {
    closeForm()
    loadDoctors()
    if (account?.temporaryPassword) setCreatedAccount(account)
  }
  const unlinkedCount = doctors.filter((doctor) => doctor.userId == null).length
  const eligibleUnlinkedCount = doctors.filter((doctor) => doctor.userId == null && doctor.status !== 'Inactive').length

  return <DashboardLayout role={role} navigation={navigation} title="Doctor Management" subtitle="Manage doctor profiles, specializations, and departments.">
    <div className="toolbar">
      <div className="filter-bar">
        <input placeholder="Search by name or ID" maxLength={100} value={searchTerm} onChange={(event) => setSearchTerm(event.target.value)} />
        <select value={departmentId} onChange={(event) => setDepartmentId(event.target.value)}>
          <option value="">All departments</option>
          {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
        </select>
        <select value={consultationTypeId} onChange={(event) => setConsultationTypeId(event.target.value)}>
          <option value="">All consultation types</option>
          {consultationTypes.map((type) => <option key={type.id} value={type.id}>{type.name}</option>)}
        </select>
        <input type="number" min="0" max="80" step="1" placeholder="Min experience (yrs)" value={minExperience} onChange={(event) => setMinExperience(event.target.value.replace(/[^0-9]/g, '').slice(0, 2))} />
      </div>
      <button className="primary-button" style={{ marginTop: 0 }} onClick={() => setShowForm(true)}>+ Add Doctor</button>
    </div>

    {error && <p className="form-error" role="alert">{error}</p>}

    {statusMessage && <p className={statusMessage.type === 'success' ? 'doctor-status-success' : 'form-error'} role={statusMessage.type === 'success' ? 'status' : 'alert'}>{statusMessage.text}</p>}

    {unlinkedCount > 0 && <p className="mb-4 p-3 rounded-lg border bg-amber-50 text-amber-800" role="status">
      {unlinkedCount} doctor profile{unlinkedCount === 1 ? ' is' : 's are'} missing a login in the current results. Review each profile and create accounts individually.
      {eligibleUnlinkedCount < unlinkedCount && ` ${unlinkedCount - eligibleUnlinkedCount} inactive profile${unlinkedCount - eligibleUnlinkedCount === 1 ? ' is' : 's are'} excluded until activated.`}
    </p>}

    <div className="panel table-scroll doctor-management-table-wrap">
      <table className="data-table doctor-management-table">
        <thead>
          <tr><th>Name</th><th>Department</th><th>Specialization</th><th>Experience</th><th>Status</th><th>Login</th><th className="doctor-actions">Actions</th></tr>
        </thead>
        <tbody>
          {doctors.map((doctor) => <tr key={doctor.id}>
            <td>Dr. {doctor.firstName} {doctor.lastName}</td>
            <td>{doctor.departmentName}</td>
            <td>{doctor.specialization}</td>
            <td>{doctor.yearsOfExperience} yrs</td>
            <td><span className={`badge badge-${doctor.status.toLowerCase()}`}>{doctor.status}</span></td>
            <td>{doctor.userId != null ? 'Linked' : doctor.status === 'Inactive' ? 'Not linked (inactive)' : <button className="link-button" onClick={() => { setAccountError(''); setAccountTarget(doctor) }}>Create login</button>}</td>
            <td className="row-actions doctor-actions">
              <button className="link-button" onClick={() => { setEditingDoctor(doctor); setShowForm(true) }}>Edit</button>
              {doctor.status === 'Inactive'
                ? <button className="link-button doctor-activate" onClick={() => { setStatusMessage(null); setStatusTarget({ doctor, action: 'activate' }) }}>Activate</button>
                : <button className="link-button danger" onClick={() => { setStatusMessage(null); setStatusTarget({ doctor, action: 'deactivate' }) }}>Deactivate</button>}
              <button className="link-button danger" onClick={() => { setStatusMessage(null); setDeleteTarget(doctor) }}>Delete</button>
            </td>
          </tr>)}
        </tbody>
      </table>
      {!loading && doctors.length === 0 && <p className="empty-state">No doctors match your search.</p>}
    </div>

    {statusTarget && <div className="modal-overlay" role="alertdialog" aria-modal="true" aria-labelledby="doctor-status-title">
      <div className="modal-card">
        <h2 id="doctor-status-title">{statusTarget.action === 'activate' ? 'Activate Doctor' : 'Deactivate Doctor'}</h2>
        <p>{statusTarget.action === 'activate'
          ? <>Activate <strong>Dr. {statusTarget.doctor.firstName} {statusTarget.doctor.lastName}</strong>? They will appear in appointment booking again.</>
          : <>Deactivate <strong>Dr. {statusTarget.doctor.firstName} {statusTarget.doctor.lastName}</strong>? They will no longer appear in appointment booking.</>}</p>
        <div className="modal-actions">
          <button type="button" className="secondary-button" disabled={statusBusy} onClick={() => setStatusTarget(null)}>Cancel</button>
          <button type="button" className={statusTarget.action === 'activate' ? 'primary-button doctor-activate' : 'primary-button doctor-deactivate'} disabled={statusBusy} onClick={handleStatusChange}>
            {statusBusy ? (statusTarget.action === 'activate' ? 'Activating...' : 'Deactivating...') : (statusTarget.action === 'activate' ? 'Activate' : 'Deactivate')}
          </button>
        </div>
      </div>
    </div>}

    {showForm && <DoctorFormModal doctor={editingDoctor} departments={departments} onClose={closeForm} onSaved={handleSaved} />}

    {accountTarget && <div className="modal-overlay" role="alertdialog" aria-modal="true" aria-labelledby="doctor-login-confirm-title">
      <div className="modal-card">
        <h2 id="doctor-login-confirm-title">Create doctor login</h2>
        <p>Create a Doctor login for <strong>Dr. {accountTarget.firstName} {accountTarget.lastName}</strong> using <strong>{accountTarget.email}</strong>?</p>
        <p>The generated initial password will be shown once after creation. Make sure you can give it directly to the doctor.</p>
        {accountError && <p className="form-error" role="alert">{accountError}</p>}
        <div className="modal-actions">
          <button type="button" className="secondary-button" disabled={accountBusy} onClick={() => setAccountTarget(null)}>Cancel</button>
          <button type="button" className="primary-button" disabled={accountBusy} onClick={handleCreateLogin}>{accountBusy ? 'Creating...' : 'Create login'}</button>
        </div>
      </div>
    </div>}

    {deleteTarget && <div className="modal-overlay" role="alertdialog" aria-modal="true" aria-labelledby="doctor-delete-title">
      <div className="modal-card">
        <h2 id="doctor-delete-title">Delete Doctor</h2>
        <p>Are you sure you want to permanently delete <strong>Dr. {deleteTarget.firstName} {deleteTarget.lastName}</strong>? This removes their doctor profile and schedule records.</p>
        <p>The linked login will be disabled and retained for historical records.</p>
        <div className="modal-actions">
          <button type="button" className="secondary-button" disabled={deleteBusy} onClick={() => setDeleteTarget(null)}>Cancel</button>
          <button type="button" className="primary-button doctor-deactivate" disabled={deleteBusy} onClick={handleDeleteDoctor}>{deleteBusy ? 'Deleting...' : 'Delete permanently'}</button>
        </div>
      </div>
    </div>}

    {createdAccount && <div className="modal-overlay" role="dialog" aria-modal="true" aria-labelledby="doctor-account-created-title">
      <div className="modal-card">
        <h2 id="doctor-account-created-title">Doctor account created</h2>
        <p>Dr. {createdAccount.doctor.firstName} {createdAccount.doctor.lastName} can sign in with this email and initial password.</p>
        <label htmlFor="doctor-login-email">Login email</label>
        <input id="doctor-login-email" readOnly value={createdAccount.doctor.email} />
        <label htmlFor="doctor-initial-password">Initial password — shown once</label>
        <input id="doctor-initial-password" readOnly value={createdAccount.temporaryPassword} />
        <p className="text-sm">Copy and give these credentials directly to the doctor. This password will not be shown again after closing this dialog.</p>
        <div className="modal-actions">
          <button type="button" className="primary-button" onClick={() => setCreatedAccount(null)}>Done</button>
        </div>
      </div>
    </div>}
  </DashboardLayout>
}
