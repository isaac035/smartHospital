import 'package:flutter/material.dart';
import '../core/storage/secure_storage_service.dart';
import '../core/network/api_exception.dart';
import '../models/user_model.dart';
import '../services/auth_service.dart';
import '../services/user_service.dart';

class AuthProvider extends ChangeNotifier {
  final AuthService _authService;
  final UserService _userService;
  final SecureStorageService _storageService;

  bool _isLoading = false;
  bool _isInitialized = false;
  UserModel? _currentUser;
  String? _error;

  AuthProvider(this._authService, this._userService, this._storageService);

  bool get isLoading => _isLoading;
  bool get isInitialized => _isInitialized;
  UserModel? get currentUser => _currentUser;
  bool get isAuthenticated => _currentUser != null;
  String? get error => _error;

  void _setLoading(bool value) {
    _isLoading = value;
    notifyListeners();
  }

  void _setError(String? message) {
    _error = message;
    notifyListeners();
  }

  void clearError() => _setError(null);

  Future<void> initialize() async {
    final token = await _storageService.getToken();
    final userIdStr = await _storageService.getUserId();
    
    if (token != null && userIdStr != null) {
      try {
        final userId = int.parse(userIdStr);
        _currentUser = await _userService.getUserProfile(userId);
      } catch (e) {
        // Token might be expired or invalid, or user not found
        await logout();
      }
    } else {
      await logout(); // Ensure a clean slate
    }
    _isInitialized = true;
    notifyListeners();
  }

  Future<bool> login(String email, String password) async {
    try {
      _setLoading(true);
      clearError();
      final response = await _authService.login(email, password);
      
      if (response.role != 'Patient') {
        _setError('Access denied. Only patients can use this app.');
        return false;
      }

      await _storageService.saveToken(response.token);
      await _storageService.saveUserId(response.userId.toString());
      // We will fetch the full user profile immediately
      _currentUser = await _userService.getUserProfile(response.userId);
      return true;
    } on ApiException catch (e) {
      _setError(e.message);
      return false;
    } catch (e) {
      _setError('An unexpected error occurred during login.');
      return false;
    } finally {
      _setLoading(false);
    }
  }

  Future<bool> register(String firstName, String lastName, String email, String password, String phone) async {
    try {
      _setLoading(true);
      clearError();
      await _authService.register(firstName, lastName, email, password, phone);
      // Registration successful, usually we redirect to login, or maybe the backend auto-logs in.
      // The backend returns a Created response with the user ID, but no token, so we must require login after.
      return true;
    } on ApiException catch (e) {
      _setError(e.message);
      return false;
    } catch (e) {
      _setError('An unexpected error occurred during registration.');
      return false;
    } finally {
      _setLoading(false);
    }
  }

  Future<void> logout() async {
    await _storageService.deleteToken();
    await _storageService.deleteUserId();
    _currentUser = null;
    notifyListeners();
  }

  Future<bool> updateProfile(String firstName, String lastName, String phone) async {
    if (_currentUser == null) return false;
    try {
      _setLoading(true);
      clearError();
      final updatedUser = await _userService.updateUserProfile(_currentUser!.id, firstName, lastName, phone);
      _currentUser = updatedUser;
      return true;
    } on ApiException catch (e) {
      _setError(e.message);
      return false;
    } catch (e) {
      _setError('An unexpected error occurred while updating profile.');
      return false;
    } finally {
      _setLoading(false);
    }
  }

  Future<bool> changePassword(String currentPassword, String newPassword) async {
    try {
      _setLoading(true);
      clearError();
      await _userService.changePassword(currentPassword, newPassword);
      return true;
    } on ApiException catch (e) {
      _setError(e.message);
      return false;
    } catch (e) {
      _setError('An unexpected error occurred while changing password.');
      return false;
    } finally {
      _setLoading(false);
    }
  }
}
