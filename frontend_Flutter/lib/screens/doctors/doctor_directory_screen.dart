import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../providers/doctor_provider.dart';
import '../../widgets/app_text_field.dart';
import '../../widgets/doctor_list_tile.dart';
import '../../widgets/error_message.dart';
import '../../widgets/filter_dropdown.dart';

class DoctorDirectoryScreen extends StatefulWidget {
  const DoctorDirectoryScreen({super.key});

  @override
  State<DoctorDirectoryScreen> createState() => _DoctorDirectoryScreenState();
}

class _DoctorDirectoryScreenState extends State<DoctorDirectoryScreen> {
  Timer? _debounce;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final provider = context.read<DoctorProvider>();
      provider.loadFilterOptions();
      provider.loadDirectory();
    });
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  void _onSearchChanged(String value) {
    context.read<DoctorProvider>().searchTerm = value;
    _scheduleReload();
  }

  void _onSpecializationChanged(String value) {
    context.read<DoctorProvider>().filterSpecialization = value.trim();
    _scheduleReload();
  }

  // Placeholder for a future feature: stores the value but doesn't filter or reload
  void _onReasonOfIllnessChanged(String value) {
    context.read<DoctorProvider>().reasonOfIllness = value.trim();
  }

  // Debounced so typing doesn't fire a request per keystroke
  void _scheduleReload() {
    final provider = context.read<DoctorProvider>();
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 350), () {
      if (mounted) provider.loadDirectory();
    });
  }

  Future<void> _pickAvailabilityDate() async {
    final provider = context.read<DoctorProvider>();
    final picked = await showDatePicker(
      context: context,
      initialDate: provider.availabilityFilterDate,
      firstDate: DateTime.now().subtract(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      provider.setAvailabilityFilterDate(picked);
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<DoctorProvider>();
    final availabilityOn = provider.availabilityFilterOn;

    return Scaffold(
      appBar: AppBar(title: const Text('Find a Doctor')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            AppTextField(
              label: 'Search',
              hint: 'Doctor name or ID',
              enabled: !availabilityOn,
              onChanged: _onSearchChanged,
            ),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Available on...'),
              value: availabilityOn,
              onChanged: (value) => provider.setAvailabilityFilterOn(value),
            ),
            if (availabilityOn)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: TextButton.icon(
                    onPressed: _pickAvailabilityDate,
                    icon: const Icon(Icons.calendar_today, size: 18),
                    label: Text(DateFormat('MMM d, yyyy').format(provider.availabilityFilterDate)),
                  ),
                ),
              ),
            Row(
              children: [
                Expanded(
                  child: FilterDropdown<int>(
                    label: 'Department',
                    value: provider.filterDepartmentId,
                    items: [
                      const DropdownMenuItem(value: null, child: Text('All')),
                      ...provider.departments.map((d) => DropdownMenuItem(value: d.id, child: Text(d.name))),
                    ],
                    onChanged: (value) {
                      provider.filterDepartmentId = value;
                      provider.loadDirectory();
                    },
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FilterDropdown<int>(
                    label: 'Consultation Type',
                    value: provider.filterConsultationTypeId,
                    items: [
                      const DropdownMenuItem(value: null, child: Text('All')),
                      ...provider.consultationTypes.map((c) => DropdownMenuItem(value: c.id, child: Text(c.name))),
                    ],
                    onChanged: (value) {
                      provider.filterConsultationTypeId = value;
                      provider.loadDirectory();
                    },
                  ),
                ),
              ],
            ),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: AppTextField(
                    label: 'Specialization',
                    hint: 'e.g. Cardiology',
                    enabled: !availabilityOn,
                    onChanged: _onSpecializationChanged,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: AppTextField(
                    label: 'Reason for Illness',
                    hint: 'e.g. Fever, chest pain',
                    onChanged: _onReasonOfIllnessChanged,
                  ),
                ),
              ],
            ),
            if (availabilityOn)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Text(
                  'Specialization and search aren\'t available when filtering by availability date.',
                  style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                ),
              )
            else
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: () => provider.loadDirectory(),
                  child: const Text('Apply filters'),
                ),
              ),
            ErrorMessage(message: provider.directoryError),
            Expanded(
              child: provider.isLoadingDirectory
                  ? const Center(child: CircularProgressIndicator())
                  : provider.directoryDoctors.isEmpty
                      ? const Center(child: Text('No doctors found.'))
                      : ListView.builder(
                          itemCount: provider.directoryDoctors.length,
                          itemBuilder: (context, index) {
                            final doctor = provider.directoryDoctors[index];
                            return DoctorListTile(
                              doctor: doctor,
                              onTap: () => context.push('/doctors/${doctor.id}'),
                            );
                          },
                        ),
            ),
          ],
        ),
      ),
    );
  }
}
