import { useState } from 'react'
import {
  createMedicalRecord,
  recordVitalSign,
  addMedicalRecordDiagnosis,
  addMedicalRecordTreatmentPlan,
  createPrescription,
  createLabOrder,
  recordLabReport,
  updateMedicalRecord,
} from '../../services/emrService'
import ConfirmationModal from './ConfirmationModal'

const STEPS = [
  { id: 1, name: 'Medical Record' },
  { id: 2, name: 'Vital Signs' },
  { id: 3, name: 'Diagnosis' },
  { id: 4, name: 'Treatment Plan' },
  { id: 5, name: 'Prescription' },
  { id: 6, name: 'Lab Order' },
  { id: 7, name: 'Lab Report' },
  { id: 8, name: 'Follow-up' },
]

export default function ClinicalVisitWorkflowModal({
  patientId,
  patientName = '',
  appointmentId = null,
  onClose,
  onCompleted,
}) {
  const [currentStep, setCurrentStep] = useState(1)
  const [medicalRecordId, setMedicalRecordId] = useState(null)
  const [createdLabOrderId, setCreatedLabOrderId] = useState(null)
  const [stepSuccessMsg, setStepSuccessMsg] = useState('')
  const [stepErrorMsg, setStepErrorMsg] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [showFinalizeConfirm, setShowFinalizeConfirm] = useState(false)

  // Step 1: Medical Record State
  const [chiefComplaint, setChiefComplaint] = useState('')
  const [symptoms, setSymptoms] = useState('')
  const [examinationNotes, setExaminationNotes] = useState('')
  const [initialDiagnosis, setInitialDiagnosis] = useState('')

  // Step 2: Vitals State
  const [temp, setTemp] = useState('')
  const [sysBP, setSysBP] = useState('')
  const [diaBP, setDiaBP] = useState('')
  const [heartRate, setHeartRate] = useState('')
  const [respRate, setRespRate] = useState('')
  const [spO2, setSpO2] = useState('')
  const [weight, setWeight] = useState('')
  const [height, setHeight] = useState('')
  const [vitalsNotes, setVitalsNotes] = useState('')

  // Step 3: Diagnosis State
  const [diagCode, setDiagCode] = useState('')
  const [diagDescription, setDiagDescription] = useState('')
  const [diagType, setDiagType] = useState(1) // Primary
  const [diagStatus, setDiagStatus] = useState(1) // Active
  const [diagSeverity, setDiagSeverity] = useState(2) // Moderate
  const [diagNotes, setDiagNotes] = useState('')

  // Step 4: Treatment Plan State
  const [planTitle, setPlanTitle] = useState('')
  const [planCategory, setPlanCategory] = useState(1) // General
  const [planDescription, setPlanDescription] = useState('')
  const [planGoals, setPlanGoals] = useState('')
  const [planInterventions, setPlanInterventions] = useState('')
  const [planTargetDate, setPlanTargetDate] = useState('')

  // Step 5: Prescription State
  const [rxItems, setRxItems] = useState([
    { medicineName: '', dosage: '', route: 'Oral', frequency: 'Once daily', durationDays: 7, specialInstructions: '' },
  ])
  const [rxInstructions, setRxInstructions] = useState('')

  // Step 6: Lab Order State
  const [labTestName, setLabTestName] = useState('')
  const [labCategory, setLabCategory] = useState('Hematology')
  const [labPriority, setLabPriority] = useState(1) // Routine
  const [labNotes, setLabNotes] = useState('')

  // Step 7: Lab Report State
  const [reportSummary, setReportSummary] = useState('')
  const [reportFindings, setReportFindings] = useState('')
  const [reportRange, setReportRange] = useState('')
  const [reportRemarks, setReportRemarks] = useState('')

  // Step 8: Follow-up State
  const [followUpDate, setFollowUpDate] = useState('')
  const [finalAdvice, setFinalAdvice] = useState('')

  const clearMessages = () => {
    setStepSuccessMsg('')
    setStepErrorMsg('')
  }

  // --- Step 1 Submit: Create Medical Record ---
  const handleStep1Submit = async (e) => {
    e.preventDefault()
    clearMessages()

    if (!chiefComplaint.trim()) {
      setStepErrorMsg('Chief complaint is required.')
      return
    }
    if (!initialDiagnosis.trim()) {
      setStepErrorMsg('Primary clinical diagnosis is required.')
      return
    }

    try {
      setSubmitting(true)
      const res = await createMedicalRecord({
        patientId: Number(patientId),
        appointmentId: appointmentId ? Number(appointmentId) : null,
        chiefComplaint: chiefComplaint.trim(),
        symptoms: symptoms.trim() || undefined,
        examinationNotes: examinationNotes.trim() || undefined,
        diagnosis: initialDiagnosis.trim(),
        treatmentPlan: 'In consultation workflow',
      })

      setMedicalRecordId(res.id)
      setDiagDescription(initialDiagnosis.trim())
      setPlanTitle(`Treatment for ${initialDiagnosis.trim()}`)
      setStepSuccessMsg(`Medical Record #${res.recordNumber || res.id} created successfully.`)
      setTimeout(() => {
        clearMessages()
        setCurrentStep(2)
      }, 700)
    } catch (err) {
      setStepErrorMsg(err.response?.data?.message || 'Failed to create medical record. Please verify fields.')
    } finally {
      setSubmitting(false)
    }
  }

  // --- Step 2 Submit: Vitals ---
  const handleStep2Submit = async (e) => {
    e.preventDefault()
    clearMessages()

    // Validate ranges if entered
    if (temp && (Number(temp) < 30 || Number(temp) > 45)) {
      setStepErrorMsg('Temperature must be between 30°C and 45°C.')
      return
    }
    if (sysBP && (Number(sysBP) < 50 || Number(sysBP) > 250)) {
      setStepErrorMsg('Systolic BP must be between 50 and 250 mmHg.')
      return
    }
    if (diaBP && (Number(diaBP) < 30 || Number(diaBP) > 150)) {
      setStepErrorMsg('Diastolic BP must be between 30 and 150 mmHg.')
      return
    }
    if (heartRate && (Number(heartRate) < 30 || Number(heartRate) > 250)) {
      setStepErrorMsg('Heart rate must be between 30 and 250 bpm.')
      return
    }
    if (spO2 && (Number(spO2) < 50 || Number(spO2) > 100)) {
      setStepErrorMsg('SpO2 must be between 50% and 100%.')
      return
    }

    try {
      setSubmitting(true)
      await recordVitalSign({
        patientId: Number(patientId),
        medicalRecordId: medicalRecordId ? Number(medicalRecordId) : null,
        temperatureCelsius: temp ? Number(temp) : null,
        systolicBloodPressure: sysBP ? Number(sysBP) : null,
        diastolicBloodPressure: diaBP ? Number(diaBP) : null,
        heartRateBpm: heartRate ? Number(heartRate) : null,
        respiratoryRateBpm: respRate ? Number(respRate) : null,
        oxygenSaturationSpO2: spO2 ? Number(spO2) : null,
        weightKg: weight ? Number(weight) : null,
        heightCm: height ? Number(height) : null,
        notes: vitalsNotes ? vitalsNotes.trim() : '',
      })

      setStepSuccessMsg('Vital signs recorded successfully.')
      setTimeout(() => {
        clearMessages()
        setCurrentStep(3)
      }, 700)
    } catch (err) {
      setStepErrorMsg(err.response?.data?.message || 'Failed to record vitals.')
    } finally {
      setSubmitting(false)
    }
  }

  // --- Step 3 Submit: Diagnosis ---
  const handleStep3Submit = async (e) => {
    e.preventDefault()
    clearMessages()

    if (!diagDescription.trim()) {
      setStepErrorMsg('Diagnosis description is required.')
      return
    }

    try {
      setSubmitting(true)
      if (medicalRecordId) {
        await addMedicalRecordDiagnosis(medicalRecordId, {
          code: diagCode.trim() || undefined,
          description: diagDescription.trim(),
          type: Number(diagType),
          status: Number(diagStatus),
          severity: Number(diagSeverity),
          notes: diagNotes ? diagNotes.trim() : '',
          diagnosedAt: new Date().toISOString(),
        })
      }
      setStepSuccessMsg('Clinical diagnosis attached successfully.')
      setTimeout(() => {
        clearMessages()
        setCurrentStep(4)
      }, 700)
    } catch (err) {
      setStepErrorMsg(err.response?.data?.message || 'Failed to add diagnosis.')
    } finally {
      setSubmitting(false)
    }
  }

  // --- Step 4 Submit: Treatment Plan ---
  const handleStep4Submit = async (e) => {
    e.preventDefault()
    clearMessages()

    if (!planTitle.trim()) {
      setStepErrorMsg('Plan title is required.')
      return
    }
    if (!planDescription.trim()) {
      setStepErrorMsg('Plan description is required.')
      return
    }

    try {
      setSubmitting(true)
      if (medicalRecordId) {
        await addMedicalRecordTreatmentPlan(medicalRecordId, {
          title: planTitle.trim(),
          category: Number(planCategory),
          description: planDescription.trim(),
          goals: planGoals ? planGoals.trim() : '',
          interventions: planInterventions ? planInterventions.trim() : '',
          status: 1, // Active
          startDate: new Date().toISOString(),
          targetDate: planTargetDate ? new Date(planTargetDate).toISOString() : null,
        })
      }
      setStepSuccessMsg('Treatment plan recorded successfully.')
      setTimeout(() => {
        clearMessages()
        setCurrentStep(5)
      }, 700)
    } catch (err) {
      setStepErrorMsg(err.response?.data?.message || 'Failed to add treatment plan.')
    } finally {
      setSubmitting(false)
    }
  }

  // --- Step 5 Submit: Prescription ---
  const handleStep5Submit = async (e) => {
    e.preventDefault()
    clearMessages()

    const validItems = rxItems.filter((i) => i.medicineName.trim() && i.dosage.trim())
    if (validItems.length === 0) {
      setStepErrorMsg('Please add at least one medication or click Skip.')
      return
    }

    try {
      setSubmitting(true)
      await createPrescription({
        patientId: Number(patientId),
        medicalRecordId: medicalRecordId ? Number(medicalRecordId) : null,
        generalInstructions: rxInstructions.trim() || undefined,
        items: validItems.map((i) => ({
          medicineName: i.medicineName.trim(),
          dosage: i.dosage.trim(),
          route: i.route || 'Oral',
          frequency: i.frequency.trim(),
          durationDays: Number(i.durationDays) || 7,
          specialInstructions: i.specialInstructions ? i.specialInstructions.trim() : '',
        })),
      })
      setStepSuccessMsg('Prescription authored successfully.')
      setTimeout(() => {
        clearMessages()
        setCurrentStep(6)
      }, 700)
    } catch (err) {
      setStepErrorMsg(err.response?.data?.message || 'Failed to issue prescription.')
    } finally {
      setSubmitting(false)
    }
  }

  // --- Step 6 Submit: Lab Order ---
  const handleStep6Submit = async (e) => {
    e.preventDefault()
    clearMessages()

    if (!labTestName.trim()) {
      setStepErrorMsg('Investigation/Test name is required.')
      return
    }

    try {
      setSubmitting(true)
      const res = await createLabOrder({
        patientId: Number(patientId),
        medicalRecordId: medicalRecordId ? Number(medicalRecordId) : null,
        testName: labTestName.trim(),
        category: labCategory || 'General',
        priority: Number(labPriority),
        clinicalNotes: labNotes ? labNotes.trim() : '',
      })
      setCreatedLabOrderId(res.id)
      setStepSuccessMsg(`Lab order #${res.orderNumber || res.id} placed successfully.`)
      setTimeout(() => {
        clearMessages()
        setCurrentStep(7)
      }, 700)
    } catch (err) {
      setStepErrorMsg(err.response?.data?.message || 'Failed to create lab order.')
    } finally {
      setSubmitting(false)
    }
  }

  // --- Step 7 Submit: Lab Report ---
  const handleStep7Submit = async (e) => {
    e.preventDefault()
    clearMessages()

    if (!reportSummary.trim() || !reportFindings.trim()) {
      setStepErrorMsg('Both summary and findings are required if recording lab results.')
      return
    }

    try {
      setSubmitting(true)
      if (createdLabOrderId) {
        await recordLabReport(createdLabOrderId, {
          resultSummary: reportSummary.trim(),
          findings: reportFindings.trim(),
          referenceRange: reportRange ? reportRange.trim() : '',
          doctorRemarks: reportRemarks ? reportRemarks.trim() : '',
          reportDate: new Date().toISOString(),
        })
      }
      setStepSuccessMsg('Diagnostic report findings recorded successfully.')
      setTimeout(() => {
        clearMessages()
        setCurrentStep(8)
      }, 700)
    } catch (err) {
      setStepErrorMsg(err.response?.data?.message || 'Failed to record diagnostic findings.')
    } finally {
      setSubmitting(false)
    }
  }

  // --- Step 8: Follow-up & Finalize Confirmation ---
  const handleFinalizeConfirm = async () => {
    setShowFinalizeConfirm(false)
    clearMessages()

    try {
      setSubmitting(true)
      if (medicalRecordId) {
        await updateMedicalRecord(medicalRecordId, {
          chiefComplaint: chiefComplaint.trim(),
          symptoms: symptoms.trim() || undefined,
          examinationNotes: examinationNotes.trim() || undefined,
          diagnosis: diagDescription.trim() || initialDiagnosis.trim(),
          treatmentPlan: finalAdvice.trim() || planDescription.trim() || 'Completed consultation encounter.',
          followUpDate: followUpDate ? new Date(followUpDate).toISOString() : null,
          appointmentId: appointmentId ? Number(appointmentId) : null,
        })
      }
      onCompleted()
    } catch (err) {
      setStepErrorMsg(err.response?.data?.message || 'Failed to finalize consultation record.')
      setSubmitting(false)
    }
  }

  const handlePresetFollowUp = (days) => {
    const d = new Date(Date.now() + days * 24 * 60 * 60 * 1000)
    setFollowUpDate(d.toISOString().slice(0, 10))
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 820px)', maxHeight: '92vh' }}>
        {/* Header */}
        <div className="flex justify-between items-center mb-3 pb-3 border-b" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <div>
            <div className="flex items-center gap-2">
              <span className="badge badge-primary">Clinical Workflow</span>
              <h2 className="m-0 text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
                Doctor Clinical Encounter
              </h2>
            </div>
            <span className="text-xs text-gray-600 mt-1 block">
              Patient: <strong>{patientName || `Patient #${patientId}`}</strong> • Step {currentStep} of {STEPS.length}: {STEPS[currentStep - 1].name}
            </span>
          </div>
          <button type="button" className="secondary-button text-xs px-2.5 py-1" onClick={onClose} disabled={submitting}>
            ✕ Cancel Visit
          </button>
        </div>

        {/* Stepper Progression Bar */}
        <div className="flex flex-wrap gap-1 mb-4 p-2 rounded-lg bg-gray-50 border text-xs justify-between">
          {STEPS.map((s) => {
            const isCompleted = s.id < currentStep
            const isCurrent = s.id === currentStep
            return (
              <div
                key={s.id}
                className={`flex items-center gap-1.5 px-2 py-1 rounded font-semibold transition-all ${
                  isCurrent
                    ? 'bg-blue-900 text-white shadow-sm'
                    : isCompleted
                    ? 'text-green-800 bg-green-50'
                    : 'text-gray-400'
                }`}
              >
                <span>{isCompleted ? '✓' : `${s.id}.`}</span>
                <span className="hidden sm:inline">{s.name}</span>
              </div>
            )
          })}
        </div>

        {/* Feedback alerts */}
        {stepSuccessMsg && (
          <div className="p-3 mb-4 rounded-lg bg-green-50 border border-green-200 text-green-800 text-xs font-semibold flex items-center gap-2">
            <span>✓</span>
            <span>{stepSuccessMsg}</span>
          </div>
        )}
        {stepErrorMsg && (
          <div className="form-error mb-4" role="alert">
            {stepErrorMsg}
          </div>
        )}

        {/* Step Forms */}
        <div className="overflow-y-auto max-h-[60vh] pr-1">
          {/* STEP 1: Clinical Visit & Medical Record */}
          {currentStep === 1 && (
            <form onSubmit={handleStep1Submit} noValidate>
              <h3 className="font-bold text-base mb-2" style={{ color: 'var(--color-accent)' }}>
                Step 1: Clinical Visit & Consultation Encounter
              </h3>
              <p className="text-xs text-gray-500 mb-4">
                Record the patient encounter, primary complaints, and clinical physical examination findings.
              </p>

              <div>
                <label htmlFor="chief">Chief Complaint *</label>
                <input
                  id="chief"
                  placeholder="e.g. Acute chest discomfort, severe dyspnea upon exertion"
                  value={chiefComplaint}
                  onChange={(e) => setChiefComplaint(e.target.value)}
                  required
                />
              </div>

              <div>
                <label htmlFor="symp">Reported Symptoms</label>
                <input
                  id="symp"
                  placeholder="e.g. Shortness of breath, diaphoresis, radiating left shoulder ache"
                  value={symptoms}
                  onChange={(e) => setSymptoms(e.target.value)}
                />
              </div>

              <div>
                <label htmlFor="exam">Physical Examination Findings</label>
                <textarea
                  id="exam"
                  rows={3}
                  className="w-full p-2.5 border rounded-lg bg-white"
                  placeholder="e.g. Alert, respiratory rate 22/min, bilateral basal crepitations, heart sounds regular"
                  value={examinationNotes}
                  onChange={(e) => setExaminationNotes(e.target.value)}
                />
              </div>

              <div>
                <label htmlFor="initDiag">Primary Clinical Impression / Diagnosis *</label>
                <input
                  id="initDiag"
                  placeholder="e.g. Acute Coronary Syndrome / Angina Pectoris"
                  value={initialDiagnosis}
                  onChange={(e) => setInitialDiagnosis(e.target.value)}
                  required
                />
              </div>

              <div className="modal-actions mt-6">
                <button type="button" className="secondary-button" onClick={onClose} disabled={submitting}>
                  Cancel
                </button>
                <button type="submit" className="primary-button" disabled={submitting}>
                  {submitting ? 'Creating Encounter...' : 'Proceed to Vitals →'}
                </button>
              </div>
            </form>
          )}

          {/* STEP 2: Vital Signs */}
          {currentStep === 2 && (
            <form onSubmit={handleStep2Submit} noValidate>
              <h3 className="font-bold text-base mb-2" style={{ color: 'var(--color-accent)' }}>
                Step 2: Record Vital Signs
              </h3>
              <p className="text-xs text-gray-500 mb-4">
                Capture the patient's physiological parameters for encounter #{medicalRecordId}.
              </p>

              <div className="field-row">
                <div>
                  <label htmlFor="vTemp">Temperature (°C)</label>
                  <input
                    id="vTemp"
                    type="number"
                    step="0.1"
                    placeholder="36.8"
                    value={temp}
                    onChange={(e) => setTemp(e.target.value)}
                  />
                </div>
                <div>
                  <label htmlFor="vPulse">Heart Rate (bpm)</label>
                  <input
                    id="vPulse"
                    type="number"
                    placeholder="75"
                    value={heartRate}
                    onChange={(e) => setHeartRate(e.target.value)}
                  />
                </div>
              </div>

              <div className="field-row">
                <div>
                  <label htmlFor="vSys">Systolic BP (mmHg)</label>
                  <input
                    id="vSys"
                    type="number"
                    placeholder="120"
                    value={sysBP}
                    onChange={(e) => setSysBP(e.target.value)}
                  />
                </div>
                <div>
                  <label htmlFor="vDia">Diastolic BP (mmHg)</label>
                  <input
                    id="vDia"
                    type="number"
                    placeholder="80"
                    value={diaBP}
                    onChange={(e) => setDiaBP(e.target.value)}
                  />
                </div>
              </div>

              <div className="field-row">
                <div>
                  <label htmlFor="vSpo2">Oxygen Saturation SpO2 (%)</label>
                  <input
                    id="vSpo2"
                    type="number"
                    step="0.1"
                    placeholder="98"
                    value={spO2}
                    onChange={(e) => setSpO2(e.target.value)}
                  />
                </div>
                <div>
                  <label htmlFor="vResp">Respiratory Rate (/min)</label>
                  <input
                    id="vResp"
                    type="number"
                    placeholder="16"
                    value={respRate}
                    onChange={(e) => setRespRate(e.target.value)}
                  />
                </div>
              </div>

              <div className="field-row">
                <div>
                  <label htmlFor="vWt">Weight (kg)</label>
                  <input
                    id="vWt"
                    type="number"
                    step="0.1"
                    placeholder="70"
                    value={weight}
                    onChange={(e) => setWeight(e.target.value)}
                  />
                </div>
                <div>
                  <label htmlFor="vHt">Height (cm)</label>
                  <input
                    id="vHt"
                    type="number"
                    step="0.1"
                    placeholder="175"
                    value={height}
                    onChange={(e) => setHeight(e.target.value)}
                  />
                </div>
              </div>

              <div>
                <label htmlFor="vNotes">Vitals Clinical Notes</label>
                <input
                  id="vNotes"
                  placeholder="e.g. Patient resting comfortably, measurements confirmed"
                  value={vitalsNotes}
                  onChange={(e) => setVitalsNotes(e.target.value)}
                />
              </div>

              <div className="modal-actions mt-6 flex justify-between">
                <button type="button" className="secondary-button" onClick={() => setCurrentStep(3)}>
                  Skip Vitals
                </button>
                <button type="submit" className="primary-button" disabled={submitting}>
                  {submitting ? 'Recording Vitals...' : 'Save Vitals & Proceed to Diagnosis →'}
                </button>
              </div>
            </form>
          )}

          {/* STEP 3: Diagnosis */}
          {currentStep === 3 && (
            <form onSubmit={handleStep3Submit} noValidate>
              <h3 className="font-bold text-base mb-2" style={{ color: 'var(--color-accent)' }}>
                Step 3: Document Clinical Diagnosis
              </h3>
              <p className="text-xs text-gray-500 mb-4">
                Attach formal ICD code, diagnosis classification, and severity.
              </p>

              <div className="field-row">
                <div>
                  <label htmlFor="dCode">ICD Code (Optional)</label>
                  <input
                    id="dCode"
                    placeholder="e.g. I20.0, E11.9, J45.0"
                    value={diagCode}
                    onChange={(e) => setDiagCode(e.target.value)}
                  />
                </div>
                <div>
                  <label htmlFor="dType">Type *</label>
                  <select
                    id="dType"
                    className="w-full p-2.5 border rounded-lg bg-white"
                    value={diagType}
                    onChange={(e) => setDiagType(e.target.value)}
                  >
                    <option value={1}>Primary</option>
                    <option value={2}>Secondary</option>
                    <option value={3}>Differential</option>
                    <option value={4}>Provisional</option>
                    <option value={5}>Confirmed</option>
                  </select>
                </div>
              </div>

              <div>
                <label htmlFor="dDesc">Diagnosis Description *</label>
                <input
                  id="dDesc"
                  placeholder="e.g. Unstable angina with underlying essential hypertension"
                  value={diagDescription}
                  onChange={(e) => setDiagDescription(e.target.value)}
                  required
                />
              </div>

              <div className="field-row">
                <div>
                  <label htmlFor="dStat">Status *</label>
                  <select
                    id="dStat"
                    className="w-full p-2.5 border rounded-lg bg-white"
                    value={diagStatus}
                    onChange={(e) => setDiagStatus(e.target.value)}
                  >
                    <option value={1}>Active</option>
                    <option value={2}>Resolved</option>
                    <option value={3}>Chronic</option>
                    <option value={4}>Inactive</option>
                  </select>
                </div>
                <div>
                  <label htmlFor="dSev">Severity *</label>
                  <select
                    id="dSev"
                    className="w-full p-2.5 border rounded-lg bg-white"
                    value={diagSeverity}
                    onChange={(e) => setDiagSeverity(e.target.value)}
                  >
                    <option value={1}>Mild</option>
                    <option value={2}>Moderate</option>
                    <option value={3}>Severe</option>
                    <option value={4}>Critical</option>
                  </select>
                </div>
              </div>

              <div>
                <label htmlFor="dNotes">Diagnostic Notes</label>
                <input
                  id="dNotes"
                  placeholder="e.g. Initial presentation, confirm with cardiac enzymes and ECG"
                  value={diagNotes}
                  onChange={(e) => setDiagNotes(e.target.value)}
                />
              </div>

              <div className="modal-actions mt-6 flex justify-between">
                <button type="button" className="secondary-button" onClick={() => setCurrentStep(4)}>
                  Skip Diagnosis Detail
                </button>
                <button type="submit" className="primary-button" disabled={submitting}>
                  {submitting ? 'Attaching Diagnosis...' : 'Save Diagnosis & Proceed to Treatment Plan →'}
                </button>
              </div>
            </form>
          )}

          {/* STEP 4: Treatment Plan */}
          {currentStep === 4 && (
            <form onSubmit={handleStep4Submit} noValidate>
              <h3 className="font-bold text-base mb-2" style={{ color: 'var(--color-accent)' }}>
                Step 4: Formulate Treatment Plan & Goals
              </h3>
              <p className="text-xs text-gray-500 mb-4">
                Define therapeutic interventions, clinical objectives, and target review timeline.
              </p>

              <div className="field-row">
                <div>
                  <label htmlFor="pTitle">Plan Title *</label>
                  <input
                    id="pTitle"
                    placeholder="e.g. Medical Optimization & Monitoring Protocol"
                    value={planTitle}
                    onChange={(e) => setPlanTitle(e.target.value)}
                    required
                  />
                </div>
                <div>
                  <label htmlFor="pCat">Category *</label>
                  <select
                    id="pCat"
                    className="w-full p-2.5 border rounded-lg bg-white"
                    value={planCategory}
                    onChange={(e) => setPlanCategory(e.target.value)}
                  >
                    <option value={1}>General</option>
                    <option value={2}>Pharmacological</option>
                    <option value={3}>Lifestyle Modification</option>
                    <option value={4}>Surgical / Procedural</option>
                    <option value={5}>Rehabilitative</option>
                    <option value={6}>Dietary</option>
                    <option value={7}>Monitoring</option>
                  </select>
                </div>
              </div>

              <div>
                <label htmlFor="pDesc">Plan Description & Medical Directions *</label>
                <textarea
                  id="pDesc"
                  rows={3}
                  className="w-full p-2.5 border rounded-lg bg-white"
                  placeholder="e.g. Initiate antiplatelet therapy, beta blocker, telemetry monitoring, low sodium diet"
                  value={planDescription}
                  onChange={(e) => setPlanDescription(e.target.value)}
                  required
                />
              </div>

              <div>
                <label htmlFor="pGoals">Therapeutic Goals</label>
                <input
                  id="pGoals"
                  placeholder="e.g. Symptom resolution, target BP < 130/80, normal serial troponins"
                  value={planGoals}
                  onChange={(e) => setPlanGoals(e.target.value)}
                />
              </div>

              <div className="field-row">
                <div>
                  <label htmlFor="pInter">Interventions</label>
                  <input
                    id="pInter"
                    placeholder="e.g. Daily weights, telemetry, cardiac rehab referral"
                    value={planInterventions}
                    onChange={(e) => setPlanInterventions(e.target.value)}
                  />
                </div>
                <div>
                  <label htmlFor="pTarget">Target Review Date</label>
                  <input
                    id="pTarget"
                    type="date"
                    value={planTargetDate}
                    onChange={(e) => setPlanTargetDate(e.target.value)}
                  />
                </div>
              </div>

              <div className="modal-actions mt-6 flex justify-between">
                <button type="button" className="secondary-button" onClick={() => setCurrentStep(5)}>
                  Skip Treatment Plan
                </button>
                <button type="submit" className="primary-button" disabled={submitting}>
                  {submitting ? 'Saving Plan...' : 'Save Plan & Proceed to Prescription →'}
                </button>
              </div>
            </form>
          )}

          {/* STEP 5: Prescription */}
          {currentStep === 5 && (
            <form onSubmit={handleStep5Submit} noValidate>
              <div className="flex justify-between items-center mb-2">
                <h3 className="font-bold text-base m-0" style={{ color: 'var(--color-accent)' }}>
                  Step 5: Author Medications / Prescription
                </h3>
                <button
                  type="button"
                  className="secondary-button text-xs px-2 py-1"
                  style={{ marginTop: 0 }}
                  onClick={() =>
                    setRxItems([
                      ...rxItems,
                      { medicineName: '', dosage: '', route: 'Oral', frequency: 'Once daily', durationDays: 7, specialInstructions: '' },
                    ])
                  }
                >
                  + Add Medication
                </button>
              </div>
              <p className="text-xs text-gray-500 mb-4">
                Prescribe pharmacological items directly linked to this consultation encounter.
              </p>

              <div>
                <label htmlFor="rxGen">General Instructions</label>
                <input
                  id="rxGen"
                  placeholder="e.g. Take with meals, do not stop abruptly"
                  value={rxInstructions}
                  onChange={(e) => setRxInstructions(e.target.value)}
                />
              </div>

              <div className="flex flex-col gap-3 my-3">
                {rxItems.map((item, idx) => (
                  <div key={idx} className="p-3 rounded-lg border bg-gray-50">
                    <div className="flex justify-between items-center mb-2">
                      <span className="text-xs font-bold text-blue-900">Medication #{idx + 1}</span>
                      {rxItems.length > 1 && (
                        <button
                          type="button"
                          className="text-xs text-red-600 bg-transparent border-0 cursor-pointer hover:underline"
                          onClick={() => setRxItems(rxItems.filter((_, i) => i !== idx))}
                        >
                          Remove
                        </button>
                      )}
                    </div>
                    <div className="field-row">
                      <div>
                        <label className="text-xs">Medicine *</label>
                        <input
                          placeholder="e.g. Aspirin / Metoprolol"
                          value={item.medicineName}
                          onChange={(e) => {
                            const updated = [...rxItems]
                            updated[idx].medicineName = e.target.value
                            setRxItems(updated)
                          }}
                        />
                      </div>
                      <div>
                        <label className="text-xs">Dosage *</label>
                        <input
                          placeholder="e.g. 75mg / 25mg"
                          value={item.dosage}
                          onChange={(e) => {
                            const updated = [...rxItems]
                            updated[idx].dosage = e.target.value
                            setRxItems(updated)
                          }}
                        />
                      </div>
                    </div>
                    <div className="field-row mt-2">
                      <div>
                        <label className="text-xs">Route</label>
                        <select
                          className="w-full p-2 text-xs border rounded bg-white"
                          value={item.route}
                          onChange={(e) => {
                            const updated = [...rxItems]
                            updated[idx].route = e.target.value
                            setRxItems(updated)
                          }}
                        >
                          <option value="Oral">Oral</option>
                          <option value="Intravenous (IV)">Intravenous (IV)</option>
                          <option value="Sublingual">Sublingual</option>
                          <option value="Subcutaneous">Subcutaneous</option>
                          <option value="Inhalation">Inhalation</option>
                        </select>
                      </div>
                      <div>
                        <label className="text-xs">Frequency *</label>
                        <input
                          placeholder="e.g. Once daily / Twice daily"
                          value={item.frequency}
                          onChange={(e) => {
                            const updated = [...rxItems]
                            updated[idx].frequency = e.target.value
                            setRxItems(updated)
                          }}
                        />
                      </div>
                      <div>
                        <label className="text-xs">Days *</label>
                        <input
                          type="number"
                          min="1"
                          max="365"
                          value={item.durationDays}
                          onChange={(e) => {
                            const updated = [...rxItems]
                            updated[idx].durationDays = e.target.value
                            setRxItems(updated)
                          }}
                        />
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              <div className="modal-actions mt-6 flex justify-between">
                <button type="button" className="secondary-button" onClick={() => setCurrentStep(6)}>
                  Skip Prescription
                </button>
                <button type="submit" className="primary-button" disabled={submitting}>
                  {submitting ? 'Issuing Prescription...' : 'Issue Prescription & Proceed to Lab Order →'}
                </button>
              </div>
            </form>
          )}

          {/* STEP 6: Lab Order */}
          {currentStep === 6 && (
            <form onSubmit={handleStep6Submit} noValidate>
              <h3 className="font-bold text-base mb-2" style={{ color: 'var(--color-accent)' }}>
                Step 6: Order Laboratory & Diagnostic Investigations
              </h3>
              <p className="text-xs text-gray-500 mb-4">
                Request laboratory tests, radiology, or pathology workup.
              </p>

              <div>
                <label htmlFor="lTest">Investigation / Test Name *</label>
                <input
                  id="lTest"
                  placeholder="e.g. High-Sensitivity Troponin I, 12-Lead ECG, Complete Blood Count"
                  value={labTestName}
                  onChange={(e) => setLabTestName(e.target.value)}
                  required
                />
              </div>

              <div className="field-row">
                <div>
                  <label htmlFor="lCat">Category</label>
                  <select
                    id="lCat"
                    className="w-full p-2.5 border rounded-lg bg-white"
                    value={labCategory}
                    onChange={(e) => setLabCategory(e.target.value)}
                  >
                    <option value="Biochemistry">Biochemistry (Cardiac/Renal/Liver)</option>
                    <option value="Hematology">Hematology</option>
                    <option value="Cardiology">Cardiology (ECG/Echo)</option>
                    <option value="Radiology">Radiology / Imaging</option>
                    <option value="Microbiology">Microbiology</option>
                    <option value="Pathology">Pathology</option>
                    <option value="General">General / Routine</option>
                  </select>
                </div>
                <div>
                  <label htmlFor="lPri">Priority Level *</label>
                  <select
                    id="lPri"
                    className="w-full p-2.5 border rounded-lg bg-white"
                    value={labPriority}
                    onChange={(e) => setLabPriority(e.target.value)}
                  >
                    <option value={1}>Routine</option>
                    <option value={2}>Urgent</option>
                    <option value={3}>Stat (Emergency)</option>
                  </select>
                </div>
              </div>

              <div>
                <label htmlFor="lNotes">Clinical Notes & Reason for Order</label>
                <input
                  id="lNotes"
                  placeholder="e.g. Acute chest discomfort, rule out myocardial infarction"
                  value={labNotes}
                  onChange={(e) => setLabNotes(e.target.value)}
                />
              </div>

              <div className="modal-actions mt-6 flex justify-between">
                <button type="button" className="secondary-button" onClick={() => setCurrentStep(8)}>
                  Skip Lab Order
                </button>
                <button type="submit" className="primary-button" disabled={submitting}>
                  {submitting ? 'Placing Order...' : 'Submit Lab Order & Proceed to Report →'}
                </button>
              </div>
            </form>
          )}

          {/* STEP 7: Lab Report */}
          {currentStep === 7 && (
            <form onSubmit={handleStep7Submit} noValidate>
              <h3 className="font-bold text-base mb-2" style={{ color: 'var(--color-accent)' }}>
                Step 7: Record Point-of-Care or Lab Findings
              </h3>
              <p className="text-xs text-gray-500 mb-4">
                Record immediate clinic findings or point-of-care results for Order #{createdLabOrderId || 'New'}.
              </p>

              <div>
                <label htmlFor="rSumm">Result Summary *</label>
                <input
                  id="rSumm"
                  placeholder="e.g. Troponin I Negative (<0.01 ng/mL), sinus rhythm on ECG"
                  value={reportSummary}
                  onChange={(e) => setReportSummary(e.target.value)}
                  required
                />
              </div>

              <div>
                <label htmlFor="rFind">Detailed Findings *</label>
                <textarea
                  id="rFind"
                  rows={3}
                  className="w-full p-2.5 border rounded-lg bg-white"
                  placeholder="e.g. Normal sinus rhythm at 76 bpm, PR interval 160ms, QRS 88ms, no ST-T segment changes"
                  value={reportFindings}
                  onChange={(e) => setReportFindings(e.target.value)}
                  required
                />
              </div>

              <div className="field-row">
                <div>
                  <label htmlFor="rRange">Reference Range</label>
                  <input
                    id="rRange"
                    placeholder="e.g. <0.04 ng/mL"
                    value={reportRange}
                    onChange={(e) => setReportRange(e.target.value)}
                  />
                </div>
                <div>
                  <label htmlFor="rRemarks">Clinical Remarks</label>
                  <input
                    id="rRemarks"
                    placeholder="e.g. Baseline normal, repeat in 3 hours"
                    value={reportRemarks}
                    onChange={(e) => setReportRemarks(e.target.value)}
                  />
                </div>
              </div>

              <div className="modal-actions mt-6 flex justify-between">
                <button type="button" className="secondary-button" onClick={() => setCurrentStep(8)}>
                  Skip Report (Await Laboratory)
                </button>
                <button type="submit" className="primary-button" disabled={submitting}>
                  {submitting ? 'Recording Report...' : 'Save Findings & Proceed to Follow-up →'}
                </button>
              </div>
            </form>
          )}

          {/* STEP 8: Follow-up & Review */}
          {currentStep === 8 && (
            <div>
              <h3 className="font-bold text-base mb-2" style={{ color: 'var(--color-accent)' }}>
                Step 8: Follow-Up & Encounter Finalization
              </h3>
              <p className="text-xs text-gray-500 mb-4">
                Schedule patient follow-up review date, provide closing medical directions, and finalize the clinical encounter.
              </p>

              <div className="mb-4">
                <label className="block mb-1">Quick Follow-Up Presets</label>
                <div className="flex flex-wrap gap-2">
                  <button type="button" className="secondary-button text-xs px-2.5 py-1" style={{ marginTop: 0 }} onClick={() => handlePresetFollowUp(3)}>
                    In 3 Days
                  </button>
                  <button type="button" className="secondary-button text-xs px-2.5 py-1" style={{ marginTop: 0 }} onClick={() => handlePresetFollowUp(7)}>
                    In 1 Week
                  </button>
                  <button type="button" className="secondary-button text-xs px-2.5 py-1" style={{ marginTop: 0 }} onClick={() => handlePresetFollowUp(14)}>
                    In 2 Weeks
                  </button>
                  <button type="button" className="secondary-button text-xs px-2.5 py-1" style={{ marginTop: 0 }} onClick={() => handlePresetFollowUp(30)}>
                    In 1 Month
                  </button>
                </div>
              </div>

              <div className="field-row">
                <div>
                  <label htmlFor="fDate">Scheduled Follow-Up Date</label>
                  <input
                    id="fDate"
                    type="date"
                    value={followUpDate}
                    onChange={(e) => setFollowUpDate(e.target.value)}
                  />
                </div>
              </div>

              <div>
                <label htmlFor="fAdvice">Final Consultation Directions & Patient Advice</label>
                <textarea
                  id="fAdvice"
                  rows={3}
                  className="w-full p-2.5 border rounded-lg bg-white"
                  placeholder="e.g. Rest, take prescribed medicines diligently, return to emergency immediately if chest pain recurs"
                  value={finalAdvice}
                  onChange={(e) => setFinalAdvice(e.target.value)}
                />
              </div>

              {/* Encounter Summary Preview */}
              <div className="p-4 rounded-lg bg-blue-50 border border-blue-200 text-xs my-4">
                <span className="font-bold text-blue-900 block mb-2 uppercase tracking-wide">
                  Encounter Summary Ready for Finalization
                </span>
                <div className="grid grid-cols-2 gap-2 text-blue-950">
                  <div><strong>Complaint:</strong> {chiefComplaint}</div>
                  <div><strong>Diagnosis:</strong> {diagDescription || initialDiagnosis}</div>
                  <div><strong>Vitals Recorded:</strong> {temp || sysBP ? 'Yes' : 'None entered'}</div>
                  <div><strong>Medications Prescribed:</strong> {rxItems.filter((i) => i.medicineName).length} items</div>
                  <div><strong>Lab Orders:</strong> {labTestName || 'None'}</div>
                  <div><strong>Follow-up Scheduled:</strong> {followUpDate || 'Not scheduled'}</div>
                </div>
              </div>

              <div className="modal-actions mt-6">
                <button type="button" className="secondary-button" onClick={onClose} disabled={submitting}>
                  Close
                </button>
                <button
                  type="button"
                  className="primary-button"
                  style={{ backgroundColor: '#0f7a3d', borderColor: '#0f7a3d' }}
                  onClick={() => setShowFinalizeConfirm(true)}
                  disabled={submitting}
                >
                  {submitting ? 'Finalizing Visit...' : '✓ Complete & Finalize Clinical Visit'}
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Finalize Confirmation Modal */}
        <ConfirmationModal
          isOpen={showFinalizeConfirm}
          title="Finalize Clinical Visit"
          message={`Are you sure you want to finalize this clinical encounter for ${patientName || `Patient #${patientId}`}? All consultation notes, vitals, diagnoses, prescriptions, and orders will be recorded to the permanent patient chart.`}
          confirmText="Yes, Finalize Encounter"
          cancelText="Review Again"
          onConfirm={handleFinalizeConfirm}
          onCancel={() => setShowFinalizeConfirm(false)}
          loading={submitting}
        />
      </div>
    </div>
  )
}
