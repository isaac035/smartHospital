import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/app_text_field.dart';
import '../../widgets/app_button.dart';
import '../../widgets/error_message.dart';
import '../../core/utils/validators.dart';
import '../../core/utils/form_focus.dart';

class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key});

  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _firstNameController;
  late TextEditingController _lastNameController;
  late TextEditingController _phoneController;

  @override
  void initState() {
    super.initState();
    final user = context.read<AuthProvider>().currentUser;
    _firstNameController = TextEditingController(text: user?.firstName ?? '');
    _lastNameController = TextEditingController(text: user?.lastName ?? '');
    _phoneController = TextEditingController(text: user?.phoneNumber ?? '');
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _updateProfile() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    authProvider.clearError();
    if (!_formKey.currentState!.validate()) {
      focusFirstInvalidField(_formKey);
      return;
    }

    FocusScope.of(context).unfocus();

    final success = await authProvider.updateProfile(
      _firstNameController.text.trim(),
      _lastNameController.text.trim(),
      _phoneController.text.trim(),
    );

    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Profile updated successfully.')),
      );
      context.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Edit Profile')),
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
                  controller: _firstNameController,
                  isRequired: true,
                  maxLength: 100,
                  errorText: authProvider.fieldErrors['firstName'],
                  onChanged: (_) => authProvider.clearFieldError('firstName'),
                  validator: (val) =>
                      Validators.validateName(val, 'First name'),
                ),

                AppTextField(
                  label: 'Last Name',
                  controller: _lastNameController,
                  isRequired: true,
                  maxLength: 100,
                  errorText: authProvider.fieldErrors['lastName'],
                  onChanged: (_) => authProvider.clearFieldError('lastName'),
                  validator: (val) => Validators.validateName(val, 'Last name'),
                ),

                AppTextField(
                  label: 'Phone Number',
                  controller: _phoneController,
                  keyboardType: TextInputType.phone,
                  isRequired: true,
                  maxLength: 20,
                  inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9+()\- ]'))],
                  errorText: authProvider.fieldErrors['phoneNumber'],
                  onChanged: (_) => authProvider.clearFieldError('phoneNumber'),
                  validator: (val) => Validators.validatePhone(val),
                ),

                const SizedBox(height: 24),

                AppButton(
                  text: 'Save Changes',
                  onPressed: _updateProfile,
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
