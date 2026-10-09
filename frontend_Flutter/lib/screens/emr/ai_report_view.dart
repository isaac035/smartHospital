import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/constants/app_constants.dart';
import '../../core/theme/app_theme.dart';
import '../../models/emr/ai_medical_report_model.dart';
import '../../widgets/app_ui.dart';

/// Hospital-style rendering of a stored AI medical report.
///
/// Works for both report formats: reports saved before the structured layout
/// (no `recommendations`, `currentEncounter`, `patientContact`) map their fields
/// into the closest section, and anything not shown in a section is still listed
/// under "More recorded details" so no stored fact is hidden.
class AiReportView extends StatelessWidget {
  const AiReportView({super.key, required this.report, this.trailing = const []});

  final AiMedicalReportModel report;

  /// Existing page actions shown after the report (unchanged by this view).
  final List<Widget> trailing;

  static const _shownRecordedKeys = {
    'patientInformation', 'appointmentSummary', 'presentingSymptoms', 'currentEncounter',
    'medicalCheckup', 'vitalSigns', 'prescriptions', 'labReports', 'clinicalDiagnoses',
    'treatmentPlans', 'followUp',
  };
  static const _shownContentKeys = {
    'recordedData', 'followUpSuggestedTests', 'aiSummary', 'aiRecommendations',
    'recommendations', 'patientContact', 'reportMetadata',
  };

