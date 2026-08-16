import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/app_text_field.dart';
import '../../widgets/password_field.dart';
import '../../widgets/app_button.dart';
import '../../widgets/error_message.dart';
import '../../core/utils/validators.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _register() async {
    if (!_formKey.currentState!.validate()) return;

    FocusScope.of(context).unfocus();

    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    final success = await authProvider.register(
      _firstNameController.text.trim(),
      _lastNameController.text.trim(),
      _emailController.text.trim(),
      _passwordController.text,
      _phoneController.text.trim(),
    );

    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Registration successful. Please log in.')),
      );
      context.pop(); // Go back to login screen
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Create Account'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                ErrorMessage(message: authProvider.error),
                
                AppTextField(
                  label: 'First Name',
                  hint: 'Enter your first name',
                  controller: _firstNameController,
                  validator: (val) => Validators.validateName(val, 'First Name'),
                ),
                
                AppTextField(
                  label: 'Last Name',
                  hint: 'Enter your last name',
                  controller: _lastNameController,
                  validator: (val) => Validators.validateName(val, 'Last Name'),
                ),
                
                AppTextField(
                  label: 'Email',
                  hint: 'Enter your email address',
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  validator: Validators.validateEmail,
                ),
                
                AppTextField(
                  label: 'Phone Number',
                  hint: 'Enter your phone number',
                  controller: _phoneController,
                  keyboardType: TextInputType.phone,
                  validator: Validators.validatePhone,
                ),
                
                PasswordField(
                  label: 'Password',
                  hint: 'Create a password',
                  controller: _passwordController,
                  validator: Validators.validatePassword,
                ),
                
                PasswordField(
                  label: 'Confirm Password',
                  hint: 'Confirm your password',
                  controller: _confirmPasswordController,
                  validator: (val) => Validators.validateConfirmPassword(val, _passwordController.text),
                ),
                
                const SizedBox(height: 24),
                
                AppButton(
                  text: 'Register',
                  onPressed: _register,
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
