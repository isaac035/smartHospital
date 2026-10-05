import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_client.dart';
import '../../core/network/api_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../models/emr/ai_medical_report_model.dart';
import '../../providers/auth_provider.dart';
import '../../services/emr_service.dart';

class AiMedicalReportsScreen extends StatefulWidget {
  const AiMedicalReportsScreen({super.key});

  @override
  State<AiMedicalReportsScreen> createState() => _AiMedicalReportsScreenState();
}

class _AiMedicalReportsScreenState extends State<AiMedicalReportsScreen> {
  late final EmrService _service;
  List<AiMedicalReportModel> _reports = [];
  bool _loading = true;
  bool _generating = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _service = EmrService(context.read<ApiClient>());
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    final patientId = context.read<AuthProvider>().currentUser?.id;
    if (patientId == null) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final reports = await _service.getAiMedicalReports(patientId);
      if (mounted) setState(() => _reports = reports);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _generate() async {
    setState(() {
      _generating = true;
      _error = null;
    });
    try {
      final patientId = context.read<AuthProvider>().currentUser?.id;
      if (patientId == null) throw ApiException('Your session has expired. Please log in again.', 401);
      final report = await _service.generateAiMedicalReport(patientId);
      if (!mounted) return;
      await _load();
      if (mounted) {
        context.push('/medical-records/ai-reports/${report.reportId}');
      }
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } catch (_) {
      if (mounted) setState(() => _error = 'The report could not be generated right now. Please try again.');
    } finally {
      if (mounted) setState(() => _generating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(title: const Text('AI Medical Reports')),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              'Reports are generated from information recorded in your medical record. Each generated report remains available in this list.',
              style: TextStyle(color: Colors.grey.shade700, height: 1.4),
            ),
            const SizedBox(height: 14),
            FilledButton.icon(
              onPressed: _generating ? null : _generate,
              icon: _generating
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.auto_awesome_outlined),
              label: Text(
                _generating ? 'Generating report…' : 'Generate latest report',
              ),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Card(
                color: Colors.red.shade50,
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Text(
                    _error!,
                    style: TextStyle(color: Colors.red.shade800),
                  ),
                ),
              ),
            ],
            const SizedBox(height: 12),
            if (_loading)
              const Padding(
                padding: EdgeInsets.all(32),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (_reports.isEmpty)
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    children: [
                      Icon(
                        Icons.description_outlined,
                        size: 36,
                        color: Colors.grey.shade500,
                      ),
                      const SizedBox(height: 8),
                      const Text('No reports generated yet.'),
                      const SizedBox(height: 4),
                      Text(
                        'Generate a report to organize your recorded medical information.',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: Colors.grey.shade600),
                      ),
                    ],
                  ),
                ),
              )
            else
              ..._reports.map(_reportCard),
          ],
        ),
      ),
    );
  }

  Widget _reportCard(AiMedicalReportModel report) {
    final priority = report.priority ?? '—';
    final priorityColor = priority.toLowerCase() == 'emergency'
        ? Colors.red
        : priority.toLowerCase() == 'urgent'
        ? Colors.orange
        : Colors.teal;
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppTheme.primaryColor.withValues(alpha: .12),
          child: const Icon(
            Icons.description_outlined,
            color: AppTheme.primaryColor,
          ),
        ),
        title: Text(
          DateFormat('MMM d, yyyy · h:mm a').format(report.createdAt.toLocal()),
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
        subtitle: Padding(
          padding: const EdgeInsets.only(top: 6),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '${report.appointmentType ?? 'Medical history'}${report.doctorName == null ? '' : ' · ${report.doctorName}'}',
              ),
              const SizedBox(height: 5),
              DecoratedBox(
                decoration: BoxDecoration(
                  color: priorityColor.withValues(alpha: .12),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 3,
                  ),
                  child: Text(
                    'Priority: $priority',
                    style: TextStyle(
                      fontSize: 11,
                      color: priorityColor,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: () =>
            context.push('/medical-records/ai-reports/${report.reportId}'),
      ),
    );
  }
}

class AiMedicalReportDetailScreen extends StatefulWidget {
  final String reportId;
  const AiMedicalReportDetailScreen({super.key, required this.reportId});

  @override
  State<AiMedicalReportDetailScreen> createState() =>
      _AiMedicalReportDetailScreenState();
}

class _AiMedicalReportDetailScreenState
    extends State<AiMedicalReportDetailScreen> {
  late final EmrService _service;
  AiMedicalReportModel? _report;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _service = EmrService(context.read<ApiClient>());
    _load();
  }

  Future<void> _load() async {
    try {
      final report = await _service.getAiMedicalReport(widget.reportId);
      if (mounted) setState(() => _report = report);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final content = _report?.content ?? {};
    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(title: const Text('Medical Report')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  'Unable to load this report. $_error',
                  textAlign: TextAlign.center,
                ),
              ),
            )
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(
                  'Version ${_report!.versionNumber} · ${DateFormat('MMMM d, yyyy · h:mm a').format(_report!.createdAt.toLocal())}',
                  style: TextStyle(color: Colors.grey.shade700),
                ),
                const SizedBox(height: 12),
                ...content.entries.map((entry) {
                  final isRecommendation = entry.key == 'aiRecommendations';
                  final isSummary = entry.key == 'aiSummary';
                  final title = entry.key == 'recordedData'
                      ? 'Recorded Data'
                      : isSummary
                      ? 'AI Summary'
                      : isRecommendation
                      ? 'AI Recommendations · for clinical review'
                      : _label(entry.key);
                  final color = isRecommendation
                      ? Colors.orange.shade50
                      : isSummary
                      ? Colors.indigo.shade50
                      : Colors.white;
                  return Card(
                    color: color,
                    margin: const EdgeInsets.only(bottom: 12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            title,
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 16,
                            ),
                          ),
                          const SizedBox(height: 10),
                          _ReportValue(value: entry.value),
                        ],
                      ),
                    ),
                  );
                }),
                const SizedBox(height: 8),
                Text(
                  'AI recommendations are suggestions for clinical review and are not a diagnosis or directive.',
                  style: TextStyle(color: Colors.grey.shade700, fontSize: 12),
                ),
                const SizedBox(height: 24),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    onPressed: () => context.go('/home'),
                    icon: const Icon(Icons.home_outlined),
                    label: const Text('Go to Home'),
                  ),
                ),
                const SizedBox(height: 8),
              ],
            ),
    );
  }
}

class _ReportValue extends StatelessWidget {
  final dynamic value;
  const _ReportValue({required this.value});

  @override
  Widget build(BuildContext context) {
    if (value == null || value == '') return const Text('Not recorded');
    if (value is List) {
      if ((value as List).isEmpty) {
        return Text(
          'None recorded',
          style: TextStyle(color: Colors.grey.shade600),
        );
      }
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: (value as List)
            .map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: _ReportValue(value: item),
              ),
            )
            .toList(),
      );
    }
    if (value is Map) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: (value as Map).entries
            .map(
              (entry) => Padding(
                padding: const EdgeInsets.only(bottom: 9),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      _label(entry.key.toString()),
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.grey.shade700,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 3),
                    _ReportValue(value: entry.value),
                  ],
                ),
              ),
            )
            .toList(),
      );
    }
    return Text(value.toString(), style: const TextStyle(height: 1.35));
  }
}

String _label(String key) => key
    .replaceAllMapped(RegExp(r'([A-Z])'), (match) => ' ${match[1]}')
    .replaceFirstMapped(RegExp(r'^.'), (match) => match[0]!.toUpperCase());
