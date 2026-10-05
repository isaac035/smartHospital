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
  final int? triageResultId;

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
    this.triageResultId,
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
      triageResultId: (json['triageResultId'] as num?)?.toInt(),
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

class SmartCareOptimization {
  final int patientId;
  final int doctorId;
  final SmartCareSlot? recommendedSlot;
  final List<SmartCareSlot> alternativeSlots;
  final String? message;

  const SmartCareOptimization({
    required this.patientId,
    required this.doctorId,
    this.recommendedSlot,
    required this.alternativeSlots,
    this.message,
  });

  factory SmartCareOptimization.fromJson(Map<String, dynamic> json) =>
      SmartCareOptimization(
        patientId: json['patientId'] as int? ?? 0,
        doctorId: json['doctorId'] as int? ?? 0,
        recommendedSlot: json['recommendedSlot'] is Map<String, dynamic>
            ? SmartCareSlot.fromJson(json['recommendedSlot'] as Map<String, dynamic>)
            : null,
        alternativeSlots: json['alternativeSlots'] is List
            ? (json['alternativeSlots'] as List)
                .whereType<Map<String, dynamic>>()
                .map(SmartCareSlot.fromJson)
                .toList()
            : const [],
        message: json['message'] as String?,
      );
}

class SmartCareSlot {
  final int doctorId;
  final int doctorProfileId;
  final String slotStart;
  final String slotEnd;
  final int durationMinutes;
  final String? reason;

  const SmartCareSlot({
    required this.doctorId,
    required this.doctorProfileId,
    required this.slotStart,
    required this.slotEnd,
    required this.durationMinutes,
    this.reason,
  });

  factory SmartCareSlot.fromJson(Map<String, dynamic> json) => SmartCareSlot(
        doctorId: json['doctorId'] as int? ?? 0,
        doctorProfileId: json['doctorProfileId'] as int? ?? 0,
        slotStart: json['slotStart'] as String? ?? '',
        slotEnd: json['slotEnd'] as String? ?? '',
        durationMinutes: json['durationMinutes'] as int? ?? 30,
        reason: json['reason'] as String?,
      );
}
