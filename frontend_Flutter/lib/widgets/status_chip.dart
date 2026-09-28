import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';

enum DoctorAvailabilityState { available, unavailable, onLeave }

class StatusChip extends StatelessWidget {
  final DoctorAvailabilityState state;

  const StatusChip({super.key, required this.state});

  @override
  Widget build(BuildContext context) {
    final (Color color, String label) = switch (state) {
      DoctorAvailabilityState.available => (AppTheme.successColor, 'Available'),
      DoctorAvailabilityState.unavailable => (
        AppTheme.textSecondary,
        'Unavailable',
      ),
      DoctorAvailabilityState.onLeave => (AppTheme.warningColor, 'On Leave'),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: color.withValues(alpha: 0.4)),
      ),
      child: Text(
        label,
        style: Theme.of(context).textTheme.labelMedium?.copyWith(color: color),
      ),
    );
  }
}
