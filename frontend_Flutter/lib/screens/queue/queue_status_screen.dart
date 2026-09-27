import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/utils/hospital_date.dart';
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
      context.read<AppointmentProvider>().loadMyAppointments();
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
    _refreshTimer = Timer.periodic(const Duration(seconds: 5), (_) {
      if (mounted) {
        context.read<AppointmentProvider>().loadQueue(doctorId);
      }
    });
  }

  QueueEntryModel? _findMyEntry(
      List<QueueEntryModel> queue, int patientId) {
    try {
      return queue.firstWhere((e) => e.patientId == patientId);
    } catch (_) {
      return null;
    }
  }

  int? _nowServing(List<QueueEntryModel> queue) {
    return queue.isEmpty ? null : queue.first.currentQueueNumber;
  }

  @override
  Widget build(BuildContext context) {
    final patientId = context.read<AuthProvider>().currentUser?.id ?? 0;

    return Scaffold(
      backgroundColor: Colors.grey.shade50,
      appBar: AppBar(
        title: const Text('Live Queue Status'),
        elevation: 0,
        backgroundColor: Colors.white,
        foregroundColor: Colors.black87,
        actions: [
          if (_selectedDoctorId != null)
            IconButton(
              icon: const Icon(Icons.refresh, color: AppTheme.primaryColor),
              onPressed: () => _startPolling(_selectedDoctorId!),
              tooltip: 'Refresh',
            ),
        ],
      ),
      body: Consumer<AppointmentProvider>(
        builder: (context, provider, _) {
          // Build doctor picker from confirmed appointments
          final doctorOptions = provider.appointments
              .where((a) => a.doctorId != null && a.doctorName != null && (a.status == 1 || a.status == 2 || a.status == 3))
              .map((a) => {'id': a.doctorId!, 'name': a.doctorName!})
              .toList();
          // Deduplicate
          final seen = <int>{};
          final uniqueDoctors = doctorOptions.where((d) => seen.add(d['id'] as int)).toList();

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Doctor picker
                const Text('Select Your Doctor', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                const SizedBox(height: 12),
                if (provider.appointmentsLoading)
                  const Center(child: Padding(padding: EdgeInsets.all(16), child: CircularProgressIndicator()))
                else if (uniqueDoctors.isEmpty)
                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(12), border: Border.all(color: Colors.grey.shade200)),
                    child: Column(
                      children: [
                        Icon(Icons.event_busy, size: 48, color: Colors.grey.shade300),
                        const SizedBox(height: 12),
                        Text(
                          'No active appointments today. Queue tracking is only available for confirmed appointments.',
                          textAlign: TextAlign.center,
                          style: TextStyle(color: Colors.grey.shade600),
                        ),
                      ],
                    ),
                  )
                else
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: uniqueDoctors.map((d) {
                      final selected = _selectedDoctorId == d['id'] as int;
                      return ChoiceChip(
                        label: Text('Dr. ${d['name']}'),
                        selected: selected,
                        selectedColor: Colors.blue.shade100,
                        backgroundColor: Colors.white,
                        side: BorderSide(color: selected ? Colors.blue.shade300 : Colors.grey.shade300),
                        labelStyle: TextStyle(
                          color: selected ? Colors.blue.shade800 : Colors.grey.shade800,
                          fontWeight: selected ? FontWeight.bold : FontWeight.normal,
                        ),
                        onSelected: (_) => _selectDoctor(d['id'] as int),
                      );
                    }).toList(),
                  ),

                const SizedBox(height: 32),

                // Queue status
                if (_selectedDoctorId == null)
                  Container(
                    padding: const EdgeInsets.all(32),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: Colors.grey.shade200),
                    ),
                    child: Column(
                      children: [
                        Icon(Icons.people_outline, size: 56, color: Colors.grey.shade300),
                        const SizedBox(height: 16),
                        Text(
                          'Select a doctor above to see your real-time queue position and estimated wait.',
                          textAlign: TextAlign.center,
                          style: TextStyle(color: Colors.grey.shade600, fontSize: 14),
                        ),
                      ],
                    ),
                  )
                else if (provider.queueLoading && provider.queue.isEmpty)
                  const Center(child: Padding(padding: EdgeInsets.all(40), child: CircularProgressIndicator()))
                else ...[
                  if (provider.queueError != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 16),
                      child: ErrorMessage(message: provider.queueError),
                    ),
                    
                  QueuePositionCard(
                    myEntry: _findMyEntry(provider.queue, patientId),
                    nowServingNumber: _nowServing(provider.queue),
                    totalWaiting: provider.queue.where((e) => e.status == 1).length,
                  ),
                  if (provider.queue.isEmpty) ...[
                    const SizedBox(height: 12),
                    Builder(builder: (context) {
                      final candidates = provider.appointments.where((a) =>
                        a.doctorId == _selectedDoctorId &&
                        (a.status == 1 || a.status == 2) &&
                        HospitalDate.isToday(a.scheduledStart)).toList();
                      final appointment = candidates.isEmpty ? null : candidates.first;
                      if (appointment == null) return const SizedBox.shrink();
                      return FilledButton.icon(
                        onPressed: provider.queueLoading ? null : () async {
                          final entry = await provider.checkIn(appointment.id, _selectedDoctorId!);
                          if (entry != null && context.mounted) {
                            ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Check-in complete · ${entry.queueCode} · Position ${entry.position}')));
                          }
                        },
                        icon: const Icon(Icons.how_to_reg),
                        label: const Text('Check in for today’s appointment'),
                      );
                    }),
                  ],
                  
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.sync, size: 14, color: Colors.grey.shade500),
                      const SizedBox(width: 6),
                      Text('Auto-refreshes every 5 seconds', style: TextStyle(fontSize: 12, color: Colors.grey.shade500, fontWeight: FontWeight.w500)),
                    ],
                  ),
                  
                  const SizedBox(height: 32),

                ],
                const SizedBox(height: 40),
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
