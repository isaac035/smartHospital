import 'package:flutter/material.dart';
import 'app_text_field.dart';

class PasswordField extends StatefulWidget {
  final String label;
  final String? hint;
  final TextEditingController? controller;
  final String? Function(String?)? validator;

  const PasswordField({
    super.key,
    required this.label,
    this.hint,
    this.controller,
    this.validator,
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
      suffixIcon: IconButton(
        icon: Icon(
          _obscure ? Icons.visibility_off : Icons.visibility,
          color: Colors.grey,
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
