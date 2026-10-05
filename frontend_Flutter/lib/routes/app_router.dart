import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../screens/splash/splash_screen.dart';
import '../screens/auth/login_screen.dart';
import '../screens/auth/register_screen.dart';
import '../screens/home/home_screen.dart';
import '../screens/profile/profile_screen.dart';
import '../screens/profile/edit_profile_screen.dart';
import '../screens/profile/change_password_screen.dart';
import '../screens/doctors/doctor_directory_screen.dart';
import '../screens/doctors/doctor_details_screen.dart';
import '../screens/doctors/doctor_availability_screen.dart';
import '../screens/emr/patient_medical_records_screen.dart';
import '../screens/emr/patient_clinical_history_screen.dart';
import '../screens/emr/patient_vitals_screen.dart';
import '../screens/emr/patient_prescriptions_screen.dart';
import '../screens/emr/patient_lab_reports_screen.dart';
import '../screens/emr/ai_medical_reports_screen.dart';
import '../screens/appointments/my_appointments_screen.dart';
import '../screens/appointments/appointment_details_screen.dart';
import '../screens/appointments/search_doctors_screen.dart';
import '../screens/appointments/doctor_slots_screen.dart';
import '../screens/appointments/book_appointment_screen.dart';
import '../screens/appointments/checkup_flow_screen.dart';
import '../screens/appointments/reschedule_screen.dart';
import '../screens/queue/queue_status_screen.dart';
import '../screens/smart_care/smart_care_screen.dart';
import '../screens/admissions/my_admission_screen.dart';
import '../widgets/patient_navigation_shell.dart';

// Unique GlobalKey for root navigator and each bottom navigation branch.
// Each branch MUST have its own unique key to avoid duplicate key conflicts.
final GlobalKey<NavigatorState> _rootNavigatorKey =
    GlobalKey<NavigatorState>(debugLabel: 'rootNav');
final GlobalKey<NavigatorState> _homeNavigatorKey =
    GlobalKey<NavigatorState>(debugLabel: 'homeNav');
final GlobalKey<NavigatorState> _appointmentsNavigatorKey =
    GlobalKey<NavigatorState>(debugLabel: 'appointmentsNav');
final GlobalKey<NavigatorState> _doctorsNavigatorKey =
    GlobalKey<NavigatorState>(debugLabel: 'doctorsNav');
final GlobalKey<NavigatorState> _admissionsNavigatorKey =
    GlobalKey<NavigatorState>(debugLabel: 'admissionsNav');
final GlobalKey<NavigatorState> _recordsNavigatorKey =
    GlobalKey<NavigatorState>(debugLabel: 'recordsNav');

class AppRouter {
  static GoRouter? _router;

  /// Resets the cached router instance (e.g. on full re-init or tests).
  static void reset() {
    _router = null;
  }

  /// Returns the singleton GoRouter instance so it is not recreated
  /// on every build or hot reload, preventing duplicate GlobalKey errors.
  static GoRouter router(BuildContext context) {
    return _router ??= _createRouter(context);
  }

  static GoRouter _createRouter(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);

    return GoRouter(
      navigatorKey: _rootNavigatorKey,
      refreshListenable: authProvider,
      initialLocation: '/splash',
      redirect: (BuildContext context, GoRouterState state) {
        final isAuth = authProvider.isAuthenticated;
        final isInitialized = authProvider.isInitialized;

        final isSplash = state.matchedLocation == '/splash';
        final isLogin = state.matchedLocation == '/login';
        final isRegister = state.matchedLocation == '/register';

        if (!isInitialized && !isSplash) {
          return '/splash';
        }

        if (isInitialized) {
          if (!isAuth) {
            if (!isLogin && !isRegister) return '/login';
          } else {
            if (isLogin || isRegister || isSplash) return '/home';
          }
        }

        return null;
      },
      routes: [
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/splash',
          builder: (context, state) => const SplashScreen(),
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/login',
          builder: (context, state) => const LoginScreen(),
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/register',
          builder: (context, state) => const RegisterScreen(),
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/profile',
          builder: (context, state) => const ProfileScreen(),
          routes: [
            GoRoute(
              parentNavigatorKey: _rootNavigatorKey,
              path: 'edit',
              builder: (context, state) => const EditProfileScreen(),
            ),
            GoRoute(
              parentNavigatorKey: _rootNavigatorKey,
              path: 'change-password',
              builder: (context, state) => const ChangePasswordScreen(),
            ),
          ],
        ),

        // Stateful bottom navigation shell with 5 separate branches,
        // each having its own unique navigatorKey.
        StatefulShellRoute.indexedStack(
          builder: (context, state, navigationShell) {
            return PatientNavigationShell(
              navigationShell: navigationShell,
            );
          },
          branches: [
            // Branch 0: Home
            StatefulShellBranch(
              navigatorKey: _homeNavigatorKey,
              routes: [
                GoRoute(
                  path: '/home',
                  builder: (context, state) => const HomeScreen(),
                ),
              ],
            ),

            // Branch 1: Appointments
            StatefulShellBranch(
              navigatorKey: _appointmentsNavigatorKey,
              routes: [
                GoRoute(
                  path: '/appointments',
                  builder: (context, state) => const MyAppointmentsScreen(),
                ),
              ],
            ),

            // Branch 2: Doctors
            StatefulShellBranch(
              navigatorKey: _doctorsNavigatorKey,
              routes: [
                GoRoute(
                  path: '/doctors',
                  builder: (context, state) => const DoctorDirectoryScreen(),
                ),
              ],
            ),

            // Branch 3: Admissions
            StatefulShellBranch(
              navigatorKey: _admissionsNavigatorKey,
              routes: [
                GoRoute(
                  path: '/my-admission',
                  builder: (context, state) => const MyAdmissionScreen(),
                ),
              ],
            ),

            // Branch 4: Records
            StatefulShellBranch(
              navigatorKey: _recordsNavigatorKey,
              routes: [
                GoRoute(
                  path: '/medical-records',
                  builder: (context, state) =>
                      const PatientMedicalRecordsScreen(),
                ),
              ],
            ),
          ],
        ),

        // Queue Status (accessible from Home card, displayed on root navigator)
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/queue',
          builder: (context, state) => const QueueStatusScreen(),
        ),

