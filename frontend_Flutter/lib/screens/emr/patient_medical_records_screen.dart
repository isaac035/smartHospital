import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../../models/emr/medical_record_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/emr_provider.dart';
import 'emr_widgets.dart';

class PatientMedicalRecordsScreen extends StatefulWidget {
  const PatientMedicalRecordsScreen({super.key});

  @override
  State<PatientMedicalRecordsScreen> createState() =>
      _PatientMedicalRecordsScreenState();
}

class _PatientMedicalRecordsScreenState
    extends State<PatientMedicalRecordsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadData();
    });
  }

  void _loadData({bool force = false}) {
    final user = context.read<AuthProvider>().currentUser;
    if (user != null) {
      final emr = context.read<EmrProvider>();
      emr.loadPatientProfile(user.id, force: force);
      emr.loadMedicalRecords(user.id, force: force);
      emr.loadVitals(user.id, force: force);
      emr.loadPrescriptions(user.id, force: force);
      emr.loadLabOrders(user.id, force: force);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.currentUser;

    if (user == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Medical Records')),
        body: EmrErrorView(
          message: 'You must be logged in to view your authorized medical records.',
          onRetry: () => context.go('/login'),
        ),
      );
    }

    final emr = context.watch<EmrProvider>();

    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(
        title: const Text('My Medical Records'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: () => _loadData(force: true),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => _loadData(force: true),
        child: ListView(
          padding: const EdgeInsets.all(16.0),
          children: [
            // Patient Medical Profile Banner
            PatientProfileCard(
              profile: emr.profile,
              isLoading: emr.profileLoading && emr.profile == null,
            ),
            const SizedBox(height: 16),

            // Quick Navigation Hub
            const Text(
              'Health Modules',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: AppTheme.textPrimary,
              ),
            ),
            const SizedBox(height: 10),
            _buildNavigationGrid(context, emr),
            const SizedBox(height: 24),

            // Clinical Consultations Header
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Consultations & Encounters',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.textPrimary,
                  ),
                ),
                if (emr.medicalRecords.isNotEmpty)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryColor.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Text(
                      '${emr.medicalRecords.length} visits',
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: AppTheme.primaryColor,
                      ),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 12),

            // Consultations List / State
            _buildConsultationsSection(emr, user.id),
          ],
        ),
      ),
    );
  }

  Widget _buildNavigationGrid(BuildContext context, EmrProvider emr) {
    return GridView(
      gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
        maxCrossAxisExtent: 320,
        mainAxisExtent: 118,
        crossAxisSpacing: 12,
        mainAxisSpacing: 12,
      ),
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      children: [
        _buildNavCard(
          title: 'Vital Signs',
          subtitle: emr.latestVital != null
              ? '${emr.latestVital!.systolicBloodPressure ?? "--"}/${emr.latestVital!.diastolicBloodPressure ?? "--"} mmHg'
              : 'Latest BP & metrics',
          icon: Icons.favorite_rounded,
          color: AppTheme.errorColor,
          onTap: () => context.push('/medical-records/vitals'),
        ),
        _buildNavCard(
          title: 'Prescriptions',
          subtitle: '${emr.prescriptions.length} recorded',
          icon: Icons.medication_rounded,
          color: AppTheme.primaryColor,
          onTap: () => context.push('/medical-records/prescriptions'),
        ),
        _buildNavCard(
          title: 'Lab Reports',
          subtitle: '${emr.labOrders.length} test orders',
          icon: Icons.biotech_rounded,
          color: AppTheme.accentColor,
          onTap: () => context.push('/medical-records/lab-reports'),
        ),
        _buildNavCard(
          title: 'Clinical Timeline',
          subtitle: 'Full health history',
          icon: Icons.timeline_rounded,
          color: AppTheme.secondaryColor,
          onTap: () => context.push('/medical-records/timeline'),
        ),
        _buildNavCard(
          title: 'AI Medical Reports',
          subtitle: 'View generated reports',
          icon: Icons.summarize_rounded,
          color: AppTheme.secondaryColor,
          onTap: () => context.push('/medical-records/ai-reports'),
        ),
      ],
    );
  }

  Widget _buildNavCard({
    required String title,
    required String subtitle,
    required IconData icon,
    required Color color,
    required VoidCallback onTap,
  }) {
    return Material(
      color: AppTheme.neutralContainer,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: const BorderSide(color: AppTheme.borderColor),
      ),
      elevation: 0,
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        splashColor: color.withValues(alpha: 0.16),
        highlightColor: color.withValues(alpha: 0.08),
        child: Padding(
          padding: const EdgeInsets.all(12.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.14),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: color.withValues(alpha: 0.25)),
                ),
                child: Icon(icon, color: color, size: 22),
              ),
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                      color: AppTheme.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    subtitle,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontSize: 11,
                      color: AppTheme.textSecondary,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildConsultationsSection(EmrProvider emr, int patientId) {
    if (emr.recordsLoading && emr.medicalRecords.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 40),
        child: EmrLoadingView(message: 'Loading consultation records...'),
      );
    }

    if (emr.recordsError != null && emr.medicalRecords.isEmpty) {
      return EmrErrorView(
        message: emr.recordsError!,
        onRetry: () => emr.loadMedicalRecords(patientId, force: true),
      );
    }

    if (emr.medicalRecords.isEmpty) {
      return const EmrEmptyState(
        icon: Icons.folder_open_rounded,
        title: 'No Consultations Recorded',
        subtitle:
            'Your doctor consultation records, notes, and visit summaries will appear here once conducted.',
      );
    }

    return Column(
      children: emr.medicalRecords.map((record) {
        return _buildRecordCard(record);
      }).toList(),
    );
  }

  Widget _buildRecordCard(MedicalRecordModel record) {
    final dateFormat = DateFormat('MMM dd, yyyy • hh:mm a');
    final formattedDate = dateFormat.format(record.visitDate);

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      elevation: 1,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: AppTheme.borderColor),
      ),
      child: InkWell(
        onTap: () => _showRecordDetailsModal(context, record),
        borderRadius: BorderRadius.circular(14),
        child: Padding(
          padding: const EdgeInsets.all(14.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      Icon(Icons.calendar_today_outlined,
                          size: 14, color: AppTheme.textSecondary),
                      const SizedBox(width: 6),
                      Text(
                        formattedDate,
                        style: TextStyle(
                          fontSize: 12,
                          color: AppTheme.textSecondary,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ],
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: AppTheme.surfaceColor,
                      borderRadius: BorderRadius.circular(6),
                      border: Border.all(color: AppTheme.borderColor),
                    ),
                    child: Text(
                      record.recordNumber,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.textPrimary,
                      ),
                    ),
                  ),
                ],
              ),
              const Divider(height: 16),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  CircleAvatar(
                    radius: 18,
                    backgroundColor:
                        AppTheme.primaryColor.withValues(alpha: 0.1),
                    child: const Icon(
                      Icons.medical_services_outlined,
                      size: 20,
                      color: AppTheme.primaryColor,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          record.doctorName.isNotEmpty
                              ? record.doctorName
                              : 'Attending Physician',
                          style: const TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Complaint: ${record.chiefComplaint}',
                          style: TextStyle(
                            fontSize: 13,
                            color: AppTheme.textPrimary,
                          ),
                        ),
                        if (record.diagnosis.isNotEmpty) ...[
                          const SizedBox(height: 6),
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 10, vertical: 4),
                            decoration: BoxDecoration(
                              color: AppTheme.primaryColor.withValues(alpha: 0.12),
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(color: AppTheme.primaryColor.withValues(alpha: 0.4)),
                            ),
                            child: Text(
                              'Diagnosis: ${record.diagnosis}',
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w600,
                                color: AppTheme.primaryColor,
                              ),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ],
              ),
              if (record.followUpDate != null) ...[
                const SizedBox(height: 10),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(
                    color: AppTheme.warningColor.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: AppTheme.warningColor.withValues(alpha: 0.4)),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.event_repeat,
                          size: 16, color: AppTheme.warningColor),
                      const SizedBox(width: 6),
                      Text(
                        'Follow-up scheduled: ${DateFormat('MMM dd, yyyy').format(record.followUpDate!)}',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: AppTheme.warningColor,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 8),
              Align(
                alignment: Alignment.centerRight,
                child: Text(
                  'Tap for clinical details →',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w500,
                    color: AppTheme.primaryColor.withValues(alpha: 0.8),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showRecordDetailsModal(BuildContext context, MedicalRecordModel record) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return DraggableScrollableSheet(
          initialChildSize: 0.7,
          minChildSize: 0.4,
          maxChildSize: 0.95,
          expand: false,
          builder: (context, scrollController) {
            return SingleChildScrollView(
              controller: scrollController,
              padding: const EdgeInsets.all(20.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Center(
                    child: Container(
                      width: 40,
                      height: 5,
                      decoration: BoxDecoration(
                        color: AppTheme.borderColor,
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Consultation Details',
                        style: TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                          color: AppTheme.textPrimary,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppTheme.primaryColor.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          record.recordNumber,
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.primaryColor,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Recorded on ${DateFormat('MMMM dd, yyyy • hh:mm a').format(record.visitDate)}',
                    style: TextStyle(
                      fontSize: 13,
                      color: AppTheme.textSecondary,
                    ),
                  ),
                  const Divider(height: 24),

                  _buildDetailSection(
                    icon: Icons.person_outline,
                    title: 'Attending Physician',
                    content: record.doctorName,
                  ),
                  _buildDetailSection(
                    icon: Icons.chat_bubble_outline,
                    title: 'Chief Complaint',
                    content: record.chiefComplaint,
                  ),
                  if (record.symptoms != null && record.symptoms!.isNotEmpty)
                    _buildDetailSection(
                      icon: Icons.sick_outlined,
                      title: 'Symptoms Reported',
                      content: record.symptoms!,
                    ),
                  if (record.examinationNotes != null &&
                      record.examinationNotes!.isNotEmpty)
                    _buildDetailSection(
                      icon: Icons.assignment_outlined,
                      title: 'Clinical Examination Notes',
                      content: record.examinationNotes!,
                    ),
                  _buildDetailSection(
                    icon: Icons.verified_outlined,
                    title: 'Diagnosis',
                    content: record.diagnosis,
                    isHighlight: true,
                  ),
                  if (record.treatmentPlan != null &&
                      record.treatmentPlan!.isNotEmpty)
                    _buildDetailSection(
                      icon: Icons.healing_outlined,
                      title: 'Treatment & Care Plan',
                      content: record.treatmentPlan!,
                    ),
                  if (record.followUpDate != null)
                    _buildDetailSection(
                      icon: Icons.event_repeat_outlined,
                      title: 'Follow-up Date',
                      content: DateFormat('MMMM dd, yyyy')
                          .format(record.followUpDate!),
                    ),
                  const SizedBox(height: 20),
                ],
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildDetailSection({
    required IconData icon,
    required String title,
    required String content,
    bool isHighlight = false,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 16, color: isHighlight ? AppTheme.primaryColor : AppTheme.primaryColor),
              const SizedBox(width: 6),
              Text(
                title,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.bold,
                  color: isHighlight ? AppTheme.primaryColor : AppTheme.textSecondary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: isHighlight ? AppTheme.primaryColor.withValues(alpha: 0.12) : AppTheme.surfaceColor,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(
                color: isHighlight ? AppTheme.primaryColor.withValues(alpha: 0.4) : AppTheme.borderColor,
              ),
            ),
            child: Text(
              content.isNotEmpty ? content : 'No specific details recorded.',
              style: TextStyle(
                fontSize: 14,
                color: isHighlight ? AppTheme.primaryColor : AppTheme.textPrimary,
                height: 1.4,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

