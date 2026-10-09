import { APP_NAME } from '../../utils/brand'

// Read-only, hospital-style rendering of a stored AI medical report (same sections as the patient app).
// Reports saved before the structured layout map their fields into the closest section; anything not
// shown in a section is listed under "More recorded details", so no stored fact is hidden.

const SHOWN_RECORDED_KEYS = new Set(['patientInformation', 'appointmentSummary', 'presentingSymptoms', 'currentEncounter',
  'medicalCheckup', 'vitalSigns', 'prescriptions', 'labReports', 'clinicalDiagnoses', 'treatmentPlans', 'followUp'])
const SHOWN_CONTENT_KEYS = new Set(['recordedData', 'followUpSuggestedTests', 'aiSummary', 'aiRecommendations',
  'recommendations', 'patientContact', 'reportMetadata'])

const muted = { color: 'color-mix(in srgb, var(--color-secondary) 62%, var(--color-primary))' }
const border = 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))'
const surface = 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))'

const obj = (value) => (value && typeof value === 'object' && !Array.isArray(value) ? value : {})
const arr = (value) => (Array.isArray(value) ? value : [])
const objs = (value) => arr(value).filter((item) => item && typeof item === 'object' && !Array.isArray(item))
const str = (value) => (value == null || String(value).trim() === '' ? null : String(value).trim())
const bloodGroup = (value) => value?.replace(/^(A|B|AB|O)(Positive|Negative)$/, (_, group, sign) => `${group}${sign === 'Positive' ? '+' : '-'}`) ?? null
const humanize = (value) => value?.replace(/([a-z])([A-Z])/g, '$1 $2') ?? null
const label = (key) => key.replace(/([A-Z])/g, ' $1').replace(/^./, (s) => s.toUpperCase())
const join = (parts, separator = ' · ') => parts.filter(Boolean).join(separator)

function fmtDate(value, withTime = true) {
  const text = str(value)
  if (!text) return null
  const date = new Date(text)
  if (Number.isNaN(date.getTime())) return text
  return withTime ? date.toLocaleString() : date.toLocaleDateString()
}

function statusTone(status) {
  const lower = (status || '').toLowerCase()
  if (/cancel|discontinued|no ?show|emergency/.test(lower)) return 'var(--color-error)'
  if (/pending|ordered|progress|scheduled|waiting|urgent/.test(lower)) return '#f59e0b'
  if (/complete|active|confirmed|admitted|occupied|normal/.test(lower)) return '#22c55e'
  return 'var(--color-accent)'
}

function Chip({ children, tone = 'var(--color-accent)' }) {
  return <span className="badge" style={{ color: tone, background: `color-mix(in srgb, ${tone} 14%, var(--color-primary))`, border: `1px solid color-mix(in srgb, ${tone} 35%, transparent)` }}>{children}</span>
}

function Muted({ children }) {
  return <p className="m-0 text-sm" style={muted}>{children}</p>
}

function SubTitle({ children }) {
  return <h5 className="mt-4 mb-2 text-sm font-bold first:mt-0" style={{ color: 'var(--color-secondary)' }}>{children}</h5>
}

function InfoGrid({ rows, emptyText = 'Not recorded', columns = 2 }) {
  const present = rows.filter(([, value]) => value != null && value !== '')
  if (!present.length) return <Muted>{emptyText}</Muted>
  return (
    <dl className={`grid grid-cols-1 ${columns === 2 ? 'sm:grid-cols-2' : ''} gap-x-6 gap-y-3 m-0`}>
      {present.map(([key, value]) => (
        <div key={key} className="min-w-0">
          <dt className="text-xs font-semibold" style={muted}>{key}</dt>
          <dd className="m-0 text-sm break-words whitespace-pre-line">{value}</dd>
        </div>
      ))}
    </dl>
  )
}

function Bullets({ items }) {
  return <ul className="m-0 pl-5 space-y-1 text-sm list-disc">{items.map((item, index) => <li key={index}>{item}</li>)}</ul>
}

