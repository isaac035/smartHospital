import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;

class ApiConstants {
  static String get baseUrl {
    if (kIsWeb) {
      return 'http://localhost:5100/api';
    }
    try {
      if (Platform.isAndroid) {
        // Since you are running on a PHYSICAL device (Samsung SM M215F), 
        // 10.0.2.2 (Emulator IP) will NOT work. We must use your PC's local Wi-Fi IP.
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
}
