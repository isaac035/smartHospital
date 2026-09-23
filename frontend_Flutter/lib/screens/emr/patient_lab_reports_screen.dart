import 'package:flutter/material.dart';

class PatientLabReportsScreen extends StatelessWidget {
  const PatientLabReportsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Lab Reports'),
      ),
      body: const Center(
        child: Padding(
          padding: EdgeInsets.all(16.0),
          child: Text(
            'No laboratory reports available.',
            style: TextStyle(color: Colors.grey),
          ),
        ),
      ),
    );
  }
}
