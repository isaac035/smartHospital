class PatientMedicalProfileModel {
  final int id;
  final int patientId;
  final String patientName;
  final DateTime? dateOfBirth;
  final String gender;
  final String bloodGroup;
  final String allergies;
  final String chronicDiseases;
  final String emergencyContactName;
  final String emergencyContactPhone;

  PatientMedicalProfileModel({
    required this.id,
    required this.patientId,
    required this.patientName,
    this.dateOfBirth,
    required this.gender,
    required this.bloodGroup,
    required this.allergies,
    required this.chronicDiseases,
    required this.emergencyContactName,
    required this.emergencyContactPhone,
  });

  factory PatientMedicalProfileModel.fromJson(Map<String, dynamic> json) {
    return PatientMedicalProfileModel(
      id: json['id'] ?? 0,
      patientId: json['patientId'] ?? 0,
      patientName: json['patientName'] ?? '',
      dateOfBirth: json['dateOfBirth'] != null ? DateTime.tryParse(json['dateOfBirth']) : null,
      gender: json['gender'] ?? '',
      bloodGroup: json['bloodGroup'] ?? '',
      allergies: json['allergies'] ?? '',
      chronicDiseases: json['chronicDiseases'] ?? '',
      emergencyContactName: json['emergencyContactName'] ?? '',
      emergencyContactPhone: json['emergencyContactPhone'] ?? '',
    );
  }
}
