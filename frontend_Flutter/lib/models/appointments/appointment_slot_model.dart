class AppointmentSlotModel {
  final DateTime slotStart;

  const AppointmentSlotModel({required this.slotStart});

  factory AppointmentSlotModel.fromJson(Map<String, dynamic> json) {
    return AppointmentSlotModel(
      slotStart: DateTime.parse(json['slotStart'] as String),
    );
  }

  String get formattedTime {
    final h = slotStart.hour;
    final m = slotStart.minute.toString().padLeft(2, '0');
    final period = h >= 12 ? 'PM' : 'AM';
    final hour = h > 12 ? h - 12 : (h == 0 ? 12 : h);
    return '$hour:$m $period';
  }
}
