import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../models/appointments/doctor_summary_model.dart';
import '../../providers/appointment_provider.dart';
import '../../widgets/error_message.dart';
import '../../widgets/app_text_field.dart';

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
      backgroundColor: AppTheme.backgroundColor,
      appBar: AppBar(
        title: const Text('Find a Doctor'),
        elevation: 0,
        backgroundColor: AppTheme.surfaceColor,
        foregroundColor: AppTheme.textPrimary,
      ),
      body: Column(
        children: [
          Container(
            color: AppTheme.surfaceColor,
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
            child: AppTextField(
              label: 'Search doctors',
              hint: 'Search doctor by name...',
              controller: _controller,
              onChanged: _search,
              prefixIcon: const Icon(Icons.search),
            ),
          ),

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

          Expanded(
            child: Consumer<AppointmentProvider>(
              builder: (context, provider, _) {
                if (provider.doctorsLoading) {
                  return const Center(child: CircularProgressIndicator());
                }
                if (provider.doctorsError != null) {
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
                        ErrorMessage(message: provider.doctorsError),
                      ],
                    ),
                  );
                }
                if (provider.doctors.isEmpty) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.search_off,
                          size: 64,
                          color: AppTheme.borderColor,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'No doctors found.',
                          style: TextStyle(
                            fontSize: AppTheme.fontHeadlineSmall,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  );
                }
                return ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: provider.doctors.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
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
        onTap: () {
          if (doctor.userId != null) {
            context.push(
              '/appointments/doctor/${doctor.id}?userId=${doctor.userId}',
            );
          } else {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text('Not available for online booking.'),
              ),
            );
          }
        },
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
                child: Text(
                  doctor.firstName.isNotEmpty ? doctor.firstName[0] : 'D',
                  style: TextStyle(
                    color: AppTheme.primaryColor,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Dr. ${doctor.fullName}',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: AppTheme.fontTitleMedium,
                      ),
                    ),
                    if (doctor.specialization != null)
                      Text(
                        doctor.specialization!,
                        style: TextStyle(
                          color: AppTheme.textSecondary,
                          fontSize: AppTheme.fontBodyMedium,
                        ),
                      ),
                    if (doctor.department != null)
                      Text(
                        doctor.department!,
                        style: TextStyle(
                          color: AppTheme.primaryColor.withValues(alpha: 0.7),
                          fontSize: AppTheme.fontBodySmall,
                        ),
                      ),
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
