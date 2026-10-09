import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../../models/emr/vital_sign_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/emr_provider.dart';
import 'emr_widgets.dart';

class PatientVitalsScreen extends StatefulWidget {
  const PatientVitalsScreen({super.key});

  @override
  State<PatientVitalsScreen> createState() => _PatientVitalsScreenState();
}

class _PatientVitalsScreenState extends State<PatientVitalsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadVitals();
    });
  }

  void _loadVitals({bool force = false}) {
    final user = context.read<AuthProvider>().currentUser;
    if (user != null) {
      context.read<EmrProvider>().loadVitals(user.id, force: force);
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;

    if (user == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('My Vital Signs')),
        body: EmrErrorView(
          message: 'You must be logged in to view your vital signs.',
          onRetry: () => Navigator.of(context).pop(),
        ),
      );
    }

    final emr = context.watch<EmrProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Vital Signs'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh Vitals',
            onPressed: () => _loadVitals(force: true),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => _loadVitals(force: true),
        child: _buildBody(emr, user.id),
      ),
    );
  }

  Widget _buildBody(EmrProvider emr, int patientId) {
    if (emr.vitalsLoading && emr.vitals.isEmpty) {
      return const EmrLoadingView(message: 'Loading your vital signs...');
    }

    if (emr.vitalsError != null && emr.vitals.isEmpty) {
      return EmrErrorView(
        message: emr.vitalsError!,
        onRetry: () => _loadVitals(force: true),
      );
    }

    if (emr.vitals.isEmpty) {
      return const EmrEmptyState(
        icon: Icons.favorite_border_rounded,
        title: 'No Vital Signs Recorded',
        subtitle:
            'Vital signs such as blood pressure, heart rate, temperature, and oxygen levels will appear here once recorded during visits.',
      );
    }

    final latest = emr.vitals.first;

    return ListView(
      padding: const EdgeInsets.all(16.0),
      children: [
        // Latest Vitals Glance Header
        const Text(
          'Latest Vitals Summary',
          style: TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.bold,
            color: AppTheme.textPrimary,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          'Recorded on ${DateFormat('MMMM dd, yyyy • hh:mm a').format(latest.recordedAt)}',
          style: TextStyle(fontSize: 12, color: AppTheme.textSecondary),
        ),
        const SizedBox(height: 12),

        // Grid of metric tiles
        _buildMetricCards(latest),
        const SizedBox(height: 24),

        // Historical Records List
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'Historical Logs',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: AppTheme.textPrimary,
              ),
            ),
            Text(
              '${emr.vitals.length} records',
              style: TextStyle(fontSize: 12, color: AppTheme.textSecondary),
            ),
          ],
        ),
        const SizedBox(height: 12),

        ...emr.vitals.map((v) => _buildHistoricalVitalCard(v)),
      ],
    );
  }

  Widget _buildMetricCards(VitalSignModel latest) {
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _buildMetricTile(
                title: 'Blood Pressure',
                value: (latest.systolicBloodPressure != null &&
                        latest.diastolicBloodPressure != null)
                    ? '${latest.systolicBloodPressure}/${latest.diastolicBloodPressure}'
                    : '--',
                unit: 'mmHg',
                icon: Icons.speed_rounded,
                color: AppTheme.errorColor,
                statusTag: _getBpStatus(latest.systolicBloodPressure, latest.diastolicBloodPressure),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildMetricTile(
                title: 'Heart Rate',
                value: latest.heartRateBpm?.toString() ?? '--',
                unit: 'bpm',
                icon: Icons.favorite_rounded,
                color: AppTheme.errorColor,
                statusTag: _getHrStatus(latest.heartRateBpm),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: _buildMetricTile(
                title: 'Temperature',
                value: latest.temperatureCelsius?.toStringAsFixed(1) ?? '--',
                unit: '°C',
                icon: Icons.thermostat_rounded,
                color: AppTheme.warningColor,
                statusTag: _getTempStatus(latest.temperatureCelsius),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildMetricTile(
                title: 'Blood Oxygen',
                value: latest.oxygenSaturationSpO2?.toStringAsFixed(0) ?? '--',
                unit: '% SpO2',
                icon: Icons.air_rounded,
                color: AppTheme.accentColor,
                statusTag: _getSpO2Status(latest.oxygenSaturationSpO2),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: _buildMetricTile(
                title: 'BMI',
                value: latest.bmi?.toStringAsFixed(1) ?? '--',
                unit: 'kg/m²',
                icon: Icons.monitor_weight_outlined,
                color: AppTheme.secondaryColor,
                statusTag: _getBmiStatus(latest.bmi),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildMetricTile(
                title: 'Respiratory Rate',
                value: latest.respiratoryRateBpm?.toString() ?? '--',
                unit: 'breaths/min',
                icon: Icons.waves_rounded,
                color: AppTheme.primaryColor,
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildMetricTile({
    required String title,
    required String value,
    required String unit,
    required IconData icon,
    required Color color,
    String? statusTag,
  }) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppTheme.neutralContainer,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppTheme.borderColor),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.02),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: color, size: 20),
              ),
              if (statusTag != null)
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: _getStatusColor(statusTag).withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: Text(
                    statusTag,
                    style: TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      color: _getStatusColor(statusTag),
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            crossAxisAlignment: CrossAxisAlignment.baseline,
            textBaseline: TextBaseline.alphabetic,
            children: [
              Text(
                value,
                style: const TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                  color: AppTheme.textPrimary,
                ),
              ),
              const SizedBox(width: 4),
              Text(
                unit,
                style: TextStyle(
                  fontSize: 12,
                  color: AppTheme.textSecondary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 2),
          Text(
            title,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w500,
              color: AppTheme.textSecondary,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHistoricalVitalCard(VitalSignModel vital) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: AppTheme.borderColor),
      ),
      elevation: 1,
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
                    Icon(Icons.access_time, size: 14, color: AppTheme.textSecondary),
                    const SizedBox(width: 6),
                    Text(
                      DateFormat('MMM dd, yyyy • hh:mm a')
                          .format(vital.recordedAt),
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
                if (vital.bmi != null)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: AppTheme.secondaryColor.withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      'BMI: ${vital.bmi!.toStringAsFixed(1)}',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.textPrimary,
                      ),
                    ),
                  ),
              ],
            ),
            const Divider(height: 18),
            Wrap(
              spacing: 12,
              runSpacing: 8,
              children: [
                if (vital.systolicBloodPressure != null &&
                    vital.diastolicBloodPressure != null)
                  _buildMetricChip(
                    'BP',
                    '${vital.systolicBloodPressure}/${vital.diastolicBloodPressure} mmHg',
                    Icons.speed,
                    AppTheme.errorColor,
                  ),
                if (vital.heartRateBpm != null)
                  _buildMetricChip(
                    'Pulse',
                    '${vital.heartRateBpm} bpm',
                    Icons.favorite,
                    AppTheme.errorColor,
                  ),
                if (vital.temperatureCelsius != null)
                  _buildMetricChip(
                    'Temp',
                    '${vital.temperatureCelsius!.toStringAsFixed(1)} °C',
                    Icons.thermostat,
                    AppTheme.warningColor,
                  ),
                if (vital.oxygenSaturationSpO2 != null)
                  _buildMetricChip(
                    'SpO2',
                    '${vital.oxygenSaturationSpO2!.toStringAsFixed(0)}%',
                    Icons.air,
                    AppTheme.accentColor,
                  ),
                if (vital.weightKg != null)
                  _buildMetricChip(
                    'Weight',
                    '${vital.weightKg!.toStringAsFixed(1)} kg',
                    Icons.scale,
                    AppTheme.textSecondary,
                  ),
                if (vital.heightCm != null)
                  _buildMetricChip(
                    'Height',
                    '${vital.heightCm!.toStringAsFixed(0)} cm',
                    Icons.height,
                    AppTheme.textSecondary,
                  ),
              ],
            ),
            if (vital.notes.isNotEmpty) ...[
              const SizedBox(height: 10),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceColor,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: AppTheme.borderColor),
                ),
                child: Text(
                  'Notes: ${vital.notes}',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppTheme.textPrimary,
                    fontStyle: FontStyle.italic,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildMetricChip(
      String label, String value, IconData icon, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withValues(alpha: 0.2)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: color),
          const SizedBox(width: 4),
          Text(
            '$label: ',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.bold,
              color: color,
            ),
          ),
          Text(
            value,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: AppTheme.textPrimary,
            ),
          ),
        ],
      ),
    );
  }

  String? _getBpStatus(int? sys, int? dia) {
    if (sys == null || dia == null) return null;
    if (sys < 120 && dia < 80) return 'Normal';
    if (sys <= 129 && dia < 80) return 'Elevated';
    if (sys <= 139 || dia <= 89) return 'Stage 1';
    return 'High';
  }

  String? _getHrStatus(int? hr) {
    if (hr == null) return null;
    if (hr < 60) return 'Low';
    if (hr <= 100) return 'Normal';
    return 'High';
  }

  String? _getTempStatus(double? temp) {
    if (temp == null) return null;
    if (temp < 36.1) return 'Low';
    if (temp <= 37.2) return 'Normal';
    if (temp <= 38.0) return 'Elevated';
    return 'Fever';
  }

  String? _getSpO2Status(double? sp) {
    if (sp == null) return null;
    if (sp >= 95) return 'Normal';
    if (sp >= 90) return 'Low';
    return 'Critical';
  }

  String? _getBmiStatus(double? bmi) {
    if (bmi == null) return null;
    if (bmi < 18.5) return 'Underweight';
    if (bmi < 25) return 'Normal';
    if (bmi < 30) return 'Overweight';
    return 'Obese';
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'normal':
        return AppTheme.successColor;
      case 'elevated':
      case 'stage 1':
      case 'low':
      case 'overweight':
        return AppTheme.warningColor;
      case 'high':
      case 'fever':
      case 'critical':
      case 'obese':
        return AppTheme.errorColor;
      default:
        return AppTheme.textSecondary;
    }
  }
}
