class QueueEntryModel {
  final int id;
  final int queueNumber;
  final String patientName;
  final int patientId;
  final int position;
  final int estimatedWaitMinutes;
  final int status; // 1=Waiting, 2=Called, 3=InProgress, 4=Completed, 5=NoShow
  final int priority;

  const QueueEntryModel({
    required this.id,
    required this.queueNumber,
    required this.patientName,
    required this.patientId,
    required this.position,
    required this.estimatedWaitMinutes,
    required this.status,
    required this.priority,
  });

  factory QueueEntryModel.fromJson(Map<String, dynamic> json) {
    return QueueEntryModel(
      id: json['id'] as int,
      queueNumber: json['queueNumber'] as int? ?? 0,
      patientName: json['patientName'] as String? ?? '',
      patientId: json['patientId'] as int? ?? 0,
      position: json['position'] as int? ?? 0,
      estimatedWaitMinutes: json['estimatedWaitMinutes'] as int? ?? 0,
      status: json['status'] as int? ?? 1,
      priority: json['priority'] as int? ?? 1,
    );
  }

  String get statusLabel {
    const map = {
      1: 'Waiting',
      2: 'Called',
      3: 'In Progress',
      4: 'Completed',
      5: 'No Show',
    };
    return map[status] ?? 'Waiting';
  }

  String get priorityLabel {
    const map = {1: 'Normal', 2: 'Urgent', 3: 'Emergency'};
    return map[priority] ?? 'Normal';
  }

  bool get isActive => status == 1 || status == 2 || status == 3;
}
