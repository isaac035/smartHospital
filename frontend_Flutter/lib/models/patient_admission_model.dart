class PatientAdmissionModel {
  final int admissionId;
  final String admissionNumber;
  final String status;
  final String priority;
  final DateTime? admissionDate;
  final DateTime? dischargeDate;
  final String? doctorName;
  final String? wardName;
  final String? wardFloor;
  final String? roomNumber;
  final String? bedNumber;
  final DateTime? allocatedAt;
  final String reasonForAdmission;

  PatientAdmissionModel({
    required this.admissionId,
    required this.admissionNumber,
    required this.status,
    required this.priority,
    this.admissionDate,
    this.dischargeDate,
    this.doctorName,
    this.wardName,
    this.wardFloor,
    this.roomNumber,
    this.bedNumber,
    this.allocatedAt,
    required this.reasonForAdmission,
  });

  static DateTime? _parseUtcToLocal(dynamic value) {
    if (value == null) return null;
    final str = value.toString().trim();
    if (str.isEmpty) return null;

    // If string already has a timezone indicator (Z or +/-offset), parse and convert to local
    if (str.endsWith('Z') || RegExp(r'[+-]\d{2}:?\d{2}$').hasMatch(str)) {
      return DateTime.tryParse(str)?.toLocal();
    }

    // Backend returns UTC timestamps; if no offset is present, append Z to ensure UTC interpretation
    final utcParsed = DateTime.tryParse('${str}Z');
    if (utcParsed != null) {
      return utcParsed.toLocal();
    }

    return DateTime.tryParse(str)?.toLocal();
  }

  factory PatientAdmissionModel.fromJson(Map<String, dynamic> json) {
    return PatientAdmissionModel(
      admissionId: json['admissionId'] is int
          ? json['admissionId']
          : int.tryParse(json['admissionId']?.toString() ?? '') ?? 0,
      admissionNumber: json['admissionNumber']?.toString() ?? '',
      status: json['status']?.toString() ?? '',
      priority: json['priority']?.toString() ?? 'Normal',
      admissionDate: _parseUtcToLocal(json['admissionDate']),
      dischargeDate: _parseUtcToLocal(json['dischargeDate']),
      doctorName: json['doctorName']?.toString(),
      wardName: json['wardName']?.toString(),
      wardFloor: json['wardFloor']?.toString(),
      roomNumber: json['roomNumber']?.toString(),
      bedNumber: json['bedNumber']?.toString(),
      allocatedAt: _parseUtcToLocal(json['allocatedAt']),
      reasonForAdmission: json['reasonForAdmission']?.toString() ?? '',
    );
  }
}
