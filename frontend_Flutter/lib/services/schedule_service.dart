import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../models/schedule_model.dart';

class ScheduleService {
  final ApiClient _apiClient;

  ScheduleService(this._apiClient);

  Future<List<Schedule>> getSchedules({
    int? doctorId,
    int? departmentId,
    String? dayOfWeek,
  }) async {
    final response = await _apiClient.get(
      ApiConstants.schedules,
      queryParameters: {
        if (doctorId != null) 'doctorId': doctorId,
        if (departmentId != null) 'departmentId': departmentId,
        if (dayOfWeek != null) 'dayOfWeek': dayOfWeek,
      },
    );
    return (response as List).map((e) => Schedule.fromJson(e)).toList();
  }
}
