import '../../core/utils/api_datetime.dart';

class AppointmentSlotModel {
  final int doctorId;
  final int doctorProfileId;
  final String doctorName;
  final int? departmentId;
  final String? departmentName;
  final DateTime slotStart;
  final DateTime slotEnd;
  final String status;
  final int durationMinutes;

  const AppointmentSlotModel({
    required this.doctorId,
    required this.doctorProfileId,
    required this.doctorName,
    required this.slotStart,
    required this.slotEnd,
    required this.status,
    required this.durationMinutes,
    this.departmentId,
    this.departmentName,
  });

  factory AppointmentSlotModel.fromJson(Map<String, dynamic> json) {
    return AppointmentSlotModel(
      doctorId: (json['doctorId'] as num).toInt(),
      doctorProfileId: (json['doctorProfileId'] as num).toInt(),
      doctorName: json['doctorName'] as String? ?? '',
      departmentId: (json['departmentId'] as num?)?.toInt(),
      departmentName: json['departmentName'] as String?,
      slotStart: ApiDateTime.parseUtcToLocal(json['slotStart'] as String),
      slotEnd: ApiDateTime.parseUtcToLocal(json['slotEnd'] as String),
      status: json['status'] as String,
      durationMinutes: (json['durationMinutes'] as num).toInt(),
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
