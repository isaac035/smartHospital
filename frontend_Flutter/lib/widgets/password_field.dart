import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'app_text_field.dart';

class PasswordField extends StatefulWidget {
  final String label;
  final String? hint;
  final TextEditingController? controller;
  final String? Function(String?)? validator;
  final bool isRequired;
  final String? errorText;
  final ValueChanged<String>? onChanged;

  const PasswordField({
    super.key,
    required this.label,
    this.hint,
    this.controller,
    this.validator,
    this.isRequired = false,
    this.errorText,
    this.onChanged,
  });

  @override
  State<PasswordField> createState() => _PasswordFieldState();
}

class _PasswordFieldState extends State<PasswordField> {
  bool _obscure = true;

  @override
  Widget build(BuildContext context) {
    return AppTextField(
      label: widget.label,
      hint: widget.hint,
      controller: widget.controller,
      obscureText: _obscure,
      validator: widget.validator,
      isRequired: widget.isRequired,
      errorText: widget.errorText,
      onChanged: widget.onChanged,
      maxLength: 128,
      suffixIcon: IconButton(
        icon: Icon(
          _obscure ? Icons.visibility_off : Icons.visibility,
          color: AppTheme.textSecondary,
        ),
        onPressed: () {
          setState(() {
            _obscure = !_obscure;
          });
        },
      ),
    );
  }
}
