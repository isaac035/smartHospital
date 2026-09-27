import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/utils/api_datetime.dart';
import '../../core/theme/app_theme.dart';
import '../../models/appointments/appointment_slot_model.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/app_button.dart';
import '../../widgets/error_message.dart';
import 'widgets/slot_picker_grid.dart';

class DoctorSlotsScreen extends StatefulWidget {
  final int doctorProfileId;
  final int bookingDoctorId;

  const DoctorSlotsScreen({
    super.key,
    required this.doctorProfileId,
    required this.bookingDoctorId,
  });

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
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadSlots();
    });
  }

  void _loadSlots() {
    context
        .read<AppointmentProvider>()
        .loadSlots(
          doctorId: widget.bookingDoctorId,
          doctorProfileId: widget.doctorProfileId,
          date: _dateString,
        );
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
      appBar: AppBar(
        title: const Text('Select Appointment Time'),
        elevation: 0,
        backgroundColor: Colors.transparent,
        foregroundColor: Colors.black87,
      ),
      body: Consumer<AppointmentProvider>(
        builder: (context, provider, _) {
          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Date picker
                const Text(
                  '1. Select Date',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                ),
                const SizedBox(height: 12),
                GestureDetector(
                  onTap: _pickDate,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.grey.shade300),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.02),
                          blurRadius: 4,
                          offset: const Offset(0, 2),
                        )
                      ]
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.calendar_month, color: AppTheme.primaryColor),
                        const SizedBox(width: 12),
                        Text(
                          '${_selectedDate.day} / ${_selectedDate.month} / ${_selectedDate.year}',
                          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
                        ),
                        const Spacer(),
                        const Text('Change', style: TextStyle(color: AppTheme.primaryColor, fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ),
                ),

                const SizedBox(height: 24),
                const Text(
                  '2. Available Slots',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                ),
                const SizedBox(height: 12),

                if (provider.slotsError != null)
                  ErrorMessage(message: provider.slotsError),
                if (provider.slotsError != null)
                  Align(
                    alignment: Alignment.centerRight,
                    child: TextButton.icon(
                      onPressed: provider.slotsLoading ? null : _loadSlots,
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ),

                SlotPickerGrid(
                  slots: provider.slots,
                  selectedSlot: _selectedSlot,
                  onSlotSelected: (slot) => setState(() => _selectedSlot = slot),
                  isLoading: provider.slotsLoading,
                ),

                  if (_selectedSlot != null && _selectedSlot!.isAvailable) ...[
                  const SizedBox(height: 32),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryColor.withValues(alpha: 0.08),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: AppTheme.primaryColor.withValues(alpha: 0.3)),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.check_circle, color: AppTheme.primaryColor, size: 28),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Selected Time',
                                style: TextStyle(color: AppTheme.primaryColor, fontSize: 12),
                              ),
                              Text(
                                '${_selectedSlot!.slotStart.day}/${_selectedSlot!.slotStart.month}/${_selectedSlot!.slotStart.year} at ${_selectedSlot!.formattedTime}',
                                style: const TextStyle(color: AppTheme.primaryColor, fontWeight: FontWeight.bold, fontSize: 15),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    height: 52,
                    child: ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppTheme.primaryColor,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                      onPressed: () => context.push(
                        '/appointments/book',
                        extra: {
                          'doctorId': widget.bookingDoctorId,
                          'slotStart': ApiDateTime.toUtcIso8601(_selectedSlot!.slotStart),
                          'durationMinutes': _selectedSlot!.durationMinutes,
                        },
                      ),
                      child: const Text('Continue to Book', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
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
