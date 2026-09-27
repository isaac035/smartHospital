import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_theme.dart';
import '../../core/utils/api_datetime.dart';
import '../../widgets/error_message.dart';
import '../../providers/appointment_provider.dart';
import '../../providers/auth_provider.dart';

class BookAppointmentScreen extends StatefulWidget {
  final int doctorId;
  final String slotStart;
  final int durationMinutes;

  const BookAppointmentScreen({
    super.key,
    required this.doctorId,
    required this.slotStart,
    required this.durationMinutes,
  });

  @override
  State<BookAppointmentScreen> createState() => _BookAppointmentScreenState();
}

class _BookAppointmentScreenState extends State<BookAppointmentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _notesController = TextEditingController();

  int _appointmentType = 1;

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    context.read<AppointmentProvider>().clearActionError();

    final user = context.read<AuthProvider>().currentUser;
    if (user == null) return;

    final result = await context.read<AppointmentProvider>().bookAppointment(
          patientId: user.id,
          doctorId: widget.doctorId,
          appointmentType: _appointmentType,
          scheduledStart: ApiDateTime.toUtcIso8601(
            ApiDateTime.parseUtcToLocal(widget.slotStart),
          ),
          estimatedDurationMinutes: widget.durationMinutes,
          priority: 1, // Patient always Normal; triage done at check-in by staff
          notes: _notesController.text.trim(),
        );

    if (!mounted) return;
    if (result != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Appointment booked successfully!'), backgroundColor: Colors.green),
      );
      context.go('/appointments/${result.id}');
    }
  }

  @override
  Widget build(BuildContext context) {
    final slotDt = ApiDateTime.parseUtcToLocal(widget.slotStart);
    final months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    final h = slotDt.hour > 12 ? slotDt.hour - 12 : (slotDt.hour == 0 ? 12 : slotDt.hour);
    final period = slotDt.hour >= 12 ? 'PM' : 'AM';
    final fmtSlot = '${slotDt.day} ${months[slotDt.month - 1]} ${slotDt.year}  $h:${slotDt.minute.toString().padLeft(2, '0')} $period';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Confirm Booking'),
        elevation: 0,
        backgroundColor: Colors.transparent,
        foregroundColor: Colors.black87,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Slot summary card
              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: AppTheme.primaryColor,
                  borderRadius: BorderRadius.circular(16),
                  boxShadow: [
                    BoxShadow(color: AppTheme.primaryColor.withValues(alpha: 0.3), blurRadius: 8, offset: const Offset(0, 4)),
                  ],
                ),
                child: Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.2), shape: BoxShape.circle),
                      child: const Icon(Icons.event_available, color: Colors.white, size: 28),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Selected Date & Time', style: TextStyle(color: Colors.white70, fontSize: 13, fontWeight: FontWeight.w500)),
                          const SizedBox(height: 4),
                          Text(fmtSlot, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 18)),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 32),
              
              const Text('Appointment Details', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              const SizedBox(height: 16),

              Text('Consultation length: ${widget.durationMinutes} minutes', style: const TextStyle(fontWeight: FontWeight.w600, color: Colors.black87)),

              const SizedBox(height: 20),

              // Notes
              const Text('Additional Notes (Optional)', style: TextStyle(fontWeight: FontWeight.w600, color: Colors.black87)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _notesController,
                maxLines: 4,
                decoration: InputDecoration(
                  hintText: 'Briefly describe your symptoms or reason for visit...',
                  contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide(color: Colors.grey.shade300)),
                  enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide(color: Colors.grey.shade300)),
                  filled: true,
                  fillColor: Colors.grey.shade50,
                ),
              ),

              const SizedBox(height: 32),

              Consumer<AppointmentProvider>(
                builder: (context, provider, _) {
                  return Column(
                    children: [
                      if (provider.actionError != null) ...[
                        ErrorMessage(message: provider.actionError),
                        const SizedBox(height: 16),
                      ],
                      SizedBox(
                        width: double.infinity,
                        height: 54,
                        child: ElevatedButton(
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppTheme.primaryColor,
                            foregroundColor: Colors.white,
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                            elevation: 2,
                          ),
                          onPressed: provider.actionLoading ? null : _submit,
                          child: provider.actionLoading 
                              ? const SizedBox(height: 24, width: 24, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                              : const Text('Confirm Booking', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                        ),
                      ),
                    ],
                  );
                },
              ),
              const SizedBox(height: 40),
            ],
          ),
        ),
      ),
    );
  }
}
