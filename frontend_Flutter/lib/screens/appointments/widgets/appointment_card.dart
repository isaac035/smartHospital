import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../models/appointments/appointment_model.dart';
import 'appointment_status_badge.dart';
import 'priority_badge.dart';

class AppointmentCard extends StatelessWidget {
  final AppointmentModel appointment;

  const AppointmentCard({super.key, required this.appointment});

  @override
  Widget build(BuildContext context) {
    final apt = appointment;
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      margin: const EdgeInsets.symmetric(vertical: 6),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.push('/appointments/${apt.id}'),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      apt.doctorName ?? 'Unassigned Doctor',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 16,
                      ),
                    ),
                  ),
                  AppointmentStatusBadge(status: apt.status),
                ],
              ),
              if (apt.departmentName != null) ...[
                const SizedBox(height: 4),
                Text(
                  apt.departmentName!,
                  style: TextStyle(color: Colors.grey.shade600, fontSize: 13),
                ),
              ],
              const SizedBox(height: 10),
              Row(
                children: [
                  const Icon(Icons.calendar_today,
                      size: 14, color: AppTheme.primaryColor),
                  const SizedBox(width: 6),
                  Text(
                    _formatDateTime(apt.scheduledStart),
                    style: const TextStyle(fontSize: 13),
                  ),
                  const SizedBox(width: 12),
                  const Icon(Icons.schedule,
                      size: 14, color: AppTheme.primaryColor),
                  const SizedBox(width: 4),
                  Text(
                    '${apt.estimatedDurationMinutes} min',
                    style: const TextStyle(fontSize: 13),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Text(
                    apt.typeLabel,
                    style: TextStyle(
                        fontSize: 12,
                        color: Colors.grey.shade600,
                        fontStyle: FontStyle.italic),
                  ),
                  const SizedBox(width: 8),
                  PriorityBadge(priority: apt.priority),
                ],
              ),
              if (apt.referenceNumber.isNotEmpty) ...[
                const SizedBox(height: 6),
                Text(
                  'Ref: ${apt.referenceNumber}',
                  style: TextStyle(fontSize: 11, color: Colors.grey.shade500),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  String _formatDateTime(DateTime dt) {
    final months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    final h = dt.hour > 12 ? dt.hour - 12 : (dt.hour == 0 ? 12 : dt.hour);
    final m = dt.minute.toString().padLeft(2, '0');
    final period = dt.hour >= 12 ? 'PM' : 'AM';
    return '${dt.day} ${months[dt.month - 1]} — $h:$m $period';
  }
}
