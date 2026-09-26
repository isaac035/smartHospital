import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../screens/splash/splash_screen.dart';
import '../screens/auth/login_screen.dart';
import '../screens/auth/register_screen.dart';
import '../screens/home/home_screen.dart';
import '../screens/profile/profile_screen.dart';
import '../screens/profile/edit_profile_screen.dart';
import '../screens/profile/change_password_screen.dart';
import '../screens/doctors/doctor_directory_screen.dart';
import '../screens/doctors/doctor_details_screen.dart';
import '../screens/doctors/doctor_schedule_screen.dart';
import '../screens/doctors/doctor_availability_screen.dart';

final GlobalKey<NavigatorState> _rootNavigatorKey = GlobalKey<NavigatorState>();

class AppRouter {
  static GoRouter router(BuildContext context) {
    return GoRouter(
      navigatorKey: _rootNavigatorKey,
      initialLocation: '/splash',
      redirect: (BuildContext context, GoRouterState state) {
        final authProvider = Provider.of<AuthProvider>(context, listen: false);
        final isAuth = authProvider.isAuthenticated;
        final isInitialized = authProvider.isInitialized;

        final isSplash = state.matchedLocation == '/splash';
        final isLogin = state.matchedLocation == '/login';
        final isRegister = state.matchedLocation == '/register';

        if (!isInitialized && !isSplash) {
          return '/splash';
        }

        if (isInitialized) {
          if (!isAuth) {
            if (!isLogin && !isRegister) return '/login';
          } else {
            if (isLogin || isRegister || isSplash) return '/home';
          }
        }
        
        return null;
      },
      routes: [
        GoRoute(
          path: '/splash',
          builder: (context, state) => const SplashScreen(),
        ),
        GoRoute(
          path: '/login',
          builder: (context, state) => const LoginScreen(),
        ),
        GoRoute(
          path: '/register',
          builder: (context, state) => const RegisterScreen(),
        ),
        GoRoute(
          path: '/home',
          builder: (context, state) => const HomeScreen(),
        ),
        GoRoute(
          path: '/profile',
          builder: (context, state) => const ProfileScreen(),
          routes: [
            GoRoute(
              path: 'edit',
              builder: (context, state) => const EditProfileScreen(),
            ),
            GoRoute(
              path: 'change-password',
              builder: (context, state) => const ChangePasswordScreen(),
            ),
          ],
        ),
        GoRoute(
          path: '/doctors',
          builder: (context, state) => const DoctorDirectoryScreen(),
          routes: [
            GoRoute(
              path: ':doctorId',
              builder: (context, state) => DoctorDetailsScreen(
                doctorId: int.parse(state.pathParameters['doctorId']!),
              ),
              routes: [
                GoRoute(
                  path: 'schedule',
                  builder: (context, state) => DoctorScheduleScreen(
                    doctorId: int.parse(state.pathParameters['doctorId']!),
                  ),
                ),
                GoRoute(
                  path: 'availability',
                  builder: (context, state) => DoctorAvailabilityScreen(
                    doctorId: int.parse(state.pathParameters['doctorId']!),
                  ),
                ),
              ],
            ),
          ],
        ),
      ],
    );
  }
}
