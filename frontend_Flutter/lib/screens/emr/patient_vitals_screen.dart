import 'package:flutter/material.dart';

class PatientVitalsScreen extends StatelessWidget {
  const PatientVitalsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Vital Signs'),
      ),
      body: const Center(
        child: Padding(
          padding: EdgeInsets.all(16.0),
          child: Text(
            'No recorded vital signs yet.',
            style: TextStyle(color: Colors.grey),
          ),
        ),
      ),
    );
  }
}
