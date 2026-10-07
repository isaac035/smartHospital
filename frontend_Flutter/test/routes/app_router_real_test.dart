import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:smart_hospital/core/network/api_client.dart';
import 'package:smart_hospital/core/storage/secure_storage_service.dart';
import 'package:smart_hospital/models/user_model.dart';
import 'package:smart_hospital/providers/appointment_provider.dart';
import 'package:smart_hospital/providers/auth_provider.dart';
import 'package:smart_hospital/providers/doctor_provider.dart';
import 'package:smart_hospital/routes/app_router.dart';
import 'package:smart_hospital/services/appointment_service.dart';
import 'package:smart_hospital/services/consultation_type_service.dart';
import 'package:smart_hospital/services/department_service.dart';
import 'package:smart_hospital/services/doctor_service.dart';
import 'package:smart_hospital/services/leave_service.dart';
import 'package:smart_hospital/services/schedule_service.dart';
import 'package:smart_hospital/widgets/app_ui.dart';
import 'package:smart_hospital/widgets/patient_navigation_shell.dart';
import 'package:smart_hospital/screens/home/home_screen.dart';
import 'package:smart_hospital/screens/appointments/my_appointments_screen.dart';
import 'package:smart_hospital/screens/doctors/doctor_directory_screen.dart';
import 'package:smart_hospital/screens/admissions/my_admission_screen.dart';
import 'package:smart_hospital/screens/emr/patient_medical_records_screen.dart';
import 'package:smart_hospital/screens/queue/queue_status_screen.dart';

class FakeAuthProvider extends ChangeNotifier implements AuthProvider {
  @override
  bool get isLoading => false;
  @override
  bool get isInitialized => true;
  @override
  bool get isAuthenticated => true;
  @override
  UserModel? get currentUser => UserModel(
        id: 1,
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        role: 'Patient',
        phoneNumber: '1234567890',
        status: 'Active',
      );
  @override
  String? get error => null;
  @override
  Map<String, String> get fieldErrors => const {};

  @override
  void clearError() {}
  @override
  void clearFieldError(String field) {}
  @override
  Future<void> initialize() async {}
  @override
  Future<bool> login(String email, String password) async => true;
  @override
  Future<bool> register(String firstName, String lastName, String email,
          String password, String phone) async =>
      true;
  @override
  Future<void> logout() async {}
  @override
  Future<bool> updateProfile(
          String firstName, String lastName, String phone) async =>
      true;
  @override
  Future<bool> changePassword(
          String currentPassword, String newPassword) async =>
      true;
}

void main() {
  testWidgets('Test real AppRouter tree structure', (tester) async {
    final originalOnError = FlutterError.onError;
    FlutterError.onError = (details) {
      if (details.exceptionAsString().contains('ListTile background color')) {
        return;
      }
      originalOnError?.call(details);
    };
    addTearDown(() => FlutterError.onError = originalOnError);

    final authProvider = FakeAuthProvider();
    final storage = SecureStorageService();
    final apiClient = ApiClient(storage);
    final doctorService = DoctorService(apiClient);
    final scheduleService = ScheduleService(apiClient);
    final leaveService = LeaveService(apiClient);
    final deptService = DepartmentService(apiClient);
    final consultService = ConsultationTypeService(apiClient);
    final doctorProvider = DoctorProvider(
      doctorService,
      scheduleService,
      leaveService,
      deptService,
      consultService,
    );
    final apptService = AppointmentService(apiClient);
    final apptProvider = AppointmentProvider(apptService);

    AppRouter.reset();

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<AuthProvider>.value(value: authProvider),
          ChangeNotifierProvider<DoctorProvider>.value(value: doctorProvider),
          ChangeNotifierProvider<AppointmentProvider>.value(value: apptProvider),
        ],
        child: Builder(
          builder: (context) {
            final router = AppRouter.router(context);
            return MaterialApp.router(
              routerConfig: router,
              builder: (context, child) =>
                  AppPageFrame(child: child ?? const SizedBox.shrink()),
            );
          },
        ),
      ),
    );

    await tester.pumpAndSettle();

    final router = AppRouter.router(tester.element(find.byType(PatientNavigationShell)));
    debugPrint('Router location: ${router.state.matchedLocation}');
    debugPrint('Router uri: ${router.state.uri}');
    final shellFinder = find.byType(PatientNavigationShell);
    final shell = tester.widget<PatientNavigationShell>(shellFinder);
    debugPrint('Shell currentIndex: ${shell.navigationShell.currentIndex}');
    debugPrint('Found HomeScreen: ${find.byType(HomeScreen).evaluate().length}');
    final rect = tester.getRect(find.text('Smart Hospital', skipOffstage: false));
    debugPrint('Rect of Smart Hospital: $rect');
    final shellRect = tester.getRect(find.byType(PatientNavigationShell));
    debugPrint('Rect of PatientNavigationShell: $shellRect');
    final homeRect = tester.getRect(find.byType(HomeScreen));
    debugPrint('Rect of HomeScreen: $homeRect');
    final navBarRect = tester.getRect(find.byType(PatientBottomNavigationBar));
    debugPrint('Rect of PatientBottomNavigationBar: $navBarRect');

    expect(find.text('Smart Hospital'), findsOneWidget);
    expect(find.text('Hello, Test'), findsOneWidget);
    expect(find.text('My Appointments'), findsWidgets);
    expect(find.text('Doctors'), findsWidgets);
    expect(find.text('Home'), findsOneWidget);

    // Switch to Appointments tab
    await tester.tap(find.text('Appointments'));
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.byType(MyAppointmentsScreen), findsOneWidget);

    // Switch to Doctors tab
    await tester.tap(find.text('Doctors').last);
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.byType(DoctorDirectoryScreen), findsOneWidget);

    debugPrint('Found Admissions: ${find.text('Admissions').evaluate().length}');
    for (final el in find.text('Admissions').evaluate()) {
      debugPrint('Admissions widget rect: ${tester.getRect(find.byWidget(el.widget))}');
    }

    // Switch to Admissions tab
    await tester.tap(find.text('Admissions').first);
    await tester.pump(const Duration(milliseconds: 300));
    debugPrint('After Admissions tap: current location=${router.state.matchedLocation}');
    expect(find.byType(MyAdmissionScreen), findsOneWidget);

    // Switch to Records tab
    await tester.tap(find.text('Records'));
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.byType(PatientMedicalRecordsScreen), findsOneWidget);

    // Switch back to Home
    await tester.tap(find.text('Home'));
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.byType(HomeScreen), findsOneWidget);

    // Verify Queue Status navigation
    router.push('/queue');
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
    debugPrint('After queue push: location=${router.state.matchedLocation}');
    expect(find.byType(QueueStatusScreen), findsOneWidget);
  });
}
