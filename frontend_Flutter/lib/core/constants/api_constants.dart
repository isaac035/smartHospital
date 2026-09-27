import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;

class ApiConstants {
  static String get baseUrl {
    if (kIsWeb) {
      return 'http://localhost:5100/api';
    }
    try {
      if (Platform.isAndroid) {
        // Since you are running on a PHYSICAL device, 10.0.2.2 (Emulator IP)
        // will NOT work. We must use your PC's local Wi-Fi IP.
        return 'http://192.168.8.180:5100/api';
      }
    } catch (_) {}
    return 'http://localhost:5100/api'; // Windows, iOS Simulator, Web
  }

  // Auth Endpoints
  static const String register = '/Auth/register';
  static const String login = '/Auth/login';

  // User Endpoints
  static const String users = '/Users';

  // Doctor Endpoints
  static const String doctors = '/doctors';
  static const String doctorsAvailable = '/doctors/available';

  // Department Endpoints
  static const String departments = '/departments';

  // Consultation Type Endpoints
  static const String consultationTypes = '/consultation-types';

  // Schedule Endpoints
  static const String schedules = '/schedules';

  // Leave Endpoints
  static const String leaves = '/leaves';

  // EMR Endpoints
  static const String medicalRecords = '/MedicalRecords';
  static const String vitals = '/Vitals';
  static const String prescriptions = '/Prescriptions';
  static const String labOrders = '/LabOrders';
  static const String patientProfiles = '/PatientProfiles';
}
