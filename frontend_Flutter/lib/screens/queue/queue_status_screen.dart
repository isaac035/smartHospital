import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_theme.dart';
import '../../models/appointments/queue_entry_model.dart';
import '../../providers/appointment_provider.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/error_message.dart';
import 'widgets/queue_position_card.dart';

/// Patient-facing queue status screen.
/// The patient picks the doctor they have an appointment with.
/// The screen shows their current queue position and the now-serving number.
class QueueStatusScreen extends StatefulWidget {
  const QueueStatusScreen({super.key});

  @override
  State<QueueStatusScreen> createState() => _QueueStatusScreenState();
}

class _QueueStatusScreenState extends State<QueueStatusScreen> {
  // The patient selects the doctor from their upcoming appointments
  int? _selectedDoctorId;
  Timer? _refreshTimer;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      // Load upcoming appointments to determine which doctors the patient has
      context.read<AppointmentProvider>().loadMyAppointments(
          statusFilter: 'Confirmed');
    });
  }

  @override
  void dispose() {
    _refreshTimer?.cancel();
    super.dispose();
  }

  void _selectDoctor(int doctorId) {
    setState(() {
      _selectedDoctorId = doctorId;
    });
    _startPolling(doctorId);
  }

  void _startPolling(int doctorId) {
    _refreshTimer?.cancel();
    context.read<AppointmentProvider>().loadQueue(doctorId);
    _refreshTimer = Timer.periodic(const Duration(seconds: 15), (_) {
      if (mounted) {
        context.read<AppointmentProvider>().loadQueue(doctorId);
      }
    });
  }

  QueueEntryModel? _findMyEntry(
      List<QueueEntryModel> queue, int patientId) {
    try {
      return queue.firstWhere(
          (e) => e.patientId == patientId && e.isActive);
    } catch (_) {
      return null;
    }
  }

  int? _nowServing(List<QueueEntryModel> queue) {
    try {
      return queue
          .firstWhere((e) => e.status == 2 || e.status == 3)
          .queueNumber;
    } catch (_) {
      return null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final patientId =
        context.read<AuthProvider>().currentUser?.id ?? 0;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Queue Status'),
        actions: [
          if (_selectedDoctorId != null)
            IconButton(
              icon: const Icon(Icons.refresh),
              onPressed: () =>
                  _startPolling(_selectedDoctorId!),
              tooltip: 'Refresh',
            ),
        ],
      ),
      body: Consumer<AppointmentProvider>(
        builder: (context, provider, _) {
          // Build doctor picker from confirmed appointments
          final doctorOptions = provider.appointments
              .where((a) =>
                  a.doctorId != null && a.doctorName != null)
              .map((a) => {'id': a.doctorId!, 'name': a.doctorName!})
              .toList();
          // Deduplicate
          final seen = <int>{};
          final uniqueDoctors = doctorOptions
              .where((d) => seen.add(d['id'] as int))
              .toList();

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Doctor picker
                const Text('Select Your Doctor',
                    style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 15)),
                const SizedBox(height: 8),
                if (provider.appointmentsLoading)
                  const Center(child: CircularProgressIndicator())
                else if (uniqueDoctors.isEmpty)
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: Colors.grey.shade100,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      'You have no confirmed appointments. Queue tracking is only available for confirmed appointments.',
                      style:
                          TextStyle(color: Colors.grey.shade600),
                    ),
                  )
                else
                  Wrap(
                    spacing: 8,
                    children: uniqueDoctors.map((d) {
                      final selected =
                          _selectedDoctorId == d['id'] as int;
                      return ChoiceChip(
                        label: Text('Dr. ${d['name']}'),
                        selected: selected,
                        onSelected: (_) => _selectDoctor(d['id'] as int),
                      );
                    }).toList(),
                  ),

                const SizedBox(height: 24),

                // Queue status
                if (_selectedDoctorId == null)
                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: Colors.grey.shade100,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: const Center(
                      child: Text(
                          'Select a doctor above to see your queue position.'),
                    ),
                  )
                else if (provider.queueLoading)
                  const Center(
                      child: Padding(
                    padding: EdgeInsets.all(32),
                    child: CircularProgressIndicator(),
                  ))
                else ...[
                  if (provider.queueError != null)
                    ErrorMessage(message: provider.queueError),
                  QueuePositionCard(
                    myEntry: _findMyEntry(provider.queue, patientId),
                    nowServingNumber: _nowServing(provider.queue),
                    totalWaiting: provider.queue
                        .where((e) => e.status == 1)
                        .length,
                  ),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.refresh,
                          size: 14, color: Colors.grey.shade500),
                      const SizedBox(width: 4),
                      Text(
                        'Auto-refreshes every 15 seconds',
                        style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey.shade500),
                      ),
                    ],
                  ),
                  const SizedBox(height: 24),

                  // Waiting list preview
                  if (provider.queue.isNotEmpty) ...[
                    Text(
                      'Waiting List (${provider.queue.where((e) => e.status == 1).length})',
                      style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 15),
                    ),
                    const SizedBox(height: 10),
                    ...provider.queue
                        .where((e) => e.status == 1)
                        .take(10)
                        .map((entry) => _WaitingTile(
                              entry: entry,
                              isMe: entry.patientId == patientId,
                            )),
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
}

class _WaitingTile extends StatelessWidget {
  final QueueEntryModel entry;
  final bool isMe;

  const _WaitingTile({required this.entry, required this.isMe});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 6),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: isMe
            ? AppTheme.primaryColor.withValues(alpha: 0.08)
            : Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
          color: isMe
              ? AppTheme.primaryColor.withValues(alpha: 0.4)
              : Colors.grey.shade200,
          width: isMe ? 1.5 : 1,
        ),
      ),
      child: Row(
        children: [
          Text(
            '#${entry.queueNumber}',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              color: isMe
                  ? AppTheme.primaryColor
                  : AppTheme.secondaryColor,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              isMe ? 'You' : 'Patient ${entry.queueNumber}',
              style: TextStyle(
                color: isMe ? AppTheme.primaryColor : Colors.grey.shade600,
                fontWeight:
                    isMe ? FontWeight.bold : FontWeight.normal,
              ),
            ),
          ),
          Text(
            '~${entry.estimatedWaitMinutes} min',
            style:
                TextStyle(fontSize: 12, color: Colors.grey.shade500),
          ),
        ],
      ),
    );
  }
}
