import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../models/consultation_type_model.dart';

class ConsultationTypeService {
  final ApiClient _apiClient;

  ConsultationTypeService(this._apiClient);

  Future<List<ConsultationType>> getConsultationTypes() async {
    final response = await _apiClient.get(ApiConstants.consultationTypes);
    return (response as List).map((e) => ConsultationType.fromJson(e)).toList();
  }
}