function DataTable({ columns, rows, emptyText = 'Not recorded', statusColumn }) {
  if (!rows.length) return <Muted>{emptyText}</Muted>
  return (
    <div className="table-scroll rounded-lg" style={{ border: `1px solid ${border}`, background: surface }}>
      <table className="data-table">
        <thead><tr>{columns.map((column) => <th key={column}>{column}</th>)}</tr></thead>
        <tbody>
          {rows.map((row, index) => [
            <tr key={`r${index}`}>{row.cells.map((cell, cellIndex) => (
              <td key={cellIndex} style={{ whiteSpace: 'normal' }}>
                {cellIndex === statusColumn && cell && cell !== '—' ? <Chip tone={statusTone(cell)}>{cell}</Chip> : (cell || '—')}
              </td>
            ))}</tr>,
            row.detail ? <tr key={`d${index}`}><td colSpan={columns.length} className="text-xs whitespace-pre-line" style={{ ...muted, whiteSpace: 'pre-line', paddingTop: 0 }}>{row.detail}</td></tr> : null,
          ])}
        </tbody>
      </table>
    </div>
  )
}

function Section({ number, title, children }) {
  return (
    <section className="panel mb-3" style={{ padding: 16 }}>
      <h4 className="mt-0 mb-3 font-bold" style={{ color: 'var(--color-accent)' }}>{number}. {title}</h4>
      {children}
    </section>
  )
}

function GenericValue({ value }) {
  if (value == null || (typeof value === 'string' && !value.trim())) return <Muted>Not recorded</Muted>
  if (Array.isArray(value)) {
    if (!value.length) return <Muted>None recorded</Muted>
    if (value.every((item) => item === null || typeof item !== 'object')) return <span className="text-sm">{value.map((item) => item ?? '—').join(', ')}</span>
    return <div className="space-y-2">{value.map((item, index) => <div key={index} className="rounded-lg p-2" style={{ border: `1px solid ${border}`, background: surface }}><GenericValue value={item} /></div>)}</div>
  }
  if (typeof value === 'object') {
    return <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-2 m-0">{Object.entries(value).map(([key, child]) => (
      <div key={key} className="min-w-0"><dt className="text-xs font-semibold" style={muted}>{label(key)}</dt><dd className="m-0 text-sm break-words"><GenericValue value={child} /></dd></div>
    ))}</dl>
  }
  return <span className="text-sm">{String(value)}</span>
}

