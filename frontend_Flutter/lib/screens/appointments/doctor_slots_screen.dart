import 'dart:async';

import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../../core/constants/api_constants.dart';
import '../../core/storage/secure_storage_service.dart';
import '../../core/utils/api_datetime.dart';
import '../../models/appointments/appointment_slot_model.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/error_message.dart';
import 'widgets/slot_picker_grid.dart';

class DoctorSlotsScreen extends StatefulWidget {
  final int doctorProfileId;
  final int bookingDoctorId;
  final int? triageResultId;

  const DoctorSlotsScreen({
    super.key,
    required this.doctorProfileId,
    required this.bookingDoctorId,
    this.triageResultId,
  });

  @override
  State<DoctorSlotsScreen> createState() => _DoctorSlotsScreenState();
}

class _DoctorSlotsScreenState extends State<DoctorSlotsScreen> {
  DateTime _selectedDate = DateTime.now();
  AppointmentSlotModel? _selectedSlot;
  HubConnection? _slotUpdatesConnection;

  String get _dateString =>
      '${_selectedDate.year}-${_selectedDate.month.toString().padLeft(2, '0')}-${_selectedDate.day.toString().padLeft(2, '0')}';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadSlots();
      _connectToSlotUpdates();
    });
  }

  Future<void> _connectToSlotUpdates() async {
    try {
      final token = await context.read<SecureStorageService>().getToken();
      if (!mounted || token == null || token.isEmpty) return;

      final apiBase = ApiConstants.baseUrl.replaceFirst(RegExp(r'/api/?$'), '');
      final connection = HubConnectionBuilder()
          .withUrl(
            '$apiBase/hubs/hospital',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => token,
            ),
          )
          .withAutomaticReconnect()
          .build();

      void refreshForSelectedDoctor(List<Object?>? arguments) {
        if (!mounted || !_matchesSelectedDoctorAndDate(arguments)) return;
        _loadSlots();
      }

      connection.on('SlotUpdated', refreshForSelectedDoctor);
      connection.on('SlotBooked', refreshForSelectedDoctor);
      connection.on('SlotReleased', refreshForSelectedDoctor);
      _slotUpdatesConnection = connection;
      await connection.start();
    } catch (_) {
      // Slot loading still works from the API on entry or when the date changes.
    }
  }

  bool _matchesSelectedDoctorAndDate(List<Object?>? arguments) {
    if (arguments == null || arguments.isEmpty) return true;
    final payload = arguments.first;
    if (payload is! Map) return true;
    final payloadDoctorId = payload['doctorId'] ?? payload['DoctorId'];
    final payloadProfileId =
        payload['doctorProfileId'] ?? payload['DoctorProfileId'];
    final matchesDoctor = payloadProfileId != null
        ? payloadProfileId.toString() == widget.doctorProfileId.toString()
        : payloadDoctorId == null ||
              payloadDoctorId.toString() == widget.bookingDoctorId.toString() ||
              payloadDoctorId.toString() == widget.doctorProfileId.toString();
    if (!matchesDoctor) return false;

    final specificDate = payload['specificDate'] ?? payload['SpecificDate'];
    if (specificDate != null) {
      return specificDate.toString().split('T').first == _dateString;
    }

    final dayOfWeek = payload['dayOfWeek'] ?? payload['DayOfWeek'];
    if (dayOfWeek == null) return true;
    const weekdays = <String>[
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
      'Sunday',
    ];
    return dayOfWeek.toString().toLowerCase() ==
        weekdays[_selectedDate.weekday - 1].toLowerCase();
  }

  @override
  void dispose() {
    final connection = _slotUpdatesConnection;
    if (connection != null) unawaited(connection.stop());
    super.dispose();
  }

  void _loadSlots() {
    context.read<AppointmentProvider>().loadSlots(
      doctorId: widget.bookingDoctorId,
      doctorProfileId: widget.doctorProfileId,
      date: _dateString,
    );
  }
//date
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
        backgroundColor: AppTheme.transparentColor,
        foregroundColor: AppTheme.textPrimary,
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
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: AppTheme.fontTitleMedium,
                  ),
                ),
                const SizedBox(height: 12),
                GestureDetector(
                  onTap: _pickDate,
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
                        const Icon(
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
                        const Text(
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
                  '2. Available Slots',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: AppTheme.fontTitleMedium,
                  ),
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
                  onSlotSelected: (slot) =>
                      setState(() => _selectedSlot = slot),
                  isLoading: provider.slotsLoading,
                ),

                if (_selectedSlot != null && _selectedSlot!.isAvailable) ...[
                  const SizedBox(height: 32),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryColor.withValues(alpha: 0.08),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                        color: AppTheme.primaryColor.withValues(alpha: 0.3),
                      ),
                    ),
                    child: Row(
                      children: [
                        const Icon(
                          Icons.check_circle,
                          color: AppTheme.primaryColor,
                          size: 28,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Selected Time',
                                style: TextStyle(
                                  color: AppTheme.primaryColor,
                                  fontSize: AppTheme.fontBodySmall,
                                ),
                              ),
                              Text(
                                '${_selectedSlot!.slotStart.day}/${_selectedSlot!.slotStart.month}/${_selectedSlot!.slotStart.year} at ${_selectedSlot!.formattedTime}',
                                style: TextStyle(
                                  color: AppTheme.primaryColor,
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
                  SizedBox(
                    width: double.infinity,
                    height: 52,
                    child: FilledButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppTheme.primaryColor,
                        foregroundColor: AppTheme.surfaceColor,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      onPressed: () => context.push(
                        '/appointments/book',
                        extra: {
                          'doctorId': widget.bookingDoctorId,
                          'slotStart': ApiDateTime.toUtcIso8601(
                            _selectedSlot!.slotStart,
                          ),
                          'durationMinutes': _selectedSlot!.durationMinutes,
                          if (widget.triageResultId != null) 'triageResultId': widget.triageResultId,
                        },
                      ),
                      child: const Text(
                        'Continue to Book',
                        style: TextStyle(
                          fontSize: AppTheme.fontTitleMedium,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
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
