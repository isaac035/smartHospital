import 'package:flutter/material.dart';

/// Shared color, radius, spacing, and typography tokens for the patient app.
class AppTheme {
  static const Color primaryColor = Color(0xFF143B52);
  static const Color secondaryColor = Color(0xFF147D77);
  static const Color accentColor = Color(0xFF2D8792);
  static const Color backgroundColor = Color(0xFFF4F7F8);
  static const Color surfaceColor = Color(0xFFFFFFFF);
  static const Color textPrimary = Color(0xFF18313F);
  static const Color textSecondary = Color(0xFF607583);
  static const Color textMuted = Color(0xFF8495A0);
  static const Color borderColor = Color(0xFFDCE5E9);
  static const Color errorColor = Color(0xFFB54751);
  static const Color successColor = Color(0xFF26785F);
  static const Color warningColor = Color(0xFF9A6700);
  static const Color infoColor = Color(0xFF356F91);
  static const Color onPrimary = Color(0xFFFFFFFF);
  static const Color transparentColor = Color(0x00000000);

  static const Color errorContainer = Color(0xFFFCEBEC);
  static const Color successContainer = Color(0xFFE8F4EF);
  static const Color warningContainer = Color(0xFFFFF3D8);
  static const Color infoContainer = Color(0xFFEAF3F7);
  static const Color accentContainer = Color(0xFFE8F3F3);
  static const Color neutralContainer = Color(0xFFEEF2F4);
  static const Color onPrimaryMuted = Color(0xFFD8E5EA);

  // Service accents share a cool, moderately saturated palette. The dark
  // foregrounds are intentionally paired with pale surfaces for readable text.
  static const Color serviceAppointmentsTint = Color(0xFFE5EEF5);
  static const Color serviceAppointmentsForeground = Color(0xFF204A68);
  static const Color serviceQueueTint = Color(0xFFE2EDF2);
  static const Color serviceQueueForeground = Color(0xFF28556B);
  static const Color serviceDoctorsTint = Color(0xFFE1F2F0);
  static const Color serviceDoctorsForeground = Color(0xFF1D625D);
  static const Color serviceRecordsTint = Color(0xFFECEAF7);
  static const Color serviceRecordsForeground = Color(0xFF4D4B78);
  static const Color serviceAdmissionTint = Color(0xFFFFF0D6);
  static const Color serviceAdmissionForeground = Color(0xFF755000);
  static const Color heroGradientEnd = Color(0xFF245D76);
  static const Color tileShadowColor = primaryColor;
  static const Color pageTopTint = Color(0xFFEAF0F3);

  static const double radiusSmall = 10;
  static const double radiusMedium = 16;
  static const double radiusLarge = 22;
  static const double fontBodySmall = 12;
  static const double fontBodyMedium = 14;
  static const double fontTitleMedium = 15;
  static const double fontTitleSmall = 13;
  static const double fontLabelSmall = 11;
  static const double fontHeadlineSmall = 18;
  static const double fontHeadlineMedium = 20;
  static const double fontDisplaySmall = 24;
  static const double fontDisplayLarge = 32;
  static const double pageMaxWidth = 980;
  static const EdgeInsets pagePadding = EdgeInsets.fromLTRB(20, 20, 20, 28);

