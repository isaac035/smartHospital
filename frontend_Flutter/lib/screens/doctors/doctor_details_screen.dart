import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/theme/app_theme.dart';
import '../../providers/doctor_provider.dart';
import '../../widgets/error_message.dart';

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
      appBar: AppBar(title: const Text('Doctor Details')),
      body: provider.isLoadingDoctor
          ? const Center(child: CircularProgressIndicator())
          : doctor == null
              ? Center(child: ErrorMessage(message: provider.doctorError ?? 'Doctor not found.'))
              : SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      ErrorMessage(message: provider.doctorError),
                      Center(
                        child: Column(
                          children: [
                            const CircleAvatar(
                              radius: 44,
                              backgroundColor: AppTheme.primaryColor,
                              child: Icon(Icons.person, size: 44, color: Colors.white),
                            ),
                            const SizedBox(height: 12),
                            Text(
                              'Dr. ${doctor.fullName}',
                              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              doctor.specialization,
                              style: TextStyle(fontSize: 15, color: Colors.grey.shade700),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 24),
                      _infoRow('Department', doctor.departmentName),
                      _infoRow('Experience', '${doctor.yearsOfExperience} years'),
                      _infoRow('Status', doctor.status),
                      if (doctor.bio.isNotEmpty) _infoRow('About', doctor.bio),
                      const SizedBox(height: 16),
                      const Text(
                        'Consultation Types',
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                      ),
                      const SizedBox(height: 8),
                      provider.selectedDoctorConsultationTypes.isEmpty
                          ? Text(
                              'No consultation types published yet.',
                              style: TextStyle(color: Colors.grey.shade600),
                            )
                          : Wrap(
                              spacing: 8,
                              runSpacing: 8,
                              children: provider.selectedDoctorConsultationTypes
                                  .map((name) => Chip(label: Text(name)))
                                  .toList(),
                            ),
                      const SizedBox(height: 32),
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              icon: const Icon(Icons.calendar_month),
                              label: const Text('View Schedule'),
                              onPressed: () => context.push('/doctors/${doctor.id}/schedule'),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: ElevatedButton.icon(
                              icon: const Icon(Icons.event_available),
                              label: const Text('Check Availability'),
                              onPressed: () => context.push('/doctors/${doctor.id}/availability'),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
    );
  }

  Widget _infoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 110, child: Text(label, style: TextStyle(color: Colors.grey.shade600))),
          Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w500))),
        ],
      ),
    );
  }
}
