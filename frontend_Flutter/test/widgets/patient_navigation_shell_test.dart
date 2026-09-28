import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_hospital/widgets/patient_navigation_shell.dart';

void main() {
  testWidgets(
      'StatefulShellRoute renders content and navigates without GlobalKey errors',
      (WidgetTester tester) async {
    final rootNavKey = GlobalKey<NavigatorState>(debugLabel: 'testRoot');
    final tabHomeKey = GlobalKey<NavigatorState>(debugLabel: 'tabHome');
    final tabAppointmentsKey =
        GlobalKey<NavigatorState>(debugLabel: 'tabAppointments');
    final tabDoctorsKey = GlobalKey<NavigatorState>(debugLabel: 'tabDoctors');
    final tabAdmissionsKey =
        GlobalKey<NavigatorState>(debugLabel: 'tabAdmissions');
    final tabRecordsKey = GlobalKey<NavigatorState>(debugLabel: 'tabRecords');

    final router = GoRouter(
      navigatorKey: rootNavKey,
      initialLocation: '/home',
      routes: [
        StatefulShellRoute.indexedStack(
          builder: (context, state, navigationShell) {
            return PatientNavigationShell(navigationShell: navigationShell);
          },
          branches: [
            StatefulShellBranch(
              navigatorKey: tabHomeKey,
              routes: [
                GoRoute(
                  path: '/home',
                  builder: (context, state) =>
                      const Scaffold(body: Center(child: Text('Home Content'))),
                ),
              ],
            ),
            StatefulShellBranch(
              navigatorKey: tabAppointmentsKey,
              routes: [
                GoRoute(
                  path: '/appointments',
                  builder: (context, state) => const Scaffold(
                      body: Center(child: Text('Appointments Content'))),
                ),
              ],
            ),
            StatefulShellBranch(
              navigatorKey: tabDoctorsKey,
              routes: [
                GoRoute(
                  path: '/doctors',
                  builder: (context, state) => const Scaffold(
                      body: Center(child: Text('Doctors Content'))),
                ),
              ],
            ),
            StatefulShellBranch(
              navigatorKey: tabAdmissionsKey,
              routes: [
                GoRoute(
                  path: '/my-admission',
                  builder: (context, state) => const Scaffold(
                      body: Center(child: Text('Admissions Content'))),
                ),
              ],
            ),
            StatefulShellBranch(
              navigatorKey: tabRecordsKey,
              routes: [
                GoRoute(
                  path: '/medical-records',
                  builder: (context, state) => const Scaffold(
                      body: Center(child: Text('Records Content'))),
                ),
              ],
            ),
          ],
        ),
      ],
    );

    await tester.pumpWidget(MaterialApp.router(routerConfig: router));
    await tester.pumpAndSettle();

    // Verify Home tab content is displayed
    expect(find.text('Home Content'), findsOneWidget);
    expect(find.text('Home'), findsOneWidget);
    expect(find.text('Appointments'), findsOneWidget);
    expect(find.text('Doctors'), findsOneWidget);
    expect(find.text('Admissions'), findsOneWidget);
    expect(find.text('Records'), findsOneWidget);

    // Switch to Appointments tab
    await tester.tap(find.text('Appointments'));
    await tester.pumpAndSettle();
    expect(find.text('Appointments Content'), findsOneWidget);

    // Switch to Doctors tab
    await tester.tap(find.text('Doctors'));
    await tester.pumpAndSettle();
    expect(find.text('Doctors Content'), findsOneWidget);

    // Switch to Admissions tab
    await tester.tap(find.text('Admissions'));
    await tester.pumpAndSettle();
    expect(find.text('Admissions Content'), findsOneWidget);

    // Switch to Records tab
    await tester.tap(find.text('Records'));
    await tester.pumpAndSettle();
    expect(find.text('Records Content'), findsOneWidget);

    // Switch back to Home tab
    await tester.tap(find.text('Home'));
    await tester.pumpAndSettle();
    expect(find.text('Home Content'), findsOneWidget);
  });
}
