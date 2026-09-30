import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_client.dart';
import '../../core/network/api_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../models/smart_care/smart_care_result.dart';
import '../../services/smart_care_service.dart';
import '../../widgets/app_button.dart';
import '../../widgets/app_text_field.dart';
import '../../widgets/app_ui.dart';
import '../../widgets/feature_tile.dart';

const _emergencyNotice =
    'If this is a medical emergency, call emergency services or go to the nearest emergency department now.';

enum _Stage { input, processing, result, error }

/// Smart Care: the patient describes what they need, gets a suggested specialty
/// and real bookable doctors, then continues in the existing booking flow.
/// Manual browsing is always one tap away.
class SmartCareScreen extends StatefulWidget {
  const SmartCareScreen({super.key});

  @override
  State<SmartCareScreen> createState() => _SmartCareScreenState();
}

class _SmartCareScreenState extends State<SmartCareScreen> {
  static const _steps = [
    'Understanding your request',
    'Finding suitable doctors',
    'Preparing recommendations',
  ];

  final _controller = TextEditingController();
  _Stage _stage = _Stage.input;
  int _step = 0;
  Timer? _stepTimer;
  String? _inputError;
  String? _errorMessage;
  SmartCareResult? _result;

  @override
  void dispose() {
    _stepTimer?.cancel();
    _controller.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final text = _controller.text.trim();
    if (text.length < 3) {
      setState(() => _inputError = 'Please describe your symptoms or what you need help with.');
      return;
    }
    if (text.length > 1000) {
      setState(() => _inputError = 'Please keep your description under 1000 characters.');
      return;
    }
    FocusScope.of(context).unfocus();

    setState(() {
      _inputError = null;
      _stage = _Stage.processing;
      _step = 0;
    });
    _stepTimer?.cancel();
    _stepTimer = Timer.periodic(const Duration(milliseconds: 1400), (timer) {
      if (!mounted || _step >= _steps.length - 1) {
        timer.cancel();
        return;
      }
      setState(() => _step++);
    });

    try {
      final result = await SmartCareService(context.read<ApiClient>()).getRecommendation(text);
      if (!mounted) return;
      setState(() {
        _result = result;
        _stage = _Stage.result;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.statusCode == 429
            ? e.message
            : 'Smart Care couldn\'t complete your request. You can still browse doctors and book an appointment yourself.';
        _stage = _Stage.error;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _errorMessage =
            'Smart Care couldn\'t complete your request. You can still browse doctors and book an appointment yourself.';
        _stage = _Stage.error;
      });
    } finally {
      _stepTimer?.cancel();
    }
  }

  void _startOver() => setState(() {
    _stage = _Stage.input;
    _result = null;
    _errorMessage = null;
  });

  void _browseManually() => context.push('/appointments/search');