export default function AiMedicalReportView({ report }) {
  const content = obj(report?.content)
  const data = obj(content.recordedData)
  const metadata = obj(content.reportMetadata)
  const summary = obj(content.aiSummary)
  const contact = obj(content.patientContact)
  const patient = obj(data.patientInformation)
  const appointment = data.appointmentSummary && typeof data.appointmentSummary === 'object' ? data.appointmentSummary : null
  const checkup = obj(data.medicalCheckup)
  const admission = checkup.admission && typeof checkup.admission === 'object' ? checkup.admission : null
  const resources = objs(checkup.resources)
  const isNewFormat = 'recommendations' in content
  let sectionNumber = 0
  const next = () => ++sectionNumber

  const moreDetails = {
    ...Object.fromEntries(Object.entries(data).filter(([key]) => !SHOWN_RECORDED_KEYS.has(key))),
    ...(appointment?.agent2Outcome ? { bookingRecord: appointment.agent2Outcome } : {}),
    ...Object.fromEntries(Object.entries(content).filter(([key]) => !SHOWN_CONTENT_KEYS.has(key))),
  }

  // 5. Clinical summary parts
  const symptoms = objs(obj(data.presentingSymptoms).records)
  const encounters = objs(obj(data.currentEncounter).records)
  const triage = obj(data.clinicalTriageSummary)
  const diagnoses = objs(obj(data.clinicalDiagnoses).records)
  const plans = objs(obj(data.treatmentPlans).records)

  // 6. Vitals
  const vitals = objs(obj(data.vitalSigns).records)
  const bp = (v) => (str(v.systolicBloodPressure) || str(v.diastolicBloodPressure) ? `${str(v.systolicBloodPressure) ?? '—'}/${str(v.diastolicBloodPressure) ?? '—'} mmHg` : null)
  const unit = (value, suffix) => (str(value) ? `${str(value)} ${suffix}` : null)

  // 9 / 10. Recommendations and follow-up
  const recommendations = obj(content.recommendations)
  const legacyRecommendations = obj(content.aiRecommendations)
  const texts = (value) => arr(value).map(str).filter(Boolean)
  const suggested = obj(content.followUpSuggestedTests)
  const recordedDates = (objs(suggested.recordedFollowUpDates).length ? objs(suggested.recordedFollowUpDates) : objs(obj(data.followUp).recordedDates))
    .map((d) => fmtDate(d.date, false)).filter(Boolean)
  const aiFollowUp = obj(recommendations.followUp)
  const recommendationGroups = [
    ['Foods to prefer', texts(obj(recommendations.diet).prefer)],
    ['Foods to limit', texts(obj(recommendations.diet).avoid)],
    ['Suitable activity', texts(obj(recommendations.exercise).suitable)],
    ['Activity to avoid', texts(obj(recommendations.exercise).avoid)],
    ['Lifestyle', texts(recommendations.lifestyle)],
    ['Warning signs: contact a doctor', texts(recommendations.warningSigns)],
  ].filter(([, items]) => items.length)

  return (
    <div>
      <header className="panel mb-3" style={{ padding: 18, background: 'linear-gradient(135deg, color-mix(in srgb, var(--color-accent) 14%, var(--color-primary)), var(--color-primary))' }}>
        <p className="m-0 font-bold" style={{ color: 'var(--color-accent)' }}>{APP_NAME}</p>
        <h3 className="mt-1 mb-3 text-xl font-bold">Medical Report</h3>
        <div className="flex flex-wrap gap-2 mb-3">
          <Chip tone="#a78bfa">{summary.mode === 'gemini' ? 'AI-assisted summary' : 'Structured summary (non-AI)'}</Chip>
          <Chip tone="color-mix(in srgb, var(--color-secondary) 70%, var(--color-primary))">Version {report.versionNumber}</Chip>
        </div>
        <InfoGrid rows={[['Report ID', str(metadata.reportId) ?? report.reportId], ['Generated', fmtDate(metadata.generatedAt) ?? fmtDate(report.createdAt)]]} />
      </header>

      <Section number={next()} title="Patient Information">
        <InfoGrid rows={[
          ['Name', str(patient.name)],
          ['Patient ID', str(patient.patientId) && `#${patient.patientId}`],
          ['Age', str(patient.age) && `${patient.age} years`],
          ['Date of birth', fmtDate(patient.dateOfBirth, false)],
          ['Gender', str(patient.gender)],
          ['Blood group', bloodGroup(str(patient.bloodGroup))],
          ['Phone', str(contact.phone)],
          ['Email', str(contact.email)],
          ['Allergies', str(patient.allergies)],
          ['Chronic conditions', str(patient.chronicConditions)],
        ]} />
      </Section>

      <Section number={next()} title="Visit / Appointment Details">
        {!appointment ? <Muted>No appointment is linked to this report. It summarises the recorded medical history.</Muted> : (
          <>
            <div className="flex flex-wrap gap-2 mb-3">
              {str(appointment.priority) && <Chip tone={statusTone(appointment.priority)}>Priority: {appointment.priority}</Chip>}
              {str(appointment.status) && <Chip tone={statusTone(appointment.status)}>{appointment.status}</Chip>}
            </div>
            <InfoGrid rows={[
              ['Reference number', str(appointment.referenceNumber)],
              ['Date & time', fmtDate(appointment.date)],
              ['Duration', str(appointment.durationMinutes ?? obj(appointment.agent2Outcome).durationMinutes) && `${appointment.durationMinutes ?? appointment.agent2Outcome.durationMinutes} minutes`],
              ['Appointment type', humanize(str(appointment.type))],
              ['Medical checkup', str(checkup.status)],
              ['Notes', str(appointment.notes)],
            ]} />
          </>
        )}
      </Section>

      <Section number={next()} title="Doctor Information">
        <InfoGrid rows={[
          ['Doctor', str(appointment?.doctor) ?? str(report.doctorName)],
          ['Specialization', str(appointment?.doctorSpecialization)],
          ['Department', str(appointment?.doctorDepartment) ?? str(appointment?.department)],
        ]} />
      </Section>

      {(admission || resources.length > 0) && (
        <Section number={next()} title="Admission / Resources">
          <InfoGrid rows={[
            ['Allocation status', str(checkup.status)],
            ...(admission ? [
              ['Admission number', str(admission.admissionNumber)],
              ['Admission status', str(admission.status)],
              ['Admitted on', fmtDate(admission.admissionDate)],
              ['Checkup date', fmtDate(admission.checkupDate)],
              ['Ward', str(obj(admission.bed).ward)],
              ['Room', str(obj(admission.bed).room)],
              ['Bed', str(obj(admission.bed).bedNumber)],
              ['Bed status', str(obj(admission.bed).status)],
              ['Reason', str(admission.reasonForAdmission)],
            ] : []),
          ]} />
          {resources.length > 0 && (<>
            <SubTitle>Allocated resources</SubTitle>
            <DataTable columns={['Resource', 'Code', 'Location', 'Status']} statusColumn={3} rows={resources.map((r) => ({
              cells: [join([str(r.name), humanize(str(r.kind))]), str(r.code) ?? '—', join([str(r.ward), str(r.room) && `Room ${r.room}`], ', '), str(r.status) ?? '—'],
              detail: fmtDate(r.allocatedAt) && `Allocated ${fmtDate(r.allocatedAt)}`,
            }))} />
          </>)}
        </Section>
      )}

      <Section number={next()} title="Clinical Summary">
        <SubTitle>Reason for visit</SubTitle>
        {symptoms.length === 0 ? <Muted>{str(obj(data.presentingSymptoms).status) ?? 'Not recorded'}</Muted>
          : symptoms.map((s, index) => <p key={index} className="mt-0 mb-2 text-sm whitespace-pre-line">{join([str(s.chiefComplaint) && `Complaint: ${s.chiefComplaint}`, str(s.symptoms) && `Symptoms: ${s.symptoms}`], '\n')}</p>)}
        {encounters.map((encounter, index) => (
          <div key={index}>
            <SubTitle>Consultation notes{str(encounter.recordNumber) ? ` · ${encounter.recordNumber}` : ''}</SubTitle>
            <InfoGrid columns={1} rows={[['Diagnosis', str(encounter.diagnosis)], ['Examination notes', str(encounter.examinationNotes)], ['Treatment plan', str(encounter.treatmentPlan)], ['Doctor', str(encounter.doctor)]]} />
          </div>
        ))}
        {str(triage.triageResultId) && (<>
          <SubTitle>Smart Care triage</SubTitle>
          <InfoGrid columns={1} rows={[['Category', str(triage.category)], ['Priority', str(triage.priority)], ['Reason', str(triage.reason)], ['Emergency notice', str(triage.emergencyNotice)]]} />
        </>)}
        <SubTitle>Diagnoses</SubTitle>
        {diagnoses.length === 0 ? <Muted>{str(obj(data.clinicalDiagnoses).status) ?? 'No diagnoses recorded'}</Muted> : (
          <DataTable columns={['Diagnosis', 'Type', 'Severity', 'Status']} statusColumn={3} rows={diagnoses.map((d) => ({
            cells: [join([str(d.description), str(d.code) && `(${d.code})`], ' '), humanize(str(d.type)) ?? '—', str(d.severity) ?? '—', str(d.status) ?? '—'],
            detail: join([fmtDate(d.diagnosedAt, false), str(d.notes)]),
          }))} />
        )}
        {plans.length > 0 && (<>
          <SubTitle>Treatment plans</SubTitle>
          {plans.map((p, index) => <p key={index} className="mt-0 mb-2 text-sm whitespace-pre-line">{join([join([str(p.title), str(p.status) && `(${p.status})`], ' '), str(p.description), str(p.goals) && `Goals: ${p.goals}`, str(p.interventions) && `Interventions: ${p.interventions}`], '\n')}</p>)}
        </>)}
        {str(summary.text) && (<>
          <SubTitle>{str(summary.label) ?? 'Summary'}</SubTitle>
          <p className="m-0 text-sm" style={{ lineHeight: 1.5 }}>{summary.text}</p>
        </>)}
      </Section>

      <Section number={next()} title="Vital Signs">
        {vitals.length === 0 ? <Muted>{str(obj(data.vitalSigns).status) ?? 'Not recorded'}</Muted> : (<>
          <p className="mt-0 mb-2 text-xs" style={muted}>Latest reading · {fmtDate(vitals[0].recordedAt) ?? 'date not recorded'}</p>
          <InfoGrid rows={[
            ['Blood pressure', bp(vitals[0])], ['Heart rate', unit(vitals[0].heartRateBpm, 'bpm')],
            ['Temperature', unit(vitals[0].temperatureCelsius, '°C')], ['SpO₂', unit(vitals[0].oxygenSaturationSpO2, '%')],
            ['Respiratory rate', unit(vitals[0].respiratoryRateBpm, 'breaths/min')], ['Weight', unit(vitals[0].weightKg, 'kg')],
            ['Height', unit(vitals[0].heightCm, 'cm')], ['BMI', str(vitals[0].bmi)], ['Notes', str(vitals[0].notes)],
          ]} />
          {vitals.length > 1 && (<>
            <SubTitle>Earlier readings ({vitals.length - 1})</SubTitle>
            <DataTable columns={['Date', 'BP', 'Heart rate', 'Temp / SpO₂']} rows={vitals.slice(1).map((v) => ({
              cells: [fmtDate(v.recordedAt) ?? '—', bp(v) ?? '—', unit(v.heartRateBpm, 'bpm') ?? '—', join([unit(v.temperatureCelsius, '°C'), unit(v.oxygenSaturationSpO2, '%')])],
              detail: join([unit(v.weightKg, 'kg'), str(v.notes)]),
            }))} />
          </>)}
        </>)}
      </Section>

      <Section number={next()} title="Medications / Prescriptions">
        {objs(obj(data.prescriptions).records).length === 0 ? <Muted>No medications prescribed</Muted> : objs(obj(data.prescriptions).records).map((rx, index) => (
          <div key={index} className={index ? 'mt-4' : ''}>
            <div className="flex flex-wrap items-center gap-2 mb-2">
              <strong className="text-sm">{str(rx.prescriptionNumber) ?? 'Prescription'}</strong>
              {str(rx.status) && <Chip tone={statusTone(rx.status)}>{rx.status}</Chip>}
              <span className="text-xs" style={muted}>{join([fmtDate(rx.issueDate, false) && `Issued ${fmtDate(rx.issueDate, false)}`, fmtDate(rx.expiryDate, false) && `Expires ${fmtDate(rx.expiryDate, false)}`])}</span>
            </div>
            <DataTable columns={['Medicine', 'Dosage', 'Frequency', 'Duration']} emptyText="No medicines listed on this prescription" rows={objs(rx.items).map((item) => ({
              cells: [str(item.medicineName) ?? '—', join([str(item.dosage), str(item.route)]), str(item.frequency) ?? '—', str(item.durationDays) ? `${item.durationDays} days` : '—'],
              detail: str(item.specialInstructions),
            }))} />
            {str(rx.generalInstructions) && <p className="mt-2 mb-0 text-sm">Instructions: {rx.generalInstructions}</p>}
          </div>
        ))}
      </Section>

      <Section number={next()} title="Lab Tests & Results">
        <DataTable columns={['Test', 'Result', 'Status', 'Date']} statusColumn={2} emptyText="No lab tests recorded" rows={objs(obj(data.labReports).records).map((order) => {
          const result = obj(order.report)
          return {
            cells: [join([str(order.testName), str(order.category) && `(${order.category})`], ' '), str(result.resultSummary) ?? 'Pending', str(order.status) ?? '—', fmtDate(result.reportDate, false) ?? fmtDate(order.orderedAt, false) ?? '—'],
            detail: join([str(result.findings) && `Findings: ${result.findings}`, str(result.referenceRange) && `Reference range: ${result.referenceRange}`, str(result.doctorRemarks) && `Doctor remarks: ${result.doctorRemarks}`, str(order.clinicalNotes) && `Clinical notes: ${order.clinicalNotes}`], '\n'),
          }
        })} />
      </Section>

      <Section number={next()} title="AI Recommendations">
        {!isNewFormat ? (
          texts(legacyRecommendations.items).length ? (<>
            {str(legacyRecommendations.label) && <p className="mt-0 mb-2 text-xs" style={muted}>{legacyRecommendations.label}</p>}
            <Bullets items={texts(legacyRecommendations.items)} />
          </>) : <Muted>Recommendations are not available for this report.</Muted>
        ) : recommendations.available !== true ? <Muted>{str(recommendations.message) ?? 'Recommendations are not available for this report.'}</Muted> : (<>
          <Chip tone="#a78bfa">{recommendations.basis === 'condition_specific' ? 'Based on the recorded condition' : 'General healthy-living advice'}</Chip>
          {recommendationGroups.map(([title, items]) => <div key={title}><SubTitle>{title}</SubTitle><Bullets items={items} /></div>)}
          {str(recommendations.note) && <p className="mt-3 mb-0 text-xs" style={muted}>{recommendations.note}</p>}
        </>)}
      </Section>

      <Section number={next()} title="Follow-up">
        <InfoGrid emptyText="No follow-up date recorded. Follow the doctor's advice." rows={[
          ['Scheduled follow-up', recordedDates.length ? recordedDates.join(', ') : null],
          ['Suggested next check-up', str(aiFollowUp.timeframe)],
        ]} />
        {texts(aiFollowUp.monitoring).length > 0 && (<><SubTitle>What to monitor</SubTitle><Bullets items={texts(aiFollowUp.monitoring)} /></>)}
        {isNewFormat && texts(legacyRecommendations.items).length > 0 && (<><SubTitle>General guidance</SubTitle><Bullets items={texts(legacyRecommendations.items)} /></>)}
        {(str(suggested.note) ?? str(obj(data.followUp).suggestedTests)) && <p className="mt-3 mb-0 text-xs" style={muted}>{str(suggested.note) ?? str(obj(data.followUp).suggestedTests)}</p>}
      </Section>

      {Object.keys(moreDetails).length > 0 && (
        <details className="panel mb-3" style={{ padding: 16 }}>
          <summary className="cursor-pointer font-bold" style={{ color: 'var(--color-accent)' }}>More recorded details</summary>
          <div className="mt-3 space-y-3">
            {Object.entries(moreDetails).map(([key, value]) => <div key={key}><SubTitle>{label(key)}</SubTitle><GenericValue value={value} /></div>)}
          </div>
        </details>
      )}

      <footer className="text-xs mt-2" style={muted}>
        <p className="m-0">Generated by {APP_NAME} with AI assistance. This report is a summary for information only and does not replace the advice of your doctor. Advice from your doctor always comes first.</p>
        <p className="mt-1 mb-0">Report version {report.versionNumber} · {str(summary.label) ?? 'Summary'} · {fmtDate(report.createdAt)}</p>
      </footer>
    </div>
  )
}
