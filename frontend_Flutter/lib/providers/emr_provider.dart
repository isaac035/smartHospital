import 'package:flutter/material.dart';
import '../core/network/api_exception.dart';
import '../models/emr/medical_record_model.dart';
import '../models/emr/patient_medical_profile_model.dart';
import '../models/emr/prescription_model.dart';
import '../models/emr/vital_sign_model.dart';
import '../models/emr/lab_order_model.dart';
import '../models/emr/timeline_event_model.dart';
import '../services/emr_service.dart';

class EmrProvider extends ChangeNotifier {
  final EmrService _service;

  EmrProvider(this._service);

  // --- Profile State ---
  PatientMedicalProfileModel? _profile;
  bool _profileLoading = false;
  String? _profileError;

  PatientMedicalProfileModel? get profile => _profile;
  bool get profileLoading => _profileLoading;
  String? get profileError => _profileError;

  // --- Medical Records State ---
  List<MedicalRecordModel> _medicalRecords = [];
  bool _recordsLoading = false;
  String? _recordsError;

  List<MedicalRecordModel> get medicalRecords => _medicalRecords;
  bool get recordsLoading => _recordsLoading;
  String? get recordsError => _recordsError;

  // --- Selected Record Detail State ---
  MedicalRecordModel? _selectedRecord;
  bool _recordDetailLoading = false;
  String? _recordDetailError;

  MedicalRecordModel? get selectedRecord => _selectedRecord;
  bool get recordDetailLoading => _recordDetailLoading;
  String? get recordDetailError => _recordDetailError;

  // --- Vital Signs State ---
  List<VitalSignModel> _vitals = [];
  bool _vitalsLoading = false;
  String? _vitalsError;

  List<VitalSignModel> get vitals => _vitals;
  bool get vitalsLoading => _vitalsLoading;
  String? get vitalsError => _vitalsError;

  VitalSignModel? get latestVital => _vitals.isNotEmpty ? _vitals.first : null;

  // --- Prescriptions State ---
  List<PrescriptionModel> _prescriptions = [];
  bool _prescriptionsLoading = false;
  String? _prescriptionsError;

  List<PrescriptionModel> get prescriptions => _prescriptions;
  bool get prescriptionsLoading => _prescriptionsLoading;
  String? get prescriptionsError => _prescriptionsError;

  // --- Lab Orders State ---
  List<LabOrderModel> _labOrders = [];
  bool _labOrdersLoading = false;
  String? _labOrdersError;

  List<LabOrderModel> get labOrders => _labOrders;
  bool get labOrdersLoading => _labOrdersLoading;
  String? get labOrdersError => _labOrdersError;

  // --- Timeline State ---
  PatientTimelineModel? _timeline;
  bool _timelineLoading = false;
  String? _timelineError;

  PatientTimelineModel? get timeline => _timeline;
  bool get timelineLoading => _timelineLoading;
  String? get timelineError => _timelineError;

  // --- Helpers for error handling ---
  String _mapError(dynamic e, String defaultMessage) {
    if (e is ApiException) {
      if (e.statusCode == 401 || e.statusCode == 403) {
        return 'Access denied. You can only view your own authorized medical records.';
      }
      return e.message;
    }
    return defaultMessage;
  }

  // --- Actions ---

  Future<void> loadPatientProfile(int patientId, {bool force = false}) async {
    if (!force && _profile != null) return;
    _profileLoading = true;
    _profileError = null;
    notifyListeners();

    try {
      _profile = await _service.getPatientProfile(patientId);
    } catch (e) {
      _profileError = _mapError(e, 'Failed to load medical profile.');
    } finally {
      _profileLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMedicalRecords(int patientId, {bool force = false}) async {
    if (!force && _medicalRecords.isNotEmpty) return;
    _recordsLoading = true;
    _recordsError = null;
    notifyListeners();

    try {
      final records = await _service.getMedicalRecords(patientId);
      // Sort in descending order of visitDate
      records.sort((a, b) => b.visitDate.compareTo(a.visitDate));
      _medicalRecords = records;
    } catch (e) {
      _recordsError = _mapError(e, 'Failed to load medical consultations.');
    } finally {
      _recordsLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMedicalRecordDetail(int id) async {
    _recordDetailLoading = true;
    _recordDetailError = null;
    _selectedRecord = null;
    notifyListeners();

    try {
      _selectedRecord = await _service.getMedicalRecordById(id);
    } catch (e) {
      _recordDetailError = _mapError(e, 'Failed to load consultation details.');
    } finally {
      _recordDetailLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadVitals(int patientId, {bool force = false}) async {
    if (!force && _vitals.isNotEmpty) return;
    _vitalsLoading = true;
    _vitalsError = null;
    notifyListeners();

    try {
      final list = await _service.getVitals(patientId);
      list.sort((a, b) => b.recordedAt.compareTo(a.recordedAt));
      _vitals = list;
    } catch (e) {
      _vitalsError = _mapError(e, 'Failed to load vital signs.');
    } finally {
      _vitalsLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadPrescriptions(int patientId, {bool force = false}) async {
    if (!force && _prescriptions.isNotEmpty) return;
    _prescriptionsLoading = true;
    _prescriptionsError = null;
    notifyListeners();

    try {
      final list = await _service.getPrescriptions(patientId);
      list.sort((a, b) => b.issueDate.compareTo(a.issueDate));
      _prescriptions = list;
    } catch (e) {
      _prescriptionsError = _mapError(e, 'Failed to load prescriptions.');
    } finally {
      _prescriptionsLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadLabOrders(int patientId, {bool force = false}) async {
    if (!force && _labOrders.isNotEmpty) return;
    _labOrdersLoading = true;
    _labOrdersError = null;
    notifyListeners();

    try {
      final list = await _service.getLabOrders(patientId);
      list.sort((a, b) => b.orderedAt.compareTo(a.orderedAt));
      _labOrders = list;
    } catch (e) {
      _labOrdersError = _mapError(e, 'Failed to load laboratory reports.');
    } finally {
      _labOrdersLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadTimeline(int patientId, {bool force = false}) async {
    if (!force && _timeline != null) return;
    _timelineLoading = true;
    _timelineError = null;
    notifyListeners();

    try {
      _timeline = await _service.getMedicalTimeline(patientId);
    } catch (e) {
      _timelineError = _mapError(e, 'Failed to load clinical history timeline.');
    } finally {
      _timelineLoading = false;
      notifyListeners();
    }
  }

  Future<void> refreshAll(int patientId) async {
    await Future.wait([
      loadPatientProfile(patientId, force: true),
      loadMedicalRecords(patientId, force: true),
      loadVitals(patientId, force: true),
      loadPrescriptions(patientId, force: true),
      loadLabOrders(patientId, force: true),
      loadTimeline(patientId, force: true),
    ]);
  }
}