  void _selectDoctor(SmartCareDoctor doctor) => context.push(
    '/appointments/doctor/${doctor.doctorProfileId}?userId=${doctor.doctorId}',
  );

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Smart Care')),
      body: AppPageFrame(
        child: ListView(
          padding: AppTheme.pagePadding,
          children: switch (_stage) {
            _Stage.input => _buildInput(context),
            _Stage.processing => _buildProcessing(context),
            _Stage.result => _buildResult(context, _result!),
            _Stage.error => _buildError(context),
          },
        ),
      ),
    );
  }

  // ── Input ─────────────────────────────────────────────────────────────────

  List<Widget> _buildInput(BuildContext context) => [
    Text('How can we help you?', style: Theme.of(context).textTheme.headlineMedium),
    const SizedBox(height: 6),
    Text(
      'Describe your symptoms or what you need. We\'ll suggest a suitable specialty and doctors you can book. '
      'This is a suggestion, not a diagnosis.',
      style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: AppTheme.textSecondary),
    ),
    const SizedBox(height: 20),
    AppTextField(
      label: 'Your symptoms or need',
      hint: 'e.g. I get chest discomfort when I climb stairs',
      controller: _controller,
      maxLines: 5,
      keyboardType: TextInputType.multiline,
      onChanged: (_) {
        if (_inputError != null) setState(() => _inputError = null);
      },
    ),
    if (_inputError case final error?) ...[
      Text(error, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppTheme.errorColor)),
      const SizedBox(height: 12),
    ],
    AppButton(text: 'Start Smart Appointment', onPressed: _submit),
    const SizedBox(height: 8),
    _BrowseManuallyButton(onPressed: _browseManually),
    const SizedBox(height: 20),
    _EmergencyFootnote(),
  ];

  // ── Processing ────────────────────────────────────────────────────────────

  List<Widget> _buildProcessing(BuildContext context) => [
    Text('Working on it…', style: Theme.of(context).textTheme.headlineMedium),
    const SizedBox(height: 18),
    AppCard(
      child: Column(
        children: [
          for (var i = 0; i < _steps.length; i++)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 8),
              child: Row(
                children: [
                  SizedBox(
                    width: 24,
                    height: 24,
                    child: i < _step
                        ? const Icon(Icons.check_circle_rounded, color: AppTheme.successColor)
                        : i == _step
                        ? const Padding(
                            padding: EdgeInsets.all(3),
                            child: CircularProgressIndicator(strokeWidth: 2.5),
                          )
                        : const Icon(Icons.radio_button_unchecked_rounded, color: AppTheme.textMuted),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      _steps[i],
                      style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                        color: i <= _step ? AppTheme.textPrimary : AppTheme.textMuted,
                        fontWeight: i == _step ? FontWeight.w600 : FontWeight.w400,
                      ),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    ),
    const SizedBox(height: 16),
    _BrowseManuallyButton(onPressed: _browseManually),
  ];

  // ── Result ────────────────────────────────────────────────────────────────

  List<Widget> _buildResult(BuildContext context, SmartCareResult result) {
    final textTheme = Theme.of(context).textTheme;
    return [
      // The emergency notice comes first, independent of whether triage succeeded.
      if (result.possibleEmergency) ...[
        _EmergencyBanner(message: result.emergencyNotice ?? _emergencyNotice),
        const SizedBox(height: 16),
      ],
      if (!result.hasRecommendation) ...[
        EmptyState(
          icon: Icons.cloud_off_rounded,
          title: 'Smart Care is unavailable right now',
          message: result.message ??
              'You can still browse doctors and book an appointment yourself.',
        ),
      ] else ...[
        AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Recommended specialty', style: textTheme.labelLarge?.copyWith(color: AppTheme.textSecondary)),
              const SizedBox(height: 4),
              Row(
                children: [
                  Expanded(child: Text(result.category!, style: textTheme.headlineSmall)),
                  if (result.priority case final priority?) PriorityBadge(label: priority),
                ],
              ),
              if (result.reason case final reason? when reason.isNotEmpty) ...[
                const SizedBox(height: 10),
                Text(reason, style: textTheme.bodyMedium),
              ],
              if (result.priority case final priority?) ...[
                const SizedBox(height: 10),
                Text(
                  'Suggested priority: $priority. You\'ll choose the priority yourself when you book, '
                  'and hospital staff review priority requests.',
                  style: textTheme.bodySmall?.copyWith(color: AppTheme.textSecondary),
                ),
              ],
              const SizedBox(height: 10),
              Text(
                'This is a suggestion to help you book, not a diagnosis.',
                style: textTheme.bodySmall?.copyWith(color: AppTheme.textMuted),
              ),
            ],
          ),
        ),
        const SizedBox(height: 20),
        if (result.recommendedDoctors.isEmpty)
          EmptyState(
            icon: Icons.person_search_rounded,
            title: 'No doctors available',
            message: result.message ??
                'No doctors in this specialty are available for online booking right now.',
          )
        else ...[
          SectionHeader('Recommended doctors', subtitle: 'Choose a doctor to see available times.'),
          const SizedBox(height: 12),
          for (final doctor in result.recommendedDoctors) ...[
            _DoctorCard(doctor: doctor, onSelect: () => _selectDoctor(doctor)),
            const SizedBox(height: 10),
          ],
        ],
      ],
      const SizedBox(height: 12),
      _BrowseManuallyButton(onPressed: _browseManually),
      Center(child: TextButton(onPressed: _startOver, child: const Text('Start over'))),
    ];
  }

  // ── Error ─────────────────────────────────────────────────────────────────

  List<Widget> _buildError(BuildContext context) => [
    EmptyState(
      icon: Icons.error_outline_rounded,
      title: 'Something went wrong',
      message: _errorMessage,
      action: FilledButton(onPressed: _browseManually, child: const Text('Browse doctors')),
    ),
    Center(child: TextButton(onPressed: _startOver, child: const Text('Try again'))),
    const SizedBox(height: 12),
    _EmergencyFootnote(),
  ];
}

class _DoctorCard extends StatelessWidget {
  const _DoctorCard({required this.doctor, required this.onSelect});
  final SmartCareDoctor doctor;
  final VoidCallback onSelect;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              const IconBadge(
                icon: Icons.medical_services_rounded,
                tint: AppTheme.serviceDoctorsTint,
                foreground: AppTheme.serviceDoctorsForeground,
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Dr. ${doctor.name}', style: textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
                    if (doctor.specialization.isNotEmpty)
                      Text(doctor.specialization, style: textTheme.bodySmall),
                    Text(
                      doctor.department,
                      style: textTheme.bodySmall?.copyWith(color: AppTheme.textSecondary),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Align(
            alignment: Alignment.centerRight,
            child: FilledButton(onPressed: onSelect, child: const Text('Select Doctor')),
          ),
        ],
      ),
    );
  }
}

class _EmergencyBanner extends StatelessWidget {
  const _EmergencyBanner({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) => Semantics(
    liveRegion: true,
    child: Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppTheme.errorContainer,
        borderRadius: BorderRadius.circular(AppTheme.radiusMedium),
        border: Border.all(color: AppTheme.errorColor, width: 1.5),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.emergency_rounded, color: AppTheme.errorColor, size: 28),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              message,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                color: AppTheme.errorColor,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    ),
  );
}

class _EmergencyFootnote extends StatelessWidget {
  @override
  Widget build(BuildContext context) => Row(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const Icon(Icons.info_outline_rounded, size: 18, color: AppTheme.textMuted),
      const SizedBox(width: 8),
      Expanded(
        child: Text(
          _emergencyNotice,
          style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppTheme.textSecondary),
        ),
      ),
    ],
  );
}

class _BrowseManuallyButton extends StatelessWidget {
  const _BrowseManuallyButton({required this.onPressed});
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) => SizedBox(
    width: double.infinity,
    child: OutlinedButton.icon(
      onPressed: onPressed,
      icon: const Icon(Icons.search_rounded),
      label: const Text('Browse doctors manually'),
    ),
  );
}
