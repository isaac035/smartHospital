class AppointmentHistoryModel {
  final int id;
  final String previousStatus;
  final String newStatus;
  final String? reason;
  final String changedByName;
  final DateTime changedAt;

  const AppointmentHistoryModel({
    required this.id,
    required this.previousStatus,
    required this.newStatus,
    this.reason,
    required this.changedByName,
    required this.changedAt,
  });

  factory AppointmentHistoryModel.fromJson(Map<String, dynamic> json) {
    return AppointmentHistoryModel(
      id: json['id'] as int,
      previousStatus: json['previousStatus'] as String? ?? '',
      newStatus: json['newStatus'] as String? ?? '',
      reason: json['reason'] as String?,
      changedByName: json['changedByName'] as String? ?? 'System',
      changedAt: DateTime.parse(json['changedAt'] as String),
    );
  }
}
