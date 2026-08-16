import '../../core/constants/api_constants.dart';
import '../../core/network/api_client.dart';
import '../models/user_model.dart';

class UserService {
  final ApiClient _apiClient;

  UserService(this._apiClient);

  Future<UserModel> getUserProfile(int userId) async {
    final response = await _apiClient.get('${ApiConstants.users}/$userId');
    return UserModel.fromJson(response);
  }

  Future<UserModel> updateUserProfile(
      int userId, String firstName, String lastName, String phone) async {
    final response = await _apiClient.put(
      '${ApiConstants.users}/$userId',
      data: {
        'firstName': firstName,
        'lastName': lastName,
        'phoneNumber': phone,
      },
    );
    return UserModel.fromJson(response);
  }

  // TODO: The backend does not currently have a change-password endpoint.
  // This is a stub for when the backend implements it.
  Future<void> changePassword(String currentPassword, String newPassword) async {
    // await _apiClient.put('/Auth/change-password', data: {
    //   'currentPassword': currentPassword,
    //   'newPassword': newPassword,
    // });
    
    // Simulate network delay for now
    await Future.delayed(const Duration(seconds: 1));
  }
}
