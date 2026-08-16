class AuthResponse {
  final int userId;
  final String firstName;
  final String lastName;
  final String email;
  final String role;
  final String token;
  final DateTime expiresAt;

  AuthResponse({
    required this.userId,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.role,
    required this.token,
    required this.expiresAt,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    return AuthResponse(
      userId: json['userId'] ?? 0,
      firstName: json['firstName'] ?? '',
      lastName: json['lastName'] ?? '',
      email: json['email'] ?? '',
      role: json['role'] ?? '',
      token: json['token'] ?? '',
      expiresAt: json['expiresAt'] != null 
          ? DateTime.parse(json['expiresAt'])
          : DateTime.now().add(const Duration(hours: 1)),
    );
  }
}