  @override
  Widget build(BuildContext context) {
    final content = report.content ?? const <String, dynamic>{};
    final data = _map(content['recordedData']);
    final metadata = _map(content['reportMetadata']);
    final appointment = data['appointmentSummary'] is Map ? _map(data['appointmentSummary']) : null;
    final checkup = _map(data['medicalCheckup']);
    final admission = checkup['admission'] is Map ? _map(checkup['admission']) : null;
    final resources = _maps(checkup['resources']);
    final isNewFormat = content.containsKey('recommendations');
    var number = 0;

    final moreDetails = <String, dynamic>{
      for (final entry in data.entries)
        if (!_shownRecordedKeys.contains(entry.key)) entry.key: entry.value,
      if (appointment?['agent2Outcome'] != null) 'bookingRecord': appointment!['agent2Outcome'],
      for (final entry in content.entries)
        if (!_shownContentKeys.contains(entry.key)) entry.key: entry.value,
    };

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 28),
      children: [
        Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: AppTheme.pageMaxWidth),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _ReportHeader(report: report, metadata: metadata, summary: _map(content['aiSummary'])),
                _Section(
                  number: ++number,
                  title: 'Patient Information',
                  icon: Icons.person_outline_rounded,
                  child: _patient(data, _map(content['patientContact'])),
                ),
                _Section(
                  number: ++number,
                  title: 'Visit / Appointment Details',
                  icon: Icons.event_note_outlined,
                  child: _visit(appointment, checkup),
                ),
                _Section(
                  number: ++number,
                  title: 'Doctor Information',
                  icon: Icons.medical_services_outlined,
                  child: _InfoGrid([
                    ('Doctor', _str(appointment?['doctor']) ?? report.doctorName),
                    ('Specialization', _str(appointment?['doctorSpecialization'])),
                    ('Department', _str(appointment?['doctorDepartment']) ?? _str(appointment?['department'])),
                  ]),
                ),
                if (admission != null || resources.isNotEmpty)
                  _Section(
                    number: ++number,
                    title: 'Admission / Resources',
                    icon: Icons.bed_outlined,
                    child: _admission(checkup, admission, resources),
                  ),
                _Section(
                  number: ++number,
                  title: 'Clinical Summary',
                  icon: Icons.assignment_outlined,
                  child: _clinicalSummary(data, _map(content['aiSummary'])),
                ),
                _Section(
                  number: ++number,
                  title: 'Vital Signs',
                  icon: Icons.monitor_heart_outlined,
                  child: _vitals(_map(data['vitalSigns'])),
                ),
                _Section(
                  number: ++number,
                  title: 'Medications / Prescriptions',
                  icon: Icons.medication_outlined,
                  child: _medications(_map(data['prescriptions'])),
                ),
                _Section(
                  number: ++number,
                  title: 'Lab Tests & Results',
                  icon: Icons.biotech_outlined,
                  child: _labs(_map(data['labReports'])),
                ),
                _Section(
                  number: ++number,
                  title: 'AI Recommendations',
                  icon: Icons.tips_and_updates_outlined,
                  child: _recommendations(content, isNewFormat),
                ),
                _Section(
                  number: ++number,
                  title: 'Follow-up',
                  icon: Icons.event_repeat_outlined,
                  child: _followUp(content, data, isNewFormat),
                ),
                if (moreDetails.isNotEmpty) _MoreDetails(details: moreDetails),
                _ReportFooter(report: report, summary: _map(content['aiSummary'])),
                ...trailing,
              ],
            ),
          ),
        ),
      ],
    );
  }

  // ── Sections ──────────────────────────────────────────────────────────

  Widget _patient(Map<String, dynamic> data, Map<String, dynamic> contact) {
    final patient = _map(data['patientInformation']);
    final dob = _date(patient['dateOfBirth'], withTime: false);
    final age = _str(patient['age']);
    return _InfoGrid([
      ('Name', _str(patient['name'])),
      ('Patient ID', _str(patient['patientId']) == null ? null : '#${patient['patientId']}'),
      ('Age', age == null ? null : '$age years'),
      ('Date of birth', dob),
      ('Gender', _str(patient['gender'])),
      ('Blood group', _bloodGroup(_str(patient['bloodGroup']))),
      ('Phone', _str(contact['phone'])),
      ('Email', _str(contact['email'])),
      ('Allergies', _str(patient['allergies'])),
      ('Chronic conditions', _str(patient['chronicConditions'])),
    ]);
  }

  Widget _visit(Map<String, dynamic>? appointment, Map<String, dynamic> checkup) {
    if (appointment == null) {
      return const _Muted('No appointment is linked to this report. It summarises your recorded medical history.');
    }
    final duration = _str(appointment['durationMinutes']) ?? _str(_map(appointment['agent2Outcome'])['durationMinutes']);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            if (_str(appointment['priority']) case final priority?) PriorityBadge(label: 'Priority: $priority'),
            if (_str(appointment['status']) case final status?) StatusBadge(label: status, color: _statusColor(status)),
          ],
        ),
        const SizedBox(height: 12),
        _InfoGrid([
          ('Reference number', _str(appointment['referenceNumber'])),
          ('Date & time', _date(appointment['date'])),
          ('Duration', duration == null ? null : '$duration minutes'),
          ('Appointment type', _humanize(_str(appointment['type']))),
          ('Medical checkup', _str(checkup['status'])),
          ('Notes', _str(appointment['notes'])),
        ]),
      ],
    );
  }

  Widget _admission(Map<String, dynamic> checkup, Map<String, dynamic>? admission, List<Map<String, dynamic>> resources) {
    final bed = admission == null ? const <String, dynamic>{} : _map(admission['bed']);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _InfoGrid([
          ('Allocation status', _str(checkup['status'])),
          if (admission != null) ...[
            ('Admission number', _str(admission['admissionNumber'])),
            ('Admission status', _str(admission['status'])),
            ('Admitted on', _date(admission['admissionDate'])),
            ('Checkup date', _date(admission['checkupDate'])),
            ('Ward', _str(bed['ward'])),
            ('Room', _str(bed['room'])),
            ('Bed', _str(bed['bedNumber'])),
            ('Bed status', _str(bed['status'])),
            ('Reason', _str(admission['reasonForAdmission'])),
          ],
        ]),
        if (resources.isNotEmpty) ...[
          const SizedBox(height: 12),
          const _SubTitle('Allocated resources'),
          _DataTable(
            columns: const ['Resource', 'Code', 'Location', 'Status'],
            rows: resources
                .map((r) => _TableRowData([
                      [_str(r['name']), _humanize(_str(r['kind']))].whereType<String>().join(' · '),
                      _str(r['code']) ?? '—',
                      [_str(r['ward']), _str(r['room']) == null ? null : 'Room ${r['room']}'].whereType<String>().join(', '),
                      _str(r['status']) ?? '—',
                    ], detail: _date(r['allocatedAt']) == null ? null : 'Allocated ${_date(r['allocatedAt'])}'))
                .toList(),
          ),
        ],
      ],
    );
  }

  Widget _clinicalSummary(Map<String, dynamic> data, Map<String, dynamic> summary) {
    final symptoms = _maps(_map(data['presentingSymptoms'])['records']);
    final encounters = _maps(_map(data['currentEncounter'])['records']);
    final triage = _map(data['clinicalTriageSummary']);
    final diagnoses = _maps(_map(data['clinicalDiagnoses'])['records']);
    final plans = _maps(_map(data['treatmentPlans'])['records']);
    final hasTriage = _str(triage['triageResultId']) != null;
    final children = <Widget>[];

    void add(String title, Widget body) {
      if (children.isNotEmpty) children.add(const SizedBox(height: 14));
      children..add(_SubTitle(title))..add(body);
    }

    add(
      'Reason for visit',
      symptoms.isEmpty
          ? _Muted(_str(_map(data['presentingSymptoms'])['status']) ?? 'Not recorded')
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: symptoms
                  .map((s) => _Paragraph([
                        if (_str(s['chiefComplaint']) case final complaint?) 'Complaint: $complaint',
                        if (_str(s['symptoms']) case final text?) 'Symptoms: $text',
                      ].join('\n')))
                  .toList(),
            ),
    );
    for (final encounter in encounters) {
      add(
        'Consultation notes${_str(encounter['recordNumber']) == null ? '' : ' · ${encounter['recordNumber']}'}',
        _InfoGrid([
          ('Diagnosis', _str(encounter['diagnosis'])),
          ('Examination notes', _str(encounter['examinationNotes'])),
          ('Treatment plan', _str(encounter['treatmentPlan'])),
          ('Doctor', _str(encounter['doctor'])),
        ], columns: 1),
      );
    }
    if (hasTriage) {
      add(
        'Smart Care triage',
        _InfoGrid([
          ('Category', _str(triage['category'])),
          ('Priority', _str(triage['priority'])),
          ('Reason', _str(triage['reason'])),
          ('Emergency notice', _str(triage['emergencyNotice'])),
        ], columns: 1),
      );
    }
    add(
      'Diagnoses',
      diagnoses.isEmpty
          ? _Muted(_str(_map(data['clinicalDiagnoses'])['status']) ?? 'No diagnoses recorded')
          : _DataTable(
              columns: const ['Diagnosis', 'Type', 'Severity', 'Status'],
              rows: diagnoses
                  .map((d) => _TableRowData([
                        [_str(d['description']), _str(d['code']) == null ? null : '(${d['code']})'].whereType<String>().join(' '),
                        _humanize(_str(d['type'])) ?? '—',
                        _str(d['severity']) ?? '—',
                        _str(d['status']) ?? '—',
                      ], detail: [_date(d['diagnosedAt'], withTime: false), _str(d['notes'])].whereType<String>().join(' · ')))
                  .toList(),
            ),
    );
    if (plans.isNotEmpty) {
      add(
        'Treatment plans',
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: plans
              .map((p) => _Paragraph([
                    [_str(p['title']), _str(p['status']) == null ? null : '(${p['status']})'].whereType<String>().join(' '),
                    ?_str(p['description']),
                    if (_str(p['goals']) case final goals?) 'Goals: $goals',
                    if (_str(p['interventions']) case final interventions?) 'Interventions: $interventions',
                  ].join('\n')))
              .toList(),
        ),
      );
    }
    if (_str(summary['text']) case final text?) {
      add(_str(summary['label']) ?? 'Summary', _Paragraph(text));
    }
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: children);
  }

  Widget _vitals(Map<String, dynamic> section) {
    final records = _maps(section['records']);
    if (records.isEmpty) return _Muted(_str(section['status']) ?? 'Not recorded');
    final latest = records.first;
    String? bp(Map<String, dynamic> v) => _str(v['systolicBloodPressure']) == null && _str(v['diastolicBloodPressure']) == null
        ? null
        : '${_str(v['systolicBloodPressure']) ?? '—'}/${_str(v['diastolicBloodPressure']) ?? '—'} mmHg';
    String? unit(dynamic value, String suffix) => _str(value) == null ? null : '${_str(value)} $suffix';
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _Muted('Latest reading · ${_date(latest['recordedAt']) ?? 'date not recorded'}'),
        const SizedBox(height: 10),
        _InfoGrid([
          ('Blood pressure', bp(latest)),
          ('Heart rate', unit(latest['heartRateBpm'], 'bpm')),
          ('Temperature', unit(latest['temperatureCelsius'], '°C')),
          ('SpO₂', unit(latest['oxygenSaturationSpO2'], '%')),
          ('Respiratory rate', unit(latest['respiratoryRateBpm'], 'breaths/min')),
          ('Weight', unit(latest['weightKg'], 'kg')),
          ('Height', unit(latest['heightCm'], 'cm')),
          ('BMI', _str(latest['bmi'])),
          ('Notes', _str(latest['notes'])),
        ]),
        if (records.length > 1) ...[
          const SizedBox(height: 12),
          _SubTitle('Earlier readings (${records.length - 1})'),
          _DataTable(
            columns: const ['Date', 'BP', 'Heart rate', 'Temp / SpO₂'],
            rows: records
                .skip(1)
                .map((v) => _TableRowData([
                      _date(v['recordedAt']) ?? '—',
                      bp(v) ?? '—',
                      unit(v['heartRateBpm'], 'bpm') ?? '—',
                      [unit(v['temperatureCelsius'], '°C'), unit(v['oxygenSaturationSpO2'], '%')].whereType<String>().join(' · '),
                    ], detail: [unit(v['weightKg'], 'kg'), _str(v['notes'])].whereType<String>().join(' · ')))
                .toList(),
          ),
        ],
      ],
    );
  }

  Widget _medications(Map<String, dynamic> section) {
    final prescriptions = _maps(section['records']);
    if (prescriptions.isEmpty) return const _Muted('No medications prescribed');
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final (index, rx) in prescriptions.indexed) ...[
          if (index > 0) const SizedBox(height: 14),
          Wrap(
            spacing: 8,
            runSpacing: 6,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Text(_str(rx['prescriptionNumber']) ?? 'Prescription', style: const TextStyle(fontWeight: FontWeight.w600, color: AppTheme.textPrimary)),
              if (_str(rx['status']) case final status?) StatusBadge(label: status, color: _statusColor(status)),
              _Muted([
                if (_date(rx['issueDate'], withTime: false) case final issued?) 'Issued $issued',
                if (_date(rx['expiryDate'], withTime: false) case final expiry?) 'Expires $expiry',
              ].join(' · ')),
            ],
          ),
          const SizedBox(height: 8),
          _DataTable(
            columns: const ['Medicine', 'Dosage', 'Frequency', 'Duration'],
            rows: _maps(rx['items'])
                .map((item) => _TableRowData([
                      _str(item['medicineName']) ?? '—',
                      [_str(item['dosage']), _str(item['route'])].whereType<String>().join(' · '),
                      _str(item['frequency']) ?? '—',
                      _str(item['durationDays']) == null ? '—' : '${item['durationDays']} days',
                    ], detail: _str(item['specialInstructions'])))
                .toList(),
            emptyText: 'No medicines listed on this prescription',
          ),
          if (_str(rx['generalInstructions']) case final instructions?) ...[
            const SizedBox(height: 6),
            _Paragraph('Instructions: $instructions'),
          ],
        ],
      ],
    );
  }

  Widget _labs(Map<String, dynamic> section) {
    final orders = _maps(section['records']);
    if (orders.isEmpty) return const _Muted('No lab tests recorded');
    return _DataTable(
      columns: const ['Test', 'Result', 'Status', 'Date'],
      rows: orders.map((order) {
        final result = _map(order['report']);
        return _TableRowData(
          [
            [_str(order['testName']), _str(order['category']) == null ? null : '(${order['category']})'].whereType<String>().join(' '),
            _str(result['resultSummary']) ?? 'Pending',
            _str(order['status']) ?? '—',
            _date(result['reportDate'], withTime: false) ?? _date(order['orderedAt'], withTime: false) ?? '—',
          ],
          detail: [
            if (_str(result['findings']) case final findings?) 'Findings: $findings',
            if (_str(result['referenceRange']) case final range?) 'Reference range: $range',
            if (_str(result['doctorRemarks']) case final remarks?) 'Doctor remarks: $remarks',
            if (_str(order['clinicalNotes']) case final notes?) 'Clinical notes: $notes',
          ].join('\n'),
          statusColumn: 2,
        );
      }).toList(),
    );
  }

  Widget _recommendations(Map<String, dynamic> content, bool isNewFormat) {
    final recommendations = _map(content['recommendations']);
    if (!isNewFormat) {
      // Reports saved before the structured layout only have the general guidance list.
      final legacy = _map(content['aiRecommendations']);
      final items = _list(legacy['items']).map(_str).whereType<String>().toList();
      if (items.isEmpty) return const _Muted('Recommendations are not available for this report.');
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (_str(legacy['label']) case final label?) _Muted(label),
          const SizedBox(height: 6),
          _Bullets(items),
        ],
      );
    }
    if (recommendations['available'] != true) {
      return _Muted(_str(recommendations['message']) ?? 'Recommendations are not available for this report.');
    }
    final diet = _map(recommendations['diet']);
    final exercise = _map(recommendations['exercise']);
    List<String> texts(dynamic value) => _list(value).map(_str).whereType<String>().toList();
    final groups = <(String, IconData, List<String>, Color)>[
      ('Foods to prefer', Icons.restaurant_outlined, texts(diet['prefer']), AppTheme.successColor),
      ('Foods to limit', Icons.no_food_outlined, texts(diet['avoid']), AppTheme.warningColor),
      ('Suitable activity', Icons.directions_walk_rounded, texts(exercise['suitable']), AppTheme.successColor),
      ('Activity to avoid', Icons.do_not_disturb_on_outlined, texts(exercise['avoid']), AppTheme.warningColor),
      ('Lifestyle', Icons.self_improvement_rounded, texts(recommendations['lifestyle']), AppTheme.primaryColor),
      ('Warning signs: contact a doctor', Icons.warning_amber_rounded, texts(recommendations['warningSigns']), AppTheme.errorColor),
    ].where((group) => group.$3.isNotEmpty).toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        StatusBadge(
          label: recommendations['basis'] == 'condition_specific' ? 'Based on your recorded condition' : 'General healthy-living advice',
          color: AppTheme.secondaryColor,
        ),
        for (final (title, icon, items, color) in groups) ...[
          const SizedBox(height: 14),
          Row(children: [
            Icon(icon, size: 18, color: color),
            const SizedBox(width: 8),
            Expanded(child: _SubTitle(title, padding: EdgeInsets.zero)),
          ]),
          const SizedBox(height: 6),
          _Bullets(items),
        ],
        if (_str(recommendations['note']) case final note?) ...[
          const SizedBox(height: 12),
          _Muted(note),
        ],
      ],
    );
  }

  Widget _followUp(Map<String, dynamic> content, Map<String, dynamic> data, bool isNewFormat) {
    final suggested = _map(content['followUpSuggestedTests']);
    final recordedDates = [
      ..._maps(suggested['recordedFollowUpDates']),
      if (_maps(suggested['recordedFollowUpDates']).isEmpty) ..._maps(_map(data['followUp'])['recordedDates']),
    ].map((d) => _date(d['date'], withTime: false)).whereType<String>().toList();
    final aiFollowUp = _map(_map(content['recommendations'])['followUp']);
    final monitoring = _list(aiFollowUp['monitoring']).map(_str).whereType<String>().toList();
    final general = isNewFormat ? _list(_map(content['aiRecommendations'])['items']).map(_str).whereType<String>().toList() : <String>[];
    final note = _str(suggested['note']) ?? _str(_map(data['followUp'])['suggestedTests']);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _InfoGrid([
          ('Scheduled follow-up', recordedDates.isEmpty ? null : recordedDates.join(', ')),
          ('Suggested next check-up', _str(aiFollowUp['timeframe'])),
        ], emptyText: 'No follow-up date recorded. Follow your doctor\'s advice.'),
        if (monitoring.isNotEmpty) ...[
          const SizedBox(height: 12),
          const _SubTitle('What to monitor'),
          _Bullets(monitoring),
        ],
        if (general.isNotEmpty) ...[
          const SizedBox(height: 12),
          const _SubTitle('General guidance'),
          _Bullets(general),
        ],
        if (note != null) ...[
          const SizedBox(height: 10),
          _Muted(note),
        ],
      ],
    );
  }
}

