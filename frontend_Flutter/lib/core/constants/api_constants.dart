import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;

class ApiConstants {
  // Set with --dart-define=API_BASE_URL=http://<backend-host>:5100/api when
  // the app runs on a device. This lets Flutter use the same backend instance
  // as React, whose VITE_API_URL is configured separately.
  static const String _configuredBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
  );

  static String get baseUrl {
    if (_configuredBaseUrl.isNotEmpty) {
      return _configuredBaseUrl.replaceFirst(RegExp(r'/$'), '');
    }
    if (kIsWeb) {
      return 'http://localhost:5100/api';
    }
    try {
      if (Platform.isAndroid) {
        // Use the development PC's current Wi-Fi address. Override with
        // --dart-define=API_BASE_URL=... when the network assigns another one.
        return 'http://192.168.1.3:5100/api';
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
  static const String medicalHistory = '/MedicalHistory';
}
