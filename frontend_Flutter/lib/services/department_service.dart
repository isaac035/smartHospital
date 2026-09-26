import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../models/department_model.dart';

class DepartmentService {
  final ApiClient _apiClient;

  DepartmentService(this._apiClient);

  Future<List<Department>> getDepartments() async {
    final response = await _apiClient.get(ApiConstants.departments);
    return (response as List).map((e) => Department.fromJson(e)).toList();
  }
}