// ── Building blocks ─────────────────────────────────────────────────────

class _ReportHeader extends StatelessWidget {
  const _ReportHeader({required this.report, required this.metadata, required this.summary});
  final AiMedicalReportModel report;
  final Map<String, dynamic> metadata;
  final Map<String, dynamic> summary;

  @override
  Widget build(BuildContext context) {
    final isAi = summary['mode'] == 'gemini';
    return Card(
      margin: const EdgeInsets.only(bottom: 14),
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(16),
          gradient: LinearGradient(
            colors: [AppTheme.primaryColor.withValues(alpha: .16), AppTheme.secondaryColor.withValues(alpha: .10)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: AppTheme.primaryColor.withValues(alpha: .18),
                    borderRadius: BorderRadius.circular(AppTheme.radiusMedium),
                  ),
                  child: const Icon(Icons.local_hospital_rounded, color: AppTheme.primaryColor),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(AppConstants.appName, style: Theme.of(context).textTheme.titleMedium?.copyWith(color: AppTheme.primaryColor, fontWeight: FontWeight.w700)),
                      Text('Medical Report', style: Theme.of(context).textTheme.headlineSmall),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                StatusBadge(label: isAi ? 'AI-assisted summary' : 'Structured summary (non-AI)', color: AppTheme.secondaryColor),
                StatusBadge(label: 'Version ${report.versionNumber}', color: AppTheme.textSecondary),
              ],
            ),
            const SizedBox(height: 12),
            _InfoGrid([
              ('Report ID', _str(metadata['reportId']) ?? report.reportId),
              ('Generated', _date(metadata['generatedAt']) ?? _date(report.createdAt.toIso8601String())),
            ]),
          ],
        ),
      ),
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.number, required this.title, required this.icon, required this.child});
  final int number;
  final String title;
  final IconData icon;
  final Widget child;

  @override
  Widget build(BuildContext context) => Card(
        margin: const EdgeInsets.only(bottom: 12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryColor.withValues(alpha: .12),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(icon, size: 20, color: AppTheme.primaryColor),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text('$number. $title', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                  ),
                ],
              ),
              const Divider(height: 24),
              child,
            ],
          ),
        ),
      );
}

