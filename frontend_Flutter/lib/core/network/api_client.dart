import 'package:dio/dio.dart';
import '../constants/api_constants.dart';
import '../storage/secure_storage_service.dart';
import 'api_interceptor.dart';
import 'api_exception.dart';

class ApiClient {
  late final Dio _dio;

  ApiClient(SecureStorageService storageService) {
    _dio = Dio(BaseOptions(
      baseUrl: ApiConstants.baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
    ));

    _dio.interceptors.add(ApiInterceptor(storageService));
  }

  Future<dynamic> get(String path) async {
    try {
      final response = await _dio.get(path);
      return response.data;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<dynamic> post(String path, {dynamic data}) async {
    try {
      final response = await _dio.post(path, data: data);
      return response.data;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<dynamic> put(String path, {dynamic data}) async {
    try {
      final response = await _dio.put(path, data: data);
      return response.data;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Exception _handleError(DioException e) {
    if (e.response != null) {
      final data = e.response?.data;
      String message = 'An unexpected error occurred.';
      if (data is Map<String, dynamic> && data.containsKey('message')) {
        message = data['message'];
      }
      return ApiException(message, e.response?.statusCode);
    } else if (e.type == DioExceptionType.connectionTimeout || e.type == DioExceptionType.receiveTimeout) {
      return ApiException('Connection timed out. Please try again later.');
    } else if (e.type == DioExceptionType.connectionError) {
      return ApiException('Unable to connect to the server. Please check your internet connection.');
    }
    return ApiException(e.message ?? 'An unknown network error occurred.');
  }
}
