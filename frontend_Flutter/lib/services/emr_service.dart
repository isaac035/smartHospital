import '../../core/constants/api_constants.dart';
import '../../core/network/api_client.dart';
import '../../core/network/api_exception.dart';
import 'dart:math';
import '../models/emr/medical_record_model.dart';
import '../models/emr/patient_medical_profile_model.dart';
import '../models/emr/prescription_model.dart';
import '../models/emr/vital_sign_model.dart';
import '../models/emr/lab_order_model.dart';
import '../models/emr/timeline_event_model.dart';
import '../models/emr/ai_medical_report_model.dart';

class EmrService {
  final ApiClient _apiClient;

  EmrService(this._apiClient);

  Future<PatientMedicalProfileModel?> getPatientProfile(int patientId) async {
    try {
      final response = await _apiClient.get('${ApiConstants.patientProfiles}/patient/$patientId');
      if (response is Map<String, dynamic>) {
        return PatientMedicalProfileModel.fromJson(response);
      }
      return null;
    } on ApiException catch (e) {
      if (e.statusCode == 404) {
        return null;
      }
      rethrow;
    } catch (_) {
      return null;
    }
  }

  Future<List<MedicalRecordModel>> getMedicalRecords(int patientId) async {
    final response = await _apiClient.get('${ApiConstants.medicalRecords}/patient/$patientId');
    if (response is List) {
      return response.map((e) => MedicalRecordModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<MedicalRecordModel?> getMedicalRecordById(int id) async {
    try {
      final response = await _apiClient.get('${ApiConstants.medicalRecords}/$id');
      if (response is Map<String, dynamic>) {
        return MedicalRecordModel.fromJson(response);
      }
      return null;
    } on ApiException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }

  Future<PatientTimelineModel?> getMedicalTimeline(int patientId) async {
    final response = await _apiClient.get('${ApiConstants.medicalHistory}/patient/$patientId');
    if (response is Map<String, dynamic>) {
      return PatientTimelineModel.fromJson(response);
    }
    return null;
  }

  Future<List<VitalSignModel>> getVitals(int patientId) async {
    final response = await _apiClient.get('${ApiConstants.vitals}/patient/$patientId');
    if (response is List) {
      return response.map((e) => VitalSignModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<List<PrescriptionModel>> getPrescriptions(int patientId) async {
    final response = await _apiClient.get('${ApiConstants.prescriptions}/patient/$patientId');
    if (response is List) {
      return response.map((e) => PrescriptionModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<List<LabOrderModel>> getLabOrders(int patientId) async {
    final response = await _apiClient.get('${ApiConstants.labOrders}/patient/$patientId');
    if (response is List) {
      return response.map((e) => LabOrderModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<List<AiMedicalReportModel>> getAiMedicalReports(int patientId) async {
    final response = await _apiClient.get('/agent4/reports', queryParameters: {'patientId': patientId});
    if (response is List) {
      return response.map((item) => AiMedicalReportModel.fromJson(Map<String, dynamic>.from(item as Map))).toList();
    }
    return [];
  }

  Future<AiMedicalReportModel> generateAiMedicalReport(
    int patientId, {
    int? appointmentId,
    bool? checkupRequested,
  }) async {
    final generationId = _newGenerationId();
    final request = <String, dynamic>{
      'generationId': generationId,
      if (appointmentId != null) 'appointmentId': appointmentId,
      if (checkupRequested != null) 'checkupRequested': checkupRequested,
    };
    try {
      try {
        final response = await _apiClient.post('/agent4/reports', data: request, receiveTimeout: const Duration(seconds: 120));
        return AiMedicalReportModel.fromJson(Map<String, dynamic>.from(response as Map));
      } on ApiException catch (retryError) {
        if (retryError.code != 'NETWORK_TIMEOUT') rethrow;
        final reports = await getAiMedicalReports(patientId);
        final matches = reports.where((item) => item.generationId == generationId);
        if (matches.isNotEmpty) return matches.first;
        rethrow;
      }
    } on ApiException catch (e) {
      if (e.code != 'NETWORK_TIMEOUT') rethrow;
      // Same generation id makes a retry return the saved report if the first response was lost.
      final response = await _apiClient.post('/agent4/reports', data: request, receiveTimeout: const Duration(seconds: 120));
      return AiMedicalReportModel.fromJson(Map<String, dynamic>.from(response as Map));
    }
  }

  String _newGenerationId() {
    final random = Random.secure();
    final bytes = List<int>.generate(16, (_) => random.nextInt(256));
    bytes[6] = (bytes[6] & 0x0f) | 0x40;
    bytes[8] = (bytes[8] & 0x3f) | 0x80;
    final hex = bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
    return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}';
  }

  Future<AiMedicalReportModel> getAiMedicalReport(String reportId) async {
    final response = await _apiClient.get('/agent4/reports/$reportId');
    return AiMedicalReportModel.fromJson(Map<String, dynamic>.from(response as Map));
  }
}
