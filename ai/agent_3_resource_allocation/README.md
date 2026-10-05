# Agent 3 — Medical Checkup and Resource Recommendations

Agent 3 runs only after an appointment has been successfully booked. It ranks real available beds and equipment for the appointment's stored clinical context. The Python service has no database connection and cannot allocate anything. The ASP.NET Core API owns identity, candidate reads, final validation, and reservation persistence.

## Appointment handoff and Agent 1/2 data

Agent 1 emits `category`, `priority`, and recommended doctors with `doctorId`, `doctorProfileId`, `specialization`, and `department`. Agent 2 accepts the category/priority and real doctor IDs, then returns a real `recommendedSlot` and `alternativeSlots`. After the user selects a slot, Flutter saves it through the existing `POST /api/appointments` flow. Only after that succeeds does Flutter show **Appointment Confirmed** and ask whether the patient wants a medical checkup.

Choosing **No, Finish** skips every Agent 3 endpoint and opens the booked appointment. Choosing **Yes, Continue** posts the real AppointmentId to the API. Agent 3 is never involved in booking, slot search, check-in, or queue handling.

The existing Flutter booking body does not send Agent 1's category or `DepartmentId`; `AppointmentService` saves the nullable request department as supplied, which is currently null for this Flutter path. The persisted appointment's doctor ID links to a Users row. That ID matches `Doctor.UserId`, from which the API reads the current `Doctor.Department.Name` and `Doctor.Specialization`. The specialty resolution order is:

1. `Appointment.Department.Name` when the appointment has a department relation.
2. The linked doctor's `Department.Name`.
3. The linked doctor's `Specialization`.

Priority is read from `RequestedPriority ?? Priority`, mirroring the appointment detail's effective priority. It ranks candidates only after clinical fit is established.

## Existing inventory and persisted reservation

The API reads the current database records:

- Ward: `Name`, `Code`, enum `Type`, `IsActive`.
- Room: `RoomNumber`, `IsActive`, and its ward.
- Bed: `BedNumber`, enum `Type`, `Status`, `IsActive`.
- MedicalResource: `ResourceCode`, `Name`, enum `Category`, `LocationDescription`, `Status`, `IsActive` and optional ward/room.

Availability is taken only from `Bed.Status == Available` or `MedicalResource.Status == Available`. Appointment scheduled start/end and duration are context only and are never queried to infer resource availability. Active ward/room/bed/resource checks exclude deactivated inventory.

The existing `BedAllocation` is admission-only (`AdmissionId` + `BedId`), so it cannot link a bed to an appointment or represent equipment. Migration `20261002120000_AddAppointmentResourceAllocations` adds a single appointment-linked allocation table with exactly one bed or equipment FK, patient, doctor, department, specialty, priority, and active/release timestamps. An active bed allocation changes the bed status to `Reserved`; equipment changes to `InUse`. The transaction and conditional `UPDATE ... WHERE Status = Available` ensure only one concurrent selection succeeds. Partial unique indexes protect active reservations. No bed/equipment inventory is copied or invented.

## Deterministic ranking rules

The API sends only available real records. Matching normalizes names/type/category text into specialty groups: cardiology/cardiac/heart/cardiovascular; maternity/postnatal/obstetrics/gynecology/birthing; pediatric/child/neonatal/NICU; emergency/accident/trauma; surgical/orthopedic; ICU/critical care; and general/internal medicine. Matching is dynamic over stored ward names/types and resource text, never a hardcoded ward ID.

Scoring constants:

- Specialty match: **100** points.
- General-purpose fallback: **35** points.
- Unclassified/general-purpose candidate: **20** points.
- Different specialized ward/type: **−30** points.
- Emergency adds **4** points only within a specialty match when the candidate itself indicates ICU/critical/intensive care. Urgent adds **2** only within a specialty match for a high-dependency/step-down/monitor candidate. Normal adds **0**.

The maximum priority bonus is four, far below the 65-point gap between a match and a general fallback; priority cannot send a cardiology case to an emergency ward. Stable sorting by resource kind, name, and database ID resolves ties. A general candidate naturally ranks above a mismatched specialized ward when no specialty-specific resource is available. Equipment uses these same availability, matching, ranking, and fallback rules.

Python is deterministic and does not require Gemini. If the Python process is unavailable, ASP.NET applies the same deterministic fallback over its database candidates. There is no LLM path that can introduce or change candidates.

## Endpoints and orchestration

Patient JWT endpoints:

- `POST /api/agent3/recommendations` with `{ "appointmentId": 123 }`: API verifies ownership, reads the appointment and current available inventory, calls Python, validates returned IDs against its supplied list, and returns ranked recommendations.
- `POST /api/agent3/allocations` with `{ "appointmentId": 123, "selectedResourceId": 45, "kind": "bed" }`: API rechecks appointment ownership, active inventory, current Available status, and active allocations, then reserves the exact selected record transactionally. A race returns `409` with “The selected resource is no longer available. Please select another available resource.” Flutter refreshes recommendations; the API never picks another resource for the patient.
- `GET /api/appointments/{id}` now includes `reservedResources` when allocations exist. When the patient declines, selects nothing, or reservation fails, it remains empty.

Python internal endpoint: `POST /v1/recommendations`, protected by `X-Internal-Api-Key`. `GET /health` is the only unauthenticated route. The service only ranks the API's supplied candidate list.

Flutter presents the Yes/No prompt after a successful booking, lists candidates with the first marked Recommended, and lets the patient choose or finish without a resource. Appointment details show reserved bed/equipment only when persisted. React Admin's existing appointment detail view displays the same `reservedResources` field alongside appointment details.

## Environment and local run

Python `.env` (copy `.env.example`; do not commit the real file):

| Name | Purpose |
| --- | --- |
| `AGENT3_INTERNAL_API_KEY` | Required shared API-to-Python secret; empty fails closed. |
| `AGENT3_MAX_RECOMMENDATIONS` | Candidate response cap, default 30. |

ASP.NET Core environment variables:

| Name | Default | Purpose |
| --- | --- | --- |
| `Agent3__BaseUrl` | `http://localhost:8003` | Private Python service address. |
| `Agent3__InternalApiKey` | empty | Must match `AGENT3_INTERNAL_API_KEY`; deterministic API fallback applies if Python is unavailable. |
| `Agent3__TimeoutSeconds` | 20 | API-to-Python timeout. |

Run from this folder with `uvicorn app.main:app --host 127.0.0.1 --port 8003`. Apply the backend migration before using the allocation/detail endpoints. Run service tests with `pytest`.

## Scope and verification limits

Agent 1, Agent 2, appointment creation, slot availability, booking conflict checks, check-in, and queue/priority logic are not changed. This workspace has no live database connection, so real inventory values and full database-backed concurrency/e2e behavior must be confirmed in the configured deployment environment.
