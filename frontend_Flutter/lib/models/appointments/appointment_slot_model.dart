import '../../core/utils/api_datetime.dart';

class AppointmentSlotModel {
  final DateTime slotStart;
  final String status;
  final int durationMinutes;

  const AppointmentSlotModel({required this.slotStart, this.status = 'Available', this.durationMinutes = 30});

  factory AppointmentSlotModel.fromJson(Map<String, dynamic> json) {
    return AppointmentSlotModel(
      slotStart: ApiDateTime.parseUtcToLocal(json['slotStart'] as String),
      status: json['status'] as String? ?? 'Available',
      durationMinutes: json['durationMinutes'] as int? ?? 30,
    );
  }

  bool get isAvailable => status == 'Available';

  String get formattedTime {
    final h = slotStart.hour;
    final m = slotStart.minute.toString().padLeft(2, '0');
    final period = h >= 12 ? 'PM' : 'AM';
    final hour = h > 12 ? h - 12 : (h == 0 ? 12 : h);
    return '$hour:$m $period';
  }
}
