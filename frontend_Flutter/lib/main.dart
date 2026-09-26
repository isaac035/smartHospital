import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/theme/app_theme.dart';
import 'core/storage/secure_storage_service.dart';
import 'core/network/api_client.dart';
import 'services/auth_service.dart';
import 'services/user_service.dart';
import 'services/doctor_service.dart';
import 'services/schedule_service.dart';
import 'services/leave_service.dart';
import 'services/department_service.dart';
import 'services/consultation_type_service.dart';
import 'providers/auth_provider.dart';
import 'providers/doctor_provider.dart';
import 'routes/app_router.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        Provider<SecureStorageService>(
          create: (_) => SecureStorageService(),
        ),
        ProxyProvider<SecureStorageService, ApiClient>(
          update: (_, storageService, previous) => ApiClient(storageService),
        ),
        ProxyProvider<ApiClient, AuthService>(
          update: (_, apiClient, previous) => AuthService(apiClient),
        ),
        ProxyProvider<ApiClient, UserService>(
          update: (_, apiClient, previous) => UserService(apiClient),
        ),
        ChangeNotifierProxyProvider3<AuthService, UserService, SecureStorageService, AuthProvider>(
          create: (_) => AuthProvider(
            AuthService(ApiClient(SecureStorageService())),
            UserService(ApiClient(SecureStorageService())),
            SecureStorageService(),
          ),
          update: (_, authService, userService, storageService, previous) =>
              previous ?? AuthProvider(authService, userService, storageService),
        ),
        ProxyProvider<ApiClient, DoctorService>(
          update: (_, apiClient, previous) => DoctorService(apiClient),
        ),
        ProxyProvider<ApiClient, ScheduleService>(
          update: (_, apiClient, previous) => ScheduleService(apiClient),
        ),
        ProxyProvider<ApiClient, LeaveService>(
          update: (_, apiClient, previous) => LeaveService(apiClient),
        ),
        ProxyProvider<ApiClient, DepartmentService>(
          update: (_, apiClient, previous) => DepartmentService(apiClient),
        ),
        ProxyProvider<ApiClient, ConsultationTypeService>(
          update: (_, apiClient, previous) => ConsultationTypeService(apiClient),
        ),
        ChangeNotifierProxyProvider5<DoctorService, ScheduleService, LeaveService, DepartmentService,
            ConsultationTypeService, DoctorProvider>(
          create: (_) => DoctorProvider(
            DoctorService(ApiClient(SecureStorageService())),
            ScheduleService(ApiClient(SecureStorageService())),
            LeaveService(ApiClient(SecureStorageService())),
            DepartmentService(ApiClient(SecureStorageService())),
            ConsultationTypeService(ApiClient(SecureStorageService())),
          ),
          update: (_, doctorService, scheduleService, leaveService, departmentService, consultationTypeService, previous) =>
              previous ??
              DoctorProvider(doctorService, scheduleService, leaveService, departmentService, consultationTypeService),
        ),
      ],
      child: Builder(
        builder: (context) {
          final router = AppRouter.router(context);
          return MaterialApp.router(
            title: 'Smart Hospital',
            theme: AppTheme.lightTheme,
            debugShowCheckedModeBanner: false,
            routerConfig: router,
          );
        },
      ),
    );
  }
}
