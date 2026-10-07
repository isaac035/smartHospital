class ApiException implements Exception {
  final String message;
  final int? statusCode;
  final String? code;
  final bool refreshRecommendations;

  /// Field-level validation errors from a 400 ValidationProblemDetails response,
  /// keyed by camelCase field name (e.g. {'phoneNumber': 'Phone number must ...'}).
  final Map<String, String> fieldErrors;

  ApiException(this.message, [this.statusCode, this.code, this.refreshRecommendations = false, this.fieldErrors = const {}]);

  @override
  String toString() => message;
}

class UnauthorizedException extends ApiException {
  UnauthorizedException([String message = 'Session expired. Please log in again.']) 
      : super(message, 401);
}
