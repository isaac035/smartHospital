import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/theme/app_theme.dart';
import '../../models/doctor_model.dart';
import '../../providers/doctor_provider.dart';
import '../../widgets/error_message.dart';
import '../../widgets/status_chip.dart';

class DoctorDetailsScreen extends StatefulWidget {
  final int doctorId;

  const DoctorDetailsScreen({super.key, required this.doctorId});

  @override
  State<DoctorDetailsScreen> createState() => _DoctorDetailsScreenState();
}

class _DoctorDetailsScreenState extends State<DoctorDetailsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      context.read<DoctorProvider>().loadDoctorDetails(widget.doctorId);
    });
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<DoctorProvider>();
    final doctor = provider.selectedDoctor;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Doctor Profile'),
        elevation: 0,
        backgroundColor: Colors.transparent,
        foregroundColor: Colors.black87,
      ),
      bottomNavigationBar: doctor != null && !provider.isLoadingDoctor ? _buildBottomActions(context, doctor) : null,
      body: provider.isLoadingDoctor
          ? const Center(child: CircularProgressIndicator())
          : doctor == null
              ? Center(child: ErrorMessage(message: provider.doctorError ?? 'Doctor not found.'))
              : RefreshIndicator(
                  onRefresh: () => provider.loadDoctorDetails(widget.doctorId),
                  child: SingleChildScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (provider.doctorError != null) ErrorMessage(message: provider.doctorError),
                        
                        // Header Card
                        Card(
                          elevation: 0,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                            side: BorderSide(color: Colors.grey.shade200),
                          ),
                          child: Padding(
                            padding: const EdgeInsets.all(24),
                            child: Column(
                              children: [
                                const CircleAvatar(
                                  radius: 50,
                                  backgroundColor: AppTheme.primaryColor,
                                  child: Icon(Icons.person, size: 50, color: Colors.white),
                                ),
                                const SizedBox(height: 16),
                                Text(
                                  'Dr. ${doctor.fullName}',
                                  style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
                                  textAlign: TextAlign.center,
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  doctor.specialization,
                                  style: TextStyle(fontSize: 16, color: AppTheme.primaryColor, fontWeight: FontWeight.w500),
                                ),
                                const SizedBox(height: 8),
                                StatusChip(
                                  state: doctor.status.toLowerCase() == 'active' || doctor.status.toLowerCase() == 'available'
                                      ? DoctorAvailabilityState.available
                                      : doctor.status.toLowerCase() == 'on leave'
                                          ? DoctorAvailabilityState.onLeave
                                          : DoctorAvailabilityState.unavailable,
                                ),
                              ],
                            ),
                          ),
                        ),
                        
                        const SizedBox(height: 20),
                        
                        // Information Section
                        const Text(
                          'Professional Information',
                          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 12),
                        Card(
                          elevation: 0,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                            side: BorderSide(color: Colors.grey.shade200),
                          ),
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              children: [
                                _infoRow(Icons.local_hospital, 'Department', doctor.departmentName),
                                const Divider(height: 24),
                                _infoRow(Icons.work_history, 'Experience', '${doctor.yearsOfExperience} years'),
                                const Divider(height: 24),
                                _infoRow(Icons.badge, 'License Number', doctor.licenseNumber.isNotEmpty ? doctor.licenseNumber : 'N/A'),
                              ],
                            ),
                          ),
                        ),
                        
                        if (doctor.bio.isNotEmpty) ...[
                          const SizedBox(height: 20),
                          const Text(
                            'About',
                            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            doctor.bio,
                            style: TextStyle(fontSize: 15, color: Colors.grey.shade700, height: 1.5),
                          ),
                        ],
                        
                        const SizedBox(height: 20),
                        
                        // Consultation Types
                        const Text(
                          'Available Consultation Types',
                          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 12),
                        provider.selectedDoctorConsultationTypes.isEmpty
                            ? Text(
                                'No specific consultation types published.',
                                style: TextStyle(color: Colors.grey.shade600, fontStyle: FontStyle.italic),
                              )
                            : Wrap(
                                spacing: 8,
                                runSpacing: 8,
                                children: provider.selectedDoctorConsultationTypes
                                    .map((name) => Chip(
                                          label: Text(name),
                                          backgroundColor: Colors.blue.shade50,
                                          side: BorderSide.none,
                                        ))
                                    .toList(),
                              ),
                        
                        // Bottom padding for the absolute bottom bar
                        const SizedBox(height: 40),
                      ],
                    ),
                  ),
                ),
    );
  }

  Widget _infoRow(IconData icon, String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 20, color: Colors.grey.shade500),
        const SizedBox(width: 12),
        SizedBox(width: 120, child: Text(label, style: TextStyle(color: Colors.grey.shade600, fontSize: 15))),
        Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 15))),
      ],
    );
  }

  Widget _buildBottomActions(BuildContext context, Doctor doctor) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            offset: const Offset(0, -4),
            blurRadius: 8,
          ),
        ],
      ),
      child: SafeArea(
        child: Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                icon: const Icon(Icons.calendar_month),
                label: const Text('View Schedule'),
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                onPressed: () => context.push('/doctors/${doctor.id}/schedule'),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: ElevatedButton.icon(
                icon: const Icon(Icons.event_available),
                label: const Text('Book Appointment'),
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  backgroundColor: AppTheme.primaryColor,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                onPressed: () {
                  if (doctor.userId != null) {
                    context.push('/appointments/doctor/${doctor.userId}');
                  } else {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('This doctor is not available for online booking.')),
                    );
                  }
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

