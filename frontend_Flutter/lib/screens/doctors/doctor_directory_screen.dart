import 'dart:async';
import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
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
  bool _filtersExpanded = false;

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
      appBar: AppBar(
        title: const Text('Find a Doctor'),
        elevation: 0,
        actions: [
          IconButton(
            icon: Icon(
              _filtersExpanded ? Icons.filter_list_off : Icons.filter_list,
            ),
            onPressed: () =>
                setState(() => _filtersExpanded = !_filtersExpanded),
            tooltip: 'Toggle Filters',
          ),
        ],
      ),
      body: Column(
        children: [
          // Search & Filters Header
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            decoration: BoxDecoration(
              color: Theme.of(context).cardColor,
              boxShadow: [
                BoxShadow(
                  color: AppTheme.textPrimary.withValues(alpha: 0.05),
                  blurRadius: 4,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            child: Column(
              children: [
                AppTextField(
                  label: '',
                  hint: 'Search doctor by name...',
                  enabled: !availabilityOn,
                  onChanged: _onSearchChanged,
                  prefixIcon: const Icon(Icons.search),
                ),
                AnimatedCrossFade(
                  firstChild: const SizedBox(height: 0, width: double.infinity),
                  secondChild: Padding(
                    padding: const EdgeInsets.only(top: 12.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // Availability Toggle
                        Container(
                          decoration: BoxDecoration(
                            color: AppTheme.primaryColor.withValues(
                              alpha: 0.05,
                            ),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: SwitchListTile(
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 12,
                            ),
                            title: const Text(
                              'Filter by specific date',
                              style: TextStyle(fontWeight: FontWeight.w500),
                            ),
                            value: availabilityOn,
                            onChanged: (value) =>
                                provider.setAvailabilityFilterOn(value),
                          ),
                        ),
                        if (availabilityOn)
                          Padding(
                            padding: const EdgeInsets.symmetric(vertical: 8.0),
                            child: OutlinedButton.icon(
                              onPressed: _pickAvailabilityDate,
                              icon: const Icon(Icons.calendar_today, size: 18),
                              label: Text(
                                'Checking: ${DateFormat('MMM d, yyyy').format(provider.availabilityFilterDate)}',
                                style: TextStyle(fontWeight: FontWeight.bold),
                              ),
                            ),
                          ),

                        if (!availabilityOn) ...[
                          const SizedBox(height: 12),
                          Row(
                            children: [
                              Expanded(
                                child: FilterDropdown<int>(
                                  label: 'Department',
                                  value: provider.filterDepartmentId,
                                  items: [
                                    const DropdownMenuItem(
                                      value: null,
                                      child: Text('All Depts'),
                                    ),
                                    ...provider.departments.map(
                                      (d) => DropdownMenuItem(
                                        value: d.id,
                                        child: Text(d.name),
                                      ),
                                    ),
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
                                  label: 'Consultation',
                                  value: provider.filterConsultationTypeId,
                                  items: [
                                    const DropdownMenuItem(
                                      value: null,
                                      child: Text('All Types'),
                                    ),
                                    ...provider.consultationTypes.map(
                                      (c) => DropdownMenuItem(
                                        value: c.id,
                                        child: Text(c.name),
                                      ),
                                    ),
                                  ],
                                  onChanged: (value) {
                                    provider.filterConsultationTypeId = value;
                                    provider.loadDirectory();
                                  },
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          AppTextField(
                            label: '',
                            hint: 'Specialization (e.g. Cardiology)',
                            onChanged: _onSpecializationChanged,
                            prefixIcon: const Icon(
                              Icons.medical_services_outlined,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                  crossFadeState: _filtersExpanded
                      ? CrossFadeState.showSecond
                      : CrossFadeState.showFirst,
                  duration: const Duration(milliseconds: 250),
                ),
              ],
            ),
          ),

          // Main List View
          Expanded(
            child: RefreshIndicator(
              onRefresh: provider.loadDirectory,
              child: _buildListContent(provider),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildListContent(DoctorProvider provider) {
    if (provider.isLoadingDirectory && provider.directoryDoctors.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (provider.directoryError != null &&
        provider.directoryError!.isNotEmpty) {
      return SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: ErrorMessage(message: provider.directoryError),
        ),
      );
    }

    if (provider.directoryDoctors.isEmpty) {
      return SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        child: Container(
          padding: const EdgeInsets.all(32.0),
          alignment: Alignment.center,
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.search_off, size: 64, color: AppTheme.textMuted),
              const SizedBox(height: 16),
              Text(
                'No doctors found',
                style: TextStyle(
                  fontSize: AppTheme.fontHeadlineSmall,
                  color: AppTheme.textSecondary,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Try adjusting your search or filters to see more results.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppTheme.onPrimary),
              ),
            ],
          ),
        ),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      physics: const AlwaysScrollableScrollPhysics(),
      itemCount: provider.directoryDoctors.length,
      itemBuilder: (context, index) {
        final doctor = provider.directoryDoctors[index];
        return DoctorListTile(
          doctor: doctor,
          onTap: () => context.push('/doctors/${doctor.id}'),
        );
      },
    );
  }
}
