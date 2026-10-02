class ResourceCandidateModel {
  final int resourceId;
  final String kind, name, code, type, location, status, reason;
  final int score;
  const ResourceCandidateModel({required this.resourceId, required this.kind, required this.name, required this.code, required this.type, required this.location, required this.status, required this.score, required this.reason});
  factory ResourceCandidateModel.fromJson(Map<String, dynamic> j) => ResourceCandidateModel(
    resourceId: (j['resourceId'] as num).toInt(), kind: j['kind'] as String? ?? 'equipment',
    name: j['name'] as String? ?? 'Resource', code: j['code'] as String? ?? '', type: j['type'] as String? ?? '',
    location: j['location'] as String? ?? '', status: j['status'] as String? ?? '',
    score: (j['score'] as num?)?.toInt() ?? 0, reason: j['reason'] as String? ?? '',
  );
}

class ResourceRecommendationModel {
  final String specialty, message;
  final List<ResourceCandidateModel> recommendations;
  const ResourceRecommendationModel({required this.specialty, required this.message, required this.recommendations});
  factory ResourceRecommendationModel.fromJson(Map<String, dynamic> j) => ResourceRecommendationModel(
    specialty: j['clinicalSpecialty'] as String? ?? '', message: j['message'] as String? ?? '',
    recommendations: (j['recommendations'] as List? ?? []).whereType<Map<String, dynamic>>().map(ResourceCandidateModel.fromJson).toList(),
  );
}

class ReservedResourceModel {
  final int id, resourceId;
  final String kind, name, code, type, location, status;
  const ReservedResourceModel({required this.id, required this.resourceId, required this.kind, required this.name, required this.code, required this.type, required this.location, required this.status});
  factory ReservedResourceModel.fromJson(Map<String, dynamic> j) => ReservedResourceModel(
    id: (j['id'] as num).toInt(), resourceId: (j['resourceId'] as num).toInt(),
    kind: j['kind'] as String? ?? '', name: j['name'] as String? ?? '', code: j['code'] as String? ?? '', type: j['type'] as String? ?? '',
    location: j['location'] as String? ?? '', status: j['status'] as String? ?? '',
  );
}
