# Agent 1 — Clinical Triage + Doctor Matching

Patients see this as **Smart Care** in the app. A patient describes their symptoms or what they need in their own words. Agent 1 then suggests:

- which of the hospital's real specialties to book with
- how urgent the booking seems
- real doctors they can book in that specialty

Agent 1 only recommends. It **never diagnoses, never prescribes, and never books**. The patient picks a doctor and finishes in the existing appointment flow, which submits through the unchanged `POST /api/appointments`.

This is an isolated addition. Nothing in the Appointment & Queue Management module was changed.

## Responsibilities

1. **Clinical triage.** Free text goes in; out comes a category, priority, reason, confidence and emergency flag.
   - **Category** is restricted to the names of the hospital's currently active departments. If the model is unsure (confidence below `AGENT1_MIN_CONFIDENCE`) or returns anything outside the list, the category falls back to General Medicine and the reply says so.
   - **Priority** is exactly one of the existing `AppointmentPriority` values: `Normal`, `Urgent` or `Emergency`.
   - **Reason** is one or two plain sentences. Diagnostic or medication wording ("you have", "diagnosed", doses, drug names) is replaced with a neutral template.
   - **Emergency flag:** a deterministic red-flag screen runs on every request, independently of the model. It covers chest pain, breathing difficulty, stroke signs, severe bleeding, loss of consciousness, seizures, anaphylaxis and self-harm, and it can only raise the flag, never lower it. The flag forces priority to `Emergency` and adds the emergency notice.
2. **Doctor matching.** It picks doctors whose **department** matches the validated category. Both sides are normalised the same way (case- and punctuation-insensitive). Candidates come only from the list of bookable doctors the ASP.NET Core API sends. The service has no database access and cannot invent doctors.

## Architecture

```
Flutter ──JWT──► ASP.NET Core API  POST /api/agent1/triage-doctor-match
                  • patient id from the JWT (never the request body)
                  • per-patient rate limit
                  • loads the active departments  → categories
                  • loads the bookable doctors    → candidateDoctors
                  │
                  ├── X-Internal-Api-Key ──► Python Agent 1 (this service)  POST /v1/triage-doctor-match
                  │                           • red-flag screen + LLM: Gemini (default) or Claude,
                  │                             with JSON-schema-constrained output
                  │                           • validates category/priority/reason/confidence
                  │                           • matches doctors from candidateDoctors
                  │
                  • re-verifies every returned doctor id against the database
                  • returns the database's own doctor fields (never the AI's)
                  ▼
Flutter: recommendation → "Select Doctor" → existing /appointments/doctor/:profileId?userId=…
         (existing slot picker → existing booking screen → POST /api/appointments)
```

**What counts as a bookable doctor** (checked in `Agent1TriageService.BookableDoctors`, on both the way out and the way back). These are the same checks the booking path enforces:

- the doctor profile is `Active` and has a linked user account (`UserId`), as `AvailabilityService` requires
- that user is `Active` and has the `Doctor` role, as `AppointmentService.BookAppointmentAsync` requires
- **plus** the doctor's department is active and they have at least one active schedule. Without a schedule a recommendation would lead to a doctor with no bookable times.

The codebase has no `IsAvailableForOnlineBooking` flag, so it isn't used.

## Final JSON contracts

### Flutter → API: `POST /api/agent1/triage-doctor-match` (Patient JWT required)

Request:
```json
{ "symptoms": "I get chest discomfort when I climb stairs" }
```

Response `200`:
```json
{
  "status": "ok",
  "patientId": 4,
  "category": "Cardiology",
  "priority": "Urgent",
  "reason": "Chest discomfort on exertion could suggest seeing a heart specialist.",
  "confidence": 0.82,
  "possibleEmergency": false,
  "emergencyNotice": null,
  "usedDefaultCategory": false,
  "recommendedDoctors": [
    { "doctorId": 11, "doctorProfileId": 2, "name": "Ashan Wijesekara",
      "specialization": "Interventional Cardiology", "department": "Cardiology" }
  ],
  "message": null
}
```

- `status` values:
  - `ok`: there is a recommendation and at least one doctor
  - `no_doctors`: triage worked, but no bookable doctor exists for the category. `message` explains, and the app offers manual browsing.
  - `ai_unavailable`: Agent 1 is down, timed out, returned bad output, or isn't configured. The category and doctors are `null`/empty, `message` explains, and `possibleEmergency` still comes from the red-flag screen.
- `doctorId` is the doctor's **user ID**, which is what `POST /api/appointments` books against. `doctorProfileId` is the `Doctors` table ID the patient app's slot screen uses.
- `400` means the symptoms text was empty or not 3–1000 characters. `401`/`403` means the caller isn't logged in as a patient. `429` means the rate limit was hit. `500` means a server error; its body has the same shape with `status: "ai_unavailable"`.

### API → Python: `POST /v1/triage-doctor-match` (header `X-Internal-Api-Key` required)

