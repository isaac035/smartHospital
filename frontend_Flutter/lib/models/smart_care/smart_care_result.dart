/// Response of POST /api/agent1/triage-doctor-match (Smart Care).
class SmartCareResult {
  /// 'ok' | 'no_doctors' | 'ai_unavailable'
  final String status;
  final String? category;

  /// Suggested priority: 'Normal' | 'Urgent' | 'Emergency'.
  final String? priority;
  final String? reason;
  final double? confidence;
  final bool possibleEmergency;
  final String? emergencyNotice;
  final bool usedDefaultCategory;
  final List<SmartCareDoctor> recommendedDoctors;
  final String? message;

  const SmartCareResult({
    required this.status,
    this.category,
    this.priority,
    this.reason,
    this.confidence,
    required this.possibleEmergency,
    this.emergencyNotice,
    required this.usedDefaultCategory,
    required this.recommendedDoctors,
    this.message,
  });

  bool get hasRecommendation => status != 'ai_unavailable' && category != null;

  factory SmartCareResult.fromJson(Map<String, dynamic> json) {
    final doctors = json['recommendedDoctors'];
    return SmartCareResult(
      status: json['status'] as String? ?? 'ai_unavailable',
      category: json['category'] as String?,
      priority: json['priority'] as String?,
      reason: json['reason'] as String?,
      confidence: (json['confidence'] as num?)?.toDouble(),
      possibleEmergency: json['possibleEmergency'] as bool? ?? false,
      emergencyNotice: json['emergencyNotice'] as String?,
      usedDefaultCategory: json['usedDefaultCategory'] as bool? ?? false,
      recommendedDoctors: doctors is List
          ? doctors
                .whereType<Map<String, dynamic>>()
                .map(SmartCareDoctor.fromJson)
                .toList()
          : const [],
      message: json['message'] as String?,
    );
  }
}

class SmartCareDoctor {
  /// The doctor's User id (what the appointment system books against).
  final int doctorId;

  /// Doctor profile id (used by the existing doctor slot screen route).
  final int doctorProfileId;
  final String name;
  final String specialization;
  final String department;

  const SmartCareDoctor({
    required this.doctorId,
    required this.doctorProfileId,
    required this.name,
    required this.specialization,
    required this.department,
  });

  factory SmartCareDoctor.fromJson(Map<String, dynamic> json) => SmartCareDoctor(
    doctorId: json['doctorId'] as int,
    doctorProfileId: json['doctorProfileId'] as int,
    name: json['name'] as String? ?? '',
    specialization: json['specialization'] as String? ?? '',
    department: json['department'] as String? ?? '',
  );
}
