import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';
import '../../models/patient_admission_model.dart';
import '../../services/admission_service.dart';

class MyAdmissionScreen extends StatefulWidget {
  const MyAdmissionScreen({super.key});

  @override
  State<MyAdmissionScreen> createState() => _MyAdmissionScreenState();
}

class _MyAdmissionScreenState extends State<MyAdmissionScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;
  late final AdmissionService _admissionService;

  // Active Stay state
  PatientAdmissionModel? _activeAdmission;
  bool _loadingActive = true;
  String? _errorActive;

  // History state
  List<PatientAdmissionModel> _history = [];
  bool _loadingHistory = true;
  String? _errorHistory;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _admissionService = AdmissionService(context.read<ApiClient>());
    _loadActiveAdmission();
    _loadHistory();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadActiveAdmission() async {
    setState(() {
      _loadingActive = true;
      _errorActive = null;
    });

    try {
      final admission = await _admissionService.getMyActiveAdmission();
      if (mounted) {
        setState(() {
          _activeAdmission = (admission != null && admission.status.toLowerCase() == 'admitted')
              ? admission
              : null;
          _loadingActive = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _errorActive = 'Unable to load admission details.';
          _loadingActive = false;
        });
      }
    }
  }

  Future<void> _loadHistory() async {
    setState(() {
      _loadingHistory = true;
      _errorHistory = null;
    });

    try {
      final history = await _admissionService.getMyAdmissionHistory();
      if (mounted) {
        setState(() {
          _history = history.where((a) => a.status.toLowerCase() == 'discharged').toList();
          _loadingHistory = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _errorHistory = 'Unable to load admission details.';
          _loadingHistory = false;
        });
      }
    }
  }

  Future<void> _refreshAll() async {
    await Future.wait([
      _loadActiveAdmission(),
      _loadHistory(),
    ]);
  }

  String _formatDateTime(DateTime? dt) {
    if (dt == null) return '—';
    return DateFormat('MMM d, yyyy h:mm a').format(dt.toLocal());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Admission'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: _refreshAll,
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: Colors.white,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white70,
          labelStyle: const TextStyle(fontWeight: FontWeight.bold),
          tabs: const [
            Tab(
              icon: Icon(Icons.hotel_rounded, size: 20),
              text: 'Active Stay',
            ),
            Tab(
              icon: Icon(Icons.history_rounded, size: 20),
              text: 'History',
            ),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _buildActiveStayTab(),
          _buildHistoryTab(),
        ],
      ),
    );
  }

  // --- Active Stay Tab ---
  Widget _buildActiveStayTab() {
    if (_loadingActive) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorActive != null) {
      return RefreshIndicator(
        onRefresh: _loadActiveAdmission,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(24.0),
          child: SizedBox(
            height: MediaQuery.of(context).size.height * 0.6,
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error_outline, size: 48, color: AppTheme.errorColor),
                  const SizedBox(height: 12),
                  Text(
                    _errorActive!,
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontSize: 16, color: AppTheme.errorColor),
                  ),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: _loadActiveAdmission,
                    child: const Text('Retry'),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }

    if (_activeAdmission == null) {
      return RefreshIndicator(
        onRefresh: _loadActiveAdmission,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(24.0),
          child: SizedBox(
            height: MediaQuery.of(context).size.height * 0.6,
            child: const Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.bed_outlined, size: 64, color: Colors.grey),
                  SizedBox(height: 16),
                  Text(
                    'No active hospital admission.',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 16,
                      color: Colors.grey,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }

    final admission = _activeAdmission!;

    return RefreshIndicator(
      onRefresh: _loadActiveAdmission,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 14.0),
        child: Card(
          elevation: 2,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          child: Padding(
            padding: const EdgeInsets.all(18.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Header: Admission Number & Status badge
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Admission Number',
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.grey.shade600,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            admission.admissionNumber,
                            style: const TextStyle(
                              fontSize: 17,
                              fontWeight: FontWeight.bold,
                              fontFamily: 'monospace',
                              color: AppTheme.primaryColor,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    _buildStatusBadge(admission.status),
                  ],
                ),
                const SizedBox(height: 10),

                // Priority Badge
                Row(
                  children: [
                    Text(
                      'Priority: ',
                      style: TextStyle(fontSize: 13, color: Colors.grey.shade600),
                    ),
                    _buildPriorityBadge(admission.priority),
                  ],
                ),
                const Divider(height: 20),

                // Admitted Date & Time
                _buildField(
                  label: 'Admitted',
                  value: _formatDateTime(admission.admissionDate),
                  labelSize: 12,
                  valueSize: 14,
                ),
                const SizedBox(height: 14),

                // Ward & Bed Location
                _buildSectionTitle('Ward & Bed Location'),
                const SizedBox(height: 8),
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                  decoration: BoxDecoration(
                    color: Colors.teal.shade50.withValues(alpha: 0.5),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: Colors.teal.shade100),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _buildDetailRow(
                        label: 'Ward',
                        value: admission.wardName != null
                            ? '${admission.wardName}${admission.wardFloor != null ? " (${admission.wardFloor})" : ""}'
                            : 'Not assigned',
                      ),
                      const SizedBox(height: 5),
                      _buildDetailRow(
                        label: 'Room',
                        value: admission.roomNumber ?? '—',
                      ),
                      const SizedBox(height: 5),
                      _buildDetailRow(
                        label: 'Bed',
                        value: admission.bedNumber ?? 'Not assigned',
                        isHighlighted: admission.bedNumber != null,
                      ),
                      if (admission.allocatedAt != null) ...[
                        const SizedBox(height: 5),
                        _buildDetailRow(
                          label: 'Allocated At',
                          value: _formatDateTime(admission.allocatedAt),
                        ),
                      ],
                    ],
                  ),
                ),

                // Admitting Doctor (only if available)
                if (admission.doctorName != null && admission.doctorName!.isNotEmpty) ...[
                  const SizedBox(height: 14),
                  _buildField(
                    label: 'Doctor',
                    value: admission.doctorName!,
                    labelSize: 12,
                    valueSize: 14,
                  ),
                ],

                // Reason for Admission
                const SizedBox(height: 14),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Reason for Admission',
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.grey.shade600,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      admission.reasonForAdmission.isNotEmpty
                          ? admission.reasonForAdmission
                          : '—',
                      style: const TextStyle(
                        fontSize: 14,
                        color: Color(0xFF1E293B),
                        height: 1.35,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  // --- Admission History Tab ---
  Widget _buildHistoryTab() {
    if (_loadingHistory) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorHistory != null) {
      return RefreshIndicator(
        onRefresh: _loadHistory,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(24.0),
          child: SizedBox(
            height: MediaQuery.of(context).size.height * 0.6,
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error_outline, size: 48, color: AppTheme.errorColor),
                  const SizedBox(height: 12),
                  Text(
                    _errorHistory!,
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontSize: 16, color: AppTheme.errorColor),
                  ),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: _loadHistory,
                    child: const Text('Retry'),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }

    if (_history.isEmpty) {
      return RefreshIndicator(
        onRefresh: _loadHistory,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(24.0),
          child: SizedBox(
            height: MediaQuery.of(context).size.height * 0.6,
            child: const Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.history_rounded, size: 64, color: Colors.grey),
                  SizedBox(height: 16),
                  Text(
                    'No previous admissions found.',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 16,
                      color: Colors.grey,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadHistory,
      child: ListView.separated(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16.0),
        itemCount: _history.length,
        separatorBuilder: (_, _) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final item = _history[index];
          return Card(
            elevation: 1.5,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: Padding(
              padding: const EdgeInsets.all(15.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Header: Admission Number & Status
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      Expanded(
                        child: Text(
                          item.admissionNumber,
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 15,
                            fontFamily: 'monospace',
                            color: AppTheme.primaryColor,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      _buildStatusBadge(item.status),
                    ],
                  ),
                  const SizedBox(height: 6),

                  // Priority
                  Row(
                    children: [
                      Text(
                        'Priority: ',
                        style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                      ),
                      _buildPriorityBadge(item.priority),
                    ],
                  ),
                  const Divider(height: 18),

                  // Admitted
                  _buildField(
                    label: 'Admitted',
                    value: _formatDateTime(item.admissionDate),
                    labelSize: 11,
                    valueSize: 13,
                  ),
                  const SizedBox(height: 8),

                  // Discharged
                  _buildField(
                    label: 'Discharged',
                    value: _formatDateTime(item.dischargeDate),
                    labelSize: 11,
                    valueSize: 13,
                  ),

                  // Ward & Bed Location
                  if (item.wardName != null || item.roomNumber != null || item.bedNumber != null) ...[
                    const SizedBox(height: 10),
                    _buildSectionTitle('Ward & Bed', fontSize: 12),
                    const SizedBox(height: 5),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
                      decoration: BoxDecoration(
                        color: Colors.teal.shade50.withValues(alpha: 0.4),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.teal.shade100),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (item.wardName != null) ...[
                            _buildDetailRow(
                              label: 'Ward',
                              value: '${item.wardName}${item.wardFloor != null ? " (${item.wardFloor})" : ""}',
                              fontSize: 12,
                            ),
                          ],
                          if (item.roomNumber != null) ...[
                            const SizedBox(height: 3),
                            _buildDetailRow(
                              label: 'Room',
                              value: item.roomNumber!,
                              fontSize: 12,
                            ),
                          ],
                          if (item.bedNumber != null) ...[
                            const SizedBox(height: 3),
                            _buildDetailRow(
                              label: 'Bed',
                              value: item.bedNumber!,
                              isHighlighted: true,
                              fontSize: 12,
                            ),
                          ],
                        ],
                      ),
                    ),
                  ],

                  // Allocated At
                  if (item.allocatedAt != null) ...[
                    const SizedBox(height: 8),
                    _buildField(
                      label: 'Allocated At',
                      value: _formatDateTime(item.allocatedAt),
                      labelSize: 11,
                      valueSize: 13,
                    ),
                  ],

                  // Doctor
                  if (item.doctorName != null && item.doctorName!.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    _buildField(
                      label: 'Doctor',
                      value: item.doctorName!,
                      labelSize: 11,
                      valueSize: 13,
                    ),
                  ],

                  // Reason
                  if (item.reasonForAdmission.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Reason',
                          style: TextStyle(
                            fontSize: 11,
                            color: Colors.grey.shade600,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          item.reasonForAdmission,
                          style: const TextStyle(
                            fontSize: 13,
                            color: Color(0xFF1E293B),
                            height: 1.3,
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  // --- Helper Widgets ---

  Widget _buildSectionTitle(String title, {double fontSize = 13}) {
    return Text(
      title,
      style: TextStyle(
        fontSize: fontSize,
        fontWeight: FontWeight.bold,
        color: AppTheme.primaryColor,
      ),
    );
  }

  Widget _buildField({
    required String label,
    required String value,
    double labelSize = 11,
    double valueSize = 13,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: labelSize,
            color: Colors.grey.shade600,
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: TextStyle(
            fontSize: valueSize,
            fontWeight: FontWeight.w600,
            color: const Color(0xFF1E293B),
          ),
        ),
      ],
    );
  }

  Widget _buildDetailRow({
    required String label,
    required String value,
    bool isHighlighted = false,
    double fontSize = 13,
  }) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          '$label: ',
          style: TextStyle(
            fontSize: fontSize,
            color: Colors.grey.shade600,
            fontWeight: FontWeight.normal,
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: fontSize,
              fontWeight: isHighlighted ? FontWeight.bold : FontWeight.w600,
              color: isHighlighted ? Colors.teal.shade800 : const Color(0xFF1E293B),
            ),
          ),
        ),
      ],
    );
  }


  Widget _buildStatusBadge(String status) {
    final s = status.toLowerCase();
    Color bg;
    Color fg;

    if (s == 'admitted') {
      bg = Colors.green.shade50;
      fg = Colors.green.shade800;
    } else if (s == 'discharged') {
      bg = Colors.grey.shade100;
      fg = Colors.grey.shade700;
    } else {
      bg = Colors.red.shade50;
      fg = Colors.red.shade700;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: fg.withValues(alpha: 0.3)),
      ),
      child: Text(
        status.isNotEmpty ? status : 'Unknown',
        style: TextStyle(
          color: fg,
          fontSize: 12,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }

  Widget _buildPriorityBadge(String priority) {
    final p = priority.toLowerCase();
    Color bg;
    Color fg;

    if (p == 'emergency') {
      bg = Colors.red.shade50;
      fg = Colors.red.shade800;
    } else if (p == 'urgent') {
      bg = Colors.amber.shade50;
      fg = Colors.amber.shade900;
    } else {
      bg = Colors.blue.shade50;
      fg = Colors.blue.shade800;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: fg.withValues(alpha: 0.2)),
      ),
      child: Text(
        priority.isNotEmpty ? priority : 'Normal',
        style: TextStyle(
          color: fg,
          fontSize: 11,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
