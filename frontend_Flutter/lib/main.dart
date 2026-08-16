import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/theme/app_theme.dart';
import 'core/storage/secure_storage_service.dart';
import 'core/network/api_client.dart';
import 'services/auth_service.dart';
import 'services/user_service.dart';
import 'providers/auth_provider.dart';
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