        // Smart Care (AI-assisted triage + doctor matching; hands off to booking)
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/smart-care',
          builder: (context, state) => const SmartCareScreen(),
        ),

        // Search Doctors for Appointment Booking
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/appointments/search',
          builder: (context, state) => const SearchDoctorsScreen(),
        ),

        // Doctor Detail Flows (full screen on root navigator)
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/doctors/:doctorId',
          builder: (context, state) => DoctorDetailsScreen(
            doctorId: int.parse(state.pathParameters['doctorId']!),
          ),
          routes: [
            GoRoute(
              path: 'timeline',
              builder: (context, state) => const PatientClinicalHistoryScreen(),
            ),
            GoRoute(
              path: 'vitals',
              builder: (context, state) => const PatientVitalsScreen(),
            ),
            GoRoute(
              path: 'prescriptions',
              builder: (context, state) => const PatientPrescriptionsScreen(),
            ),
            GoRoute(
              parentNavigatorKey: _rootNavigatorKey,
              path: 'availability',
              builder: (context, state) => DoctorAvailabilityScreen(
                doctorId: int.parse(state.pathParameters['doctorId']!),
              ),
            ),
          ],
        ),

        // Medical Records Sub-screens (full screen on root navigator)
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/medical-records/vitals',
          builder: (context, state) => const PatientVitalsScreen(),
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/medical-records/prescriptions',
          builder: (context, state) => const PatientPrescriptionsScreen(),
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/medical-records/lab-reports',
          builder: (context, state) => const PatientLabReportsScreen(),
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/medical-records/ai-reports',
          builder: (context, state) => const AiMedicalReportsScreen(),
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/medical-records/ai-reports/:reportId',
          builder: (context, state) => AiMedicalReportDetailScreen(
            reportId: state.pathParameters['reportId']!,
          ),
        ),

        // Appointment Booking Flows (full screen on root navigator)
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/appointments/confirmed/:appointmentId',
          builder: (context, state) => CheckupFlowScreen(
            appointmentId: int.parse(state.pathParameters['appointmentId']!),
          ),
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/appointments/book',
          builder: (context, state) {
            final extra = state.extra as Map<String, dynamic>;
            return BookAppointmentScreen(
              doctorId: extra['doctorId'] as int,
              slotStart: extra['slotStart'] as String,
              durationMinutes: extra['durationMinutes'] as int,
              initialPriority: extra['initialPriority'] as int? ?? 1,
              triageResultId: extra['triageResultId'] as int?,
            );
          },
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/appointments/doctor/:doctorProfileId',
          builder: (context, state) {
            final doctorProfileId =
                int.parse(state.pathParameters['doctorProfileId']!);
            final bookingDoctorId = int.tryParse(
                  state.uri.queryParameters['userId'] ?? '',
                );
            if (bookingDoctorId == null) {
              return const Scaffold(
                body: Center(
                  child: Text(
                    'This doctor is not linked to a booking account.',
                  ),
                ),
              );
            }
            return DoctorSlotsScreen(
              doctorProfileId: doctorProfileId,
              bookingDoctorId: bookingDoctorId,
              triageResultId: int.tryParse(state.uri.queryParameters['triageResultId'] ?? ''),
            );
          },
        ),
        GoRoute(
          parentNavigatorKey: _rootNavigatorKey,
          path: '/appointments/:id',
          builder: (context, state) => AppointmentDetailsScreen(
            appointmentId: int.parse(state.pathParameters['id']!),
          ),
          routes: [
            GoRoute(
              parentNavigatorKey: _rootNavigatorKey,
              path: 'reschedule',
              builder: (context, state) => RescheduleScreen(
                appointmentId: int.parse(state.pathParameters['id']!),
              ),
            ),
          ],
        ),
      ],
    );
  }
}
