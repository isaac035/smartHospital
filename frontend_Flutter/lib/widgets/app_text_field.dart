import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class AppTextField extends StatelessWidget {
  final String label;
  final String? hint;
  final TextEditingController? controller;
  final TextInputType keyboardType;
  final String? Function(String?)? validator;
  final bool obscureText;
  final Widget? suffixIcon;
  final Widget? prefixIcon;
  final bool readOnly;
  final ValueChanged<String>? onChanged;
  final bool enabled;
  final int? maxLines;

  /// Shows a red asterisk after the label.
  final bool isRequired;

  /// Limits input length (no visible counter).
  final int? maxLength;
  final List<TextInputFormatter>? inputFormatters;
  final FocusNode? focusNode;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onFieldSubmitted;

  /// An error from the server for this field (e.g. "A user with this email already exists.").
  final String? errorText;

  /// Validate as the user types once they have interacted with the field.
  final AutovalidateMode autovalidateMode;

  const AppTextField({
    super.key,
    required this.label,
    this.hint,
    this.controller,
    this.keyboardType = TextInputType.text,
    this.validator,
    this.obscureText = false,
    this.suffixIcon,
    this.prefixIcon,
    this.readOnly = false,
    this.onChanged,
    this.enabled = true,
    this.maxLines = 1,
    this.isRequired = false,
    this.maxLength,
    this.inputFormatters,
    this.focusNode,
    this.textInputAction,
    this.onFieldSubmitted,
    this.errorText,
    this.autovalidateMode = AutovalidateMode.onUserInteraction,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text.rich(
            TextSpan(
              text: label,
              children: [
                if (isRequired)
                  TextSpan(text: ' *', style: TextStyle(color: theme.colorScheme.error)),
              ],
            ),
            style: theme.textTheme.labelLarge,
          ),
          const SizedBox(height: 8),
          TextFormField(
            controller: controller,
            keyboardType: keyboardType,
            validator: validator,
            obscureText: obscureText,
            readOnly: readOnly,
            onChanged: onChanged,
            enabled: enabled,
            maxLines: maxLines,
            focusNode: focusNode,
            textInputAction: textInputAction,
            onFieldSubmitted: onFieldSubmitted,
            autovalidateMode: autovalidateMode,
            forceErrorText: errorText,
            inputFormatters: [
              if (maxLength != null) LengthLimitingTextInputFormatter(maxLength),
              ...?inputFormatters,
            ],
            decoration: InputDecoration(
              hintText: hint,
              suffixIcon: suffixIcon,
              prefixIcon: prefixIcon,
            ),
          ),
        ],
      ),
    );
  }
}
