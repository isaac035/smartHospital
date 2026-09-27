/// Date helpers for the hospital's fixed Asia/Colombo calendar.
class HospitalDate {
  HospitalDate._();

  static DateTime dateOnly(DateTime value) {
    final colombo = value.toUtc().add(const Duration(hours: 5, minutes: 30));
    return DateTime.utc(colombo.year, colombo.month, colombo.day);
  }

  static bool isToday(DateTime value, {DateTime? now}) =>
      dateOnly(value) == dateOnly(now ?? DateTime.now());
}
