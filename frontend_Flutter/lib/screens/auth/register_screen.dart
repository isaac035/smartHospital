import 'dart:math';
import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
import '../../providers/auth_provider.dart';
import '../../theme/app_theme.dart';
import '../../widgets/app_text_field.dart';
import '../../widgets/password_field.dart';
import '../../widgets/app_button.dart';
import '../../widgets/error_message.dart';
import '../../core/utils/validators.dart';
import '../../core/utils/form_focus.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> with TickerProviderStateMixin {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  late AnimationController _bgController;
  late AnimationController _cardController;
  late Animation<double> _cardFade;
  late Animation<Offset> _cardSlide;

  @override
  void initState() {
    super.initState();
    // Don't carry an error from another auth screen (e.g. a failed login) onto this one.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) Provider.of<AuthProvider>(context, listen: false).clearError();
    });
    _bgController = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 8),
    )..repeat();

    _cardController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    );
    _cardFade = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(parent: _cardController, curve: Curves.easeOut),
    );
    _cardSlide = Tween<Offset>(begin: const Offset(0, 0.08), end: Offset.zero).animate(
      CurvedAnimation(parent: _cardController, curve: Curves.easeOut),
    );
    _cardController.forward();
  }

  @override
  void dispose() {
    _bgController.dispose();
    _cardController.dispose();
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _register() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    authProvider.clearError();
    if (!_formKey.currentState!.validate()) {
      focusFirstInvalidField(_formKey);
      return;
    }

    FocusScope.of(context).unfocus();

    final success = await authProvider.register(
      _firstNameController.text.trim(),
      _lastNameController.text.trim(),
      _emailController.text.trim(),
      _passwordController.text,
      _phoneController.text.trim(),
    );

    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Registration successful. Please log in.'),
          backgroundColor: AppColors.success,
        ),
      );
      context.pop(); // Go back to login screen
    } else if (!success && mounted && authProvider.error != null) {
        ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
            content: Text(authProvider.error!),
            backgroundColor: AppColors.error,
            ),
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);

    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        title: const Text('Create Account'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        iconTheme: const IconThemeData(color: Colors.white),
        titleTextStyle: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.w600),
      ),
      body: Stack(
        children: [
          // Animated background
          Positioned.fill(
            child: AnimatedBuilder(
              animation: _bgController,
              builder: (context, child) {
                return CustomPaint(
                  painter: _OrbPainter(_bgController.value),
                  size: Size.infinite,
                );
              },
            ),
          ),
          // Register card
          SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(24),
                child: FadeTransition(
                  opacity: _cardFade,
                  child: SlideTransition(
                    position: _cardSlide,
                    child: Container(
                      constraints: const BoxConstraints(maxWidth: 480),
                      decoration: BoxDecoration(
                        color: AppColors.elevated.withOpacity(0.65),
                        borderRadius: BorderRadius.circular(24),
                        border: Border.all(
                          color: AppColors.primary.withOpacity(0.15),
                        ),
                        boxShadow: [
                          BoxShadow(
                            color: AppColors.primary.withOpacity(0.06),
                            blurRadius: 48,
                            spreadRadius: 0,
                          ),
                        ],
                      ),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(24),
                        child: BackdropFilter(
                          filter: ImageFilter.blur(sigmaX: 16, sigmaY: 16),
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 32),
                            child: Form(
                              key: _formKey,
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  ErrorMessage(message: authProvider.error),
                                  AppTextField(
                                    label: 'First Name',
                                    hint: 'Enter your first name',
                                    controller: _firstNameController,
                                    isRequired: true,
                                    maxLength: 100,
                                    textInputAction: TextInputAction.next,
                                    errorText: authProvider.fieldErrors['firstName'],
                                    onChanged: (_) => authProvider.clearFieldError('firstName'),
                                    validator: (val) =>
                                        Validators.validateName(val, 'First name'),
                                  ),
                                  const SizedBox(height: 16),
                                  AppTextField(
                                    label: 'Last Name',
                                    hint: 'Enter your last name',
                                    controller: _lastNameController,
                                    isRequired: true,
                                    maxLength: 100,
                                    textInputAction: TextInputAction.next,
                                    errorText: authProvider.fieldErrors['lastName'],
                                    onChanged: (_) => authProvider.clearFieldError('lastName'),
                                    validator: (val) => Validators.validateName(val, 'Last name'),
                                  ),
                                  const SizedBox(height: 16),
                                  AppTextField(
                                    label: 'Email',
                                    hint: 'Enter your email address',
                                    controller: _emailController,
                                    keyboardType: TextInputType.emailAddress,
                                    isRequired: true,
                                    maxLength: 255,
                                    textInputAction: TextInputAction.next,
                                    inputFormatters: [FilteringTextInputFormatter.deny(RegExp(r'\s'))],
                                    errorText: authProvider.fieldErrors['email'],
                                    onChanged: (_) => authProvider.clearFieldError('email'),
                                    validator: Validators.validateEmail,
                                  ),
                                  const SizedBox(height: 16),
                                  AppTextField(
                                    label: 'Phone Number',
                                    hint: 'Enter your phone number',
                                    controller: _phoneController,
                                    keyboardType: TextInputType.phone,
                                    isRequired: true,
                                    maxLength: 20,
                                    textInputAction: TextInputAction.next,
                                    inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9+()\- ]'))],
                                    errorText: authProvider.fieldErrors['phoneNumber'],
                                    onChanged: (_) => authProvider.clearFieldError('phoneNumber'),
                                    validator: (val) => Validators.validatePhone(val),
                                  ),
                                  const SizedBox(height: 16),
                                  PasswordField(
                                    label: 'Password',
                                    hint: 'Create a password',
                                    controller: _passwordController,
                                    isRequired: true,
                                    errorText: authProvider.fieldErrors['password'],
                                    onChanged: (_) => authProvider.clearFieldError('password'),
                                    validator: Validators.validatePassword,
                                  ),
                                  const SizedBox(height: 16),
                                  PasswordField(
                                    label: 'Confirm Password',
                                    hint: 'Confirm your password',
                                    controller: _confirmPasswordController,
                                    isRequired: true,
                                    validator: (val) => Validators.validateConfirmPassword(
                                      val,
                                      _passwordController.text,
                                    ),
                                  ),
                                  const SizedBox(height: 32),
                                  Container(
                                    decoration: BoxDecoration(
                                      gradient: AppColors.primaryGradient,
                                      borderRadius: BorderRadius.circular(12),
                                      boxShadow: [
                                        BoxShadow(
                                          color: AppColors.primary.withOpacity(0.25),
                                          blurRadius: 12,
                                          offset: const Offset(0, 4),
                                        ),
                                      ],
                                    ),
                                    child: ElevatedButton(
                                      onPressed: authProvider.isLoading ? null : _register,
                                      style: ElevatedButton.styleFrom(
                                        backgroundColor: Colors.transparent,
                                        shadowColor: Colors.transparent,
                                        shape: RoundedRectangleBorder(
                                          borderRadius: BorderRadius.circular(12),
                                        ),
                                        padding: const EdgeInsets.symmetric(vertical: 16),
                                      ),
                                      child: authProvider.isLoading
                                          ? const SizedBox(
                                              height: 24,
                                              width: 24,
                                              child: CircularProgressIndicator(
                                                color: Colors.white,
                                                strokeWidth: 2.5,
                                              ),
                                            )
                                          : const Text(
                                              'Register',
                                              style: TextStyle(
                                                fontSize: 16,
                                                fontWeight: FontWeight.w600,
                                                color: Colors.white,
                                              ),
                                            ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _OrbPainter extends CustomPainter {
  final double progress;
  _OrbPainter(this.progress);

  @override
  void paint(Canvas canvas, Size size) {
    canvas.drawRect(
      Rect.fromLTWH(0, 0, size.width, size.height),
      Paint()..color = AppColors.background,
    );

    final orbs = [
      _Orb(0.2, 0.25, 140, AppColors.primary.withOpacity(0.10), 0.0),
      _Orb(0.8, 0.15, 110, AppColors.secondary.withOpacity(0.08), 0.33),
      _Orb(0.5, 0.8, 160, AppColors.primary.withOpacity(0.07), 0.66),
      _Orb(0.12, 0.7, 90, AppColors.secondary.withOpacity(0.09), 0.5),
      _Orb(0.88, 0.65, 100, AppColors.primary.withOpacity(0.06), 0.15),
    ];

    for (final orb in orbs) {
      final phase = (progress + orb.phase) % 1.0;
      final angle = phase * 2 * pi;
      final dx = orb.baseX * size.width + sin(angle) * 35;
      final dy = orb.baseY * size.height + cos(angle) * 30;
      final paint = Paint()
        ..shader = RadialGradient(
          colors: [orb.color, orb.color.withOpacity(0)],
        ).createShader(
          Rect.fromCircle(center: Offset(dx, dy), radius: orb.radius),
        );
      canvas.drawCircle(Offset(dx, dy), orb.radius, paint);
    }
  }

  @override
  bool shouldRepaint(_OrbPainter old) => old.progress != progress;
}

class _Orb {
  final double baseX, baseY, radius;
  final Color color;
  final double phase;
  const _Orb(this.baseX, this.baseY, this.radius, this.color, this.phase);
}
