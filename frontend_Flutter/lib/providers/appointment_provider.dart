import 'package:flutter/material.dart';
import '../core/network/api_exception.dart';
import '../models/appointments/appointment_model.dart';
import '../models/appointments/appointment_slot_model.dart';
import '../models/appointments/appointment_history_model.dart';
import '../models/appointments/queue_entry_model.dart';
import '../models/appointments/doctor_summary_model.dart';
import '../services/appointment_service.dart';

class AppointmentProvider extends ChangeNotifier {
  final AppointmentService _service;

  AppointmentProvider(this._service);

  // --- My Appointments state ---
  List<AppointmentModel> _appointments = [];
  bool _appointmentsLoading = false;
  String? _appointmentsError;

  List<AppointmentModel> get appointments => _appointments;
  bool get appointmentsLoading => _appointmentsLoading;
  String? get appointmentsError => _appointmentsError;

  // --- Appointment Detail state ---
  AppointmentModel? _selectedAppointment;
  List<AppointmentHistoryModel> _selectedHistory = [];
  bool _detailLoading = false;
  String? _detailError;

  AppointmentModel? get selectedAppointment => _selectedAppointment;
  List<AppointmentHistoryModel> get selectedHistory => _selectedHistory;
  bool get detailLoading => _detailLoading;
  String? get detailError => _detailError;

  // --- Slots state ---
  List<AppointmentSlotModel> _slots = [];
  bool _slotsLoading = false;
  String? _slotsError;

  List<AppointmentSlotModel> get slots => _slots;
  bool get slotsLoading => _slotsLoading;
  String? get slotsError => _slotsError;

  // --- Queue state ---
  List<QueueEntryModel> _queue = [];
  bool _queueLoading = false;
  String? _queueError;

  List<QueueEntryModel> get queue => _queue;
  bool get queueLoading => _queueLoading;
  String? get queueError => _queueError;

  // --- Doctor search state ---
  List<DoctorSummaryModel> _doctors = [];
  bool _doctorsLoading = false;
  String? _doctorsError;

  List<DoctorSummaryModel> get doctors => _doctors;
  bool get doctorsLoading => _doctorsLoading;
  String? get doctorsError => _doctorsError;

  // --- Action state (booking / cancel / reschedule) ---
  bool _actionLoading = false;
  String? _actionError;

  bool get actionLoading => _actionLoading;
  String? get actionError => _actionError;

  void clearActionError() {
    _actionError = null;
    notifyListeners();
  }

  // --- Methods ---

  Future<void> loadMyAppointments({String? statusFilter}) async {
    _appointmentsLoading = true;
    _appointmentsError = null;
    notifyListeners();
    try {
      _appointments = await _service.getMyAppointments(status: statusFilter);
    } on ApiException catch (e) {
      _appointmentsError = e.message;
    } catch (_) {
      _appointmentsError = 'Failed to load appointments.';
    } finally {
      _appointmentsLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadAppointmentDetail(int id) async {
    _detailLoading = true;
    _detailError = null;
    _selectedAppointment = null;
    _selectedHistory = [];
    notifyListeners();
    try {
      _selectedAppointment = await _service.getAppointmentById(id);
      _selectedHistory = await _service.getAppointmentHistory(id);
    } on ApiException catch (e) {
      _detailError = e.message;
    } catch (_) {
      _detailError = 'Failed to load appointment details.';
    } finally {
      _detailLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadSlots({required int doctorId, required String date}) async {
    _slotsLoading = true;
    _slotsError = null;
    _slots = [];
    notifyListeners();
    try {
      _slots = await _service.getAvailableSlots(doctorId: doctorId, date: date);
    } on ApiException catch (e) {
      _slotsError = e.message;
    } catch (_) {
      _slotsError = 'Failed to load available slots.';
    } finally {
      _slotsLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadQueue(int doctorId) async {
    _queueLoading = true;
    _queueError = null;
    notifyListeners();
    try {
      _queue = await _service.getQueueForDoctor(doctorId);
    } on ApiException catch (e) {
      _queueError = e.message;
    } catch (_) {
      _queueError = 'Failed to load queue information.';
    } finally {
      _queueLoading = false;
      notifyListeners();
    }
  }

  Future<void> searchDoctors(String query) async {
    _doctorsLoading = true;
    _doctorsError = null;
    notifyListeners();
    try {
      _doctors = await _service.searchDoctors(query: query);
    } on ApiException catch (e) {
      _doctorsError = e.message;
    } catch (_) {
      _doctorsError = 'Failed to search doctors.';
    } finally {
      _doctorsLoading = false;
      notifyListeners();
    }
  }

  /// Returns the new appointment on success, null on failure.
  Future<AppointmentModel?> bookAppointment({
    required int doctorId,
    required int appointmentType,
    required String scheduledStart,
    required int estimatedDurationMinutes,
    required int priority,
    String? notes,
  }) async {
    _actionLoading = true;
    _actionError = null;
    notifyListeners();
    try {
      final result = await _service.bookAppointment(
        doctorId: doctorId,
        appointmentType: appointmentType,
        scheduledStart: scheduledStart,
        estimatedDurationMinutes: estimatedDurationMinutes,
        priority: priority,
        notes: notes,
      );
      // Reload list so the new appointment shows up
      await loadMyAppointments();
      return result;
    } on ApiException catch (e) {
      _actionError = e.message;
      return null;
    } catch (_) {
      _actionError = 'Booking failed. Please try again.';
      return null;
    } finally {
      _actionLoading = false;
      notifyListeners();
    }
  }

  Future<bool> cancelAppointment(int id, String reason) async {
    _actionLoading = true;
    _actionError = null;
    notifyListeners();
    try {
      await _service.cancelAppointment(id, reason);
      await loadAppointmentDetail(id);
      return true;
    } on ApiException catch (e) {
      _actionError = e.message;
      return false;
    } catch (_) {
      _actionError = 'Cancellation failed. Please try again.';
      return false;
    } finally {
      _actionLoading = false;
      notifyListeners();
    }
  }

  Future<bool> rescheduleAppointment(
    int id, {
    required String newScheduledStart,
    required int newEstimatedDurationMinutes,
    required String reason,
  }) async {
    _actionLoading = true;
    _actionError = null;
    notifyListeners();
    try {
      await _service.rescheduleAppointment(
        id,
        newScheduledStart: newScheduledStart,
        newEstimatedDurationMinutes: newEstimatedDurationMinutes,
        reason: reason,
      );
      await loadAppointmentDetail(id);
      return true;
    } on ApiException catch (e) {
      _actionError = e.message;
      return false;
    } catch (_) {
      _actionError = 'Reschedule failed. Please try again.';
      return false;
    } finally {
      _actionLoading = false;
      notifyListeners();
    }
  }
}
