import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../widgets/app_button.dart';
import '../../widgets/error_message.dart';
import '../../providers/appointment_provider.dart';

class BookAppointmentScreen extends StatefulWidget {
  final int doctorId;
  final String slotStart;

  const BookAppointmentScreen({
    super.key,
    required this.doctorId,
    required this.slotStart,
  });

  @override
  State<BookAppointmentScreen> createState() => _BookAppointmentScreenState();
}

class _BookAppointmentScreenState extends State<BookAppointmentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _notesController = TextEditingController();

  int _appointmentType = 1;
  int _duration = 30;

  static const _types = [
    {'label': 'General', 'value': 1},
    {'label': 'Follow-Up', 'value': 2},
    {'label': 'Consultation', 'value': 3},
    {'label': 'Checkup', 'value': 4},
    {'label': 'Vaccination', 'value': 5},
    {'label': 'Procedure', 'value': 6},
  ];

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    context.read<AppointmentProvider>().clearActionError();

    final result = await context.read<AppointmentProvider>().bookAppointment(
          doctorId: widget.doctorId,
          appointmentType: _appointmentType,
          scheduledStart: widget.slotStart,
          estimatedDurationMinutes: _duration,
          priority: 1, // Patient always Normal; triage done at check-in by staff
          notes: _notesController.text.trim(),
        );

    if (!mounted) return;
    if (result != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Appointment booked successfully!')),
      );
      context.go('/appointments/${result.id}');
    }
  }

  @override
  Widget build(BuildContext context) {
    final slotDt = DateTime.parse(widget.slotStart).toLocal();
    final months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    final h = slotDt.hour > 12 ? slotDt.hour - 12 : (slotDt.hour == 0 ? 12 : slotDt.hour);
    final period = slotDt.hour >= 12 ? 'PM' : 'AM';
    final fmtSlot =
        '${slotDt.day} ${months[slotDt.month - 1]} ${slotDt.year}  $h:${slotDt.minute.toString().padLeft(2, '0')} $period';

    return Scaffold(
      appBar: AppBar(title: const Text('Book Appointment')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Slot summary
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: Colors.blue.shade50,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.blue.shade200),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.event, color: Colors.blue),
                    const SizedBox(width: 10),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Selected Slot',
                            style: TextStyle(
                                color: Colors.blue,
                                fontSize: 11,
                                fontWeight: FontWeight.w600)),
                        Text(fmtSlot,
                            style: const TextStyle(
                                fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 20),

              // Appointment Type
              const Text('Appointment Type',
                  style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              DropdownButtonFormField<int>(
                initialValue: _appointmentType,
                decoration: const InputDecoration(),
                items: _types
                    .map((t) => DropdownMenuItem<int>(
                          value: t['value'] as int,
                          child: Text(t['label'] as String),
                        ))
                    .toList(),
                onChanged: (v) => setState(() => _appointmentType = v!),
              ),

              const SizedBox(height: 16),

              // Duration
              const Text('Duration',
                  style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              DropdownButtonFormField<int>(
                initialValue: _duration,
                decoration: const InputDecoration(),
                items: const [
                  DropdownMenuItem(value: 15, child: Text('15 minutes')),
                  DropdownMenuItem(value: 30, child: Text('30 minutes')),
                  DropdownMenuItem(value: 45, child: Text('45 minutes')),
                  DropdownMenuItem(value: 60, child: Text('60 minutes')),
                ],
                onChanged: (v) => setState(() => _duration = v!),
              ),

              const SizedBox(height: 16),

              // Notes
              const Text('Notes (optional)',
                  style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _notesController,
                maxLines: 3,
                decoration: const InputDecoration(
                  hintText: 'Any details for the doctor...',
                ),
              ),

              const SizedBox(height: 24),

              Consumer<AppointmentProvider>(
                builder: (context, provider, _) {
                  return Column(
                    children: [
                      if (provider.actionError != null)
                        ErrorMessage(message: provider.actionError),
                      AppButton(
                        text: 'Confirm Booking',
                        isLoading: provider.actionLoading,
                        onPressed: _submit,
                      ),
                    ],
                  );
                },
              ),

              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }
}
