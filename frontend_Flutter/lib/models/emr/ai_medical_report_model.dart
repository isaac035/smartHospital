class AiMedicalReportModel {
  final String reportId;
  final int patientId;
  final int? appointmentId;
  final int versionNumber;
  final DateTime createdAt;
  final String? appointmentType;
  final String? doctorName;
  final String? priority;
  final String? generationId;
  final Map<String, dynamic>? content;

  const AiMedicalReportModel({
    required this.reportId,
    required this.patientId,
    required this.appointmentId,
    required this.versionNumber,
    required this.createdAt,
    required this.appointmentType,
    required this.doctorName,
    required this.priority,
    this.generationId,
    this.content,
  });

  factory AiMedicalReportModel.fromJson(Map<String, dynamic> json) =>
      AiMedicalReportModel(
        reportId: json['reportId']?.toString() ?? '',
        patientId: (json['patientId'] as num?)?.toInt() ?? 0,
        appointmentId: (json['appointmentId'] as num?)?.toInt(),
        versionNumber: (json['versionNumber'] as num?)?.toInt() ?? 1,
        createdAt:
            DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
            DateTime.now(),
        appointmentType: json['appointmentType']?.toString(),
        doctorName: json['doctorName']?.toString(),
        priority: json['priority']?.toString(),
        generationId: json['generationId']?.toString(),
        content: json['content'] is Map
            ? Map<String, dynamic>.from(json['content'] as Map)
            : null,
      );
}
