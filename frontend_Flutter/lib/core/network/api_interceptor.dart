import 'package:dio/dio.dart';
import '../storage/secure_storage_service.dart';

class ApiInterceptor extends Interceptor {
  final SecureStorageService _storageService;

  ApiInterceptor(this._storageService);

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await _storageService.getToken();
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    options.headers['Content-Type'] = 'application/json';
    options.headers['Accept'] = 'application/json';
    return handler.next(options);
  }
}
