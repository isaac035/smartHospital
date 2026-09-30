# Agent 2 — Appointment Optimization

Agent 2 ranks real available appointment slots for the doctor or doctors recommended by Agent 1. The ASP.NET Core API supplies all doctor and slot data from the existing database-backed services. Agent 2 cannot query the database and cannot create doctors, slots, or bookings.

## Agent 1 input contract

Smart Care's existing `POST /api/agent1/triage-doctor-match` response is consumed as produced:

```json
{
  "status": "ok",
  "patientId": 41,
  "category": "Cardiology",
  "priority": "Urgent",
  "reason": "...",
  "confidence": 0.82,
  "possibleEmergency": false,
  "emergencyNotice": null,
  "usedDefaultCategory": false,
  "recommendedDoctors": [
    { "doctorId": 12, "doctorProfileId": 4, "name": "...", "specialization": "...", "department": "Cardiology" }
  ],
  "message": null
}
```

`doctorId` is the doctor user ID used by appointment booking; `doctorProfileId` is the schedule profile ID. The patient app passes the category, priority, and recommended doctor IDs to the API. The API derives the patient ID from the JWT, validates the category and priority, and re-queries each doctor from the database. It ignores client-supplied doctor names/profile metadata.

## Slot source and ranking

The API calls the existing `IAvailabilityService.GetAvailableSlotsAsync`, the same logic exposed by `GET /api/availability/slots`. It searches from the current Sri Lanka date through the next 30 days. The service calculates schedule slots in Asia/Colombo, removes approved leave and appointment overlaps, and returns UTC start/end instants. It can include occupied schedule entries marked `Booked`; only `Available` records are sent to Agent 2.

Agent 2 sorts those records by UTC start time. Urgent searches prefer alternatives within 14 days, and Emergency searches prefer alternatives within 7 days when such real slots exist; if not, farther real slots remain available. Normal uses the full 30-day window. Priority never promotes a fabricated or unavailable slot. Gemini can write the short reason sentence only. It cannot return, reorder, or alter slot fields. If Gemini is not configured or fails, deterministic ranking and reason text still work.

There is no patient date/time preference input in the current Smart Care flow, so Agent 2 does not add one.

## Agent 2 output contract

The public API response is the source-of-truth slot data re-read from AvailabilityService after Agent 2 ranks it:

```json
{
  "patientId": 41,
  "doctorId": 12,
  "recommendedSlot": {
    "doctorId": 12,
    "doctorProfileId": 4,
    "slotStart": "2026-10-01T04:00:00Z",
    "slotEnd": "2026-10-01T04:30:00Z",
    "durationMinutes": 30,
    "reason": "This is the earliest available appointment, tomorrow."
  },
  "alternativeSlots": [],
  "message": null
}
```

The current system has no slot ID. A slot is identified by its doctor ID and UTC `slotStart`; `durationMinutes` is preserved from the backend response. The API drops any suggestion that no longer appears as Available when rechecked. If no slots exist in the search window, `recommendedSlot` is null and `message` directs the patient to another recommended doctor or manual browsing.

## API and booking handoff

Flutter calls authenticated `POST /api/agent2/optimize-appointment` with the Agent 1 category, priority, and recommended doctor IDs. ASP.NET Core owns identity and orchestration: it validates those fields, gets real availability, sends the validated Agent 1 data plus real slots to Python Agent 2, and rechecks returned choices before responding. Requests are rate-limited per patient.

After selection, Flutter opens the same existing `/appointments/book` screen and then submits through the same `POST /api/appointments` endpoint. Suggested priority pre-fills the existing, still-editable priority selector. The existing appointment service validates the slot again at write time and saves to the existing `Appointments` table. A slot taken after the suggestion receives the existing booking conflict response.

Manual browsing remains available through the existing doctor slot screens.

## Environment variables

Python service `.env` (keep local and out of git):

| Variable | Purpose |
|---|---|
| `GEMINI_API_KEY` | Optional key used only to explain the selected real slot. |
| `GEMINI_MODEL` | Gemini model used for the explanation. |
| `AGENT2_INTERNAL_API_KEY` | Required shared secret; missing means protected routes fail closed. |
| `AGENT2_MAX_ALTERNATIVES` | Maximum alternatives returned by Python (0–10). |
| `AGENT2_GEMINI_TIMEOUT_SECONDS` | Explanation timeout; failure falls back to deterministic text. |

ASP.NET Core environment:

| Variable | Default | Purpose |
|---|---|---|
| `Agent2__BaseUrl` | `http://localhost:8002` | Private Agent 2 service URL. |
| `Agent2__InternalApiKey` | empty | Must match `AGENT2_INTERNAL_API_KEY`. |
| `Agent2__TimeoutSeconds` | `25` | API-to-Python timeout. |
| `Agent2__RateLimitPermits` | `10` | Per-patient calls per window. |
| `Agent2__RateLimitWindowMinutes` | `10` | Rate limit window. |

Do not put backend secrets or Gemini keys in Flutter or committed settings files.

## Install and run

```powershell
cd ai/agent_2_appointment_optimization
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
Copy-Item .env.example .env
# Set GEMINI_API_KEY (optional) and AGENT2_INTERNAL_API_KEY in .env
uvicorn app.main:app --host 127.0.0.1 --port 8002
```

Run the ASP.NET Core API with `Agent2__BaseUrl` set to the private service URL and `Agent2__InternalApiKey` set to the same secret. Python exposes no public documentation routes; only `/health` is unauthenticated. Flutter talks only to the ASP.NET Core API.
