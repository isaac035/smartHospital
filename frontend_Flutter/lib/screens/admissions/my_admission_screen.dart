import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';
import '../../models/patient_admission_model.dart';
import '../../services/admission_service.dart';
import '../../widgets/app_ui.dart';

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
          _loadingActive = false;
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
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(
        title: const Text('My Admission'),
        elevation: 0,
        scrolledUnderElevation: 0,
        backgroundColor: AppTheme.surfaceColor,
        foregroundColor: AppTheme.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            tooltip: 'Refresh',
            onPressed: _refreshAll,
          ),
          const SizedBox(width: 4),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(60),
          child: Container(
            color: AppTheme.surfaceColor,
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
            child: Container(
              height: 44,
              padding: const EdgeInsets.all(4),
              decoration: BoxDecoration(
                color: AppTheme.neutralContainer,
                borderRadius: BorderRadius.circular(AppTheme.radiusMedium),
                border: Border.all(color: AppTheme.borderColor),
              ),
              child: TabBar(
                controller: _tabController,
                indicatorSize: TabBarIndicatorSize.tab,
                indicator: BoxDecoration(
                  color: AppTheme.surfaceColor,
                  borderRadius: BorderRadius.circular(12),
                  boxShadow: [
                    BoxShadow(
                      color: AppTheme.primaryColor.withValues(alpha: 0.08),
                      blurRadius: 4,
                      offset: const Offset(0, 1),
                    ),
                  ],
                ),
                labelColor: AppTheme.primaryColor,
                unselectedLabelColor: AppTheme.textSecondary,
                labelStyle: const TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 13,
                ),
                unselectedLabelStyle: const TextStyle(
                  fontWeight: FontWeight.w500,
                  fontSize: 13,
                ),
                dividerColor: Colors.transparent,
                tabs: const [
                  Tab(
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.hotel_rounded, size: 18),
                        SizedBox(width: 8),
                        Text('Active Stay'),
                      ],
                    ),
                  ),
                  Tab(
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.history_rounded, size: 18),
                        SizedBox(width: 8),
                        Text('History'),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
      body: AppPageFrame(
        child: TabBarView(
          controller: _tabController,
          children: [
            _buildActiveStayTab(),
            _buildHistoryTab(),
          ],
        ),
      ),
    );
  }

  // --- Active Stay Tab ---
  Widget _buildActiveStayTab() {
    if (_loadingActive) {
      return const LoadingView(message: 'Loading admission details…');
    }

    if (_errorActive != null) {
      return RefreshIndicator(
        onRefresh: _loadActiveAdmission,
        color: AppTheme.primaryColor,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 48),
          child: ErrorState(
            message: _errorActive!,
            onRetry: _loadActiveAdmission,
          ),
        ),
      );
    }

    if (_activeAdmission == null) {
      return RefreshIndicator(
        onRefresh: _loadActiveAdmission,
        color: AppTheme.primaryColor,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 48),
          child: const EmptyState(
            icon: Icons.hotel_outlined,
            title: 'No active hospital admission.',
          ),
        ),
      );
    }

    final admission = _activeAdmission!;

    return RefreshIndicator(
      onRefresh: _loadActiveAdmission,
      color: AppTheme.primaryColor,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 16.0),
        child: Container(
          decoration: BoxDecoration(
            color: AppTheme.surfaceColor,
            borderRadius: BorderRadius.circular(AppTheme.radiusMedium),
            border: Border.all(color: AppTheme.borderColor),
            boxShadow: [
              BoxShadow(
                color: AppTheme.primaryColor.withValues(alpha: 0.04),
                blurRadius: 12,
                offset: const Offset(0, 3),
              ),
            ],
          ),
          padding: const EdgeInsets.all(18),
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
                        const Text(
                          'Admission Number',
                          style: TextStyle(
                            fontSize: AppTheme.fontLabelSmall,
                            color: AppTheme.textSecondary,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                        const SizedBox(height: 4),
                        FittedBox(
                          fit: BoxFit.scaleDown,
                          alignment: Alignment.centerLeft,
                          child: Text(
                            admission.admissionNumber,
                            maxLines: 1,
                            style: const TextStyle(
                              fontSize: 17,
                              fontWeight: FontWeight.w700,
                              fontFamily: 'monospace',
                              color: AppTheme.primaryColor,
                              letterSpacing: 0.3,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  _buildStatusBadge(admission.status),
                ],
              ),

              const SizedBox(height: 14),
              const Divider(height: 1),
              const SizedBox(height: 14),

              // Priority
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Priority',
                    style: TextStyle(
                      fontSize: AppTheme.fontLabelSmall,
                      color: AppTheme.textSecondary,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(height: 5),
                  _buildPriorityBadge(admission.priority),
                ],
              ),

              const SizedBox(height: 14),

              // Admitted Date & Time
              _buildField(
                label: 'Admitted',
                value: _formatDateTime(admission.admissionDate),
              ),

              const SizedBox(height: 16),

              // Ward & Bed Location
              _buildLocationSection(
                wardName: admission.wardName,
                wardFloor: admission.wardFloor,
                roomNumber: admission.roomNumber,
                bedNumber: admission.bedNumber,
                allocatedAt: admission.allocatedAt,
              ),

              // Doctor (only if available)
              if (admission.doctorName != null &&
                  admission.doctorName!.trim().isNotEmpty) ...[
                const SizedBox(height: 16),
                _buildSection(
                  title: 'Doctor',
                  content: Container(
                    width: double.infinity,
                    padding: const EdgeInsets.symmetric(
                      horizontal: 14,
                      vertical: 12,
                    ),
                    decoration: BoxDecoration(
                      color: AppTheme.backgroundColor,
                      borderRadius: BorderRadius.circular(AppTheme.radiusSmall),
                      border: Border.all(color: AppTheme.borderColor),
                    ),
                    child: Row(
                      children: [
                        const Icon(
                          Icons.medical_services_outlined,
                          size: 16,
                          color: AppTheme.secondaryColor,
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            admission.doctorName!.trim(),
                            style: const TextStyle(
                              fontSize: AppTheme.fontBodyMedium,
                              fontWeight: FontWeight.w600,
                              color: AppTheme.textPrimary,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],

              // Reason for Admission
              const SizedBox(height: 16),
              _buildSection(
                title: 'Reason for Admission',
                content: Container(
                  width: double.infinity,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 12,
                  ),
                  decoration: BoxDecoration(
                    color: AppTheme.backgroundColor,
                    borderRadius: BorderRadius.circular(AppTheme.radiusSmall),
                    border: Border.all(color: AppTheme.borderColor),
                  ),
                  child: Text(
                    admission.reasonForAdmission.isNotEmpty
                        ? admission.reasonForAdmission
                        : '—',
                    style: TextStyle(
                      fontSize: AppTheme.fontBodyMedium,
                      color: admission.reasonForAdmission.isNotEmpty
                          ? AppTheme.textPrimary
                          : AppTheme.textMuted,
                      height: 1.4,
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // --- Admission History Tab ---
  Widget _buildHistoryTab() {
    if (_loadingHistory) {
      return const LoadingView(message: 'Loading admission history…');
    }

    if (_errorHistory != null) {
      return RefreshIndicator(
        onRefresh: _loadHistory,
        color: AppTheme.primaryColor,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 48),
          child: ErrorState(
            message: _errorHistory!,
            onRetry: _loadHistory,
          ),
        ),
      );
    }

    if (_history.isEmpty) {
      return RefreshIndicator(
        onRefresh: _loadHistory,
        color: AppTheme.primaryColor,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 48),
          child: const EmptyState(
            icon: Icons.history_rounded,
            title: 'No previous admissions found.',
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadHistory,
      color: AppTheme.primaryColor,
      child: ListView.separated(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 14.0),
        itemCount: _history.length,
        separatorBuilder: (_, _) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final item = _history[index];
          return Container(
            decoration: BoxDecoration(
              color: AppTheme.surfaceColor,
              borderRadius: BorderRadius.circular(AppTheme.radiusMedium),
              border: Border.all(color: AppTheme.borderColor),
              boxShadow: [
                BoxShadow(
                  color: AppTheme.primaryColor.withValues(alpha: 0.03),
                  blurRadius: 10,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Header: Admission Number & Status Badge
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Admission Number',
                            style: TextStyle(
                              fontSize: AppTheme.fontLabelSmall,
                              color: AppTheme.textSecondary,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          const SizedBox(height: 3),
                          FittedBox(
                            fit: BoxFit.scaleDown,
                            alignment: Alignment.centerLeft,
                            child: Text(
                              item.admissionNumber,
                              maxLines: 1,
                              style: const TextStyle(
                                fontSize: 17,
                                fontWeight: FontWeight.w700,
                                fontFamily: 'monospace',
                                color: AppTheme.primaryColor,
                                letterSpacing: 0.3,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 12),
                    _buildStatusBadge(item.status),
                  ],
                ),

                const SizedBox(height: 10),
                const Divider(height: 1),
                const SizedBox(height: 10),

                // Priority
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Priority',
                      style: TextStyle(
                        fontSize: AppTheme.fontLabelSmall,
                        color: AppTheme.textSecondary,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 4),
                    _buildPriorityBadge(item.priority),
                  ],
                ),

                const SizedBox(height: 10),

                // Admitted
                _buildField(
                  label: 'Admitted',
                  value: _formatDateTime(item.admissionDate),
                ),

                const SizedBox(height: 6),

                // Discharged
                _buildField(
                  label: 'Discharged',
                  value: _formatDateTime(item.dischargeDate),
                ),

                // Ward & Bed Location (if any location info exists)
                if (item.wardName != null ||
                    item.roomNumber != null ||
                    item.bedNumber != null ||
                    item.allocatedAt != null) ...[
                  const SizedBox(height: 10),
                  _buildLocationSection(
                    wardName: item.wardName,
                    wardFloor: item.wardFloor,
                    roomNumber: item.roomNumber,
                    bedNumber: item.bedNumber,
                    allocatedAt: item.allocatedAt,
                  ),
                ],

                // Doctor (if available)
                if (item.doctorName != null &&
                    item.doctorName!.trim().isNotEmpty) ...[
                  const SizedBox(height: 10),
                  _buildSection(
                    title: 'Doctor',
                    content: Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 14,
                        vertical: 12,
                      ),
                      decoration: BoxDecoration(
                        color: AppTheme.backgroundColor,
                        borderRadius:
                            BorderRadius.circular(AppTheme.radiusSmall),
                        border: Border.all(color: AppTheme.borderColor),
                      ),
                      child: Row(
                        children: [
                          const Icon(
                            Icons.medical_services_outlined,
                            size: 16,
                            color: AppTheme.secondaryColor,
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              item.doctorName!.trim(),
                              style: const TextStyle(
                                fontSize: AppTheme.fontBodyMedium,
                                fontWeight: FontWeight.w600,
                                color: AppTheme.textPrimary,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],

                // Reason for Admission
                if (item.reasonForAdmission.isNotEmpty) ...[
                  const SizedBox(height: 10),
                  _buildSection(
                    title: 'Reason for Admission',
                    content: Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 14,
                        vertical: 12,
                      ),
                      decoration: BoxDecoration(
                        color: AppTheme.backgroundColor,
                        borderRadius:
                            BorderRadius.circular(AppTheme.radiusSmall),
                        border: Border.all(color: AppTheme.borderColor),
                      ),
                      child: Text(
                        item.reasonForAdmission,
                        style: const TextStyle(
                          fontSize: AppTheme.fontBodyMedium,
                          color: AppTheme.textPrimary,
                          height: 1.4,
                        ),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          );
        },
      ),
    );
  }

  // --- Helper Widgets ---

  Widget _buildField({
    required String label,
    required String value,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: AppTheme.fontLabelSmall,
            color: AppTheme.textSecondary,
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(height: 3),
        Text(
          value,
          style: const TextStyle(
            fontSize: AppTheme.fontBodyMedium,
            fontWeight: FontWeight.w600,
            color: AppTheme.textPrimary,
          ),
        ),
      ],
    );
  }

  Widget _buildSection({
    required String title,
    required Widget content,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: const TextStyle(
            fontSize: AppTheme.fontTitleSmall,
            fontWeight: FontWeight.w700,
            color: AppTheme.textPrimary,
          ),
        ),
        const SizedBox(height: 6),
        content,
      ],
    );
  }

  Widget _buildLocationSection({
    required String? wardName,
    required String? wardFloor,
    required String? roomNumber,
    required String? bedNumber,
    required DateTime? allocatedAt,
  }) {
    final rows = <Widget>[];

    // Ward row
    rows.add(
      _buildDetailRow(
        icon: Icons.apartment_rounded,
        label: 'Ward',
        value: wardName != null
            ? '$wardName${wardFloor != null ? " ($wardFloor)" : ""}'
            : 'Not assigned',
      ),
    );

    // Room row
    rows.add(
      const Padding(
        padding: EdgeInsets.symmetric(vertical: 8),
        child: Divider(height: 1),
      ),
    );
    rows.add(
      _buildDetailRow(
        icon: Icons.meeting_room_outlined,
        label: 'Room',
        value: roomNumber ?? '—',
      ),
    );

    // Bed row
    rows.add(
      const Padding(
        padding: EdgeInsets.symmetric(vertical: 8),
        child: Divider(height: 1),
      ),
    );
    rows.add(
      _buildDetailRow(
        icon: Icons.single_bed_rounded,
        label: 'Bed',
        value: bedNumber ?? 'Not assigned',
        isHighlighted: bedNumber != null,
      ),
    );

    // Allocated At row (if available)
    if (allocatedAt != null) {
      rows.add(
        const Padding(
          padding: EdgeInsets.symmetric(vertical: 8),
          child: Divider(height: 1),
        ),
      );
      rows.add(
        _buildDetailRow(
          icon: Icons.access_time_rounded,
          label: 'Allocated At',
          value: _formatDateTime(allocatedAt),
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Ward & Bed Location',
          style: TextStyle(
            fontSize: AppTheme.fontTitleSmall,
            fontWeight: FontWeight.w700,
            color: AppTheme.textPrimary,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          decoration: BoxDecoration(
            color: AppTheme.neutralContainer.withValues(alpha: 0.5),
            borderRadius: BorderRadius.circular(AppTheme.radiusSmall),
            border: Border.all(color: AppTheme.borderColor),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: rows,
          ),
        ),
      ],
    );
  }

  Widget _buildDetailRow({
    required IconData icon,
    required String label,
    required String value,
    bool isHighlighted = false,
  }) {
    const labelColor = Color(0xFF405664);
    const iconColor = Color(0xFF405664);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(top: 2),
          child: Icon(icon, size: 16, color: iconColor),
        ),
        const SizedBox(width: 8),
        Text(
          '$label: ',
          style: const TextStyle(
            fontSize: AppTheme.fontBodySmall,
            color: labelColor,
            fontWeight: FontWeight.w500,
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: AppTheme.fontBodySmall,
              fontWeight: isHighlighted ? FontWeight.w700 : FontWeight.w600,
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
