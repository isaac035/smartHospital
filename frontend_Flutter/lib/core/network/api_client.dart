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

  Future<dynamic> get(String path, {Map<String, dynamic>? queryParameters}) async {
    try {
      final response = await _dio.get(path, queryParameters: queryParameters);
      return response.data;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// [receiveTimeout] overrides the default for slow endpoints (e.g. AI triage).
  Future<dynamic> post(String path, {dynamic data, Duration? receiveTimeout}) async {
    try {
      final response = await _dio.post(
        path,
        data: data,
        options: receiveTimeout == null ? null : Options(receiveTimeout: receiveTimeout),
      );
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
      if (e.response?.statusCode == 401) {
        return UnauthorizedException();
      }
      
      final data = e.response?.data;
      String message = 'An unexpected error occurred.';
      String? code;
      var refreshRecommendations = false;
      if (data is Map<String, dynamic> && data.containsKey('message')) {
        final responseMessage = data['message'];
        if (responseMessage is String && responseMessage.isNotEmpty) message = responseMessage;
        final responseCode = data['code'];
        if (responseCode is String) code = responseCode;
        refreshRecommendations = data['refreshRecommendations'] == true;
      }
      final fieldErrors = <String, String>{};
      final errors = data is Map<String, dynamic> ? data['errors'] : null;
      if (errors is Map) {
        errors.forEach((key, value) {
          final text = value is List && value.isNotEmpty ? value.first.toString() : value?.toString();
          if (text == null || text.isEmpty) return;
          final field = key.toString().replaceFirst(r'$.', '');
          final camel = field.isEmpty ? field : field[0].toLowerCase() + field.substring(1);
          fieldErrors[camel] = text;
        });
        if (fieldErrors.isNotEmpty && message == 'An unexpected error occurred.') {
          message = fieldErrors.values.first;
        }
      }
      if (message == 'An unexpected error occurred.' && data is Map<String, dynamic>) {
        final detail = data['detail'] ?? data['title'];
        if (detail is String && detail.isNotEmpty) message = detail;
      }
      if (message == 'An unexpected error occurred.') {
        message = switch (e.response?.statusCode ?? 0) {
          400 => 'The report request was not accepted. Check the selected patient and try again.',
          403 => 'You are not allowed to generate this patient report.',
          404 => 'The patient or appointment record could not be found.',
          429 => 'Too many requests. Please wait a moment and try again.',
          >= 500 => 'The report service is temporarily unavailable. Please try again.',
          _ => 'The report request failed. Please try again.',
        };
      }
      return ApiException(message, e.response?.statusCode, code, refreshRecommendations, fieldErrors);
    } else if (e.type == DioExceptionType.connectionTimeout || e.type == DioExceptionType.receiveTimeout) {
      return ApiException('The report service took too long to respond. Please retry; a saved report will remain available in your list.', null, 'NETWORK_TIMEOUT');
    } else if (e.type == DioExceptionType.connectionError) {
      return ApiException('Unable to connect to the server. Please check your internet connection.');
    }
    return ApiException(e.message ?? 'An unknown network error occurred.');
  }
}
