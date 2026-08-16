class UserModel {
  final int id;
  final String firstName;
  final String lastName;
  final String email;
  final String phoneNumber;
  final String role;
  final String status;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  UserModel({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.phoneNumber,
    required this.role,
    required this.status,
    this.createdAt,
    this.updatedAt,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'] ?? 0,
      firstName: json['firstName'] ?? '',
      lastName: json['lastName'] ?? '',
      email: json['email'] ?? '',
      phoneNumber: json['phoneNumber'] ?? '',
      role: json['role'] ?? '',
      status: json['status'] ?? '',
      createdAt: json['createdAt'] != null ? DateTime.parse(json['createdAt']) : null,
      updatedAt: json['updatedAt'] != null ? DateTime.parse(json['updatedAt']) : null,
    );
  }

  UserModel copyWith({
    String? firstName,
    String? lastName,
    String? phoneNumber,
  }) {
    return UserModel(
      id: id,
      firstName: firstName ?? this.firstName,
      lastName: lastName ?? this.lastName,
      email: email,
      phoneNumber: phoneNumber ?? this.phoneNumber,
      role: role,
      status: status,
      createdAt: createdAt,
      updatedAt: updatedAt,
    );
  }
}
