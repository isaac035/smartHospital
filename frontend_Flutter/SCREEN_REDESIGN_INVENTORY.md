# Flutter screen redesign inventory

Generated from `lib/routes/app_router.dart`, screen files under `lib/screens/`, and navigation calls under `lib/`.

| File | Route / how it is reached | Status |
|---|---|---|
| `lib/screens/splash/splash_screen.dart` | `/splash`, initial route and auth initialization | restyled, not visually verified |
| `lib/screens/auth/login_screen.dart` | `/login`, auth redirect and sign-out | restyled, not visually verified |
| `lib/screens/auth/register_screen.dart` | `/register`, Login → Create account | restyled, not visually verified |
| `lib/screens/home/home_screen.dart` | `/home`, successful login or session redirect | restyled, not visually verified |
| `lib/screens/profile/profile_screen.dart` | `/profile`, Home profile action | restyled, not visually verified |
| `lib/screens/profile/edit_profile_screen.dart` | `/profile/edit`, Profile → Edit profile | restyled, not visually verified |
| `lib/screens/profile/change_password_screen.dart` | `/profile/change-password`, Profile → Change password | restyled, not visually verified |
| `lib/screens/doctors/doctor_directory_screen.dart` | `/doctors`, Home → Find a doctor | restyled, not visually verified |
| `lib/screens/doctors/doctor_details_screen.dart` | `/doctors/:doctorId`, directory doctor card | restyled, not visually verified |
| `lib/screens/doctors/doctor_schedule_screen.dart` | `/doctors/:doctorId/schedule`, Doctor details → View schedule | restyled, not visually verified |
| `lib/screens/doctors/doctor_availability_screen.dart` | `/doctors/:doctorId/availability`, routed availability view | restyled, not visually verified |
| `lib/screens/emr/patient_medical_records_screen.dart` | `/medical-records`, Home → Medical records | restyled, not visually verified |
| `lib/screens/emr/patient_vitals_screen.dart` | `/medical-records/vitals`, Medical records → Vitals | restyled, not visually verified |
| `lib/screens/emr/patient_prescriptions_screen.dart` | `/medical-records/prescriptions`, Medical records → Prescriptions | restyled, not visually verified |
| `lib/screens/emr/patient_lab_reports_screen.dart` | `/medical-records/lab-reports`, Medical records → Lab reports | restyled, not visually verified |
| `lib/screens/appointments/my_appointments_screen.dart` | `/appointments`, Home → My appointments | restyled, not visually verified |
| `lib/screens/appointments/search_doctors_screen.dart` | `/appointments/search`, Home or appointment empty state → Book | restyled, not visually verified |
| `lib/screens/appointments/doctor_slots_screen.dart` | `/appointments/doctor/:doctorProfileId`, doctor details/search result | restyled, not visually verified |
| `lib/screens/appointments/book_appointment_screen.dart` | `/appointments/book`, slot selection → Continue | restyled, not visually verified |
| `lib/screens/appointments/appointment_details_screen.dart` | `/appointments/:id`, appointment card | restyled, not visually verified |
| `lib/screens/appointments/reschedule_screen.dart` | `/appointments/:id/reschedule`, appointment details → Reschedule | restyled, not visually verified |
| `lib/screens/queue/queue_status_screen.dart` | `/queue`, Home or appointment check-in action | restyled, not visually verified |
| `lib/screens/admissions/my_admission_screen.dart` | `/my-admission`, Home → My admission | restyled, not visually verified |

## Non-page UI and conditional states

| UI / state | Source / trigger | Status |
|---|---|---|
| Appointment cancellation confirmation dialog | Appointment details → Cancel | restyled, not visually verified |
| Doctor search filter dialog | Doctor directory → Filter | restyled, not visually verified |
| Calendar date pickers | Doctor directory, doctor availability, slot selection, reschedule | restyled, not visually verified |
| Snackbars | Registration, booking, cancellation, check-in, reschedule, profile update, password change, unavailable doctor | restyled, not visually verified |
| Popup menus / overflow actions | Source scan found none | N/A — none found |
| Home header, greeting hero and service tiles | `/home`; profile avatar, confirmed logout, appointment summary and five service destinations | Restyled, not visually verified |
| Shared `IconBadge` / `FeatureTile` | All five Home service tiles; `IconBadge` is also used by inline error messages | Restyled, not visually verified |
| Loading, empty and error views | Splash, all provider-backed screens, no doctor, no appointment, no queue/admission/records | restyled, not visually verified |
| Session-expired / logged-out route | Auth redirect returns to `/login`; no separate screen file | restyled, not visually verified |
| Forgot-password / no-internet / coming-soon page | No route or screen file found | N/A — no screen exists |

All 23 routed screen files were restyled against the shared tokens and components. None was identified as dead code. Visual verification in Chrome was blocked by the app automatic-review usage limit; therefore all screen rows are marked not visually verified. Route-based loading, empty and error conditions remain code-reviewed but were not forced in a running browser session.
