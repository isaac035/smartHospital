import '../../core/utils/api_datetime.dart';

class AppointmentModel {
  final int id;
  final String referenceNumber;
  final int patientId;
  final String patientName;
  final int? doctorId;
  final String? doctorName;
  final String? departmentName;
  final DateTime scheduledStart;
  final int estimatedDurationMinutes;
  final int appointmentType;
  final int status;
  final int priority;
  final bool priorityNeedsReview;
  final bool emergencyConfirmed;
  final String? notes;
  final String? cancelledReason;
  final DateTime createdAt;

  const AppointmentModel({
    required this.id,
    required this.referenceNumber,
    required this.patientId,
    required this.patientName,
    this.doctorId,
    this.doctorName,
    this.departmentName,
    required this.scheduledStart,
    required this.estimatedDurationMinutes,
    required this.appointmentType,
    required this.status,
    required this.priority,
    this.priorityNeedsReview = false,
    required this.emergencyConfirmed,
    this.notes,
    this.cancelledReason,
    required this.createdAt,
  });

  factory AppointmentModel.fromJson(Map<String, dynamic> json) {
    return AppointmentModel(
      id: json['id'] as int,
      referenceNumber: json['referenceNumber'] as String? ?? '',
      patientId: json['patientId'] as int,
      patientName: json['patientName'] as String? ?? 'Unknown',
      doctorId: json['doctorId'] as int?,
      doctorName: json['doctorName'] as String?,
      departmentName: json['departmentName'] as String?,
      scheduledStart: ApiDateTime.parseUtcToLocal(json['scheduledStart'] as String),
      estimatedDurationMinutes: json['estimatedDurationMinutes'] as int? ?? 30,
      appointmentType: _enumValue(json['appointmentType'], const {
        'General': 1, 'Specialist': 2, 'FollowUp': 3, 'Emergency': 4,
        'Teleconsultation': 5, 'Procedure': 6,
      }),
      status: _enumValue(json['status'], const {
        'Scheduled': 1, 'Confirmed': 2, 'CheckedIn': 3, 'InProgress': 4,
        'Completed': 5, 'Cancelled': 6, 'NoShow': 7, 'Rescheduled': 8,
      }),
      priority: _enumValue(json['priority'], const {
        'Normal': 1, 'Urgent': 2, 'Emergency': 3,
      }),
      priorityNeedsReview: json['priorityNeedsReview'] as bool? ?? false,
      emergencyConfirmed: json['emergencyConfirmed'] as bool? ?? false,
      notes: json['notes'] as String?,
      cancelledReason: json['cancelledReason'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }

  static int _enumValue(dynamic value, Map<String, int> names) {
    if (value is int) return value;
    if (value is String) return names[value] ?? int.tryParse(value) ?? 1;
    return 1;
  }

  // --- Human-readable label helpers ---

  String get statusLabel {
    const map = {
      1: 'Scheduled',
      2: 'Confirmed',
      3: 'Checked In',
      4: 'In Progress',
      5: 'Completed',
      6: 'Cancelled',
      7: 'No Show',
      8: 'Rescheduled',
    };
    return map[status] ?? 'Unknown';
  }

  String get priorityLabel {
    const map = {1: 'Normal', 2: 'Urgent', 3: 'Emergency'};
    return map[priority] ?? 'Normal';
  }

  String get typeLabel {
    const map = {
      1: 'General',
      2: 'Specialist',
      3: 'Follow-Up',
      4: 'Emergency',
      5: 'Teleconsultation',
      6: 'Procedure',
    };
    return map[appointmentType] ?? 'General';
  }

  bool get isTerminal =>
      status == 5 || status == 6 || status == 7 || status == 8;
  bool get isCancellable => !isTerminal;
  bool get isReschedulable => status == 1 || status == 2;
}