  static ThemeData get lightTheme {
    const colorScheme = ColorScheme(
      brightness: Brightness.light,
      primary: primaryColor,
      onPrimary: onPrimary,
      secondary: secondaryColor,
      onSecondary: onPrimary,
      error: errorColor,
      onError: onPrimary,
      surface: surfaceColor,
      onSurface: textPrimary,
      primaryContainer: accentContainer,
      onPrimaryContainer: primaryColor,
      secondaryContainer: accentContainer,
      onSecondaryContainer: primaryColor,
      errorContainer: errorContainer,
      onErrorContainer: errorColor,
      surfaceContainerHighest: neutralContainer,
      onSurfaceVariant: textSecondary,
      outline: borderColor,
      outlineVariant: borderColor,
      shadow: Color(0x14183240),
      scrim: Color(0x660E2633),
      inverseSurface: primaryColor,
      onInverseSurface: onPrimary,
      inversePrimary: Color(0xFF9BC7C7),
      surfaceTint: secondaryColor,
    );

    const textTheme = TextTheme(
      displayLarge: TextStyle(
        fontSize: 32,
        height: 1.15,
        fontWeight: FontWeight.w700,
        color: textPrimary,
      ),
      displayMedium: TextStyle(
        fontSize: 28,
        height: 1.2,
        fontWeight: FontWeight.w700,
        color: textPrimary,
      ),
      displaySmall: TextStyle(
        fontSize: 24,
        height: 1.25,
        fontWeight: FontWeight.w700,
        color: textPrimary,
      ),
      headlineLarge: TextStyle(
        fontSize: 22,
        height: 1.25,
        fontWeight: FontWeight.w700,
        color: textPrimary,
      ),
      headlineMedium: TextStyle(
        fontSize: 20,
        height: 1.3,
        fontWeight: FontWeight.w700,
        color: textPrimary,
      ),
      headlineSmall: TextStyle(
        fontSize: 18,
        height: 1.35,
        fontWeight: FontWeight.w700,
        color: textPrimary,
      ),
      titleLarge: TextStyle(
        fontSize: 17,
        height: 1.35,
        fontWeight: FontWeight.w700,
        color: textPrimary,
      ),
      titleMedium: TextStyle(
        fontSize: 15,
        height: 1.35,
        fontWeight: FontWeight.w600,
        color: textPrimary,
      ),
      titleSmall: TextStyle(
        fontSize: 13,
        height: 1.35,
        fontWeight: FontWeight.w600,
        color: textPrimary,
      ),
      bodyLarge: TextStyle(fontSize: 16, height: 1.5, color: textPrimary),
      bodyMedium: TextStyle(fontSize: 14, height: 1.5, color: textPrimary),
      bodySmall: TextStyle(fontSize: 12, height: 1.45, color: textSecondary),
      labelLarge: TextStyle(
        fontSize: 14,
        height: 1.25,
        fontWeight: FontWeight.w600,
        color: textPrimary,
      ),
      labelMedium: TextStyle(
        fontSize: 12,
        height: 1.25,
        fontWeight: FontWeight.w600,
        color: textSecondary,
      ),
      labelSmall: TextStyle(
        fontSize: 11,
        height: 1.25,
        fontWeight: FontWeight.w600,
        color: textSecondary,
      ),
    );

    final roundedBorder = OutlineInputBorder(
      borderRadius: BorderRadius.circular(radiusMedium),
      borderSide: const BorderSide(color: borderColor),
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: colorScheme,
      textTheme: textTheme,
      scaffoldBackgroundColor: backgroundColor,
      visualDensity: VisualDensity.standard,
      appBarTheme: const AppBarTheme(
        backgroundColor: backgroundColor,
        foregroundColor: textPrimary,
        centerTitle: false,
        elevation: 0,
        scrolledUnderElevation: 0,
        titleTextStyle: TextStyle(
          fontSize: 19,
          fontWeight: FontWeight.w700,
          color: textPrimary,
        ),
        iconTheme: IconThemeData(color: primaryColor),
      ),
      iconTheme: const IconThemeData(color: textSecondary, size: 22),
      cardTheme: CardThemeData(
        color: surfaceColor,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        margin: const EdgeInsets.symmetric(vertical: 6),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusMedium),
          side: const BorderSide(color: borderColor),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: surfaceColor,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 15,
        ),
        hintStyle: textTheme.bodyMedium?.copyWith(color: textMuted),
        labelStyle: textTheme.labelLarge?.copyWith(color: textSecondary),
        prefixIconColor: textMuted,
        suffixIconColor: textMuted,
        border: roundedBorder,
        enabledBorder: roundedBorder,
        focusedBorder: roundedBorder.copyWith(
          borderSide: const BorderSide(color: secondaryColor, width: 1.5),
        ),
        errorBorder: roundedBorder.copyWith(
          borderSide: const BorderSide(color: errorColor),
        ),
        focusedErrorBorder: roundedBorder.copyWith(
          borderSide: const BorderSide(color: errorColor, width: 1.5),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primaryColor,
          foregroundColor: onPrimary,
          disabledBackgroundColor: neutralContainer,
          disabledForegroundColor: textMuted,
          minimumSize: const Size(48, 48),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radiusMedium),
          ),
          textStyle: textTheme.labelLarge?.copyWith(color: onPrimary),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: primaryColor,
          foregroundColor: onPrimary,
          disabledBackgroundColor: neutralContainer,
          disabledForegroundColor: textMuted,
          minimumSize: const Size(48, 48),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radiusMedium),
          ),
          textStyle: textTheme.labelLarge?.copyWith(color: onPrimary),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: primaryColor,
          minimumSize: const Size(48, 48),
          padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 13),
          side: const BorderSide(color: borderColor),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radiusMedium),
          ),
          textStyle: textTheme.labelLarge,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: secondaryColor,
          minimumSize: const Size(44, 44),
          textStyle: textTheme.labelLarge?.copyWith(color: secondaryColor),
        ),
      ),
      listTileTheme: const ListTileThemeData(
        iconColor: secondaryColor,
        textColor: textPrimary,
        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        minVerticalPadding: 12,
      ),
      chipTheme: ChipThemeData(
        backgroundColor: neutralContainer,
        selectedColor: accentContainer,
        disabledColor: neutralContainer,
        side: const BorderSide(color: borderColor),
        labelStyle:
            textTheme.labelMedium ?? const TextStyle(color: textSecondary),
        secondaryLabelStyle: textTheme.labelMedium?.copyWith(
          color: primaryColor,
        ),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: surfaceColor,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusLarge),
        ),
        titleTextStyle: textTheme.headlineSmall,
        contentTextStyle: textTheme.bodyMedium?.copyWith(color: textSecondary),
      ),
      datePickerTheme: DatePickerThemeData(
        backgroundColor: surfaceColor,
        surfaceTintColor: Colors.transparent,
        headerBackgroundColor: primaryColor,
        headerForegroundColor: onPrimary,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusLarge),
        ),
        todayForegroundColor: const WidgetStatePropertyAll(secondaryColor),
        todayBorder: const BorderSide(color: secondaryColor),
      ),
      timePickerTheme: TimePickerThemeData(
        backgroundColor: surfaceColor,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusLarge),
        ),
        dialHandColor: secondaryColor,
        hourMinuteColor: accentContainer,
        hourMinuteTextColor: primaryColor,
        dayPeriodColor: accentContainer,
        dayPeriodTextColor: primaryColor,
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: primaryColor,
        contentTextStyle: textTheme.bodyMedium?.copyWith(color: onPrimary),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusMedium),
        ),
        insetPadding: const EdgeInsets.all(16),
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: surfaceColor,
        surfaceTintColor: Colors.transparent,
        showDragHandle: true,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(radiusLarge),
          ),
        ),
      ),
      popupMenuTheme: PopupMenuThemeData(
        color: surfaceColor,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusMedium),
          side: const BorderSide(color: borderColor),
        ),
        textStyle: textTheme.bodyMedium,
      ),
      tooltipTheme: TooltipThemeData(
        decoration: BoxDecoration(
          color: primaryColor,
          borderRadius: BorderRadius.circular(radiusSmall),
        ),
        textStyle: textTheme.bodySmall?.copyWith(color: onPrimary),
      ),
      progressIndicatorTheme: const ProgressIndicatorThemeData(
        color: secondaryColor,
        linearTrackColor: accentContainer,
        circularTrackColor: accentContainer,
      ),
      dividerTheme: const DividerThemeData(
        color: borderColor,
        thickness: 1,
        space: 1,
      ),
    );
  }
}