class _InfoGrid extends StatelessWidget {
  const _InfoGrid(this.rows, {this.columns, this.emptyText = 'Not recorded'});
  final List<(String, String?)> rows;
  final int? columns;
  final String emptyText;

  @override
  Widget build(BuildContext context) {
    final present = rows.where((row) => row.$2 != null).toList();
    if (present.isEmpty) return _Muted(emptyText);
    return LayoutBuilder(builder: (context, constraints) {
      final count = columns ?? (constraints.maxWidth >= 560 ? 2 : 1);
      final width = (constraints.maxWidth - (count - 1) * 16) / count;
      return Wrap(
        spacing: 16,
        runSpacing: 12,
        children: present
            .map((row) => SizedBox(
                  width: width,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(row.$1, style: Theme.of(context).textTheme.labelMedium),
                      const SizedBox(height: 3),
                      SelectableText(row.$2!, style: Theme.of(context).textTheme.bodyMedium?.copyWith(height: 1.35)),
                    ],
                  ),
                ))
            .toList(),
      );
    });
  }
}

class _TableRowData {
  const _TableRowData(this.cells, {this.detail, this.statusColumn});
  final List<String> cells;
  final String? detail;
  final int? statusColumn;
}

/// Table on wide screens; stacked label/value rows on phones.
class _DataTable extends StatelessWidget {
  const _DataTable({required this.columns, required this.rows, this.emptyText = 'Not recorded'});
  final List<String> columns;
  final List<_TableRowData> rows;
  final String emptyText;

