import '../core/network/api_client.dart';
import '../core/network/api_exception.dart';
import '../models/patient_admission_model.dart';

class AdmissionService {
  final ApiClient _apiClient;

  AdmissionService(this._apiClient);

  Future<PatientAdmissionModel?> getMyActiveAdmission() async {
    try {
      final response = await _apiClient.get('/admissions/my/active');
      if (response is Map<String, dynamic>) {
        return PatientAdmissionModel.fromJson(response);
      }
      return null;
    } on ApiException catch (e) {
      if (e.statusCode == 404) {
        return null;
      }
      rethrow;
    }
  }

  Future<List<PatientAdmissionModel>> getMyAdmissionHistory() async {
    final response = await _apiClient.get('/admissions/my/history');
    if (response is List) {
      return response
          .map((e) => PatientAdmissionModel.fromJson(e as Map<String, dynamic>))
          .toList();
    }
    return [];
  }
}
