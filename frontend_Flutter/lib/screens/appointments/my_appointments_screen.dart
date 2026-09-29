import 'dart:async';
import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/utils/hospital_date.dart';
import '../../models/appointments/appointment_model.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/error_message.dart';
import 'widgets/appointment_card.dart';

class MyAppointmentsScreen extends StatefulWidget {
  const MyAppointmentsScreen({super.key});

  @override
  State<MyAppointmentsScreen> createState() => _MyAppointmentsScreenState();
}

class _MyAppointmentsScreenState extends State<MyAppointmentsScreen> {
  String? _selectedStatus; // null = all
  int? _checkingAppointmentId;
  Timer? _appointmentRefreshTimer;
  bool _silentRefreshInProgress = false;

  static const _filters = [
    {'label': 'All', 'value': null},
    {'label': 'Upcoming', 'value': '1'},
    {'label': 'Confirmed', 'value': '2'},
    {'label': 'Completed', 'value': '5'},
    {'label': 'Cancelled', 'value': '6'},
  ];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AppointmentProvider>().loadMyAppointments();
    });
    // Flutter's appointment list has no SignalR client. Poll silently while it
    // is open so confirmations made by staff appear without manual refresh.
    _appointmentRefreshTimer = Timer.periodic(const Duration(seconds: 5), (_) {
      if (!mounted || _silentRefreshInProgress) return;
      _silentRefreshInProgress = true;
      context
          .read<AppointmentProvider>()
          .loadMyAppointments(statusFilter: _selectedStatus, showLoading: false)
          .whenComplete(() => _silentRefreshInProgress = false);
    });
  }

  @override
  void dispose() {
    _appointmentRefreshTimer?.cancel();
    super.dispose();
  }

  Future<void> _refresh() async {
    await context.read<AppointmentProvider>().loadMyAppointments(
      statusFilter: _selectedStatus,
    );
  }

  Future<void> _checkIn(AppointmentModel appointment) async {
    final doctorId = appointment.doctorId;
    if (doctorId == null) return;

    setState(() => _checkingAppointmentId = appointment.id);
    final provider = context.read<AppointmentProvider>();
    final entry = await provider.checkIn(appointment.id, doctorId);
    if (!mounted) return;
    setState(() => _checkingAppointmentId = null);

    if (entry == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            provider.queueError ?? 'Check-in failed. Please try again.',
          ),
        ),
      );
      return;
    }

    final position = entry.position > 0 ? ' · Position ${entry.position}' : '';
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Check-in complete · ${entry.queueCode}$position'),
        action: SnackBarAction(
          label: 'VIEW QUEUE',
          onPressed: () => context.push('/queue'),
        ),
      ),
    );
    await _refresh();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(
        title: const Text('My Appointments'),
        elevation: 0,
        backgroundColor: AppTheme.surfaceColor,
        foregroundColor: AppTheme.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(
              Icons.add_circle_outline,
              size: 26,
              color: AppTheme.primaryColor,
            ),
            tooltip: 'Book Appointment',
            onPressed: () => context.push('/appointments/search'),
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Filter chips
          Container(
            color: AppTheme.surfaceColor,
            height: 60,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              separatorBuilder: (_, _) => const SizedBox(width: 8),
              itemCount: _filters.length,
              itemBuilder: (context, index) {
                final f = _filters[index];
                final selected = _selectedStatus == f['value'];
                return ChoiceChip(
                  label: Text(f['label'] as String),
                  selected: selected,
                  selectedColor: AppTheme.infoContainer,
                  labelStyle: TextStyle(
                    color: selected
                        ? AppTheme.primaryColor
                        : AppTheme.textSecondary,
                    fontWeight: selected ? FontWeight.bold : FontWeight.normal,
                  ),
                  backgroundColor: AppTheme.neutralContainer,
                  side: BorderSide.none,
                  onSelected: (bool isSelected) {
                    setState(
                      () => _selectedStatus = isSelected ? f['value'] : null,
                    );
                    context.read<AppointmentProvider>().loadMyAppointments(
                      statusFilter: _selectedStatus,
                    );
                  },
                );
              },
            ),
          ),

          // Shadow under filters
          Container(
            height: 1,
            decoration: BoxDecoration(
              boxShadow: [
                BoxShadow(
                  color: AppTheme.textPrimary.withValues(alpha: 0.05),
                  blurRadius: 4,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
          ),

          // Content
          Expanded(
            child: Consumer<AppointmentProvider>(
              builder: (context, provider, _) {
                if (provider.appointmentsLoading) {
                  return const Center(child: CircularProgressIndicator());
                }
                if (provider.appointmentsError != null) {
                  return Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(
                          Icons.error_outline,
                          size: 48,
                          color: AppTheme.errorColor,
                        ),
                        const SizedBox(height: 16),
                        ErrorMessage(message: provider.appointmentsError),
                        const SizedBox(height: 16),
                        ElevatedButton.icon(
                          onPressed: _refresh,
                          icon: const Icon(Icons.refresh),
                          label: const Text('Retry'),
                        ),
                      ],
                    ),
                  );
                }
                if (provider.appointments.isEmpty) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.event_note,
                          size: 80,
                          color: AppTheme.borderColor,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'No appointments found.',
                          style: TextStyle(
                            fontSize: AppTheme.fontHeadlineSmall,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.textSecondary,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'You have no ${_selectedStatus == '1'
                              ? 'upcoming '
                              : _selectedStatus == '5'
                              ? 'completed '
                              : ''}appointments.',
                          style: TextStyle(color: AppTheme.onPrimary),
                        ),
                        const SizedBox(height: 24),
                        ElevatedButton.icon(
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppTheme.primaryColor,
                            foregroundColor: AppTheme.surfaceColor,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 24,
                              vertical: 12,
                            ),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(8),
                            ),
                          ),
                          icon: const Icon(Icons.add),
                          label: const Text(
                            'Book a New Appointment',
                            style: TextStyle(
                              fontSize: AppTheme.fontTitleMedium,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          onPressed: () => context.push('/appointments/search'),
                        ),
                      ],
                    ),
                  );
                }
                return RefreshIndicator(
                  onRefresh: _refresh,
                  child: ListView.builder(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 12,
                    ),
                    itemCount: provider.appointments.length,
                    itemBuilder: (context, index) {
                      final appointment = provider.appointments[index];
                      final canCheckIn =
                          (appointment.status == 1 ||
                              appointment.status == 2) &&
                          HospitalDate.isToday(appointment.scheduledStart) &&
                          appointment.doctorId != null;
                      return AppointmentCard(
                        appointment: appointment,
                        onCheckIn: canCheckIn
                            ? () => _checkIn(appointment)
                            : null,
                        checkingIn: _checkingAppointmentId == appointment.id,
                      );
                    },
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
