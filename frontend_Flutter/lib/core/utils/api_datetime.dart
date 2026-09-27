/// Converts API timestamps to the device's local time for display and back to
/// UTC for requests. API timestamps without an offset are treated as UTC too.
class ApiDateTime {
  ApiDateTime._();

  static final RegExp _timezoneSuffix =
      RegExp(r'(?:[zZ]|[+-]\d{2}:?\d{2})$');

  static DateTime parseUtcToLocal(String value) {
    final timestamp = _timezoneSuffix.hasMatch(value) ? value : '${value}Z';
    return DateTime.parse(timestamp).toLocal();
  }

  static String toUtcIso8601(DateTime value) =>
      value.toUtc().toIso8601String();
}
