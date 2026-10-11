# Security & Performance Testing — Maldeniyage D.I.P

| # | Test | Tool | Where |
|---|------|------|-------|
| 1 | Security unit tests (role-based access, unauthorized data access) | xUnit | `tests/SmartHospital.Tests/Security/EmrSecurityAndNegativeTests.cs` |
| 2 | Vulnerability scan of the running API | OWASP ZAP | report saved to `tests/security/zap-report.html` |
| 3 | Load & stress test — 100 concurrent users booking appointments | Apache JMeter | `tests/performance/appointment-booking-load-test.jmx` |

All commands run from the project root. Tests 2 and 3 need the backend running **locally**:

```
dotnet run --project backend\SmartHospital.Api --launch-profile http
```

Never run ZAP or JMeter against the hosted Render API — they attack/load a shared server and write to the real database.

---

## 1. Security unit tests (xUnit)

```
dotnet test tests\SmartHospital.Tests -o "%TEMP%\smarthospital-tests" --filter "FullyQualifiedName~EmrSecurityAndNegativeTests" --logger "console;verbosity=normal"
```

Expected: `Passed! - Failed: 0, Passed: 54, Total: 54`.
(`-o` builds into a temp folder so it works while the backend is running.)

---

## 2. OWASP ZAP vulnerability scan

1. Install ZAP (Windows installer): https://www.zaproxy.org/download/
2. Start the backend locally (command above).
3. Open ZAP → choose *"No, I do not want to persist this session"*.
4. Import the API so ZAP knows every endpoint:
   **Import → Import an OpenAPI Definition from a URL** →
   `http://localhost:5100/swagger/v1/swagger.json` → Import.
5. (Optional, scans protected endpoints too) log in via Swagger
   (`http://localhost:5100/swagger`, `POST /api/auth/login`) and copy the token. In ZAP:
   **Tools → Options → Replacer → Add**: Description `JWT`, Match type `Request Header`,
   Match string `Authorization`, Replacement `Bearer <token>` → Enable.
6. Run the scan: in the **Sites** tree right-click `http://localhost:5100` →
   **Attack → Active Scan** → Start Scan. Wait for 100%.
7. Check the **Alerts** tab (High / Medium / Low / Informational).
8. Save the report: **Report → Generate Report** → Template *Traditional HTML* →
   save as `tests\security\zap-report.html`.

Result criterion: **Pass = no High-risk alerts.** Medium/Low/Informational alerts are
normal for a local HTTP dev server (e.g. missing security headers) — note them, don't hide them.

---

## 3. JMeter load & stress test (100 concurrent users)

What it does per virtual user (100 users, ramp-up 10 s, 1 iteration):

1. `POST /api/auth/login` — logs in as the seeded patient (`patient@smarthospital.local`)
2. `GET /api/appointments/doctor-options`
3. `GET /api/availability/slots?doctorId=24&date=<4 days from today>`
4. `POST /api/appointments` — books a random available slot

Pass criteria per request:
- Login / doctors / slots → HTTP 200
- Booking → **201 Created**, or **400 "This appointment slot is no longer available."**
  (100 users compete for a handful of slots — the API must reject double bookings cleanly).
  Any 5xx, timeout or other error = failure.

### Install
1. Download the **Binaries .zip** of Apache JMeter 5.6.3: https://jmeter.apache.org/download_jmeter.cgi
2. Unzip to e.g. `C:\apache-jmeter-5.6.3` (Java 8+ required — already installed).

### Run (command line — produces the HTML report)

```
C:\apache-jmeter-5.6.3\bin\jmeter.bat -n -t tests\performance\appointment-booking-load-test.jmx -l tests\performance\results.jtl -e -o tests\performance\report
```

Open `tests\performance\report\index.html` → shows total samples, error %, average /
90th-percentile response times and throughput.
Delete `results.jtl` and the `report` folder before re-running (JMeter refuses to overwrite).

### Run (GUI — for showing live)
`C:\apache-jmeter-5.6.3\bin\jmeter.bat` → **File → Open** the `.jmx` → green **Start** ▶ →
look at **Summary Report** and **View Results Tree**.

### Options (add to the command with `-J`)
| Option | Default | Example |
|---|---|---|
| `users` | 100 | `-Jusers=200` (stress) |
| `rampup` | 10 s | `-Jrampup=1` (all at once) |
| `doctorId` | 24 | `-JdoctorId=26` |
| `date` | today + 4 days | `-Jdate=2026-10-14` |

If every booking fails with "Unexpected response 400" mentioning `scheduledStart`, that date
has no available slots for the doctor — pass another `-Jdate` (check slots in Swagger first).