  @override
  Widget build(BuildContext context) {
    if (rows.isEmpty) return _Muted(emptyText);
    final label = Theme.of(context).textTheme.labelMedium;
    final body = Theme.of(context).textTheme.bodyMedium?.copyWith(height: 1.3);
    Widget cell(_TableRowData row, int index) {
      final value = row.cells[index].isEmpty ? '—' : row.cells[index];
      return row.statusColumn == index && value != '—'
          ? Align(alignment: Alignment.centerLeft, child: StatusBadge(label: value, color: _statusColor(value)))
          : Text(value, style: body);
    }

    Widget detail(_TableRowData row) => row.detail == null || row.detail!.isEmpty
        ? const SizedBox.shrink()
        : Padding(padding: const EdgeInsets.only(top: 6), child: Text(row.detail!, style: Theme.of(context).textTheme.bodySmall?.copyWith(height: 1.35)));

    return LayoutBuilder(builder: (context, constraints) {
      final wide = constraints.maxWidth >= 560;
      return Container(
        decoration: BoxDecoration(
          color: AppTheme.surfaceColor,
          borderRadius: BorderRadius.circular(AppTheme.radiusMedium),
          border: Border.all(color: AppTheme.borderColor),
        ),
        child: Column(
          children: [
            if (wide)
              Padding(
                padding: const EdgeInsets.fromLTRB(12, 10, 12, 8),
                child: Row(children: [for (final c in columns) Expanded(child: Text(c, style: label))]),
              ),
            for (final (index, row) in rows.indexed)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                decoration: BoxDecoration(
                  border: index == 0 && !wide ? null : const Border(top: BorderSide(color: AppTheme.borderColor)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (wide)
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [for (var i = 0; i < columns.length; i++) Expanded(child: Padding(padding: const EdgeInsets.only(right: 8), child: cell(row, i)))],
                      )
                    else
                      for (var i = 0; i < columns.length; i++)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 4),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              SizedBox(width: 96, child: Text(columns[i], style: label)),
                              Expanded(child: cell(row, i)),
                            ],
                          ),
                        ),
                    detail(row),
                  ],
                ),
              ),
          ],
        ),
      );
    });
  }
}

