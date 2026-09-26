import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/utils/time_format.dart';
import '../../models/schedule_model.dart';
import '../../providers/doctor_provider.dart';
import '../../widgets/error_message.dart';

class DoctorScheduleScreen extends StatefulWidget {
  final int doctorId;

  const DoctorScheduleScreen({super.key, required this.doctorId});

  @override
  State<DoctorScheduleScreen> createState() => _DoctorScheduleScreenState();
}

class _DoctorScheduleScreenState extends State<DoctorScheduleScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      context.read<DoctorProvider>().loadSchedule(widget.doctorId);
    });
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<DoctorProvider>();

    final grouped = <String, List<Schedule>>{
      for (final day in kDayOfWeekNames)
        day: provider.fullSchedule.where((s) => s.dayOfWeek == day).toList(),
    };

    return Scaffold(
      appBar: AppBar(title: const Text('Weekly Schedule')),
      body: provider.isLoadingSchedule
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                ErrorMessage(message: provider.scheduleError),
                if (provider.fullSchedule.isEmpty)
                  const Padding(
                    padding: EdgeInsets.only(top: 32),
                    child: Center(child: Text('No published schedule yet.')),
                  ),
                for (final day in kDayOfWeekNames)
                  if (grouped[day]!.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 16),
                      child: Card(
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(day, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                              const SizedBox(height: 8),
                              for (final slot in grouped[day]!)
                                Padding(
                                  padding: const EdgeInsets.symmetric(vertical: 4),
                                  child: Row(
                                    children: [
                                      const Icon(Icons.access_time, size: 16, color: Colors.grey),
                                      const SizedBox(width: 8),
                                      Text('${formatTimeOfDayString(slot.startTime)} – ${formatTimeOfDayString(slot.endTime)}'),
                                      const SizedBox(width: 12),
                                      Chip(label: Text(slot.consultationTypeName), visualDensity: VisualDensity.compact),
                                    ],
                                  ),
                                ),
                            ],
                          ),
                        ),
                      ),
                    ),
              ],
            ),
    );
  }
}
