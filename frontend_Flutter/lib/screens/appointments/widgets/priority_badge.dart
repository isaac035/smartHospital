import 'package:flutter/material.dart';
import '../../../models/appointments/appointment_model.dart';
import '../../../widgets/app_ui.dart' as shared;

class PriorityBadge extends StatelessWidget {
  final int priority;

  const PriorityBadge({super.key, required this.priority});

  @override
  Widget build(BuildContext context) {
    final label = AppointmentModel(
      id: 0,
      referenceNumber: '',
      patientId: 0,
      patientName: '',
      scheduledStart: DateTime.now(),
      estimatedDurationMinutes: 0,
      appointmentType: 1,
      status: 1,
      priority: priority,
      emergencyConfirmed: false,
      createdAt: DateTime.now(),
    ).priorityLabel;

    return shared.PriorityBadge(label: label);
  }
}
