import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_theme.dart';
import '../../models/appointments/doctor_summary_model.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/error_message.dart';

class SearchDoctorsScreen extends StatefulWidget {
  const SearchDoctorsScreen({super.key});

  @override
  State<SearchDoctorsScreen> createState() => _SearchDoctorsScreenState();
}

class _SearchDoctorsScreenState extends State<SearchDoctorsScreen> {
  final _controller = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AppointmentProvider>().searchDoctors('');
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _search(String query) {
    context.read<AppointmentProvider>().searchDoctors(query);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Find a Doctor')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: TextField(
              controller: _controller,
              onChanged: _search,
              decoration: const InputDecoration(
                hintText: 'Search by name or specialization...',
                prefixIcon: Icon(Icons.search),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: Consumer<AppointmentProvider>(
              builder: (context, provider, _) {
                if (provider.doctorsLoading) {
                  return const Center(child: CircularProgressIndicator());
                }
                if (provider.doctorsError != null) {
                  return Padding(
                    padding: const EdgeInsets.all(16),
                    child: ErrorMessage(message: provider.doctorsError),
                  );
                }
                if (provider.doctors.isEmpty) {
                  return Center(
                    child: Text(
                      'No doctors found.',
                      style: TextStyle(color: Colors.grey.shade600),
                    ),
                  );
                }
                return ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: provider.doctors.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 8),
                  itemBuilder: (context, index) {
                    return _DoctorTile(doctor: provider.doctors[index]);
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _DoctorTile extends StatelessWidget {
  final DoctorSummaryModel doctor;

  const _DoctorTile({required this.doctor});

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 1,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.push('/appointments/doctor/${doctor.id}'),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
                child: Text(
                  doctor.firstName.isNotEmpty ? doctor.firstName[0] : 'D',
                  style: const TextStyle(
                      color: AppTheme.primaryColor,
                      fontWeight: FontWeight.bold),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Dr. ${doctor.fullName}',
                      style: const TextStyle(
                          fontWeight: FontWeight.bold, fontSize: 15),
                    ),
                    if (doctor.specialization != null)
                      Text(doctor.specialization!,
                          style: TextStyle(
                              color: Colors.grey.shade600, fontSize: 13)),
                    if (doctor.department != null)
                      Text(doctor.department!,
                          style: TextStyle(
                              color: AppTheme.primaryColor.withValues(alpha: 0.7),
                              fontSize: 12)),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right, color: AppTheme.primaryColor),
            ],
          ),
        ),
      ),
    );
  }
}
