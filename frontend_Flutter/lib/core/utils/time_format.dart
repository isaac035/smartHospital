import 'package:intl/intl.dart';

/// Formats a backend "HH:mm:ss" time string (e.g. "09:00:00") into a
/// human-readable form (e.g. "9:00 AM"). Returns the raw string unchanged
/// if it can't be parsed.
String formatTimeOfDayString(String raw) {
  try {
    final parsed = DateFormat('HH:mm:ss').parse(raw);
    return DateFormat('h:mm a').format(parsed);
  } catch (_) {
    return raw;
  }
}
