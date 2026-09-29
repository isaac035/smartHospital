import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
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
          color: AppTheme.neutralContainer,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppTheme.borderColor),
        ),
        child: Center(
          child: Text(
            'You are not currently in a queue.',
            style: TextStyle(color: AppTheme.textSecondary),
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
            style: TextStyle(
              color: AppTheme.onPrimary,
              fontSize: AppTheme.fontBodyMedium,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            entry.queueCode.isEmpty ? '#${entry.queueNumber}' : entry.queueCode,
            style: TextStyle(
              color: AppTheme.surfaceColor,
              fontSize: AppTheme.fontDisplaySmall,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            entry.isYourTurn
                ? 'Your turn'
                : '${entry.patientsAhead} patients ahead',
            style: TextStyle(
              color: AppTheme.onPrimary,
              fontSize: AppTheme.fontBodyMedium,
            ),
          ),
          const Divider(color: AppTheme.onPrimary, height: 28),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _stat(
                label: 'Now Serving',
                value:
                    entry.currentQueueCode ??
                    (nowServingNumber != null ? '#$nowServingNumber' : '—'),
              ),
              _stat(
                label: 'Est. Wait',
                value: '~${entry.estimatedWaitMinutes} min',
              ),
              _stat(label: 'Status', value: entry.statusLabel),
            ],
          ),
        ],
      ),
    );
  }

  Widget _stat({required String label, required String value}) {
    return Column(
      children: [
        Text(
          value,
          style: TextStyle(
            color: AppTheme.surfaceColor,
            fontWeight: FontWeight.bold,
            fontSize: AppTheme.fontTitleMedium,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          label,
          style: TextStyle(
            color: AppTheme.onPrimary,
            fontSize: AppTheme.fontBodySmall,
          ),
        ),
      ],
    );
  }
}
