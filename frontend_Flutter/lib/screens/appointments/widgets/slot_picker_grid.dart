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
        final isAvailable = slot.isAvailable;
        return GestureDetector(
          onTap: isAvailable ? () => onSlotSelected(slot) : null,
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: isSelected ? AppTheme.primaryColor : (isAvailable ? Colors.white : Colors.grey.shade100),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: isSelected ? AppTheme.primaryColor : (isAvailable ? Colors.grey.shade300 : Colors.orange.shade300),
              ),
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  slot.formattedTime,
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: isSelected ? Colors.white : (isAvailable ? AppTheme.primaryColor : Colors.grey.shade500),
                  ),
                ),
                if (!isAvailable)
                  Text(slot.status, style: TextStyle(fontSize: 10, color: Colors.grey.shade600)),
              ],
            ),
          ),
        );
      },
    );
  }
}
