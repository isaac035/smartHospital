class Doctor {
  final int id;
  final int? userId;
  final int departmentId;
  final String departmentName;
  final String firstName;
  final String lastName;
  final String email;
  final String phoneNumber;
  final String specialization;
  final String licenseNumber;
  final int yearsOfExperience;
  final String bio;
  final String status;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  Doctor({
    required this.id,
    this.userId,
    required this.departmentId,
    required this.departmentName,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.phoneNumber,
    required this.specialization,
    required this.licenseNumber,
    required this.yearsOfExperience,
    required this.bio,
    required this.status,
    this.createdAt,
    this.updatedAt,
  });

  String get fullName => '$firstName $lastName';

  factory Doctor.fromJson(Map<String, dynamic> json) {
    return Doctor(
      id: json['id'] ?? 0,
      userId: json['userId'],
      departmentId: json['departmentId'] ?? 0,
      departmentName: json['departmentName'] ?? '',
      firstName: json['firstName'] ?? '',
      lastName: json['lastName'] ?? '',
      email: json['email'] ?? '',
      phoneNumber: json['phoneNumber'] ?? '',
      specialization: json['specialization'] ?? '',
      licenseNumber: json['licenseNumber'] ?? '',
      yearsOfExperience: json['yearsOfExperience'] ?? 0,
      bio: json['bio'] ?? '',
      status: json['status'] ?? '',
      createdAt: json['createdAt'] != null ? DateTime.parse(json['createdAt']) : null,
      updatedAt: json['updatedAt'] != null ? DateTime.parse(json['updatedAt']) : null,
    );
  }
}
