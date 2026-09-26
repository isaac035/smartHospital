import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
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

  static const _filters = [
    {'label': 'All', 'value': null},
    {'label': 'Upcoming', 'value': 'Scheduled'},
    {'label': 'Confirmed', 'value': 'Confirmed'},
    {'label': 'Completed', 'value': 'Completed'},
    {'label': 'Cancelled', 'value': 'Cancelled'},
  ];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AppointmentProvider>().loadMyAppointments();
    });
  }

  Future<void> _refresh() async {
    await context
        .read<AppointmentProvider>()
        .loadMyAppointments(statusFilter: _selectedStatus);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Appointments'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Book Appointment',
            onPressed: () => context.push('/appointments/search'),
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter chips
          SizedBox(
            height: 52,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              separatorBuilder: (_, __) => const SizedBox(width: 8),
              itemCount: _filters.length,
              itemBuilder: (context, index) {
                final f = _filters[index];
                final selected = _selectedStatus == f['value'];
                return FilterChip(
                  label: Text(f['label'] as String),
                  selected: selected,
                  onSelected: (_) {
                    setState(() => _selectedStatus = f['value'] as String?);
                    context.read<AppointmentProvider>().loadMyAppointments(
                        statusFilter: f['value'] as String?);
                  },
                );
              },
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
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        ErrorMessage(message: provider.appointmentsError),
                        const SizedBox(height: 12),
                        ElevatedButton(
                          onPressed: _refresh,
                          child: const Text('Retry'),
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
                        Icon(Icons.calendar_today,
                            size: 64, color: Colors.grey.shade400),
                        const SizedBox(height: 16),
                        Text(
                          'No appointments found.',
                          style: TextStyle(color: Colors.grey.shade600),
                        ),
                        const SizedBox(height: 12),
                        ElevatedButton.icon(
                          icon: const Icon(Icons.add),
                          label: const Text('Book Appointment'),
                          onPressed: () => context.push('/appointments/search'),
                        ),
                      ],
                    ),
                  );
                }
                return RefreshIndicator(
                  onRefresh: _refresh,
                  child: ListView.builder(
                    padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
                    itemCount: provider.appointments.length,
                    itemBuilder: (context, index) {
                      return AppointmentCard(
                          appointment: provider.appointments[index]);
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
