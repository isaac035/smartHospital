import 'package:flutter/material.dart';
import '../../theme/app_theme.dart' as new_theme;

class AppTheme {
  // Map old names to new dark theme AppColors
  static const Color primaryColor = new_theme.AppColors.primary;
  static const Color secondaryColor = new_theme.AppColors.secondary;
  static const Color accentColor = new_theme.AppColors.primaryDark;
  static const Color backgroundColor = new_theme.AppColors.background;
  static const Color surfaceColor = new_theme.AppColors.surface;
  static const Color textPrimary = new_theme.AppColors.textPrimary;
  static const Color textSecondary = new_theme.AppColors.textSecondary;
  static const Color textMuted = new_theme.AppColors.textHint;
  static const Color borderColor = new_theme.AppColors.border;
  static const Color errorColor = new_theme.AppColors.error;
  static const Color successColor = new_theme.AppColors.success;
  static const Color warningColor = new_theme.AppColors.warning;
  static const Color infoColor = new_theme.AppColors.primary;
  static const Color onPrimary = Colors.white;
  static const Color transparentColor = Colors.transparent;
  static const Color errorContainer = Color(0xFF2C1014); 
  static const Color successContainer = Color(0xFF0C2419); 
  static const Color warningContainer = Color(0xFF2E1C00); 
  static const Color infoContainer = Color(0xFF091F2C); 
  static const Color accentContainer = Color(0xFF0A1F29); 
  static const Color neutralContainer = new_theme.AppColors.elevated;
  static const Color onPrimaryMuted = new_theme.AppColors.textSecondary;
  
  static const Color serviceAppointmentsTint = Color(0xFF132A3B);
  static const Color serviceAppointmentsForeground = new_theme.AppColors.primary;
  static const Color serviceQueueTint = Color(0xFF152A36);
  static const Color serviceQueueForeground = new_theme.AppColors.primaryDark;
  static const Color serviceDoctorsTint = Color(0xFF15312F);
  static const Color serviceDoctorsForeground = new_theme.AppColors.success;
  static const Color serviceRecordsTint = Color(0xFF1D1B36);
  static const Color serviceRecordsForeground = new_theme.AppColors.secondary;
  static const Color serviceAdmissionTint = Color(0xFF332000);
  static const Color serviceAdmissionForeground = new_theme.AppColors.warning;
  static const Color heroGradientEnd = new_theme.AppColors.primaryDark;
  static const Color tileShadowColor = Colors.transparent;
  static const Color pageTopTint = new_theme.AppColors.background;

  static const double radiusSmall = 6.0;
  static const double radiusMedium = 12.0;
  static const double radiusLarge = 24.0;

  static ThemeData get lightTheme => new_theme.AppTheme.darkTheme();
  static ThemeData get darkTheme => new_theme.AppTheme.darkTheme();

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
}
