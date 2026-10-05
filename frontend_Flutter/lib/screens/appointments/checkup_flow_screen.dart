import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/network/api_client.dart';
import '../../core/network/api_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../models/appointments/appointment_model.dart';
import '../../models/appointments/resource_allocation_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/appointment_provider.dart';
import '../../services/emr_service.dart';

class CheckupFlowScreen extends StatefulWidget {
  final int appointmentId;
  const CheckupFlowScreen({super.key, required this.appointmentId});
  @override
  State<CheckupFlowScreen> createState() => _CheckupFlowScreenState();
}

class _CheckupFlowScreenState extends State<CheckupFlowScreen> {
  AppointmentModel? _appointment;
  ResourceRecommendationModel? _result;
  String? _error;
  bool _loading = false;
  bool _preparingReport = false;
  int? _allocatingId;
  DateTime? _checkupDate;
  bool _checkupRequested = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      try {
        final provider = context.read<AppointmentProvider>();
        await provider.loadAppointmentDetail(widget.appointmentId);
        if (mounted) setState(() => _appointment = provider.selectedAppointment);
      } catch (_) { /* The appointment detail route remains available. */ }
    });
  }

  Future<void> _checkResources() async {
    setState(() { _loading = true; _error = null; });
    try {
      final result = await context.read<AppointmentProvider>().recommendResources(widget.appointmentId);
      if (!mounted) return;
      setState(() { _result = result; _loading = false; });
      // No recommendations is a final Agent 3 outcome. Continue directly to the
      // report so the patient does not have to discover it later in Records.
      if (result.recommendations.isEmpty) await _prepareReport();
    } catch (_) {
      if (mounted) setState(() { _loading = false; _error = 'Resource suggestions are unavailable right now. Your appointment is still confirmed.'; });
    }
  }

  Future<void> _chooseCheckupDate() async {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final selected = await showDatePicker(
      context: context,
      initialDate: _checkupDate ?? today,
      firstDate: today,
      lastDate: DateTime(today.year + 2, today.month, today.day),
      helpText: 'Choose a checkup date',
    );
    if (selected == null || !mounted) return;
    // Keep the calendar's local year/month/day; the API transports this as a
    // date-only string so UTC conversion cannot move it to another day.
    setState(() {
      _checkupDate = DateTime(selected.year, selected.month, selected.day);
      _checkupRequested = true;
    });
    await _checkResources();
  }

  Future<void> _select(ResourceCandidateModel resource) async {
    setState(() { _allocatingId = resource.resourceId; _error = null; });
    try {
      final checkupDate = _checkupDate;
      if (checkupDate == null) throw StateError('Choose a checkup date first.');
      await context.read<AppointmentProvider>().allocateResource(widget.appointmentId, resource, checkupDate);
      if (!mounted) return;
      try {
        await context.read<AppointmentProvider>().loadAppointmentDetail(widget.appointmentId);
      } catch (_) {
        // Allocation already succeeded; a detail refresh failure must not skip Agent 4.
      }
      if (mounted) await _prepareReport();
    } on ApiException catch (e) {
      if (!mounted) return;
      if (e.refreshRecommendations) {
        await _checkResources();
      }
      if (!mounted) return;
      if (e.code == 'RESOURCE_UNAVAILABLE') {
        setState(() => _error = 'That resource was just taken. The available list has been refreshed.');
      } else {
        setState(() => _error = e.message.isNotEmpty ? e.message : 'Could not reserve the selected resource. Your appointment is still confirmed.');
      }
    } catch (_) {
      if (mounted) setState(() => _error = 'Could not reserve the selected resource. Your appointment is still confirmed.');
    } finally {
      if (mounted) setState(() => _allocatingId = null);
    }
  }

  Future<void> _finish() => _prepareReport();

  Future<void> _prepareReport() async {
    if (_preparingReport) return;
    setState(() => _preparingReport = true);
    try {
      final patientId = _appointment?.patientId ?? context.read<AuthProvider>().currentUser?.id;
      if (patientId == null) {
        throw ApiException('Your session has expired. Please log in again.', 401);
      }
      final report = await EmrService(context.read<ApiClient>()).generateAiMedicalReport(
        patientId,
        appointmentId: widget.appointmentId,
        checkupRequested: _checkupRequested,
      );
      if (!mounted) return;
      context.go('/medical-records/ai-reports/${report.reportId}');
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Your report will be available shortly in Records. You can generate it from the AI Medical Reports section.'),
          duration: Duration(seconds: 5),
        ),
      );
      context.go('/appointments/${widget.appointmentId}');
    } finally {
      if (mounted) setState(() => _preparingReport = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final appointment = _appointment;
    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(title: const Text('Appointment Confirmed'), backgroundColor: AppTheme.surfaceColor, foregroundColor: AppTheme.textPrimary),
      body: SafeArea(child: ListView(padding: AppTheme.pagePadding, children: [
        Card(child: Padding(padding: const EdgeInsets.all(20), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('Your appointment is booked', style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 8),
          Text(appointment?.doctorName ?? 'Doctor'),
          if (appointment != null) Text('${_formatDate(appointment.scheduledStart)} · ${appointment.departmentName ?? ''}'),
          const SizedBox(height: 20),
          Text('Would you like a medical checkup?', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 12),
          if (_result == null) Row(children: [
            OutlinedButton(onPressed: _loading || _preparingReport ? null : _finish, child: const Text('No, Finish')),
            const SizedBox(width: 12),
            FilledButton(onPressed: _loading || _preparingReport ? null : _chooseCheckupDate, child: const Text('Yes, Continue')),
          ]),
        ]))),
        if (_preparingReport)
          const Padding(padding: EdgeInsets.all(24), child: Column(children: [CircularProgressIndicator(), SizedBox(height: 12), Text('Preparing your report...')])),
        if (_loading) const Padding(padding: EdgeInsets.all(24), child: Column(children: [CircularProgressIndicator(), SizedBox(height: 12), Text('Checking available resources...')])),
        if (_error != null) ...[
          const SizedBox(height: 12),
          Card(child: Padding(padding: const EdgeInsets.all(16), child: Text(_error!, style: const TextStyle(color: AppTheme.errorColor)))),
        ],
        if (_result case final result?) ...[
          const SizedBox(height: 8),
          if (_checkupDate != null)
            Text('Checkup Date: ${_formatCalendarDate(_checkupDate!)}', style: Theme.of(context).textTheme.bodyMedium),
          Text('Available resources for ${result.specialty}', style: Theme.of(context).textTheme.titleLarge),
          if (result.message.isNotEmpty)
            Card(child: Padding(padding: const EdgeInsets.all(16), child: Text(result.message))),
          if (result.recommendations.isEmpty)
            Card(child: Padding(padding: const EdgeInsets.all(16), child: Text(result.message.isNotEmpty ? result.message : 'No suitable resources are currently available. Your appointment remains confirmed.'))),
          for (var i = 0; i < result.recommendations.length; i++) ...[
            const SizedBox(height: 10),
            _ResourceTile(candidate: result.recommendations[i], recommended: i == 0, busy: _allocatingId != null, onSelect: () => _select(result.recommendations[i])),
          ],
          const SizedBox(height: 18),
          OutlinedButton(onPressed: _allocatingId == null && !_preparingReport ? _finish : null, child: const Text('Skip resources and finish')),
        ],
      ])),
    );
  }

  String _formatDate(DateTime value) => '${value.day}/${value.month}/${value.year} ${value.hour.toString().padLeft(2, '0')}:${value.minute.toString().padLeft(2, '0')}';
  String _formatCalendarDate(DateTime value) => '${value.day}/${value.month}/${value.year}';
}

class _ResourceTile extends StatelessWidget {
  final ResourceCandidateModel candidate;
  final bool recommended, busy;
  final VoidCallback onSelect;
  const _ResourceTile({required this.candidate, required this.recommended, required this.busy, required this.onSelect});
  @override
  Widget build(BuildContext context) => Card(child: Padding(padding: const EdgeInsets.all(16), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
    Row(children: [Expanded(child: Text(candidate.kind == 'equipment' && candidate.code.isNotEmpty ? '${candidate.name} · ${candidate.code}' : candidate.name, style: Theme.of(context).textTheme.titleMedium)), if (recommended) const Chip(label: Text('Recommended'))]),
    Text('${candidate.type} · ${candidate.status}'),
    if (candidate.location.isNotEmpty) Text(candidate.location),
    const SizedBox(height: 4),
    Text(candidate.reason, style: Theme.of(context).textTheme.bodySmall),
    const SizedBox(height: 10),
    Align(alignment: Alignment.centerRight, child: FilledButton(onPressed: busy ? null : onSelect, child: Text(busy ? 'Reserving...' : 'Select this resource'))),
  ])));
}
