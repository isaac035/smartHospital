import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:go_router/go_router.dart';

class PatientMedicalRecordsScreen extends StatelessWidget {
  const PatientMedicalRecordsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(
        title: const Text('Medical Records'),
        elevation: 0,
        backgroundColor: AppTheme.surfaceColor,
        foregroundColor: AppTheme.textPrimary,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          const Text(
            'Health Data',
            style: TextStyle(
              fontSize: AppTheme.fontTitleMedium,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),
          _HubCard(
            icon: Icons.favorite,
            iconColor: AppTheme.errorColor,
            title: 'Vital Signs',
            subtitle: 'View temperature, BP, heart rate history',
            onTap: () => context.push('/medical-records/vitals'),
          ),
          const SizedBox(height: 12),
          _HubCard(
            icon: Icons.medication,
            iconColor: AppTheme.primaryColor,
            title: 'Prescriptions',
            subtitle: 'Active and completed medications',
            onTap: () => context.push('/medical-records/prescriptions'),
          ),
          const SizedBox(height: 12),
          _HubCard(
            icon: Icons.biotech,
            iconColor: AppTheme.secondaryColor,
            title: 'Lab Reports',
            subtitle: 'Diagnostic test results and findings',
            onTap: () => context.push('/medical-records/lab-reports'),
          ),

          const SizedBox(height: 32),
          const Text(
            'Recent Consultations',
            style: TextStyle(
              fontSize: AppTheme.fontTitleMedium,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(32),
            decoration: BoxDecoration(
              color: AppTheme.surfaceColor,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppTheme.neutralContainer),
            ),
            child: Column(
              children: [
                Icon(Icons.folder_open, size: 48, color: AppTheme.borderColor),
                const SizedBox(height: 16),
                Text(
                  'No past consultations recorded.',
                  style: TextStyle(
                    color: AppTheme.textSecondary,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Once a doctor completes your visit, your consultation notes will appear here.',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: AppTheme.onPrimary,
                    fontSize: AppTheme.fontBodyMedium,
                  ),
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
        color: AppTheme.surfaceColor,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppTheme.neutralContainer),
        boxShadow: [
          BoxShadow(
            color: AppTheme.textPrimary.withValues(alpha: 0.02),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Material(
        color: AppTheme.transparentColor,
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
                      Text(
                        title,
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: AppTheme.fontTitleMedium,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        subtitle,
                        style: TextStyle(
                          color: AppTheme.textSecondary,
                          fontSize: AppTheme.fontBodyMedium,
                        ),
                      ),
                    ],
                  ),
                ),
                Icon(Icons.chevron_right, color: AppTheme.textMuted),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
