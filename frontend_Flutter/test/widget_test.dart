import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_hospital/widgets/patient_navigation_shell.dart';

void main() {
  testWidgets('PatientBottomNavigationBar renders all 5 tabs and handles selection', (WidgetTester tester) async {
    int selectedIndex = 0;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          bottomNavigationBar: StatefulBuilder(
            builder: (context, setState) {
              return PatientBottomNavigationBar(
                selectedIndex: selectedIndex,
                onItemTapped: (index) {
                  setState(() {
                    selectedIndex = index;
                  });
                },
              );
            },
          ),
        ),
      ),
    );

    expect(find.text('Home'), findsOneWidget);
    expect(find.text('Appointments'), findsOneWidget);
    expect(find.text('Doctors'), findsOneWidget);
    expect(find.text('Admissions'), findsOneWidget);
    expect(find.text('Records'), findsOneWidget);

    // Tap Doctors tab
    await tester.tap(find.text('Doctors'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 2);

    // Tap Admissions tab
    await tester.tap(find.text('Admissions'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 3);
  });
}

