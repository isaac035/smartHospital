import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../models/smart_care/smart_care_result.dart';

/// Smart Care talks only to the hospital API, which calls the AI service
/// internally. The patient id comes from the login token on the server.
class SmartCareService {
  final ApiClient _apiClient;

  SmartCareService(this._apiClient);

  Future<SmartCareResult> getRecommendation(String symptoms) async {
    final response = await _apiClient.post(
      ApiConstants.smartCareTriage,
      data: {'symptoms': symptoms},
      // Triage can take longer than an ordinary request.
      receiveTimeout: const Duration(seconds: 40),
    );
    return SmartCareResult.fromJson(response as Map<String, dynamic>);
  }
}