class _MoreDetails extends StatelessWidget {
  const _MoreDetails({required this.details});
  final Map<String, dynamic> details;

  @override
  Widget build(BuildContext context) => Card(
        margin: const EdgeInsets.only(bottom: 12),
        clipBehavior: Clip.antiAlias,
        child: Theme(
          data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
          child: ExpansionTile(
            leading: const Icon(Icons.folder_open_outlined, color: AppTheme.primaryColor),
            title: const Text('More recorded details'),
            subtitle: Text('History, timeline, encounters and source references', style: Theme.of(context).textTheme.bodySmall),
            iconColor: AppTheme.textSecondary,
            collapsedIconColor: AppTheme.textSecondary,
            childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            expandedCrossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (final entry in details.entries) ...[
                _SubTitle(_label(entry.key)),
                _GenericValue(value: entry.value),
                const SizedBox(height: 12),
              ],
            ],
          ),
        ),
      );
}

class _ReportFooter extends StatelessWidget {
  const _ReportFooter({required this.report, required this.summary});
  final AiMedicalReportModel report;
  final Map<String, dynamic> summary;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(4, 8, 4, 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Generated by ${AppConstants.appName} with AI assistance. This report is a summary for information only and does not replace the advice of your doctor. Advice from your doctor always comes first.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(height: 1.45),
            ),
            const SizedBox(height: 6),
            Text(
              'Report version ${report.versionNumber} · ${_str(summary['label']) ?? 'Summary'} · ${DateFormat('MMM d, yyyy · h:mm a').format(report.createdAt.toLocal())}',
              style: Theme.of(context).textTheme.labelSmall,
            ),
          ],
        ),
      );
}

