import 'package:intl/intl.dart';

import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../models/leave_model.dart';

class LeaveService {
  final ApiClient _apiClient;

  LeaveService(this._apiClient);

  Future<List<Leave>> getLeaves({
    int? doctorId,
    DateTime? fromDate,
    DateTime? toDate,
  }) async {
    final response = await _apiClient.get(
      ApiConstants.leaves,
      queryParameters: {
        if (doctorId != null) 'doctorId': doctorId,
        if (fromDate != null) 'fromDate': DateFormat('yyyy-MM-dd').format(fromDate),
        if (toDate != null) 'toDate': DateFormat('yyyy-MM-dd').format(toDate),
      },
    );
    return (response as List).map((e) => Leave.fromJson(e)).toList();
  }
}
