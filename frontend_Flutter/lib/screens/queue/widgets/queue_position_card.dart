import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../models/appointments/queue_entry_model.dart';

class QueuePositionCard extends StatelessWidget {
  final QueueEntryModel? myEntry;
  final int? nowServingNumber;
  final int? totalWaiting;

  const QueuePositionCard({
    super.key,
    required this.myEntry,
    this.nowServingNumber,
    this.totalWaiting,
  });

  @override
  Widget build(BuildContext context) {
    if (myEntry == null) {
      return Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: Colors.grey.shade100,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: Colors.grey.shade300),
        ),
        child: Center(
          child: Text(
            'You are not currently in a queue.',
            style: TextStyle(color: Colors.grey.shade600),
          ),
        ),
      );
    }

    final entry = myEntry!;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppTheme.primaryColor,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        children: [
          const Text(
            'Your Queue Position',
            style: TextStyle(color: Colors.white70, fontSize: 14),
          ),
          const SizedBox(height: 8),
          Text(
            '#${entry.queueNumber}',
            style: const TextStyle(
              color: Colors.white,
              fontSize: 56,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Position ${entry.position} in line',
            style: const TextStyle(color: Colors.white70, fontSize: 14),
          ),
          const Divider(color: Colors.white24, height: 28),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _stat(
                label: 'Now Serving',
                value: nowServingNumber != null ? '#$nowServingNumber' : '—',
              ),
              _stat(
                label: 'Est. Wait',
                value: '~${entry.estimatedWaitMinutes} min',
              ),
              _stat(
                label: 'Status',
                value: entry.statusLabel,
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _stat({required String label, required String value}) {
    return Column(
      children: [
        Text(value,
            style: const TextStyle(
                color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16)),
        const SizedBox(height: 2),
        Text(label, style: const TextStyle(color: Colors.white60, fontSize: 11)),
      ],
    );
  }
}
