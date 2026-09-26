import 'package:flutter/material.dart';

import '../core/network/api_exception.dart';
import '../models/consultation_type_model.dart';
import '../models/department_model.dart';
import '../models/doctor_model.dart';
import '../models/schedule_model.dart';
import '../services/consultation_type_service.dart';
import '../services/department_service.dart';
import '../services/doctor_service.dart';
import '../services/leave_service.dart';
import '../services/schedule_service.dart';

const List<String> kDayOfWeekNames = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];

String dayOfWeekNameFor(DateTime date) => kDayOfWeekNames[date.weekday - 1];

class DoctorProvider extends ChangeNotifier {
  final DoctorService _doctorService;
  final ScheduleService _scheduleService;
  final LeaveService _leaveService;
  final DepartmentService _departmentService;
  final ConsultationTypeService _consultationTypeService;

  DoctorProvider(
    this._doctorService,
    this._scheduleService,
    this._leaveService,
    this._departmentService,
    this._consultationTypeService,
  );

  // Filter reference data (departments/consultation types for dropdowns).
  bool _isLoadingFilterOptions = false;
  List<Department> _departments = [];
  List<ConsultationType> _consultationTypes = [];

  bool get isLoadingFilterOptions => _isLoadingFilterOptions;
  List<Department> get departments => _departments;
  List<ConsultationType> get consultationTypes => _consultationTypes;

  // Directory filter state.
  int? filterDepartmentId;
  String filterSpecialization = '';
  int? filterMinExperience;
  int? filterConsultationTypeId;
  String searchTerm = '';
  bool availabilityFilterOn = false;
  DateTime availabilityFilterDate = DateTime.now();

  // Directory results.
  bool _isLoadingDirectory = false;
  String? _directoryError;
  List<Doctor> _directoryDoctors = [];

  bool get isLoadingDirectory => _isLoadingDirectory;
  String? get directoryError => _directoryError;
  List<Doctor> get directoryDoctors => _directoryDoctors;

  // Selected doctor (Details screen).
  bool _isLoadingDoctor = false;
  String? _doctorError;
  Doctor? _selectedDoctor;
  List<Schedule> _selectedDoctorSchedules = [];

  bool get isLoadingDoctor => _isLoadingDoctor;
  String? get doctorError => _doctorError;
  Doctor? get selectedDoctor => _selectedDoctor;
  List<String> get selectedDoctorConsultationTypes =>
      _selectedDoctorSchedules.map((s) => s.consultationTypeName).toSet().toList();

  // Full weekly schedule (Schedule screen).
  bool _isLoadingSchedule = false;
  String? _scheduleError;
  List<Schedule> _fullSchedule = [];

  bool get isLoadingSchedule => _isLoadingSchedule;
  String? get scheduleError => _scheduleError;
  List<Schedule> get fullSchedule => _fullSchedule;

  // Availability check for one date (Availability screen).
  bool _isLoadingAvailability = false;
  String? _availabilityError;
  List<Schedule> _availabilitySlotsForDate = [];
  bool _isOnLeaveForDate = false;

  bool get isLoadingAvailability => _isLoadingAvailability;
  String? get availabilityError => _availabilityError;
  List<Schedule> get availabilitySlotsForDate => _availabilitySlotsForDate;
  bool get isOnLeaveForDate => _isOnLeaveForDate;

  Future<void> loadFilterOptions() async {
    try {
      _isLoadingFilterOptions = true;
      notifyListeners();
      _departments = await _departmentService.getDepartments();
      _consultationTypes = await _consultationTypeService.getConsultationTypes();
    } on ApiException catch (e) {
      _directoryError = e.message;
    } catch (e) {
      _directoryError = 'Unable to load filter options.';
    } finally {
      _isLoadingFilterOptions = false;
      notifyListeners();
    }
  }

  Future<void> loadDirectory() async {
    try {
      _isLoadingDirectory = true;
      _directoryError = null;
      notifyListeners();

      if (availabilityFilterOn) {
        _directoryDoctors = await _doctorService.getAvailableDoctors(
          departmentId: filterDepartmentId,
          consultationTypeId: filterConsultationTypeId,
          date: availabilityFilterDate,
        );
      } else {
        _directoryDoctors = await _doctorService.getDoctors(
          departmentId: filterDepartmentId,
          specialization: filterSpecialization,
          minExperience: filterMinExperience,
          consultationTypeId: filterConsultationTypeId,
          searchTerm: searchTerm,
        );
      }
    } on ApiException catch (e) {
      _directoryError = e.message;
    } catch (e) {
      _directoryError = 'Unable to load doctors.';
    } finally {
      _isLoadingDirectory = false;
      notifyListeners();
    }
  }

  void setAvailabilityFilterOn(bool value) {
    availabilityFilterOn = value;
    loadDirectory();
  }

  void setAvailabilityFilterDate(DateTime date) {
    availabilityFilterDate = date;
    if (availabilityFilterOn) {
      loadDirectory();
    }
  }

  void clearFilters() {
    filterDepartmentId = null;
    filterSpecialization = '';
    filterMinExperience = null;
    filterConsultationTypeId = null;
    searchTerm = '';
    availabilityFilterOn = false;
    availabilityFilterDate = DateTime.now();
    loadDirectory();
  }

  Future<void> loadDoctorDetails(int doctorId) async {
    try {
      _isLoadingDoctor = true;
      _doctorError = null;
      notifyListeners();
      _selectedDoctor = await _doctorService.getDoctorById(doctorId);
      _selectedDoctorSchedules = await _scheduleService.getSchedules(doctorId: doctorId);
    } on ApiException catch (e) {
      _doctorError = e.message;
    } catch (e) {
      _doctorError = 'Unable to load doctor details.';
    } finally {
      _isLoadingDoctor = false;
      notifyListeners();
    }
  }

  Future<void> loadSchedule(int doctorId) async {
    try {
      _isLoadingSchedule = true;
      _scheduleError = null;
      notifyListeners();
      final slots = await _scheduleService.getSchedules(doctorId: doctorId);
      slots.sort((a, b) {
        final dayCompare = kDayOfWeekNames.indexOf(a.dayOfWeek).compareTo(kDayOfWeekNames.indexOf(b.dayOfWeek));
        if (dayCompare != 0) return dayCompare;
        return a.startTime.compareTo(b.startTime);
      });
      _fullSchedule = slots;
    } on ApiException catch (e) {
      _scheduleError = e.message;
    } catch (e) {
      _scheduleError = 'Unable to load schedule.';
    } finally {
      _isLoadingSchedule = false;
      notifyListeners();
    }
  }

  Future<void> checkAvailability(int doctorId, DateTime date) async {
    try {
      _isLoadingAvailability = true;
      _availabilityError = null;
      notifyListeners();

      final dayOfWeek = dayOfWeekNameFor(date);

      final slots = await _scheduleService.getSchedules(doctorId: doctorId, dayOfWeek: dayOfWeek);
      final leaves = await _leaveService.getLeaves(doctorId: doctorId, fromDate: date, toDate: date);

      final approvedLeaveCoversDate = leaves.any((leave) =>
          leave.status == 'Approved' &&
          !date.isBefore(leave.startDate) &&
          !date.isAfter(leave.endDate));

      _isOnLeaveForDate = approvedLeaveCoversDate;
      _availabilitySlotsForDate = approvedLeaveCoversDate ? [] : slots;
    } on ApiException catch (e) {
      _availabilityError = e.message;
    } catch (e) {
      _availabilityError = 'Unable to check availability.';
    } finally {
      _isLoadingAvailability = false;
      notifyListeners();
    }
  }
}
