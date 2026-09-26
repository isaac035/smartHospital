import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/utils/time_format.dart';
import '../../providers/doctor_provider.dart';
import '../../widgets/error_message.dart';
import '../../widgets/status_chip.dart';

class DoctorAvailabilityScreen extends StatefulWidget {
  final int doctorId;

  const DoctorAvailabilityScreen({super.key, required this.doctorId});

  @override
  State<DoctorAvailabilityScreen> createState() => _DoctorAvailabilityScreenState();
}

class _DoctorAvailabilityScreenState extends State<DoctorAvailabilityScreen> {
  DateTime _selectedDate = DateTime.now();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      context.read<DoctorProvider>().checkAvailability(widget.doctorId, _selectedDate);
    });
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime.now().subtract(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      setState(() => _selectedDate = picked);
      if (mounted) {
        context.read<DoctorProvider>().checkAvailability(widget.doctorId, picked);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<DoctorProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Check Availability')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              child: ListTile(
                leading: const Icon(Icons.calendar_today),
                title: Text(DateFormat('EEEE, MMM d, yyyy').format(_selectedDate)),
                trailing: TextButton(onPressed: _pickDate, child: const Text('Change')),
              ),
            ),
            const SizedBox(height: 16),
            ErrorMessage(message: provider.availabilityError),
            if (provider.isLoadingAvailability)
              const Expanded(child: Center(child: CircularProgressIndicator()))
            else if (provider.isOnLeaveForDate)
              const Expanded(
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      StatusChip(state: DoctorAvailabilityState.onLeave),
                      SizedBox(height: 12),
                      Text('This doctor is on leave on the selected date.'),
                    ],
                  ),
                ),
              )
            else if (provider.availabilitySlotsForDate.isEmpty)
              const Expanded(
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      StatusChip(state: DoctorAvailabilityState.unavailable),
                      SizedBox(height: 12),
                      Text('No availability on the selected date.'),
                    ],
                  ),
                ),
              )
            else
              Expanded(
                child: ListView(
                  children: [
                    const StatusChip(state: DoctorAvailabilityState.available),
                    const SizedBox(height: 12),
                    for (final slot in provider.availabilitySlotsForDate)
                      Card(
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        child: ListTile(
                          leading: const Icon(Icons.access_time, color: Colors.green),
                          title: Text('${formatTimeOfDayString(slot.startTime)} – ${formatTimeOfDayString(slot.endTime)}'),
                          subtitle: Text(slot.consultationTypeName),
                        ),
                      ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}
