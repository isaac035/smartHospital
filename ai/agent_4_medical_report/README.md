# Agent 4 — Medical Report Generation

Agent 4 creates an immutable, versioned report snapshot from patient and encounter data that ASP.NET Core reads from the Smart Hospital database. The Python service receives only that assembled data and produces a clearly labeled synthesis and advisory suggestions. It never connects to the database.

## Confirmed data sources and limits

The backend has persisted entities and APIs for `MedicalRecord` encounters/history, `VitalSign`, `Prescription` and prescription items, `LabOrder` and `LabReport`, `Appointment`, `Admission`/`BedAllocation`, and `AppointmentResourceAllocation`. Medical timeline events are also backed by these real database records. There is no separate Consultation or Encounter entity; the report uses `MedicalRecord` and appointment records for that context. A given patient may have no rows in any of these modules, and the report returns explicit empty-state text in that case.

Agent 1's original symptoms and triage result (category, priority, reason, confidence, emergency flag, and status) are persisted in `Agent1TriageResults` and linked to the appointment created from that triage flow. The report uses this saved result when available; for appointments booked without Agent 1, it clearly marks triage details as unavailable and does not infer them from appointment notes. Agent 2's selected slot is represented by the actual appointment record. Agent 3's actual resource allocation and linked admission/bed allocation are read by appointment ID; when Agent 3 did not run or did not save an allocation, the report records that no allocation is available and still generates from Agents 1 and 2 and the rest of the patient's chart.

Whether a particular patient has any records must be determined from that patient's rows in the running database; no live patient database was inspected during implementation.

## Report structure and versioning

`recordedData` contains database-backed patient, appointment, symptoms, triage provenance, checkup/admission/resources, previous history, vitals, prescriptions, lab orders/results, timeline, encounters, follow-up dates, and source record IDs. Empty modules include a plain status such as `No vital signs recorded`. Historical medical records are marked `Previously recorded` and kept separate from the selected encounter.

`aiSummary` is a generated synthesis or is explicitly labeled `Non-AI fallback summary`. `aiRecommendations` is a distinct list marked advisory and for clinical review. Suggestions never count as completed tests. Emergency/Urgent/Normal language follows the recorded appointment priority and does not predict disease.

Every report also includes the same four general wellness suggestions (hydration, light activity as tolerated, adequate rest, and avoiding documented triggers) plus a deterministic follow-up sentence selected from the persisted triage/appointment priority. These use cautious suggestion wording and never invent a follow-up date or interval. Model-generated recommendation text is not exposed in the report; the persisted priority-specific wording is used for both the Python and backend fallback paths.

`AiMedicalReport` stores `ReportId`, patient and optional appointment references, creation timestamp, a per-patient version number, and the structured report JSON. Every generation inserts a new row. A unique patient/version index and serializable insert transaction prevent overwrites. An optional client generation ID makes a timed-out Flutter request safe to retry without duplicating the saved report; source entity IDs remain embedded as references in the snapshot.

## API

All endpoints require JWT authentication. Patient IDs for patient callers are derived from the JWT, and detail retrieval verifies report ownership. Admin, Staff, and Doctor callers can use the patient ID passed to list/generate reports under the existing role convention.

- `POST /api/agent4/reports` — generate and persist a report; optional `appointmentId` selects a patient-owned appointment. When omitted, the latest appointment is included if one exists.
- `GET /api/agent4/reports?patientId=...` — list report metadata, newest first.
- `GET /api/agent4/reports/{reportId}` — retrieve a report detail.

ASP.NET assembles live DB data, calls Python, applies a local structured fallback if the Python service is unavailable, and persists the result. The Python private endpoint is `POST /v1/reports/reason` and requires `X-Internal-Api-Key`.

Flutter automatically requests a report after the patient declines a checkup, after a successful resource allocation, or when Agent 3 returns no suitable resources. On success it opens the report detail; on failure it returns the patient to the appointment and directs them to generate it from Records. The request includes the booked appointment ID and the patient's checkup choice so the snapshot uses the correct persisted encounter and labels a declined checkup `Not requested`.

## Configuration

Backend environment variables use the existing configuration binding convention:

- `Agent4__BaseUrl` (default `http://localhost:8004`)
- `Agent4__InternalApiKey` (must match the Python key)
- `Agent4__TimeoutSeconds` (default `30`)

Python reads `AGENT4_INTERNAL_API_KEY`, optional `GEMINI_API_KEY`, and optional `GEMINI_MODEL` (default `gemini-2.5-flash`). Copy `.env.example` to a local `.env`; do not commit secrets. Without a Gemini key or when Gemini is unavailable, the Python service returns a `non_ai_fallback`, and ASP.NET has a second deterministic fallback if Python itself is unavailable.

Run locally from this directory with `uvicorn app.main:app --env-file .env --host 0.0.0.0 --port 8004` after installing `requirements.txt`.

## Client surfaces

React adds the AI Medical Reports section to the existing Laboratory & Diagnostic Reports patient view and reuses its patient name/ID lookup. Flutter adds one Health Modules card and report list/detail screens inside Records; it does not add a bottom-navigation item. Both clients use the authenticated ASP.NET API and receive only the logged-in patient's data for patient-role requests.
