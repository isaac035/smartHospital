import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/utils/api_datetime.dart';
import '../../models/appointments/appointment_slot_model.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/error_message.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_text_field.dart';
import 'widgets/slot_picker_grid.dart';

class RescheduleScreen extends StatefulWidget {
  final int appointmentId;

  const RescheduleScreen({super.key, required this.appointmentId});

  @override
  State<RescheduleScreen> createState() => _RescheduleScreenState();
}

class _RescheduleScreenState extends State<RescheduleScreen> {
  DateTime _selectedDate = DateTime.now().add(const Duration(days: 1));
  AppointmentSlotModel? _selectedSlot;
  final _reasonController = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  String get _dateString =>
      '${_selectedDate.year}-${_selectedDate.month.toString().padLeft(2, '0')}-${_selectedDate.day.toString().padLeft(2, '0')}';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final apt = context.read<AppointmentProvider>().selectedAppointment;
      if (apt != null) {
        _loadSlots(apt.doctorId);
      }
    });
  }

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  void _loadSlots(int? doctorId) {
    if (doctorId == null) return;
    context.read<AppointmentProvider>().loadSlots(
      doctorId: doctorId,
      date: _dateString,
    );
  }

  Future<void> _pickDate(int? doctorId) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime.now().add(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 90)),
    );
    if (picked != null) {
      setState(() {
        _selectedDate = picked;
        _selectedSlot = null;
      });
      _loadSlots(doctorId);
    }
  }

  Future<void> _submit(int durationMinutes) async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedSlot == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please select a new time slot.')),
      );
      return;
    }

    final ok = await context.read<AppointmentProvider>().rescheduleAppointment(
      widget.appointmentId,
      newScheduledStart: ApiDateTime.toUtcIso8601(_selectedSlot!.slotStart),
      newEstimatedDurationMinutes: durationMinutes,
      reason: _reasonController.text.trim(),
    );

    if (!mounted) return;
    if (ok) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Appointment rescheduled.')));
      context.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Reschedule Appointment'),
        elevation: 0,
        backgroundColor: AppTheme.transparentColor,
        foregroundColor: AppTheme.textPrimary,
      ),
      body: Consumer<AppointmentProvider>(
        builder: (context, provider, _) {
          final apt = provider.selectedAppointment;

          if (apt == null) {
            return const Center(child: CircularProgressIndicator());
          }

          if (!apt.isReschedulable) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.block,
                      size: 64,
                      color: AppTheme.errorColor,
                    ),
                    const SizedBox(height: 16),
                    const Text(
                      'Cannot Reschedule',
                      style: TextStyle(
                        fontSize: AppTheme.fontHeadlineSmall,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'This appointment is currently marked as ${apt.statusLabel}. Only Scheduled or Confirmed appointments can be rescheduled.',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: AppTheme.textSecondary),
                    ),
                  ],
                ),
              ),
            );
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Current appointment info
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppTheme.warningContainer,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: AppTheme.warningColor),
                    ),
                    child: Row(
                      children: [
                        const Icon(
                          Icons.info_outline,
                          color: AppTheme.warningColor,
                          size: 28,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Currently Scheduled',
                                style: TextStyle(
                                  color: AppTheme.warningColor,
                                  fontSize: AppTheme.fontBodySmall,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                '${apt.scheduledStart.day}/${apt.scheduledStart.month}/${apt.scheduledStart.year} at ${apt.scheduledStart.hour > 12 ? apt.scheduledStart.hour - 12 : (apt.scheduledStart.hour == 0 ? 12 : apt.scheduledStart.hour)}:${apt.scheduledStart.minute.toString().padLeft(2, '0')} ${apt.scheduledStart.hour >= 12 ? 'PM' : 'AM'}',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  fontSize: AppTheme.fontTitleMedium,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 24),

                  // Date picker
                  const Text(
                    '1. Select New Date',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: AppTheme.fontTitleMedium,
                    ),
                  ),
                  const SizedBox(height: 12),
                  GestureDetector(
                    onTap: () => _pickDate(apt.doctorId),
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 16,
                      ),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceColor,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: AppTheme.borderColor),
                        boxShadow: [
                          BoxShadow(
                            color: AppTheme.textPrimary.withValues(alpha: 0.02),
                            blurRadius: 4,
                            offset: const Offset(0, 2),
                          ),
                        ],
                      ),
                      child: Row(
                        children: [
                          Icon(
                            Icons.calendar_month,
                            color: AppTheme.primaryColor,
                          ),
                          const SizedBox(width: 12),
                          Text(
                            '${_selectedDate.day} / ${_selectedDate.month} / ${_selectedDate.year}',
                            style: TextStyle(
                              fontSize: AppTheme.fontTitleMedium,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          const Spacer(),
                          Text(
                            'Change',
                            style: TextStyle(
                              color: AppTheme.primaryColor,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 24),
                  const Text(
                    '2. Select New Time Slot',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: AppTheme.fontTitleMedium,
                    ),
                  ),
                  const SizedBox(height: 12),

                  if (provider.slotsError != null)
                    ErrorMessage(message: provider.slotsError),

                  SlotPickerGrid(
                    slots: provider.slots,
                    selectedSlot: _selectedSlot,
                    onSlotSelected: (s) => setState(() => _selectedSlot = s),
                    isLoading: provider.slotsLoading,
                  ),

                  const SizedBox(height: 24),
                  const Text(
                    '3. Reason for Rescheduling *',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: AppTheme.fontTitleMedium,
                    ),
                  ),
                  const SizedBox(height: 12),
                  AppTextField(
                    label: 'Reason for Rescheduling *',
                    hint: 'e.g. Schedule conflict, feeling unwell...',
                    controller: _reasonController,
                    maxLines: 3,
                    validator: (v) => (v == null || v.trim().isEmpty)
                        ? 'Please provide a reason to inform the doctor.'
                        : null,
                  ),

                  const SizedBox(height: 32),
                  if (provider.actionError != null) ...[
                    ErrorMessage(message: provider.actionError),
                    const SizedBox(height: 16),
                  ],

                  AppButton(
                    text: 'Confirm Reschedule',
                    onPressed: () => _submit(apt.estimatedDurationMinutes),
                    isLoading: provider.actionLoading,
                  ),
                  const SizedBox(height: 32),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
