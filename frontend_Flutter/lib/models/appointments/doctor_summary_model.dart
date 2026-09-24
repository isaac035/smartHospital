/// Minimal doctor stub for patient-facing doctor search.
/// Full doctor model is owned by the Doctor & Clinical Schedule Management team.
/// TODO: Replace with shared DoctorModel once available from that module.
class DoctorSummaryModel {
  final int id;
  final String firstName;
  final String lastName;
  final String? specialization;
  final String? department;
  final String? email;

  const DoctorSummaryModel({
    required this.id,
    required this.firstName,
    required this.lastName,
    this.specialization,
    this.department,
    this.email,
  });

  String get fullName => '$firstName $lastName';

  factory DoctorSummaryModel.fromJson(Map<String, dynamic> json) {
    return DoctorSummaryModel(
      id: json['id'] as int,
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      specialization: json['specialization'] as String?,
      department: json['department'] as String?,
      email: json['email'] as String?,
    );
  }
}
