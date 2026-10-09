import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import '../../models/emr/patient_medical_profile_model.dart';

class EmrLoadingView extends StatelessWidget {
  final String message;

  const EmrLoadingView({
    super.key,
    this.message = 'Loading medical records...',
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const CircularProgressIndicator(color: AppTheme.primaryColor),
          const SizedBox(height: 16),
          Text(
            message,
            style: TextStyle(
              color: AppTheme.textSecondary,
              fontSize: 14,
            ),
          ),
        ],
      ),
    );
  }
}

class EmrErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const EmrErrorView({
    super.key,
    required this.message,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    final isAuthError = message.toLowerCase().contains('unauthorized') ||
        message.toLowerCase().contains('access denied') ||
        message.toLowerCase().contains('permission');

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              isAuthError ? Icons.lock_person_outlined : Icons.cloud_off_outlined,
              size: 56,
              color: isAuthError ? AppTheme.warningColor : AppTheme.errorColor,
            ),
            const SizedBox(height: 16),
            Text(
              isAuthError ? 'Access Restricted' : 'Unable to Load Records',
              style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              message,
              textAlign: TextAlign.center,
              style: TextStyle(color: AppTheme.textSecondary, fontSize: 14),
            ),
            const SizedBox(height: 20),
            ElevatedButton.icon(
              onPressed: onRetry,
              icon: const Icon(Icons.refresh, size: 18),
              label: const Text('Try Again'),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primaryColor,
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class EmrEmptyState extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final Widget? action;

  const EmrEmptyState({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    this.action,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32.0),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: AppTheme.primaryColor.withValues(alpha: 0.08),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 48, color: AppTheme.primaryColor),
            ),
            const SizedBox(height: 16),
            Text(
              title,
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppTheme.textPrimary,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              subtitle,
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 14,
                color: AppTheme.textSecondary,
              ),
            ),
            if (action != null) ...[
              const SizedBox(height: 20),
              action!,
            ],
          ],
        ),
      ),
    );
  }
}

class EmrStatusBadge extends StatelessWidget {
  final String status;
  final Color? color;

  const EmrStatusBadge({
    super.key,
    required this.status,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    Color badgeColor = color ?? AppTheme.primaryColor;
    final lower = status.toLowerCase();

    if (color == null) {
      if (lower.contains('active') || lower.contains('completed') || lower.contains('normal')) {
        badgeColor = AppTheme.successColor;
      } else if (lower.contains('pending') || lower.contains('ordered') || lower.contains('in progress')) {
        badgeColor = AppTheme.warningColor;
      } else if (lower.contains('discontinued') || lower.contains('cancelled') || lower.contains('high') || lower.contains('stat')) {
        badgeColor = AppTheme.errorColor;
      } else {
        badgeColor = AppTheme.textSecondary;
      }
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: badgeColor.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: badgeColor.withValues(alpha: 0.3)),
      ),
      child: Text(
        status.toUpperCase(),
        style: TextStyle(
          color: badgeColor,
          fontSize: 11,
          fontWeight: FontWeight.bold,
          letterSpacing: 0.5,
        ),
      ),
    );
  }
}

class PatientProfileCard extends StatelessWidget {
  final PatientMedicalProfileModel? profile;
  final bool isLoading;

  const PatientProfileCard({
    super.key,
    required this.profile,
    this.isLoading = false,
  });

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return Container(
        height: 140,
        margin: const EdgeInsets.only(bottom: 16),
        decoration: BoxDecoration(
          color: AppTheme.neutralContainer,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppTheme.borderColor),
        ),
        child: const Center(
          child: CircularProgressIndicator(strokeWidth: 2),
        ),
      );
    }

    final hasAllergies = profile != null &&
        profile!.allergies.isNotEmpty &&
        profile!.allergies.toLowerCase() != 'none';

    final hasChronic = profile != null &&
        profile!.chronicDiseases.isNotEmpty &&
        profile!.chronicDiseases.toLowerCase() != 'none';

    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  radius: 22,
                  backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
                  child: const Icon(Icons.person, color: AppTheme.primaryColor),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        profile?.patientName.isNotEmpty == true
                            ? profile!.patientName
                            : 'Patient Medical Profile',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      if (profile != null)
                        Text(
                          '${profile!.gender} • Blood: ${profile!.bloodGroup.isNotEmpty ? profile!.bloodGroup : "Not recorded"}',
                          style: TextStyle(
                            fontSize: 13,
                            color: AppTheme.textSecondary,
                          ),
                        ),
                    ],
                  ),
                ),
                if (profile != null && profile!.bloodGroup.isNotEmpty)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                    decoration: BoxDecoration(
                      color: AppTheme.errorColor.withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: AppTheme.errorColor.withValues(alpha: 0.4)),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.water_drop, size: 16, color: AppTheme.errorColor),
                        const SizedBox(width: 4),
                        Text(
                          profile!.bloodGroup,
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            color: AppTheme.errorColor,
                            fontSize: 13,
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 14),

            // Allergies Banner
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(
                color: hasAllergies ? AppTheme.errorColor.withValues(alpha: 0.12) : AppTheme.successColor.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(
                  color: hasAllergies ? AppTheme.errorColor.withValues(alpha: 0.4) : AppTheme.successColor.withValues(alpha: 0.4),
                ),
              ),
              child: Row(
                children: [
                  Icon(
                    hasAllergies ? Icons.warning_amber_rounded : Icons.check_circle_outline,
                    size: 18,
                    color: hasAllergies ? AppTheme.errorColor : AppTheme.successColor,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      hasAllergies
                          ? 'Allergies: ${profile!.allergies}'
                          : 'No known drug or environmental allergies',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: hasAllergies ? AppTheme.errorColor : AppTheme.successColor,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            if (hasChronic) ...[
              const SizedBox(height: 8),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: AppTheme.warningColor.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: AppTheme.warningColor.withValues(alpha: 0.4)),
                ),
                child: Row(
                  children: [
                    Icon(Icons.healing, size: 18, color: AppTheme.warningColor),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Chronic Conditions: ${profile!.chronicDiseases}',
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w500,
                          color: AppTheme.warningColor,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],

            if (profile != null &&
                profile!.emergencyContactName.isNotEmpty &&
                profile!.emergencyContactPhone.isNotEmpty) ...[
              const SizedBox(height: 10),
              Row(
                children: [
                  Icon(Icons.contact_phone_outlined, size: 16, color: AppTheme.textSecondary),
                  const SizedBox(width: 6),
                  Text(
                    'Emergency: ${profile!.emergencyContactName} (${profile!.emergencyContactPhone})',
                    style: TextStyle(
                      fontSize: 12,
                      color: AppTheme.textSecondary,
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}
