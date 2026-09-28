import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../widgets/app_ui.dart';

import '../../core/network/api_client.dart';
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
          _activeAdmission =
              (admission != null &&
                  admission.status.toLowerCase() == 'admitted')
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
          _history = history
              .where((a) => a.status.toLowerCase() == 'discharged')
              .toList();
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
    await Future.wait([_loadActiveAdmission(), _loadHistory()]);
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
          indicatorColor: AppTheme.surfaceColor,
          labelColor: AppTheme.surfaceColor,
          unselectedLabelColor: AppTheme.onPrimary,
          labelStyle: TextStyle(fontWeight: FontWeight.bold),
          tabs: const [
            Tab(icon: Icon(Icons.hotel_rounded, size: 20), text: 'Active Stay'),
            Tab(icon: Icon(Icons.history_rounded, size: 20), text: 'History'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [_buildActiveStayTab(), _buildHistoryTab()],
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
                  const Icon(
                    Icons.error_outline,
                    size: 48,
                    color: AppTheme.errorColor,
                  ),
                  const SizedBox(height: 12),
                  Text(
                    _errorActive!,
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: AppTheme.fontTitleMedium,
                      color: AppTheme.errorColor,
                    ),
                  ),
                  const SizedBox(height: 16),
                  FilledButton(
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
                  Icon(
                    Icons.bed_outlined,
                    size: 64,
                    color: AppTheme.textSecondary,
                  ),
                  SizedBox(height: 16),
                  Text(
                    'No active hospital admission.',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: AppTheme.fontTitleMedium,
                      color: AppTheme.textSecondary,
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
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
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
                              fontSize: AppTheme.fontBodySmall,
                              color: AppTheme.textSecondary,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            admission.admissionNumber,
                            style: TextStyle(
                              fontSize: AppTheme.fontHeadlineSmall,
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
                      style: TextStyle(
                        fontSize: AppTheme.fontBodyMedium,
                        color: AppTheme.textSecondary,
                      ),
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
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 10,
                  ),
                  decoration: BoxDecoration(
                    color: AppTheme.accentContainer.withValues(alpha: 0.5),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: AppTheme.accentContainer),
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
                if (admission.doctorName != null &&
                    admission.doctorName!.isNotEmpty) ...[
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
                        fontSize: AppTheme.fontBodySmall,
                        color: AppTheme.textSecondary,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      admission.reasonForAdmission.isNotEmpty
                          ? admission.reasonForAdmission
                          : '—',
                      style: TextStyle(
                        fontSize: AppTheme.fontBodyMedium,
                        color: AppTheme.textPrimary,
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
                  const Icon(
                    Icons.error_outline,
                    size: 48,
                    color: AppTheme.errorColor,
                  ),
                  const SizedBox(height: 12),
                  Text(
                    _errorHistory!,
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: AppTheme.fontTitleMedium,
                      color: AppTheme.errorColor,
                    ),
                  ),
                  const SizedBox(height: 16),
                  FilledButton(
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
                  Icon(
                    Icons.history_rounded,
                    size: 64,
                    color: AppTheme.textSecondary,
                  ),
                  SizedBox(height: 16),
                  Text(
                    'No previous admissions found.',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: AppTheme.fontTitleMedium,
                      color: AppTheme.textSecondary,
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
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
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
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: AppTheme.fontTitleMedium,
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
                        style: TextStyle(
                          fontSize: AppTheme.fontBodySmall,
                          color: AppTheme.textSecondary,
                        ),
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
                  if (item.wardName != null ||
                      item.roomNumber != null ||
                      item.bedNumber != null) ...[
                    const SizedBox(height: 10),
                    _buildSectionTitle(
                      'Ward & Bed',
                      fontSize: AppTheme.fontBodySmall,
                    ),
                    const SizedBox(height: 5),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 7,
                      ),
                      decoration: BoxDecoration(
                        color: AppTheme.accentContainer.withValues(alpha: 0.4),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: AppTheme.accentContainer),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (item.wardName != null) ...[
                            _buildDetailRow(
                              label: 'Ward',
                              value:
                                  '${item.wardName}${item.wardFloor != null ? " (${item.wardFloor})" : ""}',
                              fontSize: AppTheme.fontBodySmall,
                            ),
                          ],
                          if (item.roomNumber != null) ...[
                            const SizedBox(height: 3),
                            _buildDetailRow(
                              label: 'Room',
                              value: item.roomNumber!,
                              fontSize: AppTheme.fontBodySmall,
                            ),
                          ],
                          if (item.bedNumber != null) ...[
                            const SizedBox(height: 3),
                            _buildDetailRow(
                              label: 'Bed',
                              value: item.bedNumber!,
                              isHighlighted: true,
                              fontSize: AppTheme.fontBodySmall,
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
                  if (item.doctorName != null &&
                      item.doctorName!.isNotEmpty) ...[
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
                            fontSize: AppTheme.fontBodySmall,
                            color: AppTheme.textSecondary,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          item.reasonForAdmission,
                          style: TextStyle(
                            fontSize: AppTheme.fontBodyMedium,
                            color: AppTheme.textPrimary,
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

  Widget _buildSectionTitle(
    String title, {
    double fontSize = AppTheme.fontTitleSmall,
  }) {
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
    double labelSize = AppTheme.fontLabelSmall,
    double valueSize = AppTheme.fontTitleSmall,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: labelSize,
            color: AppTheme.textSecondary,
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: TextStyle(
            fontSize: valueSize,
            fontWeight: FontWeight.w600,
            color: AppTheme.textPrimary,
          ),
        ),
      ],
    );
  }

  Widget _buildDetailRow({
    required String label,
    required String value,
    bool isHighlighted = false,
    double fontSize = AppTheme.fontTitleSmall,
  }) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          '$label: ',
          style: TextStyle(
            fontSize: fontSize,
            color: AppTheme.textSecondary,
            fontWeight: FontWeight.normal,
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: fontSize,
              fontWeight: isHighlighted ? FontWeight.bold : FontWeight.w600,
              color: isHighlighted
                  ? AppTheme.secondaryColor
                  : AppTheme.textPrimary,
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
      bg = AppTheme.successContainer;
      fg = AppTheme.successColor;
    } else if (s == 'discharged') {
      bg = AppTheme.neutralContainer;
      fg = AppTheme.textSecondary;
    } else {
      bg = AppTheme.errorContainer;
      fg = AppTheme.errorColor;
    }

    return StatusBadge(
      label: status.isNotEmpty ? status : 'Unknown',
      color: fg,
      backgroundColor: bg,
    );
  }

  Widget _buildPriorityBadge(String priority) {
    return PriorityBadge(label: priority.isNotEmpty ? priority : 'Normal');
  }
}
