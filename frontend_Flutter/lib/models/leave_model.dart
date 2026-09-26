class Leave {
  final int id;
  final int doctorId;
  final String doctorName;
  final DateTime startDate;
  final DateTime endDate;
  final String reason;
  final String status;

  Leave({
    required this.id,
    required this.doctorId,
    required this.doctorName,
    required this.startDate,
    required this.endDate,
    required this.reason,
    required this.status,
  });

  factory Leave.fromJson(Map<String, dynamic> json) {
    return Leave(
      id: json['id'] ?? 0,
      doctorId: json['doctorId'] ?? 0,
      doctorName: json['doctorName'] ?? '',
      startDate: DateTime.parse(json['startDate']),
      endDate: DateTime.parse(json['endDate']),
      reason: json['reason'] ?? '',
      status: json['status'] ?? '',
    );
  }
}
