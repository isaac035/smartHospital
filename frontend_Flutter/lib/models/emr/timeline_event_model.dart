class TimelineEventModel {
  final String eventType;
  final DateTime eventDate;
  final int patientId;
  final String patientName;
  final int? doctorId;
  final String? doctorName;
  final String summary;
  final int sourceRecordId;
  final String? category;
  final String? status;

  TimelineEventModel({
    required this.eventType,
    required this.eventDate,
    required this.patientId,
    required this.patientName,
    this.doctorId,
    this.doctorName,
    required this.summary,
    required this.sourceRecordId,
    this.category,
    this.status,
  });

  factory TimelineEventModel.fromJson(Map<String, dynamic> json) {
    return TimelineEventModel(
      eventType: json['eventType'] ?? 'Event',
      eventDate: json['eventDate'] != null
          ? DateTime.parse(json['eventDate'])
          : DateTime.now(),
      patientId: json['patientId'] ?? 0,
      patientName: json['patientName'] ?? '',
      doctorId: json['doctorId'],
      doctorName: json['doctorName'],
      summary: json['summary'] ?? '',
      sourceRecordId: json['sourceRecordId'] ?? 0,
      category: json['category'],
      status: json['status'],
    );
  }
}

class PatientTimelineModel {
  final int patientId;
  final String patientName;
  final int totalEvents;
  final List<TimelineEventModel> events;

  PatientTimelineModel({
    required this.patientId,
    required this.patientName,
    required this.totalEvents,
    required this.events,
  });

  factory PatientTimelineModel.fromJson(Map<String, dynamic> json) {
    var rawEvents = json['events'] as List<dynamic>? ?? [];
    return PatientTimelineModel(
      patientId: json['patientId'] ?? 0,
      patientName: json['patientName'] ?? '',
      totalEvents: json['totalEvents'] ?? 0,
      events: rawEvents.map((e) => TimelineEventModel.fromJson(e)).toList(),
    );
  }
}
