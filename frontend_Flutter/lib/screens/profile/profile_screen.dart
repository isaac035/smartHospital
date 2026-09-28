import 'package:flutter/material.dart';
import 'package:smart_hospital/core/theme/app_theme.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
import '../../providers/auth_provider.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;

    if (user == null) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Profile'),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit),
            onPressed: () => context.push('/profile/edit'),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            const CircleAvatar(
              radius: 50,
              backgroundColor: AppTheme.primaryColor,
              child: Icon(Icons.person, size: 50, color: AppTheme.surfaceColor),
            ),
            const SizedBox(height: 16),
            Text(
              '${user.firstName} ${user.lastName}',
              style: TextStyle(
                fontSize: AppTheme.fontHeadlineMedium,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              user.role,
              style: TextStyle(
                fontSize: AppTheme.fontTitleMedium,
                color: AppTheme.primaryColor,
                fontWeight: FontWeight.w500,
              ),
            ),
            const SizedBox(height: 32),
            _buildProfileItem(Icons.email, 'Email', user.email),
            _buildProfileItem(Icons.phone, 'Phone', user.phoneNumber),
            _buildProfileItem(
              Icons.verified_user,
              'Account Status',
              user.status,
            ),

            const SizedBox(height: 24),
            ListTile(
              leading: const Icon(Icons.lock),
              title: const Text('Change Password'),
              trailing: const Icon(Icons.arrow_forward_ios, size: 16),
              onTap: () => context.push('/profile/change-password'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildProfileItem(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8.0),
      child: ListTile(
        leading: Icon(icon, color: AppTheme.textSecondary),
        title: Text(
          label,
          style: TextStyle(
            fontSize: AppTheme.fontBodyMedium,
            color: AppTheme.textSecondary,
          ),
        ),
        subtitle: Text(
          value,
          style: TextStyle(
            fontSize: AppTheme.fontTitleMedium,
            color: AppTheme.textPrimary,
            fontWeight: FontWeight.w500,
          ),
        ),
      ),
    );
  }
}
