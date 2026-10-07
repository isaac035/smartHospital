import 'dart:async';
import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/error_message.dart';
import '../../widgets/app_ui.dart' show AppDialog;
import '../../widgets/app_text_field.dart';
import '../../core/utils/validators.dart';
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
  Timer? _refreshTimer;
  bool _refreshInProgress = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AppointmentProvider>().loadAppointmentDetail(
        widget.appointmentId,
      );
    });
    _refreshTimer = Timer.periodic(const Duration(seconds: 5), (_) {
      if (!mounted || _refreshInProgress) return;
      _refreshInProgress = true;
      context
          .read<AppointmentProvider>()
          .loadAppointmentDetail(widget.appointmentId, showLoading: false)
          .whenComplete(() => _refreshInProgress = false);
    });
  }

  @override
  void dispose() {
    _refreshTimer?.cancel();
    super.dispose();
  }

  Future<void> _showCancelDialog() async {
    final controller = TextEditingController();
    final formKey = GlobalKey<FormState>();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AppDialog(
        title: const Text('Cancel Appointment'),
        content: Form(
          key: formKey,
          child: AppTextField(
            label: 'Reason for cancellation',
            isRequired: true,
            controller: controller,
            maxLines: 3,
            maxLength: 500,
            hint: 'Enter a reason',
            validator: (v) => Validators.validateText(v, 'Cancellation reason', required: true, max: 500),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Back'),
          ),
          FilledButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: AppTheme.errorColor,
            ),
            onPressed: () {
              // Stay open and show the red error until a valid reason is entered.
              if (formKey.currentState?.validate() ?? false) Navigator.pop(ctx, true);
            },
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
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Appointment cancelled.')));
      } else {
        final err = context.read<AppointmentProvider>().actionError;
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(err ?? 'Cancellation failed.')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(
        title: const Text('Appointment Details'),
        elevation: 0,
        backgroundColor: AppTheme.surfaceColor,
        foregroundColor: AppTheme.textPrimary,
      ),
      body: Consumer<AppointmentProvider>(
        builder: (context, provider, _) {
          if (provider.detailLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          if (provider.detailError != null ||
              provider.selectedAppointment == null) {
            return Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(
                    Icons.error_outline,
                    size: 48,
                    color: AppTheme.errorColor,
                  ),
                  const SizedBox(height: 16),
                  ErrorMessage(message: provider.detailError ?? 'Not found.'),
                  const SizedBox(height: 16),
                  ElevatedButton.icon(
                    onPressed: () =>
                        provider.loadAppointmentDetail(widget.appointmentId),
                    icon: const Icon(Icons.refresh),
                    label: const Text('Retry'),
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
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Check-in Reference Card
                if (apt.referenceNumber.isNotEmpty &&
                    (apt.status == 1 || apt.status == 2))
                  Container(
                    margin: const EdgeInsets.only(bottom: 20),
                    padding: const EdgeInsets.symmetric(
                      vertical: 20,
                      horizontal: 24,
                    ),
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        colors: [AppTheme.primaryColor, AppTheme.primaryColor],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      borderRadius: BorderRadius.circular(16),
                      boxShadow: [
                        BoxShadow(
                          color: AppTheme.primaryColor.withValues(alpha: 0.3),
                          blurRadius: 8,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Column(
                      children: [
                        const Text(
                          'CHECK-IN REFERENCE',
                          style: TextStyle(
                            color: AppTheme.onPrimary,
                            fontSize: AppTheme.fontBodySmall,
                            fontWeight: FontWeight.bold,
                            letterSpacing: 1.5,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          apt.referenceNumber,
                          style: TextStyle(
                            color: AppTheme.surfaceColor,
                            fontSize: AppTheme.fontDisplaySmall,
                            fontWeight: FontWeight.bold,
                            letterSpacing: 2,
                          ),
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'Show this at the reception kiosk to check in.',
                          style: TextStyle(
                            color: AppTheme.surfaceColor,
                            fontSize: AppTheme.fontBodyMedium,
                          ),
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),

                // Header card
                _InfoCard(
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          width: 48,
                          height: 48,
                          decoration: BoxDecoration(
                            color: AppTheme.primaryColor.withValues(alpha: 0.1),
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(
                            Icons.person,
                            color: AppTheme.primaryColor,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                apt.doctorName ?? 'Unassigned',
                                style: TextStyle(
                                  fontSize: AppTheme.fontHeadlineSmall,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              if (apt.departmentName != null)
                                Text(
                                  apt.departmentName!,
                                  style: TextStyle(
                                    color: AppTheme.textSecondary,
                                    fontSize: AppTheme.fontBodyMedium,
                                  ),
                                ),
                            ],
                          ),
                        ),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            AppointmentStatusBadge(status: apt.status),
                            const SizedBox(height: 6),
                            PriorityBadge(priority: apt.priority),
                            if (apt.priorityNeedsReview) ...[
                              const SizedBox(height: 4),
                              Text(
                                'Requested',
                                style: TextStyle(
                                  fontSize: AppTheme.fontTitleMedium,
                                  color: AppTheme.textSecondary,
                                ),
                              ),
                            ],
                          ],
                        ),
                      ],
                    ),
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 12),
                      child: Divider(height: 1),
                    ),
                    _DetailRow(
                      icon: Icons.calendar_month,
                      label: 'Date & Time',
                      value: _fmt(apt.scheduledStart),
                    ),
                    _DetailRow(
                      icon: Icons.schedule,
                      label: 'Duration',
                      value: '${apt.estimatedDurationMinutes} minutes',
                    ),
                    _DetailRow(
                      icon: Icons.medical_services,
                      label: 'Visit Type',
                      value: apt.typeLabel,
                    ),
                    if (apt.notes != null && apt.notes!.isNotEmpty)
                      _DetailRow(
                        icon: Icons.notes,
                        label: 'Patient Notes',
                        value: apt.notes!,
                      ),
                    if (apt.cancelledReason != null)
                      _DetailRow(
                        icon: Icons.cancel,
                        label: 'Cancellation Reason',
                        value: apt.cancelledReason!,
                        valueColor: AppTheme.errorColor,
                      ),
                  ],
                ),

                if (apt.reservedResources.isNotEmpty) ...[
                  const SizedBox(height: 20),
                  Text('Reserved Resources', style: TextStyle(fontSize: AppTheme.fontTitleMedium, fontWeight: FontWeight.bold, color: AppTheme.textPrimary)),
                  const SizedBox(height: 10),
                  _InfoCard(children: [
                    for (final resource in apt.reservedResources) ...[
                      _DetailRow(icon: resource.kind == 'bed' ? Icons.bed : Icons.medical_services, label: resource.kind == 'equipment' && resource.code.isNotEmpty ? '${resource.name} · ${resource.code}' : resource.name, value: '${resource.type}\n${resource.location}\nStatus: ${resource.status}'),
                      if (resource != apt.reservedResources.last) const Divider(height: 16),
                    ],
                  ]),
                ],

                // History Timeline
                if (history.isNotEmpty) ...[
                  const SizedBox(height: 24),
                  const Text(
                    'Appointment History',
                    style: TextStyle(
                      fontSize: AppTheme.fontTitleMedium,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 12),
                  _InfoCard(
                    children: history.map((h) => _HistoryTile(h: h)).toList(),
                  ),
                ],

                // Actions
                if (apt.isReschedulable || apt.isCancellable) ...[
                  const SizedBox(height: 32),
                  if (apt.isReschedulable)
                    SizedBox(
                      height: 50,
                      child: OutlinedButton.icon(
                        style: OutlinedButton.styleFrom(
                          side: BorderSide(color: AppTheme.primaryColor),
                          foregroundColor: AppTheme.primaryColor,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                        icon: const Icon(Icons.edit_calendar),
                        label: const Text(
                          'Reschedule Appointment',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: AppTheme.fontTitleMedium,
                          ),
                        ),
                        onPressed: () =>
                            context.push('/appointments/${apt.id}/reschedule'),
                      ),
                    ),
                  if (apt.isCancellable) ...[
                    const SizedBox(height: 12),
                    SizedBox(
                      height: 50,
                      child: ElevatedButton.icon(
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTheme.surfaceColor,
                          foregroundColor: AppTheme.errorColor,
                          elevation: 0,
                          side: BorderSide(
                            color: AppTheme.errorColor.withValues(alpha: 0.3),
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                        icon: const Icon(Icons.cancel_outlined),
                        label: provider.actionLoading
                            ? const SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : const Text(
                                'Cancel Appointment',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  fontSize: AppTheme.fontBodySmall,
                                ),
                              ),
                        onPressed: provider.actionLoading
                            ? null
                            : _showCancelDialog,
                      ),
                    ),
                  ],
                ],
                const SizedBox(height: 40),
              ],
            ),
          );
        },
      ),
    );
  }

  String _fmt(DateTime dt) {
    final months = [
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec',
    ];
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
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: children,
        ),
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color? valueColor;

  const _DetailRow({
    required this.icon,
    required this.label,
    required this.value,
    this.valueColor,
  });

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
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: TextStyle(
                    fontSize: AppTheme.fontBodyMedium,
                    color: AppTheme.onPrimary,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                Text(
                  value,
                  style: TextStyle(
                    fontSize: AppTheme.fontBodySmall,
                    color: valueColor ?? AppTheme.secondaryColor,
                  ),
                ),
              ],
            ),
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
        Column(
          children: [
            Container(
              width: 10,
              height: 10,
              decoration: const BoxDecoration(
                color: AppTheme.primaryColor,
                shape: BoxShape.circle,
              ),
            ),
            Container(width: 2, height: 36, color: AppTheme.borderColor),
          ],
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  h.newStatus,
                  style: TextStyle(fontWeight: FontWeight.bold),
                ),
                Text(
                  '${_fmtDate(h.changedAt)}  ·  ${h.changedByName}',
                  style: TextStyle(
                    fontSize: AppTheme.fontBodyMedium,
                    color: AppTheme.onPrimary,
                  ),
                ),
                if (h.reason != null)
                  Text(
                    h.reason!,
                    style: TextStyle(
                      fontSize: AppTheme.fontBodySmall,
                      fontStyle: FontStyle.italic,
                    ),
                  ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  String _fmtDate(DateTime dt) {
    final months = [
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec',
    ];
    return '${dt.day} ${months[dt.month - 1]}  ${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')}';
  }
}
