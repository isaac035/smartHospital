import '../../core/constants/api_constants.dart';
import '../../core/network/api_client.dart';
import '../../core/network/api_exception.dart';
import '../models/emr/medical_record_model.dart';
import '../models/emr/patient_medical_profile_model.dart';
import '../models/emr/prescription_model.dart';
import '../models/emr/vital_sign_model.dart';
import '../models/emr/lab_order_model.dart';
import '../models/emr/timeline_event_model.dart';

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
}
