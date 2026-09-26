import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_theme.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/error_message.dart';
import 'widgets/appointment_status_badge.dart';
import 'widgets/priority_badge.dart';

class AppointmentDetailsScreen extends StatefulWidget {
  final int appointmentId;

  const AppointmentDetailsScreen({super.key, required this.appointmentId});

  @override
  State<AppointmentDetailsScreen> createState() =>
      _AppointmentDetailsScreenState();
}

class _AppointmentDetailsScreenState extends State<AppointmentDetailsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context
          .read<AppointmentProvider>()
          .loadAppointmentDetail(widget.appointmentId);
    });
  }

  Future<void> _showCancelDialog() async {
    final controller = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cancel Appointment'),
        content: TextField(
          controller: controller,
          maxLines: 3,
          decoration: const InputDecoration(
            hintText: 'Reason for cancellation...',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Back'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.errorColor),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Confirm Cancel'),
          ),
        ],
      ),
    );
    if (confirmed == true && controller.text.trim().isNotEmpty) {
      if (!mounted) return;
      final ok = await context.read<AppointmentProvider>().cancelAppointment(
            widget.appointmentId,
            controller.text.trim(),
          );
      if (!mounted) return;
      if (ok) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Appointment cancelled.')),
        );
      } else {
        final err = context.read<AppointmentProvider>().actionError;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(err ?? 'Cancellation failed.')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Appointment Details')),
      body: Consumer<AppointmentProvider>(
        builder: (context, provider, _) {
          if (provider.detailLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          if (provider.detailError != null || provider.selectedAppointment == null) {
            return Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  ErrorMessage(
                      message: provider.detailError ?? 'Not found.'),
                  const SizedBox(height: 12),
                  ElevatedButton(
                    onPressed: () => provider
                        .loadAppointmentDetail(widget.appointmentId),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          final apt = provider.selectedAppointment!;
          final history = provider.selectedHistory;

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Header card
                _InfoCard(children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(apt.doctorName ?? 'Unassigned',
                                style: const TextStyle(
                                    fontSize: 18, fontWeight: FontWeight.bold)),
                            if (apt.departmentName != null)
                              Text(apt.departmentName!,
                                  style: TextStyle(
                                      color: Colors.grey.shade600,
                                      fontSize: 13)),
                          ],
                        ),
                      ),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          AppointmentStatusBadge(status: apt.status),
                          const SizedBox(height: 4),
                          PriorityBadge(priority: apt.priority),
                        ],
                      ),
                    ],
                  ),
                  const Divider(height: 20),
                  _DetailRow(
                      icon: Icons.calendar_today,
                      label: 'Date & Time',
                      value: _fmt(apt.scheduledStart)),
                  _DetailRow(
                      icon: Icons.schedule,
                      label: 'Duration',
                      value: '${apt.estimatedDurationMinutes} minutes'),
                  _DetailRow(
                      icon: Icons.medical_services,
                      label: 'Type',
                      value: apt.typeLabel),
                  if (apt.referenceNumber.isNotEmpty)
                    _DetailRow(
                        icon: Icons.tag,
                        label: 'Reference',
                        value: apt.referenceNumber),
                  if (apt.notes != null && apt.notes!.isNotEmpty)
                    _DetailRow(
                        icon: Icons.notes, label: 'Notes', value: apt.notes!),
                  if (apt.cancelledReason != null)
                    _DetailRow(
                        icon: Icons.cancel,
                        label: 'Cancellation Reason',
                        value: apt.cancelledReason!,
                        valueColor: AppTheme.errorColor),
                ]),

                // History
                if (history.isNotEmpty) ...[
                  const SizedBox(height: 20),
                  const Text('Status History',
                      style: TextStyle(
                          fontSize: 16, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 10),
                  ...history.map((h) => _HistoryTile(h: h)),
                ],

                // Actions
                if (apt.isReschedulable || apt.isCancellable) ...[
                  const SizedBox(height: 24),
                  if (apt.isReschedulable)
                    SizedBox(
                      width: double.infinity,
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.edit_calendar),
                        label: const Text('Reschedule'),
                        onPressed: () => context.push(
                            '/appointments/${apt.id}/reschedule'),
                      ),
                    ),
                  if (apt.isCancellable) ...[
                    const SizedBox(height: 10),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        style: ElevatedButton.styleFrom(
                            backgroundColor: AppTheme.errorColor),
                        icon: const Icon(Icons.cancel_outlined),
                        label: provider.actionLoading
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(
                                    strokeWidth: 2, color: Colors.white))
                            : const Text('Cancel Appointment'),
                        onPressed: provider.actionLoading
                            ? null
                            : _showCancelDialog,
                      ),
                    ),
                  ],
                ],
                const SizedBox(height: 32),
              ],
            ),
          );
        },
      ),
    );
  }

  String _fmt(DateTime dt) {
    final months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    final h = dt.hour > 12 ? dt.hour - 12 : (dt.hour == 0 ? 12 : dt.hour);
    final m = dt.minute.toString().padLeft(2, '0');
    final period = dt.hour >= 12 ? 'PM' : 'AM';
    return '${dt.day} ${months[dt.month - 1]} ${dt.year}  $h:$m $period';
  }
}

class _InfoCard extends StatelessWidget {
  final List<Widget> children;
  const _InfoCard({required this.children});

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: children),
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color? valueColor;

  const _DetailRow(
      {required this.icon,
      required this.label,
      required this.value,
      this.valueColor});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 16, color: AppTheme.primaryColor),
          const SizedBox(width: 10),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(label,
                  style: TextStyle(
                      fontSize: 11,
                      color: Colors.grey.shade500,
                      fontWeight: FontWeight.w600)),
              Text(value,
                  style: TextStyle(
                      fontSize: 14,
                      color: valueColor ?? AppTheme.secondaryColor)),
            ]),
          ),
        ],
      ),
    );
  }
}

class _HistoryTile extends StatelessWidget {
  final dynamic h;
  const _HistoryTile({required this.h});

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(children: [
          Container(
            width: 10,
            height: 10,
            decoration: const BoxDecoration(
              color: AppTheme.primaryColor,
              shape: BoxShape.circle,
            ),
          ),
          Container(width: 2, height: 36, color: Colors.grey.shade300),
        ]),
        const SizedBox(width: 12),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(h.newStatus,
                  style: const TextStyle(fontWeight: FontWeight.bold)),
              Text(
                  '${_fmtDate(h.changedAt)}  ·  ${h.changedByName}',
                  style:
                      TextStyle(fontSize: 12, color: Colors.grey.shade500)),
              if (h.reason != null)
                Text(h.reason!,
                    style: const TextStyle(fontSize: 13, fontStyle: FontStyle.italic)),
            ]),
          ),
        ),
      ],
    );
  }

  String _fmtDate(DateTime dt) {
    final months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    return '${dt.day} ${months[dt.month - 1]}  ${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')}';
  }
}
