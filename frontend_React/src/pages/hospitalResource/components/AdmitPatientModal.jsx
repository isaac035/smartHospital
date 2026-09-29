import { useState, useEffect, useRef, useMemo, useCallback } from 'react'
import { createAdmission } from '../../../services/hospitalResourceService'
import { listUsers } from '../../../services/userService'
import { listDoctors } from '../../../services/doctorService'

function getInitials(name) {
  if (!name) return '?'
  const parts = name.trim().split(/\s+/)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

export default function AdmitPatientModal({ isOpen, onClose, onSuccess }) {
  const [formData, setFormData] = useState({
    patientId: '',
    admittingDoctorId: '',
    priority: 1, // 1: Normal, 2: Urgent, 3: Emergency
    reasonForAdmission: '',
    diagnosis: '',
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  // Directory state
  const [patients, setPatients] = useState([])
  const [doctors, setDoctors] = useState([])
  const [loadingDirectory, setLoadingDirectory] = useState(false)
  const [directoryError, setDirectoryError] = useState(null)

  // Selection & Search state
  const [selectedPatient, setSelectedPatient] = useState(null)
  const [selectedDoctor, setSelectedDoctor] = useState(null)
  const [patientSearch, setPatientSearch] = useState('')
  const [doctorSearch, setDoctorSearch] = useState('')
  const [patientDropdownOpen, setPatientDropdownOpen] = useState(false)
  const [doctorDropdownOpen, setDoctorDropdownOpen] = useState(false)
  const [patientHighlightedIndex, setPatientHighlightedIndex] = useState(-1)
  const [doctorHighlightedIndex, setDoctorHighlightedIndex] = useState(-1)

  const patientDropdownRef = useRef(null)
  const doctorDropdownRef = useRef(null)
  const patientInputRef = useRef(null)
  const doctorInputRef = useRef(null)

  // Fetch directory of patients and doctors
  const fetchDirectory = useCallback(async () => {
    try {
      setLoadingDirectory(true)
      setDirectoryError(null)

      const [usersData, doctorsData] = await Promise.all([
        listUsers(),
        listDoctors().catch(() => []),
      ])

      const rawUsers = Array.isArray(usersData) ? usersData : []
      const rawDoctors = Array.isArray(doctorsData) ? doctorsData : []

      // 1. Filter Patient-role users only
      const patientList = rawUsers
        .filter((u) => u.role?.toLowerCase() === 'patient')
        .map((u) => {
          const firstName = (u.firstName || '').trim()
          const lastName = (u.lastName || '').trim()
          const fullName = `${firstName} ${lastName}`.trim() || `Patient #${u.id}`
          return {
            id: u.id, // Local Users.Id
            firstName,
            lastName,
            fullName,
            email: (u.email || '').trim(),
            phone: (u.phoneNumber || '').trim(),
          }
        })

      // 2. Build doctor profiles lookup (by userId and email)
      const docProfileMap = new Map()
      rawDoctors.forEach((d) => {
        if (d.userId != null) {
          docProfileMap.set(`id_${d.userId}`, d)
        }
        if (d.email) {
          docProfileMap.set(`email_${d.email.toLowerCase().trim()}`, d)
        }
      })

      // 3. Filter Doctor-role users and enrich with specialty & department
      const doctorList = rawUsers
        .filter((u) => u.role?.toLowerCase() === 'doctor')
        .map((u) => {
          const firstName = (u.firstName || '').trim()
          const lastName = (u.lastName || '').trim()
          const fullName = `Dr. ${firstName} ${lastName}`.trim() || `Doctor #${u.id}`
          const profile =
            docProfileMap.get(`id_${u.id}`) ||
            (u.email ? docProfileMap.get(`email_${u.email.toLowerCase().trim()}`) : null)

          return {
            id: u.id, // Local Users.Id
            firstName,
            lastName,
            fullName,
            email: (u.email || profile?.email || '').trim(),
            phone: (u.phoneNumber || profile?.phoneNumber || '').trim(),
            specialty: (profile?.specialization || '').trim(),
            department: (profile?.departmentName || '').trim(),
          }
        })

      // Also ensure any doctor from rawDoctors that has a valid userId not yet in doctorList is included
      rawDoctors.forEach((doc) => {
        if (doc.userId != null && !doctorList.some((d) => d.id === doc.userId)) {
          const firstName = (doc.firstName || '').trim()
          const lastName = (doc.lastName || '').trim()
          doctorList.push({
            id: doc.userId, // Local Users.Id
            firstName,
            lastName,
            fullName: `Dr. ${firstName} ${lastName}`.trim() || `Doctor #${doc.userId}`,
            email: (doc.email || '').trim(),
            phone: (doc.phoneNumber || '').trim(),
            specialty: (doc.specialization || '').trim(),
            department: (doc.departmentName || '').trim(),
          })
        }
      })

      setPatients(patientList)
      setDoctors(doctorList)
    } catch (err) {
      setDirectoryError(
        err.response?.data?.message || 'Failed to load user directory. Please try again.'
      )
    } finally {
      setLoadingDirectory(false)
    }
  }, [])

  // Initialize or reset form state when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setFormData({
        patientId: '',
        admittingDoctorId: '',
        priority: 1,
        reasonForAdmission: '',
        diagnosis: '',
      })
      setSelectedPatient(null)
      setSelectedDoctor(null)
      setPatientSearch('')
      setDoctorSearch('')
      setPatientDropdownOpen(false)
      setDoctorDropdownOpen(false)
      setPatientHighlightedIndex(-1)
      setDoctorHighlightedIndex(-1)
      setError(null)
      fetchDirectory()
    }
  }, [isOpen, fetchDirectory])

  // Dismiss dropdowns on outside click
  useEffect(() => {
    const handleOutsideClick = (e) => {
      if (patientDropdownRef.current && !patientDropdownRef.current.contains(e.target)) {
        setPatientDropdownOpen(false)
      }
      if (doctorDropdownRef.current && !doctorDropdownRef.current.contains(e.target)) {
        setDoctorDropdownOpen(false)
      }
    }
    document.addEventListener('mousedown', handleOutsideClick)
    return () => document.removeEventListener('mousedown', handleOutsideClick)
  }, [])

  // Filtered patients for search
  const filteredPatients = useMemo(() => {
    const q = patientSearch.trim().toLowerCase()
    if (!q) return patients
    return patients.filter(
      (p) =>
        p.fullName.toLowerCase().includes(q) ||
        p.email.toLowerCase().includes(q) ||
        p.phone.toLowerCase().includes(q)
    )
  }, [patients, patientSearch])

  // Filtered doctors for search
  const filteredDoctors = useMemo(() => {
    const q = doctorSearch.trim().toLowerCase()
    if (!q) return doctors
    return doctors.filter(
      (d) =>
        d.fullName.toLowerCase().includes(q) ||
        d.specialty.toLowerCase().includes(q) ||
        d.department.toLowerCase().includes(q) ||
        d.email.toLowerCase().includes(q)
    )
  }, [doctors, doctorSearch])

  const handleSelectPatient = (patient) => {
    setSelectedPatient(patient)
    setFormData((prev) => ({ ...prev, patientId: String(patient.id) }))
    setPatientSearch('')
    setPatientDropdownOpen(false)
    setPatientHighlightedIndex(-1)
  }

  const handleClearPatient = () => {
    setSelectedPatient(null)
    setFormData((prev) => ({ ...prev, patientId: '' }))
    setPatientSearch('')
    setPatientHighlightedIndex(-1)
    setTimeout(() => {
      patientInputRef.current?.focus()
    }, 50)
  }

  const handleSelectDoctor = (doctor) => {
    setSelectedDoctor(doctor)
    setFormData((prev) => ({ ...prev, admittingDoctorId: String(doctor.id) }))
    setDoctorSearch('')
    setDoctorDropdownOpen(false)
    setDoctorHighlightedIndex(-1)
  }

  const handleClearDoctor = () => {
    setSelectedDoctor(null)
    setFormData((prev) => ({ ...prev, admittingDoctorId: '' }))
    setDoctorSearch('')
    setDoctorHighlightedIndex(-1)
    setTimeout(() => {
      doctorInputRef.current?.focus()
    }, 50)
  }

  const handlePatientKeyDown = (e) => {
    if (!patientDropdownOpen) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault()
        setPatientDropdownOpen(true)
        return
      }
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setPatientHighlightedIndex((prev) =>
        filteredPatients.length === 0 ? -1 : (prev + 1) % filteredPatients.length
      )
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setPatientHighlightedIndex((prev) =>
        filteredPatients.length === 0
          ? -1
          : (prev - 1 + filteredPatients.length) % filteredPatients.length
      )
    } else if (e.key === 'Enter') {
      if (
        patientDropdownOpen &&
        patientHighlightedIndex >= 0 &&
        filteredPatients[patientHighlightedIndex]
      ) {
        e.preventDefault()
        handleSelectPatient(filteredPatients[patientHighlightedIndex])
      }
    } else if (e.key === 'Escape') {
      e.preventDefault()
      setPatientDropdownOpen(false)
      setPatientHighlightedIndex(-1)
    }
  }

  const handleDoctorKeyDown = (e) => {
    if (!doctorDropdownOpen) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault()
        setDoctorDropdownOpen(true)
        return
      }
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setDoctorHighlightedIndex((prev) =>
        filteredDoctors.length === 0 ? -1 : (prev + 1) % filteredDoctors.length
      )
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setDoctorHighlightedIndex((prev) =>
        filteredDoctors.length === 0
          ? -1
          : (prev - 1 + filteredDoctors.length) % filteredDoctors.length
      )
    } else if (e.key === 'Enter') {
      if (
        doctorDropdownOpen &&
        doctorHighlightedIndex >= 0 &&
        filteredDoctors[doctorHighlightedIndex]
      ) {
        e.preventDefault()
        handleSelectDoctor(filteredDoctors[doctorHighlightedIndex])
      }
    } else if (e.key === 'Escape') {
      e.preventDefault()
      setDoctorDropdownOpen(false)
      setDoctorHighlightedIndex(-1)
    }
  }

  const handleChange = (e) => {
    const { name, value } = e.target
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'priority' ? parseInt(value, 10) : value,
    }))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    const parsedPatientId = parseInt(formData.patientId, 10)
    if (isNaN(parsedPatientId) || parsedPatientId <= 0) {
      setError('Please search and select a patient.')
      return
    }

    const parsedDoctorId = parseInt(formData.admittingDoctorId, 10)
    if (isNaN(parsedDoctorId) || parsedDoctorId <= 0) {
      setError('Please select an admitting doctor.')
      return
    }

    if (!formData.reasonForAdmission.trim()) {
      setError('Reason for admission is required.')
      return
    }

    const payload = {
      patientId: parsedPatientId,
      admittingDoctorId: parsedDoctorId,
      priority: formData.priority,
      reasonForAdmission: formData.reasonForAdmission.trim(),
      diagnosis: formData.diagnosis.trim() || null,
    }

    try {
      setLoading(true)
      await createAdmission(payload)
      onSuccess('Patient admission created successfully.')
      onClose()
    } catch (err) {
      const msg =
        err.response?.data?.message ||
        'Failed to create admission. Verify the selected Patient and Doctor.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 overflow-y-auto">
      <div
        className="w-full max-w-lg sm:max-w-xl my-8 p-6 rounded-2xl shadow-2xl relative"
        style={{
          background: 'var(--color-primary)',
          border: '1px solid color-mix(in srgb, var(--color-secondary) 18%, var(--color-primary))',
        }}
      >
        <div
          className="flex items-center justify-between pb-4 mb-4 border-b"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}
        >
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Admit Patient
            </h2>
            <p
              className="text-xs mt-1"
              style={{ color: 'color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))' }}
            >
              Register a new patient admission and initial diagnosis.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-2xl leading-none font-bold"
            aria-label="Close"
          >
            &times;
          </button>
        </div>

        {error && <div className="form-error mb-4">{error}</div>}

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {/* Patient Selector */}
            <div ref={patientDropdownRef} className={`relative ${patientDropdownOpen ? 'z-40' : 'z-10'}`}>
              <label htmlFor="admit-patient-search" className="block text-xs font-semibold mb-1">
                Patient <span className="text-red-500">*</span>
              </label>

              {selectedPatient ? (
                <div
                  className="flex items-center justify-between p-2 text-sm border rounded-lg"
                  style={{
                    borderColor: 'color-mix(in srgb, var(--color-accent) 30%, var(--color-primary))',
                    background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))',
                  }}
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <div
                      className="w-7 h-7 rounded-full flex items-center justify-center shrink-0 text-xs font-bold text-white shadow-sm"
                      style={{ background: 'var(--color-accent)' }}
                    >
                      {getInitials(selectedPatient.fullName)}
                    </div>
                    <div className="min-w-0">
                      <div className="font-semibold text-xs truncate" style={{ color: 'var(--color-accent)' }}>
                        {selectedPatient.fullName}
                      </div>
                      <div className="text-[11px] opacity-70 truncate">
                        {[selectedPatient.email, selectedPatient.phone].filter(Boolean).join(' • ') || 'Patient'}
                      </div>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={handleClearPatient}
                    className="text-xs font-medium px-2 py-0.5 rounded hover:bg-black/5 transition-colors shrink-0 ml-1.5"
                    style={{ color: 'var(--color-accent)' }}
                    title="Change selected patient"
                  >
                    Change
                  </button>
                </div>
              ) : (
                <div className="relative">
                  <input
                    ref={patientInputRef}
                    id="admit-patient-search"
                    type="text"
                    value={patientSearch}
                    onChange={(e) => {
                      setPatientSearch(e.target.value)
                      setPatientDropdownOpen(true)
                      setPatientHighlightedIndex(0)
                    }}
                    onFocus={() => {
                      setPatientDropdownOpen(true)
                    }}
                    onKeyDown={handlePatientKeyDown}
                    placeholder="Search by name, email or phone"
                    autoComplete="off"
                    className="w-full !pl-9 pr-7 py-2 text-sm border rounded-lg outline-none"
                    style={{
                      paddingLeft: '2.25rem',
                      borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                    }}
                  />
                  <svg
                    className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                    />
                  </svg>
                  {patientSearch && (
                    <button
                      type="button"
                      onClick={() => {
                        setPatientSearch('')
                        patientInputRef.current?.focus()
                      }}
                      className="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 text-sm font-bold"
                      aria-label="Clear patient search"
                    >
                      &times;
                    </button>
                  )}
                </div>
              )}

              {/* Dropdown list */}
              {patientDropdownOpen && !selectedPatient && (
                <div
                  className="absolute left-0 right-0 top-full mt-1 z-50 max-h-44 overflow-y-auto rounded-lg shadow-2xl border"
                  style={{
                    backgroundColor: '#ffffff',
                    background: 'var(--color-primary, #ffffff)',
                    borderColor: 'color-mix(in srgb, var(--color-secondary) 22%, var(--color-primary))',
                  }}
                >
                  {loadingDirectory ? (
                    <div className="p-3 text-xs text-center text-gray-500 flex items-center justify-center gap-2">
                      <span className="inline-block w-3 h-3 border-2 border-current border-t-transparent rounded-full animate-spin" />
                      Loading patients...
                    </div>
                  ) : directoryError ? (
                    <div className="p-3 text-xs text-center text-red-600">
                      <div>{directoryError}</div>
                      <button
                        type="button"
                        onClick={fetchDirectory}
                        className="mt-1 text-xs underline font-semibold hover:opacity-80"
                      >
                        Retry
                      </button>
                    </div>
                  ) : filteredPatients.length === 0 ? (
                    <div className="p-3 text-xs text-center text-gray-500">
                      {patientSearch.trim()
                        ? `No patients found matching "${patientSearch.trim()}".`
                        : 'No patient accounts available.'}
                    </div>
                  ) : (
                    filteredPatients.map((patient, idx) => (
                      <div
                        key={patient.id}
                        role="option"
                        aria-selected={idx === patientHighlightedIndex}
                        onClick={() => handleSelectPatient(patient)}
                        onMouseEnter={() => setPatientHighlightedIndex(idx)}
                        className="px-2.5 py-1.5 cursor-pointer border-b last:border-b-0 transition-colors"
                        style={{
                          background:
                            idx === patientHighlightedIndex
                              ? 'color-mix(in srgb, var(--color-accent) 8%, var(--color-primary))'
                              : 'transparent',
                          borderColor: 'color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))',
                        }}
                      >
                        <div className="font-semibold text-xs leading-tight" style={{ color: 'var(--color-accent)' }}>
                          {patient.fullName}
                        </div>
                        {patient.email && (
                          <div className="text-[11px] opacity-75 leading-tight truncate">
                            {patient.email}
                          </div>
                        )}
                        {patient.phone && (
                          <div className="text-[11px] opacity-60 leading-tight truncate">
                            {patient.phone}
                          </div>
                        )}
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>

            {/* Doctor Selector */}
            <div ref={doctorDropdownRef} className={`relative ${doctorDropdownOpen ? 'z-40' : 'z-10'}`}>
              <label htmlFor="admit-doctor-search" className="block text-xs font-semibold mb-1">
                Admitting Doctor <span className="text-red-500">*</span>
              </label>

              {selectedDoctor ? (
                <div
                  className="flex items-center justify-between p-2 text-sm border rounded-lg"
                  style={{
                    borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                    background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
                  }}
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <div
                      className="w-7 h-7 rounded-full flex items-center justify-center shrink-0 text-[11px] font-bold"
                      style={{
                        background: 'color-mix(in srgb, var(--color-accent) 15%, var(--color-primary))',
                        color: 'var(--color-accent)',
                      }}
                    >
                      Dr
                    </div>
                    <div className="min-w-0">
                      <div className="font-semibold text-xs truncate" style={{ color: 'var(--color-accent)' }}>
                        {selectedDoctor.fullName}
                      </div>
                      <div className="text-[11px] opacity-70 truncate">
                        {[selectedDoctor.specialty, selectedDoctor.department].filter(Boolean).join(' • ') || 'Doctor'}
                      </div>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={handleClearDoctor}
                    className="text-xs font-medium px-2 py-0.5 rounded hover:bg-black/5 transition-colors shrink-0 ml-1.5"
                    style={{ color: 'var(--color-accent)' }}
                    title="Change selected doctor"
                  >
                    Change
                  </button>
                </div>
              ) : (
                <div className="relative">
                  <input
                    ref={doctorInputRef}
                    id="admit-doctor-search"
                    type="text"
                    value={doctorSearch}
                    onChange={(e) => {
                      setDoctorSearch(e.target.value)
                      setDoctorDropdownOpen(true)
                      setDoctorHighlightedIndex(0)
                    }}
                    onFocus={() => {
                      setDoctorDropdownOpen(true)
                    }}
                    onKeyDown={handleDoctorKeyDown}
                    placeholder="Search by name, specialty or dept"
                    autoComplete="off"
                    className="w-full !pl-9 pr-7 py-2 text-sm border rounded-lg outline-none"
                    style={{
                      paddingLeft: '2.25rem',
                      borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                    }}
                  />
                  <svg
                    className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                    />
                  </svg>
                  {doctorSearch && (
                    <button
                      type="button"
                      onClick={() => {
                        setDoctorSearch('')
                        doctorInputRef.current?.focus()
                      }}
                      className="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 text-sm font-bold"
                      aria-label="Clear doctor search"
                    >
                      &times;
                    </button>
                  )}
                </div>
              )}

              {/* Dropdown list */}
              {doctorDropdownOpen && !selectedDoctor && (
                <div
                  className="absolute left-0 right-0 top-full mt-1 z-50 max-h-44 overflow-y-auto rounded-lg shadow-2xl border"
                  style={{
                    backgroundColor: '#ffffff',
                    background: 'var(--color-primary, #ffffff)',
                    borderColor: 'color-mix(in srgb, var(--color-secondary) 22%, var(--color-primary))',
                  }}
                >
                  {loadingDirectory ? (
                    <div className="p-3 text-xs text-center text-gray-500 flex items-center justify-center gap-2">
                      <span className="inline-block w-3 h-3 border-2 border-current border-t-transparent rounded-full animate-spin" />
                      Loading doctors...
                    </div>
                  ) : directoryError ? (
                    <div className="p-3 text-xs text-center text-red-600">
                      <div>{directoryError}</div>
                      <button
                        type="button"
                        onClick={fetchDirectory}
                        className="mt-1 text-xs underline font-semibold hover:opacity-80"
                      >
                        Retry
                      </button>
                    </div>
                  ) : filteredDoctors.length === 0 ? (
                    <div className="p-3 text-xs text-center text-gray-500">
                      {doctorSearch.trim()
                        ? `No doctors found matching "${doctorSearch.trim()}".`
                        : 'No doctor accounts available.'}
                    </div>
                  ) : (
                    filteredDoctors.map((doc, idx) => (
                      <div
                        key={doc.id}
                        role="option"
                        aria-selected={idx === doctorHighlightedIndex}
                        onClick={() => handleSelectDoctor(doc)}
                        onMouseEnter={() => setDoctorHighlightedIndex(idx)}
                        className="px-2.5 py-1.5 cursor-pointer border-b last:border-b-0 transition-colors"
                        style={{
                          background:
                            idx === doctorHighlightedIndex
                              ? 'color-mix(in srgb, var(--color-accent) 8%, var(--color-primary))'
                              : 'transparent',
                          borderColor: 'color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))',
                        }}
                      >
                        <div className="font-semibold text-xs leading-tight" style={{ color: 'var(--color-accent)' }}>
                          {doc.fullName}
                        </div>
                        {(doc.specialty || doc.department) && (
                          <div className="text-[11px] opacity-75 leading-tight truncate">
                            {[doc.specialty, doc.department].filter(Boolean).join(' • ')}
                          </div>
                        )}
                        {doc.email && (
                          <div className="text-[11px] opacity-60 leading-tight truncate">
                            {doc.email}
                          </div>
                        )}
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </div>

          <div>
            <label htmlFor="admit-priority" className="block text-xs font-semibold mb-1">
              Priority <span className="text-red-500">*</span>
            </label>
            <select
              id="admit-priority"
              name="priority"
              value={formData.priority}
              onChange={handleChange}
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none bg-transparent"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))',
                color: 'var(--color-secondary)',
              }}
            >
              <option value={1}>Normal</option>
              <option value={2}>Urgent</option>
              <option value={3}>Emergency</option>
            </select>
          </div>

          <div>
            <label htmlFor="admit-reason" className="block text-xs font-semibold mb-1">
              Reason for Admission <span className="text-red-500">*</span>
            </label>
            <textarea
              id="admit-reason"
              name="reasonForAdmission"
              required
              maxLength={500}
              rows={3}
              value={formData.reasonForAdmission}
              onChange={handleChange}
              placeholder="Primary symptoms or reason for inpatient stay (max 500 characters)"
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            />
          </div>

          <div>
            <label htmlFor="admit-diagnosis" className="block text-xs font-semibold mb-1">
              Initial Diagnosis <span className="text-xs font-normal opacity-70">(optional)</span>
            </label>
            <textarea
              id="admit-diagnosis"
              name="diagnosis"
              maxLength={1000}
              rows={2}
              value={formData.diagnosis}
              onChange={handleChange}
              placeholder="Clinical diagnosis notes (max 1000 characters)"
              className="w-full px-3 py-2 text-sm border rounded-lg outline-none"
              style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 25%, var(--color-primary))' }}
            />
          </div>

          <div
            className="flex justify-end gap-2 mt-4 pt-3 border-t"
            style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}
          >
            <button
              type="button"
              disabled={loading}
              onClick={onClose}
              className="secondary-button text-sm px-4 py-2"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="primary-button text-sm px-5 py-2"
              style={{ marginTop: 0 }}
            >
              {loading ? 'Submitting...' : 'Admit Patient'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

