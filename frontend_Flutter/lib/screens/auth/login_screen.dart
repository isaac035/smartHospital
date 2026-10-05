import 'dart:math';
import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import '../../theme/app_theme.dart';
import 'register_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> with TickerProviderStateMixin {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;
  bool _isLoading = false;

  late AnimationController _bgController;
  late AnimationController _cardController;
  late Animation<double> _cardFade;
  late Animation<Offset> _cardSlide;

  @override
  void initState() {
    super.initState();
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
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  void _login() async {
    if (_formKey.currentState!.validate()) {
      setState(() => _isLoading = true);
      try {
        await Provider.of<AuthProvider>(context, listen: false)
            .login(_emailController.text, _passwordController.text);
      } catch (e) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(e.toString()), backgroundColor: AppColors.error),
          );
        }
      } finally {
        if (mounted) setState(() => _isLoading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          // Animated background
          Positioned.fill(
            child: AnimatedBuilder(
              animation: _bgController,
              builder: (context, child) {
                return CustomPaint(
                  painter: _HealthcareScenePainter(_bgController.value),
                  size: Size.infinite,
                );
              },
            ),
          ),
          // Login card
          Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: FadeTransition(
                opacity: _cardFade,
                child: SlideTransition(
                  position: _cardSlide,
                  child: Container(
                    constraints: const BoxConstraints(maxWidth: 420),
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
                          padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 40),
                          child: Form(
                            key: _formKey,
                            child: Column(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(16),
                                  decoration: BoxDecoration(
                                    gradient: AppColors.primaryGradient,
                                    borderRadius: BorderRadius.circular(16),
                                    boxShadow: [
                                      BoxShadow(
                                        color: AppColors.primary.withOpacity(0.3),
                                        blurRadius: 20,
                                        spreadRadius: 0,
                                      ),
                                    ],
                                  ),
                                  child: const Icon(Icons.local_hospital, size: 40, color: Colors.white),
                                ),
                                const SizedBox(height: 24),
                                Text(
                                  'Smart Hospital',
                                  style: Theme.of(context).textTheme.headlineMedium,
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  'Patient Portal',
                                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                                const SizedBox(height: 36),
                                TextFormField(
                                  controller: _emailController,
                                  decoration: const InputDecoration(
                                    labelText: 'Email',
                                    prefixIcon: Icon(Icons.email_outlined),
                                  ),
                                  keyboardType: TextInputType.emailAddress,
                                  validator: (value) =>
                                      value == null || value.isEmpty ? 'Please enter your email' : null,
                                ),
                                const SizedBox(height: 16),
                                TextFormField(
                                  controller: _passwordController,
                                  decoration: InputDecoration(
                                    labelText: 'Password',
                                    prefixIcon: const Icon(Icons.lock_outline),
                                    suffixIcon: IconButton(
                                      icon: Icon(
                                        _obscurePassword ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                                      ),
                                      onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                                    ),
                                  ),
                                  obscureText: _obscurePassword,
                                  validator: (value) =>
                                      value == null || value.isEmpty ? 'Please enter your password' : null,
                                ),
                                const SizedBox(height: 28),
                                SizedBox(
                                  width: double.infinity,
                                  height: 52,
                                  child: Container(
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
                                      onPressed: _isLoading ? null : _login,
                                      style: ElevatedButton.styleFrom(
                                        backgroundColor: Colors.transparent,
                                        shadowColor: Colors.transparent,
                                        shape: RoundedRectangleBorder(
                                          borderRadius: BorderRadius.circular(12),
                                        ),
                                      ),
                                      child: _isLoading
                                          ? const SizedBox(
                                              height: 24,
                                              width: 24,
                                              child: CircularProgressIndicator(
                                                color: Colors.white,
                                                strokeWidth: 2.5,
                                              ),
                                            )
                                          : const Text(
                                              'Sign In',
                                              style: TextStyle(
                                                fontSize: 16,
                                                fontWeight: FontWeight.w600,
                                                color: Colors.white,
                                              ),
                                            ),
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 20),
                                TextButton(
                                  onPressed: () => Navigator.push(
                                    context,
                                    MaterialPageRoute(builder: (_) => const RegisterScreen()),
                                  ),
                                  child: const Text("Don't have an account? Register"),
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
        ],
      ),
    );
  }
}

class _HealthcareScenePainter extends CustomPainter {
  final double progress;
  _HealthcareScenePainter(this.progress);

  @override
  void paint(Canvas canvas, Size size) {
    // 1. Deep navy-to-purple gradient background
    final bgRect = Rect.fromLTWH(0, 0, size.width, size.height);
    final bgPaint = Paint()
      ..shader = const LinearGradient(
        begin: Alignment.topLeft,
        end: Alignment.bottomRight,
        colors: [Color(0xFF0B1220), Color(0xFF0D1B2A), Color(0xFF112240)],
      ).createShader(bgRect);
    canvas.drawRect(bgRect, bgPaint);

    // 2. Drifting glowing blobs (Orbs)
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
        ).createShader(Rect.fromCircle(center: Offset(dx, dy), radius: orb.radius));
      canvas.drawCircle(Offset(dx, dy), orb.radius, paint);
    }

    // 3. Skyline silhouettes at the bottom
    final skylinePaint = Paint()..color = const Color(0xFF17233D).withOpacity(0.4);
    final path = Path();
    path.moveTo(0, size.height);
    path.lineTo(0, size.height * 0.75);
    path.lineTo(size.width * 0.1, size.height * 0.75);
    path.lineTo(size.width * 0.1, size.height * 0.65);
    path.lineTo(size.width * 0.25, size.height * 0.65);
    path.lineTo(size.width * 0.25, size.height * 0.7);
    path.lineTo(size.width * 0.4, size.height * 0.7);
    path.lineTo(size.width * 0.4, size.height * 0.6);
    path.lineTo(size.width * 0.55, size.height * 0.6);
    path.lineTo(size.width * 0.55, size.height * 0.75);
    path.lineTo(size.width * 0.7, size.height * 0.75);
    path.lineTo(size.width * 0.7, size.height * 0.65);
    path.lineTo(size.width * 0.85, size.height * 0.65);
    path.lineTo(size.width * 0.85, size.height * 0.8);
    path.lineTo(size.width, size.height * 0.8);
    path.lineTo(size.width, size.height);
    path.close();
    canvas.drawPath(path, skylinePaint);

    // 4. Floating dashboard cards
    final cardPaint = Paint()
      ..color = const Color(0xFF1E3054).withOpacity(0.5)
      ..style = PaintingStyle.fill;
    
    _drawFloatingCard(canvas, size.width * 0.15, size.height * 0.3, 80, 50, progress, 0.0, cardPaint);
    _drawFloatingCard(canvas, size.width * 0.85, size.height * 0.4, 60, 80, progress, 0.5, cardPaint);
    _drawFloatingCard(canvas, size.width * 0.75, size.height * 0.2, 70, 40, progress, 0.2, cardPaint);

    // 5. Pulsing ECG/heartbeat line
    final ecgPaint = Paint()
      ..color = AppColors.primary.withOpacity(0.6 + 0.4 * sin(progress * 4 * pi))
      ..strokeWidth = 2.0
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;
      
    final ecgPath = Path();
    final ecgBaseY = size.height * 0.85;
    final ecgStartX = size.width * 0.1;
    ecgPath.moveTo(ecgStartX, ecgBaseY);
    ecgPath.lineTo(ecgStartX + 40, ecgBaseY);
    ecgPath.lineTo(ecgStartX + 50, ecgBaseY - 15);
    ecgPath.lineTo(ecgStartX + 65, ecgBaseY + 20);
    ecgPath.lineTo(ecgStartX + 80, ecgBaseY - 30);
    ecgPath.lineTo(ecgStartX + 95, ecgBaseY + 10);
    ecgPath.lineTo(ecgStartX + 105, ecgBaseY);
    ecgPath.lineTo(ecgStartX + 150, ecgBaseY);
    
    canvas.drawPath(ecgPath, ecgPaint);

    // 6. Softly glowing medical cross icons
    _drawCross(canvas, size.width * 0.3, size.height * 0.2, progress, 0.1, AppColors.success);
    _drawCross(canvas, size.width * 0.7, size.height * 0.6, progress, 0.6, AppColors.primary);
  }

  void _drawFloatingCard(Canvas canvas, double x, double y, double w, double h, double progress, double phase, Paint paint) {
    final floatOffset = sin((progress + phase) * 2 * pi) * 15;
    final rect = RRect.fromRectAndRadius(
      Rect.fromCenter(center: Offset(x, y + floatOffset), width: w, height: h),
      const Radius.circular(8),
    );
    canvas.drawRRect(rect, paint);
    
    final linePaint = Paint()
      ..color = Colors.white.withOpacity(0.2)
      ..strokeWidth = 3
      ..strokeCap = StrokeCap.round;
    canvas.drawLine(Offset(x - w/3, y + floatOffset - h/4), Offset(x + w/3, y + floatOffset - h/4), linePaint);
    canvas.drawLine(Offset(x - w/3, y + floatOffset), Offset(x + w/6, y + floatOffset), linePaint);
  }

  void _drawCross(Canvas canvas, double cx, double cy, double progress, double phase, Color color) {
    final scale = 1.0 + 0.1 * sin((progress + phase) * 2 * pi);
    final opacity = 0.3 + 0.3 * sin((progress + phase) * 2 * pi);
    final paint = Paint()
      ..color = color.withOpacity(opacity)
      ..style = PaintingStyle.fill;
    
    final w = 6.0 * scale;
    final h = 20.0 * scale;
    
    canvas.save();
    canvas.translate(cx, cy);
    final glowPaint = Paint()
      ..color = color.withOpacity(opacity * 0.5)
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 8);
    canvas.drawRect(Rect.fromCenter(center: Offset.zero, width: w, height: h), glowPaint);
    canvas.drawRect(Rect.fromCenter(center: Offset.zero, width: h, height: w), glowPaint);
    
    canvas.drawRect(Rect.fromCenter(center: Offset.zero, width: w, height: h), paint);
    canvas.drawRect(Rect.fromCenter(center: Offset.zero, width: h, height: w), paint);
    canvas.restore();
  }

  @override
  bool shouldRepaint(_HealthcareScenePainter old) => old.progress != progress;
}

class _Orb {
  final double baseX, baseY, radius;
  final Color color;
  final double phase;
  const _Orb(this.baseX, this.baseY, this.radius, this.color, this.phase);
}

