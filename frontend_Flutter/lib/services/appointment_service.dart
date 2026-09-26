import '../../core/network/api_client.dart';
import '../models/appointments/appointment_model.dart';
import '../models/appointments/appointment_slot_model.dart';
import '../models/appointments/appointment_history_model.dart';
import '../models/appointments/queue_entry_model.dart';
import '../models/appointments/doctor_summary_model.dart';

class AppointmentService {
  final ApiClient _apiClient;

  AppointmentService(this._apiClient);

  // --- Appointment endpoints ---

  Future<List<AppointmentModel>> getMyAppointments({
    String? status,
    String? fromDate,
    String? toDate,
  }) async {
    final queryParts = <String>[];
    if (status != null) queryParts.add('status=$status');
    if (fromDate != null) queryParts.add('fromDate=$fromDate');
    if (toDate != null) queryParts.add('toDate=$toDate');
    final query = queryParts.isEmpty ? '' : '?${queryParts.join('&')}';
    final response = await _apiClient.get('/appointments$query');
    if (response is List) {
      return response
          .map((e) => AppointmentModel.fromJson(e as Map<String, dynamic>))
          .toList();
    }
    return [];
  }

  Future<AppointmentModel> getAppointmentById(int id) async {
    final response = await _apiClient.get('/appointments/$id');
    return AppointmentModel.fromJson(response as Map<String, dynamic>);
  }

  Future<List<AppointmentHistoryModel>> getAppointmentHistory(int id) async {
    try {
      final response = await _apiClient.get('/appointments/$id/history');
      if (response is List) {
        return response
            .map((e) =>
                AppointmentHistoryModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
    } catch (_) {
      // History may not be available — fail silently
    }
    return [];
  }

  Future<AppointmentModel> bookAppointment({
    required int doctorId,
    required int appointmentType,
    required String scheduledStart,
    required int estimatedDurationMinutes,
    required int priority,
    String? notes,
  }) async {
    final response = await _apiClient.post('/appointments', data: {
      'doctorId': doctorId,
      'appointmentType': appointmentType,
      'scheduledStart': scheduledStart,
      'estimatedDurationMinutes': estimatedDurationMinutes,
      'priority': priority,
      if (notes != null && notes.isNotEmpty) 'notes': notes,
    });
    return AppointmentModel.fromJson(response as Map<String, dynamic>);
  }

  Future<void> cancelAppointment(int id, String reason) async {
    await _apiClient.post('/appointments/$id/cancel',
        data: {'reason': reason});
  }

  Future<AppointmentModel> rescheduleAppointment(
    int id, {
    required String newScheduledStart,
    required int newEstimatedDurationMinutes,
    required String reason,
  }) async {
    final response = await _apiClient.post('/appointments/$id/reschedule', data: {
      'newScheduledStart': newScheduledStart,
      'newEstimatedDurationMinutes': newEstimatedDurationMinutes,
      'reason': reason,
    });
    return AppointmentModel.fromJson(response as Map<String, dynamic>);
  }

  // --- Availability endpoint ---

  Future<List<AppointmentSlotModel>> getAvailableSlots({
    int? doctorId,
    String? date,
  }) async {
    final queryParts = <String>[];
    if (doctorId != null) queryParts.add('doctorId=$doctorId');
    if (date != null) queryParts.add('date=$date');
    final query = queryParts.isEmpty ? '' : '?${queryParts.join('&')}';
    final response = await _apiClient.get('/availability/slots$query');
    if (response is List) {
      return response
          .map((e) =>
              AppointmentSlotModel.fromJson(e as Map<String, dynamic>))
          .toList();
    }
    return [];
  }

  // --- Queue endpoints ---

  Future<List<QueueEntryModel>> getQueueForDoctor(int doctorId) async {
    final response = await _apiClient.get('/queues/$doctorId');
    if (response is List) {
      return response
          .map((e) => QueueEntryModel.fromJson(e as Map<String, dynamic>))
          .toList();
    }
    return [];
  }

  // --- Doctor search (reads from Users endpoint, Doctor role) ---
  // TODO: Replace with shared Doctor module endpoint once available.

  Future<List<DoctorSummaryModel>> searchDoctors({String? query}) async {
    final q = query != null && query.isNotEmpty ? '?search=$query&role=Doctor' : '?role=Doctor';
    final response = await _apiClient.get('/Users$q');
    if (response is List) {
      return response
          .map((e) => DoctorSummaryModel.fromJson(e as Map<String, dynamic>))
          .toList();
    }
    return [];
  }
}