Request:
```json
{
  "patientId": 4,
  "text": "I get chest discomfort when I climb stairs",
  "categories": ["Cardiology", "Dermatology", "General Medicine", "Neurology", "Orthopedics", "Pediatrics"],
  "defaultCategory": "General Medicine",
  "candidateDoctors": [
    { "doctorId": 11, "doctorProfileId": 2, "name": "Ashan Wijesekara",
      "specialization": "Interventional Cardiology", "department": "Cardiology" }
  ]
}
```

- **`200`:** the same fields as the API response above, minus `status`.
- **`503`:** the AI failed or isn't configured, and the body is `{ "detail": "...", "possibleEmergency": bool, "emergencyNotice": string|null }`.
- **`401`:** wrong or missing internal key.
- **`422`:** invalid input.

## Environment variables

### Python service (`.env`, git-ignored; see `.env.example`)

| Variable | Default | Purpose |
|---|---|---|
| `AI_PROVIDER` | `gemini` | `gemini` (Google Gemini) or `anthropic` (Claude) |
| `AI_API_KEY` | – (required) | API key for that provider. For Gemini, create one at https://aistudio.google.com/apikey |
| `AI_MODEL` | empty | Model ID. When empty, uses the provider default: `gemini-3.8-flash` for Gemini, `claude-opus-5-5` for Claude |
| `AI_FALLBACK_MODELS` | `gemini-3.5-flash-lite,gemini-flash-lite-latest` | Gemini only: models tried in order if the main one is overloaded, rate-limited or unavailable to the key. Empty turns fallback off. |
| `AI_TIMEOUT_SECONDS` | `20` | Timeout for each LLM call |
| `AI_ENABLE_FALLBACKS` | `true` | Claude only: if the model declines, retry on a server-side fallback model |
| `AGENT1_INTERNAL_API_KEY` | – (required) | Shared secret. When it's unset, the service rejects every request. |
| `AGENT1_MIN_CONFIDENCE` | `0.5` | Below this confidence the category falls back to the default |
| `AGENT1_MAX_RECOMMENDATIONS` | `5` | Maximum doctors returned |

### ASP.NET Core API (environment variables; never put these in `appsettings.json`)

| Variable | Default | Purpose |
|---|---|---|
| `Agent1__BaseUrl` | `http://localhost:8001` | Where the Python service is running |
| `Agent1__InternalApiKey` | – | Must equal `AGENT1_INTERNAL_API_KEY`. When it's empty, Smart Care replies `ai_unavailable`. |
| `Agent1__TimeoutSeconds` | `25` | Timeout for the API → Python call |
| `Agent1__RateLimitPermits` / `Agent1__RateLimitWindowMinutes` | `10` / `10` | Requests allowed per patient per time window |

## Install and run

```bash
cd ai/agent_1_clinical_triage_doctor_matching
python -m venv .venv
.venv\Scripts\activate            # Windows  (macOS/Linux: source .venv/bin/activate)
pip install -r requirements.txt
copy .env.example .env            # then fill in AI_API_KEY and AGENT1_INTERNAL_API_KEY
uvicorn app.main:app --host 127.0.0.1 --port 8001
```

Bind to `127.0.0.1`, or a private network only. The service is internal: Flutter and the internet must never reach it directly, and every triage route also requires the internal key. `GET /health` is the only route that works without the key.

Start the API with the matching secret:
```powershell
$env:Agent1__InternalApiKey = "<same value as AGENT1_INTERNAL_API_KEY>"
dotnet run --project backend/SmartHospital.Api
```

Run the tests (they use a fake model, so no API key is needed):
```bash
pytest
```
The backend's own tests are in `tests/SmartHospital.Tests/Unit/Services/Agent1TriageServiceTests.cs`.

## Hand-off to the existing appointment system

In the patient app, **Select Doctor** opens the existing route, `/appointments/doctor/{doctorProfileId}?userId={doctorId}`, with that doctor already selected. From there the patient picks a date, time and priority in the existing screens. Booking goes through the unchanged `POST /api/appointments` into the same `Appointments` table, so an AI-assisted booking looks exactly like a manual one.

The suggested priority is shown on the recommendation screen, but it is **not** pre-filled in the booking screen yet. Pre-filling would mean changing Appointment-module files (the router, `DoctorSlotsScreen` and `BookAppointmentScreen`), so it's left for the module owners to decide.

At every step the patient can choose **Browse doctors manually** (`/appointments/search`) instead.

## For future agents (Agent 2+)

Future agents, such as appointment optimisation, resource allocation or follow-up coordination, can live alongside this one under `ai/`, and can use Agent 1's output as input:

- `category`: a real, active department name
- `priority`: an `AppointmentPriority` name
- `possibleEmergency` and `confidence`
- `recommendedDoctors[].doctorId`: already verified as bookable

A scheduling agent could take `{patientId, category, priority, recommendedDoctors}` and propose times using the existing `/api/availability/slots`.

To keep things composable, follow the same pattern:
- run as an internal service behind its own `X-Internal-Api-Key`
- be called only by the ASP.NET Core API, which owns identity and data
- have the API re-verify anything the AI returns before a client sees it
