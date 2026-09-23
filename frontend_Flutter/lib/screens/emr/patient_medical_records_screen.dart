import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class PatientMedicalRecordsScreen extends StatelessWidget {
  const PatientMedicalRecordsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Medical Records'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: ListTile(
              leading: const Icon(Icons.favorite, color: Colors.red),
              title: const Text('My Vital Signs'),
              subtitle: const Text('View temperature, BP, heart rate history'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push('/medical-records/vitals'),
            ),
          ),
          const SizedBox(height: 8),
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: ListTile(
              leading: const Icon(Icons.medication, color: Colors.blue),
              title: const Text('My Prescriptions'),
              subtitle: const Text('Active and completed medications'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push('/medical-records/prescriptions'),
            ),
          ),
          const SizedBox(height: 8),
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: ListTile(
              leading: const Icon(Icons.biotech, color: Colors.teal),
              title: const Text('My Lab Reports'),
              subtitle: const Text('Diagnostic test results and findings'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push('/medical-records/lab-reports'),
            ),
          ),
          const SizedBox(height: 24),
          const Text(
            'Recent Consultations',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          const Center(
            child: Padding(
              padding: EdgeInsets.all(32.0),
              child: Text(
                'No past consultations recorded.',
                style: TextStyle(color: Colors.grey),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
