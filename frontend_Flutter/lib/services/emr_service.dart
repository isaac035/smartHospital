import '../../core/constants/api_constants.dart';
import '../../core/network/api_client.dart';
import '../models/emr/medical_record_model.dart';
import '../models/emr/patient_medical_profile_model.dart';
import '../models/emr/prescription_model.dart';
import '../models/emr/vital_sign_model.dart';
import '../models/emr/lab_order_model.dart';

class EmrService {
  final ApiClient _apiClient;

  EmrService(this._apiClient);

  Future<PatientMedicalProfileModel?> getPatientProfile(int patientId) async {
    try {
      final response = await _apiClient.get('${ApiConstants.patientProfiles}/patient/$patientId');
      return PatientMedicalProfileModel.fromJson(response);
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
