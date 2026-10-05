class ApiException implements Exception {
  final String message;
  final int? statusCode;
  final String? code;
  final bool refreshRecommendations;

  ApiException(this.message, [this.statusCode, this.code, this.refreshRecommendations = false]);

  @override
  String toString() => message;
}

class UnauthorizedException extends ApiException {
  UnauthorizedException([String message = 'Session expired. Please log in again.']) 
      : super(message, 401);
}
