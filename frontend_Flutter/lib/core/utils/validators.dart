/// Shared form validation for the patient app.
///
/// Rules and messages mirror the backend
/// (backend/SmartHospital.Api/Validation/ValidationAttributes.cs and the request DTOs)
/// and the React admin (frontend_React/src/utils/validators.js).
/// Every validator returns an error message, or null when the value is valid.
class Validators {
  static final RegExp _emailPattern = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]{2,}$');
  static final RegExp _namePattern = RegExp(r"^\p{L}[\p{L}\p{M} .'\-]*$", unicode: true);
  static final RegExp _phonePattern = RegExp(r'^\+?[0-9 ()\-]+$');
  static final RegExp _htmlPattern = RegExp(r'<\s*/?\s*[A-Za-z!?]|javascript\s*:', caseSensitive: false);
  static final RegExp _letterPattern = RegExp(r'\p{L}', unicode: true);

  static const String passwordRuleMessage =
      'Password must be 8 to 128 characters and include an uppercase letter, a lowercase letter, a number and a special character.';
  static const String phoneRuleMessage =
      'Phone number must contain 7 to 15 digits and may include +, spaces, hyphens or brackets.';

  static bool _isBlank(String? value) => value == null || value.trim().isEmpty;

  static String? validateEmail(String? value) {
    // Screens trim before submitting, so validate the trimmed value too
    final email = value?.trim() ?? '';
    if (email.isEmpty) return 'Email is required.';
    if (email.length > 255) return 'Email cannot exceed 255 characters.';
    if (!_emailPattern.hasMatch(email)) return 'Email must be a valid email address.';
    return null;
  }

  /// Login only checks that a password was entered; strength rules apply to new passwords.
  static String? validateLoginPassword(String? value) {
    if (value == null || value.isEmpty) return 'Password is required.';
    return null;
  }

  /// New passwords (registration / change password): same strength rule as the backend.
  static String? validatePassword(String? value) {
    if (value == null || value.isEmpty) return 'Password is required.';
    final ok = value.length >= 8 &&
        value.length <= 128 &&
        RegExp(r'[A-Z]').hasMatch(value) &&
        RegExp(r'[a-z]').hasMatch(value) &&
        RegExp(r'[0-9]').hasMatch(value) &&
        RegExp(r'[^A-Za-z0-9\s]').hasMatch(value);
    return ok ? null : passwordRuleMessage;
  }

  /// The current password only needs to be entered (it may predate the strength rule).
  static String? validateCurrentPassword(String? value) {
    if (value == null || value.isEmpty) return 'Current password is required.';
    return null;
  }

  static String? validateConfirmPassword(String? value, String password) {
    if (value == null || value.isEmpty) return 'Please confirm your password.';
    if (value != password) return 'Passwords do not match.';
    return null;
  }

  static String? validateName(String? value, String fieldName, {int max = 100}) {
    if (_isBlank(value)) return '$fieldName is required.';
    final name = value!.trim();
    if (name.length < 2) return '$fieldName must be at least 2 characters.';
    if (name.length > max) return '$fieldName cannot exceed $max characters.';
    if (!_namePattern.hasMatch(name)) {
      return '$fieldName may only contain letters, spaces, hyphens, apostrophes and dots.';
    }
    return null;
  }

  static String? validatePhone(String? value, {bool required = true}) {
    if (_isBlank(value)) return required ? 'Phone number is required.' : null;
    final phone = value!.trim();
    final digits = RegExp(r'[0-9]').allMatches(phone).length;
    if (!_phonePattern.hasMatch(phone) || digits < 7 || digits > 15) return phoneRuleMessage;
    return null;
  }

  /// Free text: optional/required, length limits, no HTML or script markup.
  static String? validateText(
    String? value,
    String fieldName, {
    bool required = false,
    int? min,
    int? max,
    bool mustContainLetters = false,
  }) {
    if (_isBlank(value)) return required ? '$fieldName is required.' : null;
    final text = value!.trim();
    if (min != null && text.length < min) return '$fieldName must be at least $min characters.';
    if (max != null && text.length > max) return '$fieldName cannot exceed $max characters.';
    if (mustContainLetters && !_letterPattern.hasMatch(text)) {
      return '$fieldName must contain words, not only numbers or symbols.';
    }
    if (_htmlPattern.hasMatch(text)) return '$fieldName must not contain HTML or script tags.';
    return null;
  }

  /// Smart Care symptom text: same limits as TriageDoctorMatchRequest (3-1000 characters, words required).
  static String? validateSymptoms(String? value) {
    if (_isBlank(value)) return 'Please describe your symptoms or what you need help with.';
    final text = value!.trim();
    if (text.length < 3 || text.length > 1000) return 'Please use between 3 and 1000 characters.';
    if (!_letterPattern.hasMatch(text)) return 'Please describe your symptoms in words, not only numbers or symbols.';
    if (_htmlPattern.hasMatch(text)) return 'Symptoms must not contain HTML or script tags.';
    return null;
  }

  static String? validateSelection(Object? value, String what) =>
      value == null || (value is String && value.trim().isEmpty) ? 'Please select $what.' : null;
}
