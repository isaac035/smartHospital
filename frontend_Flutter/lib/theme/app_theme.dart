import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class AppColors {
  static const Color background = Color(0xFF0B1220);
  static const Color surface = Color(0xFF111A2E);
  static const Color elevated = Color(0xFF17233D);
  static const Color border = Color(0xFF1E3054);
  static const Color primary = Color(0xFF00BCD4);
  static const Color primaryDark = Color(0xFF0097A7);
  static const Color secondary = Color(0xFF7C4DFF);
  static const Color success = Color(0xFF4CAF50);
  static const Color warning = Color(0xFFFF9800);
  static const Color error = Color(0xFFEF5350);
  static const Color textPrimary = Color(0xFFEAEFF4);
  static const Color textSecondary = Color(0xFF8899AA);
  static const Color textHint = Color(0xFF5C6F82);
  static const Color divider = Color(0xFF1E3054);

  static const LinearGradient primaryGradient = LinearGradient(
    colors: [Color(0xFF00BCD4), Color(0xFF0097A7)],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  static const LinearGradient secondaryGradient = LinearGradient(
    colors: [Color(0xFF7C4DFF), Color(0xFF651FFF)],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  static const LinearGradient loginBgGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF0B1220), Color(0xFF0D1B2A), Color(0xFF112240)],
  );
}

class AppTheme {
  static ThemeData darkTheme() {
    final textTheme = GoogleFonts.interTextTheme(ThemeData.dark().textTheme).copyWith(
      headlineLarge: GoogleFonts.inter(fontSize: 28, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
      headlineMedium: GoogleFonts.inter(fontSize: 24, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
      headlineSmall: GoogleFonts.inter(fontSize: 20, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
      titleLarge: GoogleFonts.inter(fontSize: 18, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
      titleMedium: GoogleFonts.inter(fontSize: 16, fontWeight: FontWeight.w500, color: AppColors.textPrimary),
      titleSmall: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w500, color: AppColors.textPrimary),
      bodyLarge: GoogleFonts.inter(fontSize: 16, color: AppColors.textPrimary),
      bodyMedium: GoogleFonts.inter(fontSize: 14, color: AppColors.textPrimary),
      bodySmall: GoogleFonts.inter(fontSize: 12, color: AppColors.textSecondary),
      labelLarge: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
      labelMedium: GoogleFonts.inter(fontSize: 12, fontWeight: FontWeight.w500, color: AppColors.textSecondary),
      labelSmall: GoogleFonts.inter(fontSize: 10, fontWeight: FontWeight.w500, color: AppColors.textSecondary),
    );

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: AppColors.background,
      canvasColor: AppColors.elevated,
      textTheme: textTheme,
      colorScheme: const ColorScheme.dark(
        primary: AppColors.primary,
        secondary: AppColors.secondary,
        surface: AppColors.surface,
        error: AppColors.error,
        onPrimary: Colors.white,
        onSecondary: Colors.white,
        onSurface: AppColors.textPrimary,
        onError: Colors.white,
      ),
      appBarTheme: AppBarTheme(
        backgroundColor: AppColors.surface,
        foregroundColor: AppColors.textPrimary,
        elevation: 0,
        centerTitle: true,
        titleTextStyle: GoogleFonts.inter(
          fontSize: 18,
          fontWeight: FontWeight.w600,
          color: AppColors.textPrimary,
        ),
        iconTheme: const IconThemeData(color: AppColors.textPrimary),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: AppColors.surface,
        selectedItemColor: AppColors.primary,
        unselectedItemColor: AppColors.textHint,
        type: BottomNavigationBarType.fixed,
        elevation: 8,
      ),
      cardTheme: CardThemeData(
        color: AppColors.elevated,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: AppColors.border, width: 1),
        ),
        margin: EdgeInsets.zero,
      ),
      chipTheme: ChipThemeData(
        backgroundColor: AppColors.elevated,
        labelStyle: GoogleFonts.inter(fontSize: 12, color: AppColors.textPrimary),
        side: const BorderSide(color: AppColors.border),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          textStyle: GoogleFonts.inter(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primary,
          side: const BorderSide(color: AppColors.primary),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          textStyle: GoogleFonts.inter(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.primary,
          textStyle: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w500),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.surface,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.border),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.primary, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.error),
        ),
        labelStyle: GoogleFonts.inter(color: AppColors.textSecondary),
        hintStyle: GoogleFonts.inter(color: AppColors.textHint),
        prefixIconColor: AppColors.textSecondary,
        suffixIconColor: AppColors.textSecondary,
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: AppColors.elevated,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        titleTextStyle: GoogleFonts.inter(fontSize: 20, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: AppColors.elevated,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(16))),
      ),
      timePickerTheme: TimePickerThemeData(
        backgroundColor: AppColors.elevated,
        hourMinuteTextColor: AppColors.textPrimary,
        hourMinuteColor: AppColors.surface,
        dialHandColor: AppColors.primary,
        dialBackgroundColor: AppColors.surface,
        dialTextColor: AppColors.textPrimary,
      ),
      checkboxTheme: CheckboxThemeData(
        fillColor: WidgetStateProperty.resolveWith((states) => states.contains(WidgetState.selected) ? AppColors.primary : AppColors.surface),
        checkColor: WidgetStateProperty.all(Colors.white),
      ),
      switchTheme: SwitchThemeData(
        thumbColor: WidgetStateProperty.resolveWith((states) => states.contains(WidgetState.selected) ? AppColors.primary : AppColors.textSecondary),
        trackColor: WidgetStateProperty.resolveWith((states) => states.contains(WidgetState.selected) ? AppColors.primary.withValues(alpha: 0.5) : AppColors.surface),
      ),
      tabBarTheme: const TabBarThemeData(
        labelColor: AppColors.primary,
        unselectedLabelColor: AppColors.textSecondary,
        indicatorColor: AppColors.primary,
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: AppColors.elevated,
        contentTextStyle: GoogleFonts.inter(color: AppColors.textPrimary),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        behavior: SnackBarBehavior.floating,
      ),
      dividerTheme: const DividerThemeData(
        color: AppColors.divider,
        thickness: 1,
      ),
      datePickerTheme: DatePickerThemeData(
        backgroundColor: AppColors.elevated,
        headerBackgroundColor: AppColors.primary,
        headerForegroundColor: Colors.white,
        dayForegroundColor: WidgetStateProperty.all(AppColors.textPrimary),
        todayForegroundColor: WidgetStateProperty.all(AppColors.primary),
        todayBackgroundColor: WidgetStateProperty.all(Colors.transparent),
        todayBorder: const BorderSide(color: AppColors.primary),
      ),
      dropdownMenuTheme: DropdownMenuThemeData(
        menuStyle: MenuStyle(
          backgroundColor: WidgetStateProperty.all(AppColors.elevated),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: AppColors.surface,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: const BorderSide(color: AppColors.border),
          ),
        ),
      ),
      popupMenuTheme: PopupMenuThemeData(
        color: AppColors.elevated,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      listTileTheme: const ListTileThemeData(
        textColor: AppColors.textPrimary,
        iconColor: AppColors.primary,
      ),
      iconTheme: const IconThemeData(color: AppColors.textSecondary),
      progressIndicatorTheme: const ProgressIndicatorThemeData(
        color: AppColors.primary,
      ),
    );
  }
}
