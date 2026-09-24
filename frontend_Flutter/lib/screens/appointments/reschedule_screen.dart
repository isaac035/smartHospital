import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../models/appointments/appointment_slot_model.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/app_button.dart';
import '../../widgets/error_message.dart';
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
    context
        .read<AppointmentProvider>()
        .loadSlots(doctorId: doctorId, date: _dateString);
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
          newScheduledStart: _selectedSlot!.slotStart.toIso8601String(),
          newEstimatedDurationMinutes: durationMinutes,
          reason: _reasonController.text.trim(),
        );

    if (!mounted) return;
    if (ok) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Appointment rescheduled.')),
      );
      context.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Reschedule Appointment')),
      body: Consumer<AppointmentProvider>(
        builder: (context, provider, _) {
          final apt = provider.selectedAppointment;

          if (apt == null) {
            return const Center(child: CircularProgressIndicator());
          }

          if (!apt.isReschedulable) {
            return const Center(
              child: Text('This appointment cannot be rescheduled.'),
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
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: Colors.orange.shade50,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.orange.shade200),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.swap_horiz, color: Colors.orange),
                        const SizedBox(width: 10),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Current slot',
                                style: TextStyle(
                                    color: Colors.orange,
                                    fontSize: 11,
                                    fontWeight: FontWeight.w600)),
                            Text(
                              'Dr. ${apt.doctorName ?? 'Unknown'}  ·  ${apt.scheduledStart.day}/${apt.scheduledStart.month}/${apt.scheduledStart.year}',
                              style:
                                  const TextStyle(fontWeight: FontWeight.bold),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 20),

                  // Date picker
                  const Text('Select New Date',
                      style: TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 8),
                  GestureDetector(
                    onTap: () => _pickDate(apt.doctorId),
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 16, vertical: 14),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: Colors.grey.shade300),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.calendar_today),
                          const SizedBox(width: 12),
                          Text(
                              '${_selectedDate.day} / ${_selectedDate.month} / ${_selectedDate.year}'),
                          const Spacer(),
                          const Icon(Icons.arrow_drop_down),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 20),
                  const Text('Select New Time Slot',
                      style: TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 8),

                  if (provider.slotsError != null)
                    ErrorMessage(message: provider.slotsError),

                  SlotPickerGrid(
                    slots: provider.slots,
                    selectedSlot: _selectedSlot,
                    onSlotSelected: (s) => setState(() => _selectedSlot = s),
                    isLoading: provider.slotsLoading,
                  ),

                  const SizedBox(height: 20),
                  const Text('Reason for Rescheduling *',
                      style: TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 8),
                  TextFormField(
                    controller: _reasonController,
                    maxLines: 2,
                    validator: (v) => (v == null || v.trim().isEmpty)
                        ? 'Please provide a reason.'
                        : null,
                    decoration: const InputDecoration(
                        hintText: 'e.g. Schedule conflict'),
                  ),

                  const SizedBox(height: 24),
                  if (provider.actionError != null)
                    ErrorMessage(message: provider.actionError),
                  AppButton(
                    text: 'Confirm Reschedule',
                    isLoading: provider.actionLoading,
                    onPressed: () => _submit(apt.estimatedDurationMinutes),
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
