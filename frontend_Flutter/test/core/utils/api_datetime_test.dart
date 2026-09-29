import 'package:flutter_test/flutter_test.dart';
import 'package:smart_hospital/core/utils/api_datetime.dart';

void main() {
  group('ApiDateTime', () {
    test('parses UTC API timestamps for local display and preserves the instant', () {
      final local = ApiDateTime.parseUtcToLocal('2026-10-15T08:00:00Z');

      expect(local.isUtc, isFalse);
      expect(local.toUtc(), DateTime.utc(2026, 10, 15, 8));
      expect(
        DateTime.parse(ApiDateTime.toUtcIso8601(local)),
        DateTime.utc(2026, 10, 15, 8),
      );
    });

    test('treats timestamps without an offset as UTC', () {
      final local = ApiDateTime.parseUtcToLocal('2026-10-15T08:00:00');

      expect(local.toUtc(), DateTime.utc(2026, 10, 15, 8));
    });

    test('round trips a timestamp across the local day boundary', () {
      final utc = DateTime.utc(2026, 10, 15, 19, 30);
      final local = ApiDateTime.parseUtcToLocal(utc.toIso8601String());
      final submitted = DateTime.parse(ApiDateTime.toUtcIso8601(local));

      expect(submitted, utc);
      expect(submitted.day, 15);
    });
  });
}
