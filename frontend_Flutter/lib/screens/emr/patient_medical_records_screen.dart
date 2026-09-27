import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class PatientMedicalRecordsScreen extends StatelessWidget {
  const PatientMedicalRecordsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey.shade50,
      appBar: AppBar(
        title: const Text('Medical Records'),
        elevation: 0,
        backgroundColor: Colors.white,
        foregroundColor: Colors.black87,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          const Text('Health Data', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          const SizedBox(height: 12),
          _HubCard(
            icon: Icons.favorite,
            iconColor: Colors.redAccent,
            title: 'Vital Signs',
            subtitle: 'View temperature, BP, heart rate history',
            onTap: () => context.push('/medical-records/vitals'),
          ),
          const SizedBox(height: 12),
          _HubCard(
            icon: Icons.medication,
            iconColor: Colors.blue.shade600,
            title: 'Prescriptions',
            subtitle: 'Active and completed medications',
            onTap: () => context.push('/medical-records/prescriptions'),
          ),
          const SizedBox(height: 12),
          _HubCard(
            icon: Icons.biotech,
            iconColor: Colors.teal.shade600,
            title: 'Lab Reports',
            subtitle: 'Diagnostic test results and findings',
            onTap: () => context.push('/medical-records/lab-reports'),
          ),
          
          const SizedBox(height: 32),
          const Text('Recent Consultations', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(32),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: Colors.grey.shade200),
            ),
            child: Column(
              children: [
                Icon(Icons.folder_open, size: 48, color: Colors.grey.shade300),
                const SizedBox(height: 16),
                Text(
                  'No past consultations recorded.',
                  style: TextStyle(color: Colors.grey.shade600, fontWeight: FontWeight.w500),
                ),
                const SizedBox(height: 8),
                Text(
                  'Once a doctor completes your visit, your consultation notes will appear here.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Colors.grey.shade500, fontSize: 13),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _HubCard extends StatelessWidget {
  final IconData icon;
  final Color iconColor;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  const _HubCard({
    required this.icon,
    required this.iconColor,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Colors.grey.shade200),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.02), blurRadius: 4, offset: const Offset(0, 2))],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          borderRadius: BorderRadius.circular(16),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: iconColor.withValues(alpha: 0.1),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(icon, color: iconColor, size: 28),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                      const SizedBox(height: 4),
                      Text(subtitle, style: TextStyle(color: Colors.grey.shade600, fontSize: 13)),
                    ],
                  ),
                ),
                Icon(Icons.chevron_right, color: Colors.grey.shade400),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
