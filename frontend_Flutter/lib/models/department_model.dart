class Department {
  final int id;
  final String name;
  final String description;
  final String status;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  Department({
    required this.id,
    required this.name,
    required this.description,
    required this.status,
    this.createdAt,
    this.updatedAt,
  });

  factory Department.fromJson(Map<String, dynamic> json) {
    return Department(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      description: json['description'] ?? '',
      status: json['status'] ?? '',
      createdAt: json['createdAt'] != null ? DateTime.parse(json['createdAt']) : null,
      updatedAt: json['updatedAt'] != null ? DateTime.parse(json['updatedAt']) : null,
    );
  }
}
