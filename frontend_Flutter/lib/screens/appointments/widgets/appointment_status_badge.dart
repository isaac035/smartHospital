import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../models/appointments/appointment_model.dart';

class AppointmentStatusBadge extends StatelessWidget {
  final int status;

  const AppointmentStatusBadge({super.key, required this.status});

  static Color _bgColor(int status) {
    switch (status) {
      case 1: // Scheduled
        return Colors.blue.shade100;
      case 2: // Confirmed
        return Colors.green.shade100;
      case 3: // CheckedIn
        return Colors.teal.shade100;
      case 4: // InProgress
        return Colors.orange.shade100;
      case 5: // Completed
        return Colors.grey.shade200;
      case 6: // Cancelled
        return AppTheme.errorColor.withValues(alpha: 0.1);
      case 7: // NoShow
        return Colors.deepOrange.shade100;
      case 8: // Rescheduled
        return Colors.purple.shade100;
      default:
        return Colors.grey.shade100;
    }
  }

  static Color _fgColor(int status) {
    switch (status) {
      case 1: return Colors.blue.shade800;
      case 2: return Colors.green.shade800;
      case 3: return Colors.teal.shade800;
      case 4: return Colors.orange.shade800;
      case 5: return Colors.grey.shade700;
      case 6: return AppTheme.errorColor;
      case 7: return Colors.deepOrange.shade800;
      case 8: return Colors.purple.shade800;
      default: return Colors.grey.shade700;
    }
  }

  @override
  Widget build(BuildContext context) {
    final label = AppointmentModel(
      id: 0, referenceNumber: '', patientId: 0, patientName: '',
      scheduledStart: DateTime.now(), estimatedDurationMinutes: 0,
      appointmentType: 1, status: status, priority: 1,
      emergencyConfirmed: false, createdAt: DateTime.now(),
    ).statusLabel;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: _bgColor(status),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: _fgColor(status),
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
