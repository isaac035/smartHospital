import 'package:flutter/material.dart';

enum DoctorAvailabilityState { available, unavailable, onLeave }

class StatusChip extends StatelessWidget {
  final DoctorAvailabilityState state;

  const StatusChip({super.key, required this.state});

  @override
  Widget build(BuildContext context) {
    final (MaterialColor color, String label) = switch (state) {
      DoctorAvailabilityState.available => (Colors.green, 'Available'),
      DoctorAvailabilityState.unavailable => (Colors.grey, 'Unavailable'),
      DoctorAvailabilityState.onLeave => (Colors.orange, 'On Leave'),
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
        style: TextStyle(color: color.shade700, fontWeight: FontWeight.w600, fontSize: 13),
      ),
    );
  }
}
