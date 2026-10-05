import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import '../../../models/appointments/appointment_model.dart';
import '../../../widgets/app_ui.dart' as shared;

class AppointmentStatusBadge extends StatelessWidget {
  final int status;

  const AppointmentStatusBadge({super.key, required this.status});

  static Color _fgColor(int status) {
    switch (status) {
      case 1:
        return AppTheme.primaryColor;
      case 2:
        return AppTheme.successColor;
      case 3:
        return AppTheme.secondaryColor;
      case 4:
        return AppTheme.warningColor;
      case 5:
        return AppTheme.textSecondary;
      case 6:
        return AppTheme.errorColor;
      case 7:
        return AppTheme.errorColor;
      case 8:
        return AppTheme.infoColor;
      default:
        return AppTheme.textSecondary;
    }
  }

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
      status: status,
      priority: 1,
      emergencyConfirmed: false,
      createdAt: DateTime.now(),
    ).statusLabel;

    return shared.StatusBadge(label: label, color: _fgColor(status));
  }
}
