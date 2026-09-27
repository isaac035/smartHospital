import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import DashboardLayout from '../../layouts/DashboardLayout'
import { adminNavigation as navigation } from './adminNavigation'
import { listDepartments } from '../../services/departmentService'
import { listConsultationTypes } from '../../services/consultationTypeService'
import { createDoctor, deactivateDoctor, listDoctors, updateDoctor } from '../../services/doctorService'

function DoctorFormModal({ doctor, departments, onClose, onSaved }) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm({
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
      if (doctor) await updateDoctor(doctor.id, payload)
      else await createDoctor(payload)
      onSaved()
    } catch (requestError) {
      setError('root', { message: requestError.response?.data?.message || 'Something went wrong. Please try again.' })
    }
  }

  return <div className="modal-overlay" role="dialog" aria-modal="true">
    <div className="modal-card">
      <h2>{doctor ? 'Edit Doctor' : 'Add Doctor'}</h2>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {errors.root && <p className="form-error" role="alert">{errors.root.message}</p>}
        <div className="field-row">
          <div><label htmlFor="firstName">First name</label><input id="firstName" {...register('firstName', { required: 'Required.' })} />{errors.firstName && <p className="field-error">{errors.firstName.message}</p>}</div>
          <div><label htmlFor="lastName">Last name</label><input id="lastName" {...register('lastName', { required: 'Required.' })} />{errors.lastName && <p className="field-error">{errors.lastName.message}</p>}</div>
        </div>
        <label htmlFor="email">Email</label>
        <input id="email" type="email" {...register('email', { required: 'Required.' })} />
        {errors.email && <p className="field-error">{errors.email.message}</p>}
        <label htmlFor="phoneNumber">Phone number</label>
        <input id="phoneNumber" {...register('phoneNumber', { required: 'Required.' })} />
        <label htmlFor="departmentId">Department</label>
        <select id="departmentId" {...register('departmentId', { required: true })}>
          {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
        </select>
        <div className="field-row">
          <div><label htmlFor="specialization">Specialization</label><input id="specialization" {...register('specialization', { required: 'Required.' })} /></div>
          <div><label htmlFor="licenseNumber">License number</label><input id="licenseNumber" {...register('licenseNumber', { required: 'Required.' })} /></div>
        </div>
        <label htmlFor="yearsOfExperience">Years of experience</label>
        <input id="yearsOfExperience" type="number" min="0" {...register('yearsOfExperience')} />
        <label htmlFor="bio">Bio</label>
        <input id="bio" {...register('bio')} />
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button type="submit" className="primary-button" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Save'}</button>
        </div>
      </form>
    </div>
  </div>
}

export default function DoctorManagement() {
  const [doctors, setDoctors] = useState([])
  const [departments, setDepartments] = useState([])
  const [consultationTypes, setConsultationTypes] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editingDoctor, setEditingDoctor] = useState(null)
  const [showForm, setShowForm] = useState(false)

  const [searchTerm, setSearchTerm] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [minExperience, setMinExperience] = useState('')
  const [consultationTypeId, setConsultationTypeId] = useState('')

  const loadDoctors = async () => {
    try {
      setLoading(true)
      const filters = {}
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

  const handleDeactivate = async (id) => {
    if (!window.confirm('Deactivate this doctor?')) return
    await deactivateDoctor(id)
    loadDoctors()
  }

  const closeForm = () => { setShowForm(false); setEditingDoctor(null) }
  const handleSaved = () => { closeForm(); loadDoctors() }

  return <DashboardLayout role="Admin" navigation={navigation} title="Doctor Management" subtitle="Manage doctor profiles, specializations, and departments.">
    <div className="toolbar">
      <div className="filter-bar">
        <input placeholder="Search by name or ID" value={searchTerm} onChange={(event) => setSearchTerm(event.target.value)} />
        <select value={departmentId} onChange={(event) => setDepartmentId(event.target.value)}>
          <option value="">All departments</option>
          {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
        </select>
        <select value={consultationTypeId} onChange={(event) => setConsultationTypeId(event.target.value)}>
          <option value="">All consultation types</option>
          {consultationTypes.map((type) => <option key={type.id} value={type.id}>{type.name}</option>)}
        </select>
        <input type="number" min="0" placeholder="Min experience (yrs)" value={minExperience} onChange={(event) => setMinExperience(event.target.value)} />
      </div>
      <button className="primary-button" style={{ marginTop: 0 }} onClick={() => setShowForm(true)}>+ Add Doctor</button>
    </div>

    {error && <p className="form-error" role="alert">{error}</p>}

    <div className="panel table-scroll">
      <table className="data-table">
        <thead>
          <tr><th>Name</th><th>Department</th><th>Specialization</th><th>Experience</th><th>Status</th><th></th></tr>
        </thead>
        <tbody>
          {doctors.map((doctor) => <tr key={doctor.id}>
            <td>Dr. {doctor.firstName} {doctor.lastName}</td>
            <td>{doctor.departmentName}</td>
            <td>{doctor.specialization}</td>
            <td>{doctor.yearsOfExperience} yrs</td>
            <td><span className={`badge badge-${doctor.status.toLowerCase()}`}>{doctor.status}</span></td>
            <td className="row-actions">
              <button className="link-button" onClick={() => { setEditingDoctor(doctor); setShowForm(true) }}>Edit</button>
              {doctor.status !== 'Inactive' && <button className="link-button danger" onClick={() => handleDeactivate(doctor.id)}>Deactivate</button>}
            </td>
          </tr>)}
        </tbody>
      </table>
      {!loading && doctors.length === 0 && <p className="empty-state">No doctors match your search.</p>}
    </div>

    {showForm && <DoctorFormModal doctor={editingDoctor} departments={departments} onClose={closeForm} onSaved={handleSaved} />}
  </DashboardLayout>
}
