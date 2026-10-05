import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/theme/app_theme.dart';

/// Navigation shell for main patient-facing screens that hosts
/// a persistent, modern bottom navigation bar using StatefulNavigationShell.
class PatientNavigationShell extends StatelessWidget {
  const PatientNavigationShell({
    super.key,
    required this.navigationShell,
  });

  final StatefulNavigationShell navigationShell;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundColor,
      resizeToAvoidBottomInset: false,
      body: navigationShell,
      bottomNavigationBar: PatientBottomNavigationBar(
        selectedIndex: navigationShell.currentIndex,
        onItemTapped: (index) {
          navigationShell.goBranch(
            index,
            initialLocation: index == navigationShell.currentIndex,
          );
        },
      ),
    );
  }
}

/// A modern, responsive bottom navigation bar with 5 primary sections:
/// Home, Appointments, Doctors, Admissions, and Records.
class PatientBottomNavigationBar extends StatelessWidget {
  const PatientBottomNavigationBar({
    super.key,
    required this.selectedIndex,
    required this.onItemTapped,
  });

  final int selectedIndex;
  final ValueChanged<int> onItemTapped;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppTheme.surfaceColor,
        border: const Border(
          top: BorderSide(color: AppTheme.borderColor, width: 1),
        ),
        boxShadow: [
          BoxShadow(
            color: AppTheme.primaryColor.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: SizedBox(
          height: 58,
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: AppTheme.pageMaxWidth),
              child: Row(
                children: [
                  _buildNavItem(
                    context: context,
                    index: 0,
                    label: 'Home',
                    icon: Icons.home_outlined,
                    selectedIcon: Icons.home_rounded,
                    isSelected: selectedIndex == 0,
                  ),
                  _buildNavItem(
                    context: context,
                    index: 1,
                    label: 'Appointments',
                    icon: Icons.event_available_outlined,
                    selectedIcon: Icons.event_available_rounded,
                    isSelected: selectedIndex == 1,
                  ),
                  _buildNavItem(
                    context: context,
                    index: 2,
                    label: 'Doctors',
                    icon: Icons.medical_services_outlined,
                    selectedIcon: Icons.medical_services_rounded,
                    isSelected: selectedIndex == 2,
                  ),
                  _buildNavItem(
                    context: context,
                    index: 3,
                    label: 'Admissions',
                    icon: Icons.bed_outlined,
                    selectedIcon: Icons.bed_rounded,
                    isSelected: selectedIndex == 3,
                  ),
                  _buildNavItem(
                    context: context,
                    index: 4,
                    label: 'Records',
                    icon: Icons.monitor_heart_outlined,
                    selectedIcon: Icons.monitor_heart_rounded,
                    isSelected: selectedIndex == 4,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildNavItem({
    required BuildContext context,
    required int index,
    required String label,
    required IconData icon,
    required IconData selectedIcon,
    required bool isSelected,
  }) {
    const activeColor = AppTheme.secondaryColor;
    const inactiveColor = AppTheme.textSecondary;

    return Expanded(
      child: Semantics(
        button: true,
        selected: isSelected,
        label: '$label tab',
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: () => onItemTapped(index),
            borderRadius: BorderRadius.circular(12),
            splashColor: AppTheme.accentContainer,
            highlightColor: Colors.transparent,
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  AnimatedContainer(
                    duration: const Duration(milliseconds: 200),
                    curve: Curves.easeInOut,
                    padding: const EdgeInsets.symmetric(
                      horizontal: 14,
                      vertical: 3,
                    ),
                    decoration: BoxDecoration(
                      color: isSelected
                          ? AppTheme.accentContainer
                          : Colors.transparent,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: Icon(
                      isSelected ? selectedIcon : icon,
                      size: 22,
                      color: isSelected ? activeColor : inactiveColor,
                    ),
                  ),
                  const SizedBox(height: 2),
                  FittedBox(
                    fit: BoxFit.scaleDown,
                    child: Text(
                      label,
                      maxLines: 1,
                      style: TextStyle(
                        fontSize: 11,
                        height: 1.15,
                        fontWeight:
                            isSelected ? FontWeight.w700 : FontWeight.w500,
                        color: isSelected ? activeColor : inactiveColor,
                        letterSpacing: 0.1,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
