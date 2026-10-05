class Schedule {
  final int id;
  final int doctorId;
  final String doctorName;
  final int consultationTypeId;
  final String consultationTypeName;
  final String dayOfWeek;
  final String startTime;
  final String endTime;
  final String status;

  Schedule({
    required this.id,
    required this.doctorId,
    required this.doctorName,
    required this.consultationTypeId,
    required this.consultationTypeName,
    required this.dayOfWeek,
    required this.startTime,
    required this.endTime,
    required this.status,
  });

  factory Schedule.fromJson(Map<String, dynamic> json) {
    return Schedule(
      id: json['id'] ?? 0,
      doctorId: json['doctorId'] ?? 0,
      doctorName: json['doctorName'] ?? '',
      consultationTypeId: json['consultationTypeId'] ?? 0,
      consultationTypeName: json['consultationTypeName'] ?? '',
      dayOfWeek: json['dayOfWeek'] ?? '',
      startTime: json['startTime'] ?? '',
      endTime: json['endTime'] ?? '',
      status: json['status'] ?? '',
    );
  }
}
