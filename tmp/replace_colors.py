import re

filepath = r"C:\Users\idppl\Documents\SLIIT\Y3S1\SEF\SE_PROJECT\smartHospital\frontend_Flutter\lib\screens\home\home_screen.dart"

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace import
content = content.replace("import '../../core/theme/app_theme.dart';", "import '../../theme/app_theme.dart';")

replacements = {
    "AppTheme.backgroundColor": "AppColors.background",
    "AppTheme.pageTopTint": "AppColors.surface",
    "AppTheme.transparentColor": "Colors.transparent",
    "AppTheme.primaryColor": "AppColors.primary",
    "AppTheme.onPrimary": "Colors.white",
    "AppTheme.borderColor": "AppColors.border",
    
    "AppTheme.serviceAppointmentsTint": "AppColors.primary.withOpacity(0.15)",
    "AppTheme.serviceAppointmentsForeground": "AppColors.primary",
    
    "AppTheme.serviceQueueTint": "AppColors.warning.withOpacity(0.15)",
    "AppTheme.serviceQueueForeground": "AppColors.warning",
    
    "AppTheme.serviceDoctorsTint": "AppColors.success.withOpacity(0.15)",
    "AppTheme.serviceDoctorsForeground": "AppColors.success",
    
    "AppTheme.serviceRecordsTint": "AppColors.secondary.withOpacity(0.15)",
    "AppTheme.serviceRecordsForeground": "AppColors.secondary",
    
    "AppTheme.serviceAdmissionTint": "AppColors.error.withOpacity(0.15)",
    "AppTheme.serviceAdmissionForeground": "AppColors.error",
    
    "const Color(0xFFD2E3F0)": "AppColors.primaryDark.withOpacity(0.15)",
    
    "AppTheme.heroGradientEnd": "AppColors.primaryDark",
    "AppTheme.onPrimaryMuted": "Colors.white70",
    "AppTheme.radiusLarge": "24.0",
}

for k, v in replacements.items():
    content = content.replace(k, v)
    
# Wait, let's fix `withOpacity` vs `withValues(alpha: ...)`. Flutter 3.27+ deprecates withOpacity. In the original file it was `AppTheme.primaryColor.withValues(alpha: .16)`.
# Let's just use withValues(alpha: ...)
content = content.replace(".withOpacity(0.15)", ".withValues(alpha: 0.15)")

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)
print("Done")
