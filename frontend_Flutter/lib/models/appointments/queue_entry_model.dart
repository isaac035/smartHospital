class QueueEntryModel {
  final int id;
  final int queueNumber;
  final String patientName;
  final int patientId;
  final int position;
  final int estimatedWaitMinutes;
  final int status; // 1=Waiting, 2=Called, 3=InProgress, 4=Completed, 5=NoShow
  final int priority;
  final String queueCode;
  final int patientsAhead;
  final String? currentQueueCode;
  final int? currentQueueNumber;
  final bool isYourTurn;

  const QueueEntryModel({
    required this.id,
    required this.queueNumber,
    required this.patientName,
    required this.patientId,
    required this.position,
    required this.estimatedWaitMinutes,
    required this.status,
    required this.priority,
    this.queueCode = '',
    this.patientsAhead = 0,
    this.currentQueueCode,
    this.currentQueueNumber,
    this.isYourTurn = false,
  });

  factory QueueEntryModel.fromJson(Map<String, dynamic> json) {
    return QueueEntryModel(
      id: json['id'] as int,
      queueNumber: json['queueNumber'] as int? ?? 0,
      patientName: json['patientName'] as String? ?? '',
      patientId: json['patientId'] as int? ?? 0,
      position: json['queuePosition'] as int? ?? 0,
      estimatedWaitMinutes: json['estimatedWaitMinutes'] as int? ?? 0,
      status: _parseStatus(json['status']),
      priority: _parsePriority(json['priority']),
      queueCode: json['queueCode'] as String? ?? '',
      patientsAhead: json['patientsAhead'] as int? ?? 0,
      currentQueueCode: json['currentQueueCode'] as String?,
      currentQueueNumber: json['currentQueueNumber'] as int?,
      isYourTurn: json['isYourTurn'] as bool? ?? false,
    );
  }

  static int _parseStatus(dynamic value) {
    if (value is int) return value;
    return const {'Waiting': 1, 'Called': 2, 'InProgress': 3, 'Completed': 4, 'NoShow': 5, 'Skipped': 6}[value] ?? 1;
  }

  static int _parsePriority(dynamic value) {
    if (value is int) return value;
    return const {'Normal': 1, 'Urgent': 2, 'Emergency': 3}[value] ?? 1;
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
