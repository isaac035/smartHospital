import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../../models/emr/timeline_event_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/emr_provider.dart';
import 'emr_widgets.dart';

class PatientClinicalHistoryScreen extends StatefulWidget {
  const PatientClinicalHistoryScreen({super.key});

  @override
  State<PatientClinicalHistoryScreen> createState() =>
      _PatientClinicalHistoryScreenState();
}

class _PatientClinicalHistoryScreenState
    extends State<PatientClinicalHistoryScreen> {
  String _selectedFilter = 'All';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadTimeline();
    });
  }

  void _loadTimeline({bool force = false}) {
    final user = context.read<AuthProvider>().currentUser;
    if (user != null) {
      context.read<EmrProvider>().loadTimeline(user.id, force: force);
    }
  }

  List<TimelineEventModel> _getFilteredEvents(List<TimelineEventModel> events) {
    if (_selectedFilter == 'All') return events;

    return events.where((e) {
      final type = e.eventType.toLowerCase();
      final cat = (e.category ?? '').toLowerCase();
      switch (_selectedFilter) {
        case 'Consultations':
          return type.contains('record') ||
              type.contains('consult') ||
              cat.contains('consult');
        case 'Vitals':
          return type.contains('vital') || cat.contains('vital');
        case 'Prescriptions':
          return type.contains('prescription') ||
              type.contains('medication') ||
              cat.contains('prescription');
        case 'Lab Tests':
          return type.contains('lab') || cat.contains('lab');
        default:
          return true;
      }
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;

    if (user == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Clinical History')),
        body: EmrErrorView(
          message: 'You must be logged in to view your clinical timeline.',
          onRetry: () => Navigator.of(context).pop(),
        ),
      );
    }

    final emr = context.watch<EmrProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Clinical History Timeline'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh Timeline',
            onPressed: () => _loadTimeline(force: true),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => _loadTimeline(force: true),
        child: Column(
          children: [
            // Filter Bar
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              color: AppTheme.surfaceColor,
              child: SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: Row(
                  children: [
                    _buildFilterChip('All'),
                    const SizedBox(width: 8),
                    _buildFilterChip('Consultations'),
                    const SizedBox(width: 8),
                    _buildFilterChip('Vitals'),
                    const SizedBox(width: 8),
                    _buildFilterChip('Prescriptions'),
                    const SizedBox(width: 8),
                    _buildFilterChip('Lab Tests'),
                  ],
                ),
              ),
            ),
            const Divider(height: 1),

            // Content Area
            Expanded(
              child: _buildTimelineContent(emr, user.id),
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
      backgroundColor: AppTheme.surfaceColor,
      labelStyle: TextStyle(
        color: isSelected ? Colors.white : AppTheme.textPrimary,
        fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
        fontSize: 13,
      ),
      side: BorderSide(
        color: isSelected ? AppTheme.primaryColor : AppTheme.borderColor,
      ),
    );
  }

  Widget _buildTimelineContent(EmrProvider emr, int patientId) {
    if (emr.timelineLoading && emr.timeline == null) {
      return const EmrLoadingView(message: 'Loading clinical timeline...');
    }

    if (emr.timelineError != null && emr.timeline == null) {
      return EmrErrorView(
        message: emr.timelineError!,
        onRetry: () => _loadTimeline(force: true),
      );
    }

    final events = emr.timeline?.events ?? [];
    final filtered = _getFilteredEvents(events);

    if (filtered.isEmpty) {
      return EmrEmptyState(
        icon: Icons.timeline,
        title: _selectedFilter == 'All'
            ? 'No Clinical History Yet'
            : 'No $_selectedFilter Recorded',
        subtitle: _selectedFilter == 'All'
            ? 'Your unified medical timeline including doctor visits, vitals, prescriptions, and lab tests will be chronicled here.'
            : 'No events matching "$_selectedFilter" were found in your timeline.',
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
      itemCount: filtered.length,
      itemBuilder: (context, index) {
        final event = filtered[index];
        final isLast = index == filtered.length - 1;
        return _buildTimelineItem(event, isLast);
      },
    );
  }

  Widget _buildTimelineItem(TimelineEventModel event, bool isLast) {
    final (IconData icon, Color color, String eventLabel) = _getEventStyling(event);
    final dateFormat = DateFormat('MMM dd, yyyy • hh:mm a');
    final formattedDate = dateFormat.format(event.eventDate);

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Timeline indicator with connector
          Column(
            children: [
              Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.15),
                  shape: BoxShape.circle,
                  border: Border.all(color: color, width: 2),
                ),
                child: Icon(icon, size: 20, color: color),
              ),
              if (!isLast)
                Expanded(
                  child: Container(
                    width: 2,
                    margin: const EdgeInsets.symmetric(vertical: 4),
                    color: AppTheme.borderColor,
                  ),
                ),
            ],
          ),
          const SizedBox(width: 14),

          // Event card
          Expanded(
            child: Container(
              margin: const EdgeInsets.only(bottom: 16),
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: AppTheme.neutralContainer,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppTheme.borderColor),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.03),
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
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: color.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          eventLabel,
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: color,
                          ),
                        ),
                      ),
                      Text(
                        formattedDate,
                        style: TextStyle(
                          fontSize: 11,
                          color: AppTheme.textSecondary,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(
                    event.summary,
                    style: const TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w600,
                      color: AppTheme.textPrimary,
                    ),
                  ),
                  if (event.doctorName != null &&
                      event.doctorName!.isNotEmpty) ...[
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        Icon(Icons.person_outline,
                            size: 14, color: AppTheme.textSecondary),
                        const SizedBox(width: 4),
                        Text(
                          event.doctorName!,
                          style: TextStyle(
                            fontSize: 12,
                            color: AppTheme.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ],
                  if (event.status != null && event.status!.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    EmrStatusBadge(status: event.status!),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  (IconData, Color, String) _getEventStyling(TimelineEventModel event) {
    final type = event.eventType.toLowerCase();
    final cat = (event.category ?? '').toLowerCase();

    if (type.contains('vital') || cat.contains('vital')) {
      return (Icons.favorite_rounded, AppTheme.errorColor, 'VITAL SIGNS');
    } else if (type.contains('prescription') || cat.contains('prescription')) {
      return (Icons.medication_rounded, AppTheme.primaryColor, 'PRESCRIPTION');
    } else if (type.contains('lab') || cat.contains('lab')) {
      return (Icons.biotech_rounded, AppTheme.accentColor, 'LAB ORDER / REPORT');
    } else if (type.contains('diagnosis')) {
      return (Icons.medical_information_rounded, AppTheme.warningColor, 'DIAGNOSIS');
    } else if (type.contains('treatment')) {
      return (Icons.healing_rounded, AppTheme.successColor, 'TREATMENT PLAN');
    } else {
      return (Icons.assignment_rounded, AppTheme.primaryColor, 'CONSULTATION');
    }
  }
}