/// Dark-theme renderer for the less common stored fields (lists, nested maps, text).
class _GenericValue extends StatelessWidget {
  const _GenericValue({required this.value});
  final dynamic value;

  @override
  Widget build(BuildContext context) {
    final muted = Theme.of(context).textTheme.bodySmall;
    if (value == null || (value is String && (value as String).trim().isEmpty)) return Text('Not recorded', style: muted);
    if (value is List) {
      final items = value as List;
      if (items.isEmpty) return Text('None recorded', style: muted);
      if (items.every((item) => item is! Map && item is! List)) {
        return Text(items.map((item) => item?.toString() ?? '—').join(', '), style: Theme.of(context).textTheme.bodyMedium?.copyWith(height: 1.35));
      }
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: items
            .map((item) => Container(
                  width: double.infinity,
                  margin: const EdgeInsets.only(bottom: 6),
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: AppTheme.surfaceColor,
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: AppTheme.borderColor),
                  ),
                  child: _GenericValue(value: item),
                ))
            .toList(),
      );
    }
    if (value is Map) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: (value as Map)
            .entries
            .map((entry) => Padding(
                  padding: const EdgeInsets.only(bottom: 6),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(_label(entry.key.toString()), style: Theme.of(context).textTheme.labelMedium),
                      const SizedBox(height: 2),
                      _GenericValue(value: entry.value),
                    ],
                  ),
                ))
            .toList(),
      );
    }
    final text = value.toString();
    return Text(_date(text) != null && text.contains('T') ? _date(text)! : text, style: Theme.of(context).textTheme.bodyMedium?.copyWith(height: 1.35));
  }
}

