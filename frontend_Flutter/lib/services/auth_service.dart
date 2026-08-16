import '../../core/constants/api_constants.dart';
import '../../core/network/api_client.dart';
import '../models/auth_response.dart';

class AuthService {
  final ApiClient _apiClient;

  AuthService(this._apiClient);

  Future<AuthResponse> login(String email, String password) async {
    final response = await _apiClient.post(
      ApiConstants.login,
      data: {
        'email': email,
        'password': password,
      },
    );
    return AuthResponse.fromJson(response);
  }

  Future<AuthResponse> register(
      String firstName, String lastName, String email, String password, String phone) async {
    final response = await _apiClient.post(
      ApiConstants.register,
      data: {
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'password': password,
        'phoneNumber': phone,
      },
    );
    return AuthResponse.fromJson(response);
  }
}
