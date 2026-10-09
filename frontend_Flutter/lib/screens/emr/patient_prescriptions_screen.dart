import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../../models/emr/prescription_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/emr_provider.dart';
import 'emr_widgets.dart';

class PatientPrescriptionsScreen extends StatefulWidget {
  const PatientPrescriptionsScreen({super.key});

  @override
  State<PatientPrescriptionsScreen> createState() =>
      _PatientPrescriptionsScreenState();
}

class _PatientPrescriptionsScreenState
    extends State<PatientPrescriptionsScreen> {
  String _selectedFilter = 'All';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadPrescriptions();
    });
  }

  void _loadPrescriptions({bool force = false}) {
    final user = context.read<AuthProvider>().currentUser;
    if (user != null) {
      context.read<EmrProvider>().loadPrescriptions(user.id, force: force);
    }
  }

  List<PrescriptionModel> _getFilteredList(List<PrescriptionModel> list) {
    if (_selectedFilter == 'All') return list;
    return list.where((p) {
      final s = p.status.toLowerCase();
      if (_selectedFilter == 'Active') return s == 'active';
      if (_selectedFilter == 'Completed') return s == 'completed';
      if (_selectedFilter == 'Discontinued') {
        return s == 'discontinued' || s == 'cancelled';
      }
      return true;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;

    if (user == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('My Prescriptions')),
        body: EmrErrorView(
          message: 'You must be logged in to view your prescriptions.',
          onRetry: () => Navigator.of(context).pop(),
        ),
      );
    }

    final emr = context.watch<EmrProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Prescriptions'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh Prescriptions',
            onPressed: () => _loadPrescriptions(force: true),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => _loadPrescriptions(force: true),
        child: Column(
          children: [
            // Filter Bar
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              color: AppTheme.surfaceColor,
              child: Row(
                children: [
                  _buildFilterChip('All'),
                  const SizedBox(width: 8),
                  _buildFilterChip('Active'),
                  const SizedBox(width: 8),
                  _buildFilterChip('Completed'),
                  const SizedBox(width: 8),
                  _buildFilterChip('Discontinued'),
                ],
              ),
            ),
            const Divider(height: 1),

            // Main List Area
            Expanded(
              child: _buildPrescriptionsList(emr, user.id),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildFilterChip(String label) {
    final isSelected = _selectedFilter == label;
    return ChoiceChip(
      label: Text(label),
      selected: isSelected,
      onSelected: (val) {
        if (val) {
          setState(() {
            _selectedFilter = label;
          });
        }
      },
      selectedColor: AppTheme.primaryColor,
      backgroundColor: AppTheme.surfaceColor,
      labelStyle: TextStyle(
        color: isSelected ? Colors.white : AppTheme.textPrimary,
        fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
        fontSize: 13,
      ),
      side: BorderSide(
        color: isSelected ? AppTheme.primaryColor : AppTheme.borderColor,
      ),
    );
  }

  Widget _buildPrescriptionsList(EmrProvider emr, int patientId) {
    if (emr.prescriptionsLoading && emr.prescriptions.isEmpty) {
      return const EmrLoadingView(message: 'Loading your prescriptions...');
    }

    if (emr.prescriptionsError != null && emr.prescriptions.isEmpty) {
      return EmrErrorView(
        message: emr.prescriptionsError!,
        onRetry: () => _loadPrescriptions(force: true),
      );
    }

    final filtered = _getFilteredList(emr.prescriptions);

    if (filtered.isEmpty) {
      return EmrEmptyState(
        icon: Icons.medication_outlined,
        title: _selectedFilter == 'All'
            ? 'No Prescriptions Found'
            : 'No $_selectedFilter Prescriptions',
        subtitle: _selectedFilter == 'All'
            ? 'Your doctor-issued medication orders and dosing guidelines will appear here.'
            : 'You do not have any prescriptions with status "$_selectedFilter".',
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: filtered.length,
      itemBuilder: (context, index) {
        final prescription = filtered[index];
        return _buildPrescriptionCard(prescription);
      },
    );
  }

  Widget _buildPrescriptionCard(PrescriptionModel prescription) {
    final dateFormat = DateFormat('MMM dd, yyyy');
    final formattedIssueDate = dateFormat.format(prescription.issueDate);
    final formattedExpiryDate = prescription.expiryDate != null
        ? dateFormat.format(prescription.expiryDate!)
        : null;

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: AppTheme.borderColor),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Top Bar
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Icon(Icons.receipt_long, size: 16, color: AppTheme.primaryColor),
                    const SizedBox(width: 6),
                    Text(
                      prescription.prescriptionNumber,
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.primaryColor,
                      ),
                    ),
                  ],
                ),
                EmrStatusBadge(status: prescription.status),
              ],
            ),
            const Divider(height: 18),

            // Doctor and Dates
            Row(
              children: [
                CircleAvatar(
                  radius: 16,
                  backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
                  child: const Icon(Icons.person, size: 18, color: AppTheme.primaryColor),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        prescription.doctorName.isNotEmpty
                            ? prescription.doctorName
                            : 'Prescribing Physician',
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: AppTheme.textPrimary,
                        ),
                      ),
                      Text(
                        'Issued: $formattedIssueDate${formattedExpiryDate != null ? ' • Valid until: $formattedExpiryDate' : ''}',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppTheme.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),

            if (prescription.generalInstructions.isNotEmpty) ...[
              const SizedBox(height: 12),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: AppTheme.primaryColor.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: AppTheme.primaryColor.withValues(alpha: 0.4)),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(Icons.info_outline, size: 16, color: AppTheme.primaryColor),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        prescription.generalInstructions,
                        style: TextStyle(
                          fontSize: 12,
                          color: AppTheme.primaryColor,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],

            const SizedBox(height: 16),
            Text(
              'Medications (${prescription.items.length})',
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.bold,
                color: AppTheme.textPrimary,
              ),
            ),
            const SizedBox(height: 8),

            // Medicines List
            ...prescription.items.asMap().entries.map((entry) {
              final idx = entry.key + 1;
              final item = entry.value;
              return Container(
                margin: const EdgeInsets.only(bottom: 8),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceColor,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: AppTheme.borderColor),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          width: 22,
                          height: 22,
                          alignment: Alignment.center,
                          decoration: BoxDecoration(
                            color: AppTheme.primaryColor.withValues(alpha: 0.1),
                            shape: BoxShape.circle,
                          ),
                          child: Text(
                            '$idx',
                            style: const TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.bold,
                              color: AppTheme.primaryColor,
                            ),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                item.medicineName,
                                style: const TextStyle(
                                  fontSize: 14,
                                  fontWeight: FontWeight.bold,
                                  color: AppTheme.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                '${item.dosage} • ${item.route}',
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                  color: AppTheme.primaryColor,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        _buildItemTag(Icons.repeat, item.frequency),
                        const SizedBox(width: 8),
                        _buildItemTag(Icons.schedule, '${item.durationDays} days'),
                      ],
                    ),
                    if (item.specialInstructions.isNotEmpty) ...[
                      const SizedBox(height: 6),
                      Text(
                        'Note: ${item.specialInstructions}',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppTheme.textSecondary,
                          fontStyle: FontStyle.italic,
                        ),
                      ),
                    ],
                  ],
                ),
              );
            }),
          ],
        ),
      ),
    );
  }

  Widget _buildItemTag(IconData icon, String text) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: AppTheme.neutralContainer,
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: AppTheme.borderColor),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: AppTheme.textSecondary),
          const SizedBox(width: 4),
          Text(
            text,
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w500,
              color: AppTheme.textPrimary,
            ),
          ),
        ],
      ),
    );
  }
}