class _SubTitle extends StatelessWidget {
  const _SubTitle(this.text, {this.padding = const EdgeInsets.only(bottom: 6)});
  final String text;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) => Padding(
        padding: padding,
        child: Text(text, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700, color: AppTheme.textPrimary)),
      );
}

class _Paragraph extends StatelessWidget {
  const _Paragraph(this.text);
  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: SelectableText(text.isEmpty ? 'Not recorded' : text, style: Theme.of(context).textTheme.bodyMedium?.copyWith(height: 1.45)),
      );
}

class _Muted extends StatelessWidget {
  const _Muted(this.text);
  final String text;

  @override
  Widget build(BuildContext context) => Text(text, style: Theme.of(context).textTheme.bodySmall?.copyWith(height: 1.4));
}

class _Bullets extends StatelessWidget {
  const _Bullets(this.items);
  final List<String> items;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: items
            .map((item) => Padding(
                  padding: const EdgeInsets.only(bottom: 5),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Padding(
                        padding: EdgeInsets.only(top: 7, right: 8),
                        child: Icon(Icons.circle, size: 6, color: AppTheme.primaryColor),
                      ),
                      Expanded(child: Text(item, style: Theme.of(context).textTheme.bodyMedium?.copyWith(height: 1.4))),
                    ],
                  ),
                ))
            .toList(),
      );
}

// ── Helpers ─────────────────────────────────────────────────────────────

Map<String, dynamic> _map(dynamic value) => value is Map ? Map<String, dynamic>.from(value) : <String, dynamic>{};

List<dynamic> _list(dynamic value) => value is List ? value : const [];

List<Map<String, dynamic>> _maps(dynamic value) => _list(value).whereType<Map>().map((m) => Map<String, dynamic>.from(m)).toList();

String? _str(dynamic value) {
  if (value == null) return null;
  final text = value.toString().trim();
  return text.isEmpty ? null : text;
}

String? _date(dynamic value, {bool withTime = true}) {
  final text = _str(value);
  if (text == null) return null;
  final parsed = DateTime.tryParse(text);
  if (parsed == null) return text;
  return DateFormat(withTime ? 'MMM d, yyyy · h:mm a' : 'MMM d, yyyy').format(parsed.toLocal());
}

/// "APositive" -> "A+", "ABNegative" -> "AB-"; other values are shown as stored.
String? _bloodGroup(String? value) {
  final match = value == null ? null : RegExp(r'^(A|B|AB|O)(Positive|Negative)$').firstMatch(value);
  return match == null ? value : '${match[1]}${match[2] == 'Positive' ? '+' : '-'}';
}

String? _humanize(String? value) => value?.replaceAllMapped(RegExp(r'(?<=[a-z])([A-Z])'), (m) => ' ${m[1]}');

String _label(String key) => key
    .replaceAllMapped(RegExp(r'([A-Z])'), (match) => ' ${match[1]}')
    .replaceFirstMapped(RegExp(r'^.'), (match) => match[0]!.toUpperCase());

Color _statusColor(String status) {
  final lower = status.toLowerCase();
  if (lower.contains('cancel') || lower.contains('discontinued') || lower.contains('noshow') || lower.contains('no show')) {
    return AppTheme.errorColor;
  }
  if (lower.contains('pending') || lower.contains('ordered') || lower.contains('progress') || lower.contains('scheduled') || lower.contains('waiting')) {
    return AppTheme.warningColor;
  }
  if (lower.contains('complete') || lower.contains('active') || lower.contains('confirmed') || lower.contains('admitted') || lower.contains('occupied')) {
    return AppTheme.successColor;
  }
  return AppTheme.primaryColor;
}
