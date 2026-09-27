import 'package:intl/intl.dart';

import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../models/doctor_model.dart';

class DoctorService {
  final ApiClient _apiClient;

  DoctorService(this._apiClient);

  Future<List<Doctor>> getDoctors({
    int? departmentId,
    String? specialization,
    int? consultationTypeId,
    String? searchTerm,
  }) async {
    final response = await _apiClient.get(
      ApiConstants.doctors,
      queryParameters: {
        if (departmentId != null) 'departmentId': departmentId,
        if (specialization != null && specialization.isNotEmpty) 'specialization': specialization,
        if (consultationTypeId != null) 'consultationTypeId': consultationTypeId,
        if (searchTerm != null && searchTerm.isNotEmpty) 'searchTerm': searchTerm,
      },
    );
    return (response as List).map((e) => Doctor.fromJson(e)).toList();
  }

  Future<List<Doctor>> getAvailableDoctors({
    int? departmentId,
    int? consultationTypeId,
    DateTime? date,
  }) async {
    final response = await _apiClient.get(
      ApiConstants.doctorsAvailable,
      queryParameters: {
        if (departmentId != null) 'departmentId': departmentId,
        if (consultationTypeId != null) 'consultationTypeId': consultationTypeId,
        if (date != null) 'date': DateFormat('yyyy-MM-dd').format(date),
      },
    );
    return (response as List).map((e) => Doctor.fromJson(e)).toList();
  }

  Future<Doctor> getDoctorById(int id) async {
    final response = await _apiClient.get('${ApiConstants.doctors}/$id');
    return Doctor.fromJson(response);
  }
}
