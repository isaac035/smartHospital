import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_theme.dart';
import '../../models/appointments/appointment_slot_model.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/app_button.dart';
import '../../widgets/error_message.dart';
import 'widgets/slot_picker_grid.dart';

class DoctorSlotsScreen extends StatefulWidget {
  final int doctorId;

  const DoctorSlotsScreen({super.key, required this.doctorId});

  @override
  State<DoctorSlotsScreen> createState() => _DoctorSlotsScreenState();
}

class _DoctorSlotsScreenState extends State<DoctorSlotsScreen> {
  DateTime _selectedDate = DateTime.now();
  AppointmentSlotModel? _selectedSlot;

  String get _dateString =>
      '${_selectedDate.year}-${_selectedDate.month.toString().padLeft(2, '0')}-${_selectedDate.day.toString().padLeft(2, '0')}';

  @override
  void initState() {
    super.initState();
    _loadSlots();
  }

  void _loadSlots() {
    context
        .read<AppointmentProvider>()
        .loadSlots(doctorId: widget.doctorId, date: _dateString);
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 90)),
    );
    if (picked != null) {
      setState(() {
        _selectedDate = picked;
        _selectedSlot = null;
      });
      _loadSlots();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Available Slots')),
      body: Consumer<AppointmentProvider>(
        builder: (context, provider, _) {
          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Date picker
                GestureDetector(
                  onTap: _pickDate,
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 16, vertical: 14),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(12),
                      border:
                          Border.all(color: Colors.grey.shade300),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.calendar_today,
                            color: AppTheme.primaryColor),
                        const SizedBox(width: 12),
                        Text(
                          '${_selectedDate.day} / ${_selectedDate.month} / ${_selectedDate.year}',
                          style: const TextStyle(fontSize: 15),
                        ),
                        const Spacer(),
                        const Icon(Icons.arrow_drop_down),
                      ],
                    ),
                  ),
                ),

                const SizedBox(height: 20),
                Text(
                  'Select a Time Slot',
                  style: const TextStyle(
                      fontWeight: FontWeight.bold, fontSize: 15),
                ),
                const SizedBox(height: 12),

                if (provider.slotsError != null)
                  ErrorMessage(message: provider.slotsError),

                SlotPickerGrid(
                  slots: provider.slots,
                  selectedSlot: _selectedSlot,
                  onSlotSelected: (slot) =>
                      setState(() => _selectedSlot = slot),
                  isLoading: provider.slotsLoading,
                ),

                if (_selectedSlot != null) ...[
                  const SizedBox(height: 24),
                  Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryColor.withValues(alpha: 0.08),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                          color:
                              AppTheme.primaryColor.withValues(alpha: 0.3)),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.check_circle,
                            color: AppTheme.primaryColor),
                        const SizedBox(width: 10),
                        Text(
                          'Selected: ${_selectedSlot!.formattedTime}',
                          style: const TextStyle(
                              color: AppTheme.primaryColor,
                              fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  AppButton(
                    text: 'Continue to Book',
                    onPressed: () => context.push(
                      '/appointments/book',
                      extra: {
                        'doctorId': widget.doctorId,
                        'slotStart':
                            _selectedSlot!.slotStart.toIso8601String(),
                      },
                    ),
                  ),
                ],
                const SizedBox(height: 32),
              ],
            ),
          );
        },
      ),
    );
  }
}
