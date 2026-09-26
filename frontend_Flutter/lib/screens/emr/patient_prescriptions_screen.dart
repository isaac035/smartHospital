import 'package:flutter/material.dart';

class PatientPrescriptionsScreen extends StatelessWidget {
  const PatientPrescriptionsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Prescriptions'),
      ),
      body: const Center(
        child: Padding(
          padding: EdgeInsets.all(16.0),
          child: Text(
            'No active prescriptions found.',
            style: TextStyle(color: Colors.grey),
          ),
        ),
      ),
    );
  }
}
