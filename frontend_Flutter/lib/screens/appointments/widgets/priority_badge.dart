import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../models/appointments/appointment_model.dart';

class PriorityBadge extends StatelessWidget {
  final int priority;

  const PriorityBadge({super.key, required this.priority});

  static Color _color(int priority) {
    switch (priority) {
      case 3: return AppTheme.errorColor;        // Emergency — uses existing token
      case 2: return Colors.orange;              // Urgent — pattern from home_screen.dart
      default: return AppTheme.primaryColor;     // Normal — primary token
    }
  }

  @override
  Widget build(BuildContext context) {
    if (priority == 1) return const SizedBox.shrink(); // Don't show badge for Normal
    final label = AppointmentModel(
      id: 0, referenceNumber: '', patientId: 0, patientName: '',
      scheduledStart: DateTime.now(), estimatedDurationMinutes: 0,
      appointmentType: 1, status: 1, priority: priority,
      emergencyConfirmed: false, createdAt: DateTime.now(),
    ).priorityLabel;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: _color(priority).withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: _color(priority).withValues(alpha: 0.4)),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: _color(priority),
          fontSize: 11,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }
}
