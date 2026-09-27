class ConsultationType {
  final int id;
  final String name;
  final int durationMinutes;
  final String description;
  final String status;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  ConsultationType({
    required this.id,
    required this.name,
    required this.durationMinutes,
    required this.description,
    required this.status,
    this.createdAt,
    this.updatedAt,
  });

  factory ConsultationType.fromJson(Map<String, dynamic> json) {
    return ConsultationType(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      durationMinutes: json['durationMinutes'] ?? 0,
      description: json['description'] ?? '',
      status: json['status'] ?? '',
      createdAt: json['createdAt'] != null ? DateTime.parse(json['createdAt']) : null,
      updatedAt: json['updatedAt'] != null ? DateTime.parse(json['updatedAt']) : null,
    );
  }
}
