import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../models/appointments/appointment_slot_model.dart';

class SlotPickerGrid extends StatelessWidget {
  final List<AppointmentSlotModel> slots;
  final AppointmentSlotModel? selectedSlot;
  final ValueChanged<AppointmentSlotModel> onSlotSelected;
  final bool isLoading;

  const SlotPickerGrid({
    super.key,
    required this.slots,
    required this.selectedSlot,
    required this.onSlotSelected,
    this.isLoading = false,
  });

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: CircularProgressIndicator(),
        ),
      );
    }
    if (slots.isEmpty) {
      return Container(
        padding: const EdgeInsets.all(20),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: Colors.grey.shade100,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Text(
          'No available slots for this date.',
          style: TextStyle(color: Colors.grey.shade600),
        ),
      );
    }

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3,
        mainAxisSpacing: 8,
        crossAxisSpacing: 8,
        childAspectRatio: 2.5,
      ),
      itemCount: slots.length,
      itemBuilder: (context, index) {
        final slot = slots[index];
        final isSelected = selectedSlot?.slotStart == slot.slotStart;
        return GestureDetector(
          onTap: () => onSlotSelected(slot),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: isSelected ? AppTheme.primaryColor : Colors.white,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: isSelected ? AppTheme.primaryColor : Colors.grey.shade300,
              ),
            ),
            child: Text(
              slot.formattedTime,
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: isSelected ? Colors.white : AppTheme.primaryColor,
              ),
            ),
          ),
        );
      },
    );
  }
}
