import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/utils/api_datetime.dart';
import '../../widgets/error_message.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_text_field.dart';
import '../../providers/appointment_provider.dart';
import '../../providers/auth_provider.dart';
import 'widgets/priority_badge.dart';

class BookAppointmentScreen extends StatefulWidget {
  final int doctorId;
  final String slotStart;
  final int durationMinutes;
  final int initialPriority;
  final int? triageResultId;

  const BookAppointmentScreen({
    super.key,
    required this.doctorId,
    required this.slotStart,
    required this.durationMinutes,
    this.initialPriority = 1,
    this.triageResultId,
  });

  @override
  State<BookAppointmentScreen> createState() => _BookAppointmentScreenState();
}

class _BookAppointmentScreenState extends State<BookAppointmentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _notesController = TextEditingController();

  final int _appointmentType = 1;
  // Matches the backend AppointmentPriority values: Normal=1, Urgent=2, Emergency=3.
  late int _priority;

  @override
  void initState() {
    super.initState();
    _priority = widget.initialPriority.clamp(1, 3).toInt();
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    context.read<AppointmentProvider>().clearActionError();

    final user = context.read<AuthProvider>().currentUser;
    if (user == null) return;

    final result = await context.read<AppointmentProvider>().bookAppointment(
      patientId: user.id,
      doctorId: widget.doctorId,
      appointmentType: _appointmentType,
      scheduledStart: ApiDateTime.toUtcIso8601(
        ApiDateTime.parseUtcToLocal(widget.slotStart),
      ),
      estimatedDurationMinutes: widget.durationMinutes,
      priority: _priority,
      notes: _notesController.text.trim(),
      triageResultId: widget.triageResultId,
    );

    if (!mounted) return;
    if (result != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Appointment booked successfully!'),
          backgroundColor: AppTheme.successColor,
        ),
      );
      context.go('/appointments/confirmed/${result.id}');
    }
  }

  Widget _priorityChip(int value, String label) => ChoiceChip(
    label: Text(label),
    selected: _priority == value,
    onSelected: (selected) {
      if (selected) setState(() => _priority = value);
    },
  );

  @override
  Widget build(BuildContext context) {
    final slotDt = ApiDateTime.parseUtcToLocal(widget.slotStart);
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
    final h = slotDt.hour > 12
        ? slotDt.hour - 12
        : (slotDt.hour == 0 ? 12 : slotDt.hour);
    final period = slotDt.hour >= 12 ? 'PM' : 'AM';
    final fmtSlot =
        '${slotDt.day} ${months[slotDt.month - 1]} ${slotDt.year}  $h:${slotDt.minute.toString().padLeft(2, '0')} $period';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Confirm Booking'),
        elevation: 0,
        backgroundColor: AppTheme.transparentColor,
        foregroundColor: AppTheme.textPrimary,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Slot summary card
              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: AppTheme.primaryColor,
                  borderRadius: BorderRadius.circular(16),
                  boxShadow: [
                    BoxShadow(
                      color: AppTheme.primaryColor.withValues(alpha: 0.3),
                      blurRadius: 8,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceColor.withValues(alpha: 0.2),
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(
                        Icons.event_available,
                        color: AppTheme.surfaceColor,
                        size: 28,
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Selected Date & Time',
                            style: TextStyle(
                              color: AppTheme.onPrimary,
                              fontSize: AppTheme.fontBodyMedium,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            fmtSlot,
                            style: TextStyle(
                              color: AppTheme.surfaceColor,
                              fontWeight: FontWeight.bold,
                              fontSize: AppTheme.fontHeadlineSmall,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 32),

              const Text(
                'Appointment Details',
                style: TextStyle(
                  fontSize: AppTheme.fontHeadlineSmall,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 16),

              Text(
                'Consultation length: ${widget.durationMinutes} minutes',
                style: TextStyle(
                  fontWeight: FontWeight.w600,
                  color: AppTheme.textPrimary,
                ),
              ),

              const SizedBox(height: 20),

              const Text(
                'Priority',
                style: TextStyle(
                  fontWeight: FontWeight.w600,
                  color: AppTheme.textPrimary,
                ),
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  _priorityChip(1, 'Normal'),
                  _priorityChip(2, 'Urgent'),
                  _priorityChip(3, 'Emergency'),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                switch (_priority) {
                  2 => 'Urgent: needs attention soon.',
                  3 => 'Emergency: severe or sudden symptoms.',
                  _ => 'Normal: routine visit.',
                },
                style: TextStyle(
                  color: AppTheme.textSecondary,
                  fontSize: AppTheme.fontTitleMedium,
                ),
              ),
              if (_priority == 3) ...[
                const SizedBox(height: 10),
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: AppTheme.errorContainer,
                    border: Border.all(color: AppTheme.errorContainer),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    'If this is life-threatening, go to the nearest emergency department or call emergency services. An appointment slot is not an emergency response.',
                    style: TextStyle(
                      color: AppTheme.errorColor,
                      fontSize: AppTheme.fontBodySmall,
                    ),
                  ),
                ),
              ],
              const SizedBox(height: 8),
              Align(
                alignment: Alignment.centerLeft,
                child: PriorityBadge(priority: _priority),
              ),
              if (_priority != 1) ...[
                const SizedBox(height: 4),
                Text(
                  'This is a priority request. Hospital staff will review it before it affects queue order.',
                  style: TextStyle(
                    color: AppTheme.textSecondary,
                    fontSize: AppTheme.fontBodySmall,
                  ),
                ),
              ],

              const SizedBox(height: 20),

              AppTextField(
                label: 'Additional Notes (Optional)',
                hint: 'Briefly describe your symptoms or reason for visit...',
                controller: _notesController,
                maxLines: 4,
              ),

              const SizedBox(height: 32),

              Consumer<AppointmentProvider>(
                builder: (context, provider, _) {
                  return Column(
                    children: [
                      if (provider.actionError != null) ...[
                        ErrorMessage(message: provider.actionError),
                        const SizedBox(height: 16),
                      ],
                      AppButton(
                        text: 'Confirm Booking',
                        onPressed: _submit,
                        isLoading: provider.actionLoading,
                      ),
                    ],
                  );
                },
              ),
              const SizedBox(height: 40),
            ],
          ),
        ),
      ),
    );
  }
}
