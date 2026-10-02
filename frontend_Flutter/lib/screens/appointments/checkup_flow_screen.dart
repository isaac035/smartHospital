import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/network/api_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../models/appointments/appointment_model.dart';
import '../../models/appointments/resource_allocation_model.dart';
import '../../providers/appointment_provider.dart';

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
  int? _allocatingId;
  DateTime? _admissionDate;

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
      if (mounted) setState(() { _result = result; _loading = false; });
    } catch (_) {
      if (mounted) setState(() { _loading = false; _error = 'Resource suggestions are unavailable right now. Your appointment is still confirmed.'; });
    }
  }

  Future<void> _chooseAdmissionDate() async {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final selected = await showDatePicker(
      context: context,
      initialDate: _admissionDate ?? today,
      firstDate: today,
      lastDate: DateTime(today.year + 2, today.month, today.day),
      helpText: 'Choose an admission date',
    );
    if (selected == null || !mounted) return;
    // Keep the calendar's local year/month/day; the API transports this as a
    // date-only string so UTC conversion cannot move it to another day.
    setState(() => _admissionDate = DateTime(selected.year, selected.month, selected.day));
    await _checkResources();
  }

  Future<void> _select(ResourceCandidateModel resource) async {
    setState(() { _allocatingId = resource.resourceId; _error = null; });
    try {
      final admissionDate = _admissionDate;
      if (admissionDate == null) throw StateError('Choose an admission date first.');
      await context.read<AppointmentProvider>().allocateResource(widget.appointmentId, resource, admissionDate);
      if (!mounted) return;
      await context.read<AppointmentProvider>().loadAppointmentDetail(widget.appointmentId);
      if (mounted) context.go('/appointments/${widget.appointmentId}');
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

  void _finish() => context.go('/appointments/${widget.appointmentId}');

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
            OutlinedButton(onPressed: _loading ? null : _finish, child: const Text('No, Finish')),
            const SizedBox(width: 12),
            FilledButton(onPressed: _loading ? null : _chooseAdmissionDate, child: const Text('Yes, Continue')),
          ]),
        ]))),
        if (_loading) const Padding(padding: EdgeInsets.all(24), child: Column(children: [CircularProgressIndicator(), SizedBox(height: 12), Text('Checking available resources...')])),
        if (_error != null) ...[
          const SizedBox(height: 12),
          Card(child: Padding(padding: const EdgeInsets.all(16), child: Text(_error!, style: const TextStyle(color: AppTheme.errorColor)))),
        ],
        if (_result case final result?) ...[
          const SizedBox(height: 8),
          if (_admissionDate != null)
            Text('Admission date: ${_formatCalendarDate(_admissionDate!)}', style: Theme.of(context).textTheme.bodyMedium),
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
          OutlinedButton(onPressed: _allocatingId == null ? _finish : null, child: const Text('Skip resources and finish')),
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
