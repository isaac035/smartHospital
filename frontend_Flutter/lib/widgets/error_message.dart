import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'feature_tile.dart';

class ErrorMessage extends StatelessWidget {
  final String? message;

  const ErrorMessage({super.key, this.message});

  @override
  Widget build(BuildContext context) {
    if (message == null || message!.isEmpty) {
      return const SizedBox.shrink();
    }

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: AppTheme.errorContainer,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: AppTheme.errorColor.withValues(alpha: 0.5)),
      ),
      child: Row(
        children: [
          const IconBadge(
            icon: Icons.error_outline_rounded,
            tint: AppTheme.errorContainer,
            foreground: AppTheme.errorColor,
            size: 40,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Text(message!, style: TextStyle(color: AppTheme.errorColor)),
          ),
        ],
      ),
    );
  }
}
