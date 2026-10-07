import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/password_field.dart';
import '../../widgets/app_button.dart';
import '../../widgets/error_message.dart';
import '../../core/utils/validators.dart';
import '../../core/utils/form_focus.dart';

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void dispose() {
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _changePassword() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    authProvider.clearError();
    if (!_formKey.currentState!.validate()) {
      focusFirstInvalidField(_formKey);
      return;
    }

    FocusScope.of(context).unfocus();

    final success = await authProvider.changePassword(
      _currentPasswordController.text,
      _newPasswordController.text,
    );

    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Password changed successfully. Please log in again.'),
        ),
      );
      // Simulate backend behavior by forcing logout after password change
      await authProvider.logout();
      if (mounted) {
        context.go('/login');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Change Password')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                ErrorMessage(message: authProvider.error),

                PasswordField(
                  label: 'Current Password',
                  hint: 'Enter your current password',
                  controller: _currentPasswordController,
                  isRequired: true,
                  validator: Validators.validateCurrentPassword,
                ),

                PasswordField(
                  label: 'New Password',
                  hint: 'Enter a new password',
                  controller: _newPasswordController,
                  isRequired: true,
                  validator: (val) {
                    final strength = Validators.validatePassword(val);
                    if (strength != null) return strength;
                    return val == _currentPasswordController.text
                        ? 'New password must be different from the current password.'
                        : null;
                  },
                ),

                PasswordField(
                  label: 'Confirm New Password',
                  hint: 'Confirm your new password',
                  controller: _confirmPasswordController,
                  isRequired: true,
                  validator: (val) => Validators.validateConfirmPassword(
                    val,
                    _newPasswordController.text,
                  ),
                ),

                const SizedBox(height: 24),

                AppButton(
                  text: 'Change Password',
                  onPressed: _changePassword,
                  isLoading: authProvider.isLoading,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
