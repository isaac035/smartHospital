import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../../models/emr/lab_order_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/emr_provider.dart';
import 'emr_widgets.dart';

class PatientLabReportsScreen extends StatefulWidget {
  const PatientLabReportsScreen({super.key});

  @override
  State<PatientLabReportsScreen> createState() =>
      _PatientLabReportsScreenState();
}

class _PatientLabReportsScreenState extends State<PatientLabReportsScreen> {
  String _selectedFilter = 'All';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadLabOrders();
    });
  }

  void _loadLabOrders({bool force = false}) {
    final user = context.read<AuthProvider>().currentUser;
    if (user != null) {
      context.read<EmrProvider>().loadLabOrders(user.id, force: force);
    }
  }

  List<LabOrderModel> _getFilteredList(List<LabOrderModel> list) {
    if (_selectedFilter == 'All') return list;
    return list.where((o) {
      final s = o.status.toLowerCase();
      if (_selectedFilter == 'Completed') {
        return s == 'completed' || o.report != null;
      }
      if (_selectedFilter == 'Pending') {
        return s == 'pending' || s == 'ordered' || s == 'inprogress';
      }
      return true;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;

    if (user == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('My Lab Reports')),
        body: EmrErrorView(
          message: 'You must be logged in to view your lab reports.',
          onRetry: () => Navigator.of(context).pop(),
        ),
      );
    }

    final emr = context.watch<EmrProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Lab Reports'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh Lab Reports',
            onPressed: () => _loadLabOrders(force: true),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => _loadLabOrders(force: true),
        child: Column(
          children: [
            // Filter Bar
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              color: Colors.white,
              child: Row(
                children: [
                  _buildFilterChip('All'),
                  const SizedBox(width: 8),
                  _buildFilterChip('Completed'),
                  const SizedBox(width: 8),
                  _buildFilterChip('Pending'),
                ],
              ),
            ),
            const Divider(height: 1),

            // Content Area
            Expanded(
              child: _buildLabList(emr, user.id),
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
      backgroundColor: Colors.grey.shade100,
      labelStyle: TextStyle(
        color: isSelected ? Colors.white : Colors.grey.shade800,
        fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
        fontSize: 13,
      ),
      side: BorderSide(
        color: isSelected ? AppTheme.primaryColor : Colors.grey.shade300,
      ),
    );
  }

  Widget _buildLabList(EmrProvider emr, int patientId) {
    if (emr.labOrdersLoading && emr.labOrders.isEmpty) {
      return const EmrLoadingView(message: 'Loading laboratory reports...');
    }

    if (emr.labOrdersError != null && emr.labOrders.isEmpty) {
      return EmrErrorView(
        message: emr.labOrdersError!,
        onRetry: () => _loadLabOrders(force: true),
      );
    }

    final filtered = _getFilteredList(emr.labOrders);

    if (filtered.isEmpty) {
      return EmrEmptyState(
        icon: Icons.biotech_outlined,
        title: _selectedFilter == 'All'
            ? 'No Lab Orders Found'
            : 'No $_selectedFilter Lab Tests',
        subtitle: _selectedFilter == 'All'
            ? 'Diagnostic lab orders and test findings will appear here once requested by your doctor.'
            : 'No lab orders with status "$_selectedFilter" were found.',
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: filtered.length,
      itemBuilder: (context, index) {
        final order = filtered[index];
        return _buildLabOrderCard(order);
      },
    );
  }

  Widget _buildLabOrderCard(LabOrderModel order) {
    final hasReport = order.report != null;
    final dateFormat = DateFormat('MMM dd, yyyy');
    final formattedOrderedDate = dateFormat.format(order.orderedAt);

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: Colors.grey.shade200),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header: Order Number and Badges
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Icon(Icons.biotech, size: 16, color: Colors.teal.shade700),
                    const SizedBox(width: 6),
                    Text(
                      order.orderNumber,
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: Colors.teal.shade900,
                      ),
                    ),
                  ],
                ),
                Row(
                  children: [
                    if (order.priority.isNotEmpty &&
                        order.priority.toLowerCase() != 'routine')
                      Container(
                        margin: const EdgeInsets.only(right: 6),
                        padding: const EdgeInsets.symmetric(
                            horizontal: 6, vertical: 2),
                        decoration: BoxDecoration(
                          color: Colors.red.shade50,
                          borderRadius: BorderRadius.circular(4),
                          border: Border.all(color: Colors.red.shade200),
                        ),
                        child: Text(
                          order.priority.toUpperCase(),
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                            color: Colors.red.shade700,
                          ),
                        ),
                      ),
                    EmrStatusBadge(status: order.status),
                  ],
                ),
              ],
            ),
            const Divider(height: 18),

            // Test Name and Category
            Text(
              order.testName,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: AppTheme.secondaryColor,
              ),
            ),
            const SizedBox(height: 4),
            Row(
              children: [
                if (order.category.isNotEmpty) ...[
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: Colors.teal.shade50,
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      order.category,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                        color: Colors.teal.shade800,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                ],
                Text(
                  'Ordered: $formattedOrderedDate',
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey.shade600,
                  ),
                ),
              ],
            ),

            if (order.doctorName.isNotEmpty) ...[
              const SizedBox(height: 8),
              Row(
                children: [
                  Icon(Icons.person_outline, size: 14, color: Colors.grey.shade600),
                  const SizedBox(width: 4),
                  Text(
                    'Ordered by: ${order.doctorName}',
                    style: TextStyle(fontSize: 12, color: Colors.grey.shade700),
                  ),
                ],
              ),
            ],

            const SizedBox(height: 14),

            // Report Details (if completed) or Pending Notice
            if (hasReport)
              _buildReportSection(order.report!)
            else
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.amber.shade50,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: Colors.amber.shade200),
                ),
                child: Row(
                  children: [
                    Icon(Icons.hourglass_top,
                        size: 18, color: Colors.amber.shade800),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Test specimen collected or processing. Official report will be available once finalized by the lab pathologist.',
                        style: TextStyle(
                          fontSize: 12,
                          color: Colors.amber.shade900,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildReportSection(LabReportModel report) {
    final dateFormat = DateFormat('MMM dd, yyyy • hh:mm a');
    final formattedDate = dateFormat.format(report.reportDate);

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.teal.shade50.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: Colors.teal.shade200),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Icon(Icons.verified, size: 16, color: Colors.teal.shade800),
                  const SizedBox(width: 6),
                  Text(
                    'Official Lab Result',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: Colors.teal.shade900,
                    ),
                  ),
                ],
              ),
              Text(
                formattedDate,
                style: TextStyle(
                  fontSize: 11,
                  color: Colors.teal.shade800,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),

          // Result Summary
          if (report.resultSummary.isNotEmpty) ...[
            Text(
              'Result Summary: ${report.resultSummary}',
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.bold,
                color: Colors.teal.shade900,
              ),
            ),
            const SizedBox(height: 8),
          ],

          // Findings
          if (report.findings.isNotEmpty) ...[
            Text(
              'Findings:',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: Colors.grey.shade800,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              report.findings,
              style: TextStyle(
                fontSize: 13,
                color: Colors.grey.shade900,
              ),
            ),
            const SizedBox(height: 8),
          ],

          // Reference Range
          if (report.referenceRange.isNotEmpty) ...[
            Text(
              'Reference Range:',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: Colors.grey.shade800,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              report.referenceRange,
              style: TextStyle(
                fontSize: 12,
                color: Colors.grey.shade700,
                fontFamily: 'monospace',
              ),
            ),
            const SizedBox(height: 8),
          ],

          // Doctor remarks
          if (report.doctorRemarks.isNotEmpty) ...[
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(6),
                border: Border.all(color: Colors.teal.shade100),
              ),
              child: Text(
                'Doctor Remarks: ${report.doctorRemarks}',
                style: TextStyle(
                  fontSize: 12,
                  fontStyle: FontStyle.italic,
                  color: Colors.grey.shade800,
                ),
              ),
            ),
            const SizedBox(height: 8),
          ],

          if (report.conductedByUserName.isNotEmpty)
            Row(
              children: [
                Icon(Icons.badge_outlined,
                    size: 13, color: Colors.grey.shade600),
                const SizedBox(width: 4),
                Text(
                  'Conducted by: ${report.conductedByUserName}',
                  style: TextStyle(
                    fontSize: 11,
                    color: Colors.grey.shade700,
                  ),
                ),
              ],
            ),
        ],
      ),
    );
  }
}
