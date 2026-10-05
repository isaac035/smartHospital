import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../theme/app_theme.dart';
import '../../core/utils/api_datetime.dart';
import '../../models/appointments/appointment_model.dart';
import '../../models/user_model.dart';
import '../../providers/appointment_provider.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/app_ui.dart';
import '../../widgets/feature_tile.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  bool _scrolled = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      if (!mounted) return;
      try {
        // Reuse the app's existing appointments provider and endpoint. Its
        // error state is used only to hide this optional summary quietly.
        await context.read<AppointmentProvider>().loadMyAppointments();
      } catch (_) {
        // The home experience must remain available if this optional request fails.
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;
    final reduceMotion = MediaQuery.of(context).disableAnimations;
    final horizontalPadding = MediaQuery.sizeOf(context).width <= 600
        ? 16.0
        : 20.0;

    return NotificationListener<ScrollNotification>(
      onNotification: (notification) {
        final nowScrolled = notification.metrics.pixels > 0;
        if (nowScrolled != _scrolled) setState(() => _scrolled = nowScrolled);
        return false;
      },
      child: Scaffold(
        backgroundColor: AppColors.background,
        body: user == null
            ? const SafeArea(
                child: LoadingView(message: 'Loading your portal…'),
              )
            : CustomScrollView(
                slivers: [
                  SliverAppBar(
                    pinned: true,
                    toolbarHeight: 68,
                    backgroundColor: AppColors.surface,
                    surfaceTintColor: Colors.transparent,
                    scrolledUnderElevation: 0,
                    titleSpacing: horizontalPadding,
                    title: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Container(
                          width: 34,
                          height: 34,
                          decoration: BoxDecoration(
                            color: AppColors.primary,
                            borderRadius: BorderRadius.circular(10),
                          ),
                          alignment: Alignment.center,
                          child: const Icon(
                            Icons.add_rounded,
                            color: Colors.white,
                            size: 26,
                          ),
                        ),
                        const SizedBox(width: 10),
                        Text(
                          'Smart Hospital',
                          style: Theme.of(context).textTheme.titleMedium
                              ?.copyWith(fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                    actions: [
                      Semantics(
                        button: true,
                        label: 'Profile',
                        child: InkWell(
                          customBorder: const CircleBorder(),
                          onTap: () => context.push('/profile'),
                          child: CircleAvatar(
                            radius: 19,
                            backgroundColor: AppColors.primary.withValues(alpha: 0.15),
                            foregroundColor:
                                AppColors.primary,
                            child: Text(
                              _initials(user),
                              style: Theme.of(context).textTheme.labelLarge
                                  ?.copyWith(
                                    color:
                                        AppColors.primary,
                                    fontWeight: FontWeight.w700,
                                  ),
                            ),
                          ),
                        ),
                      ),
                      IconButton(
                        tooltip: 'Log out',
                        onPressed: _confirmLogout,
                        icon: const Icon(Icons.logout_rounded),
                      ),
                      SizedBox(width: horizontalPadding - 4),
                    ],
                    bottom: PreferredSize(
                      preferredSize: const Size.fromHeight(1),
                      child: AnimatedContainer(
                        duration: reduceMotion
                            ? Duration.zero
                            : const Duration(milliseconds: 150),
                        height: 1,
                        color: _scrolled
                            ? AppColors.border
                            : Colors.transparent,
                      ),
                    ),
                  ),
                  SliverToBoxAdapter(
                    child: LayoutBuilder(
                      builder: (context, constraints) {
                        final columns = constraints.maxWidth <= 600 ? 2 : 3;
                        final services = _services(context);
                        final rows = <Widget>[];
                        var index = 0;
                        for (
                          var start = 0;
                          start < services.length;
                          start += columns
                        ) {
                          final end = (start + columns).clamp(
                            0,
                            services.length,
                          );
                          final rowItems = services.sublist(start, end);
                          final orphanOnPhone =
                              columns == 2 && rowItems.length == 1;
                          if (orphanOnPhone) {
                            rows.add(
                              SizedBox(
                                height: 144,
                                child: FeatureTile(
                                  icon: rowItems.single.icon,
                                  title: rowItems.single.title,
                                  subtitle: rowItems.single.subtitle,
                                  semanticDestination:
                                      rowItems.single.semanticDestination,
                                  tint: rowItems.single.tint,
                                  foreground: rowItems.single.foreground,
                                  onTap: rowItems.single.onTap,
                                  horizontal: true,
                                  staggerIndex: index++,
                                ),
                              ),
                            );
                          } else {
                            rows.add(
                              Row(
                                children: [
                                  for (final item in rowItems) ...[
                                    Expanded(
                                      child: SizedBox(
                                        height: 144,
                                        child: FeatureTile(
                                          icon: item.icon,
                                          title: item.title,
                                          subtitle: item.subtitle,
                                          semanticDestination:
                                              item.semanticDestination,
                                          tint: item.tint,
                                          foreground: item.foreground,
                                          onTap: item.onTap,
                                          staggerIndex: index++,
                                        ),
                                      ),
                                    ),
                                    if (item != rowItems.last)
                                      const SizedBox(width: 14),
                                  ],
                                ],
                              ),
                            );
                          }
                          if (end < services.length) {
                            rows.add(const SizedBox(height: 14));
                          }
                        }

                        return Container(
                          decoration: const BoxDecoration(
                            gradient: LinearGradient(
                              begin: Alignment.topCenter,
                              end: Alignment.bottomCenter,
                              colors: [
                                AppColors.surface,
                                AppColors.background,
                              ],
                              stops: [0, 1],
                            ),
                          ),
                          child: Padding(
                            padding: EdgeInsets.fromLTRB(
                              horizontalPadding,
                              18,
                              horizontalPadding,
                              32,
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                _entrance(
                                  child: _GreetingHero(user: user),
                                  reduceMotion: reduceMotion,
                                ),
                                const SizedBox(height: 28),
                                SectionHeader('Services'),
                                const SizedBox(height: 14),
                                ...rows,
                              ],
                            ),
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
      ),
    );
  }

  List<_HomeService> _services(BuildContext context) => [
    _HomeService(
      icon: Icons.event_available_rounded,
      title: 'My Appointments',
      subtitle: 'View and manage your bookings',
      semanticDestination: 'your bookings',
      tint: AppColors.primary.withValues(alpha: 0.15),
      foreground: AppColors.primary,
      onTap: () => context.go('/appointments'),
    ),
    _HomeService(
      icon: Icons.groups_rounded,
      title: 'Queue Status',
      subtitle: 'See your live position',
      semanticDestination: 'your live queue position',
      tint: AppColors.warning.withValues(alpha: 0.15),
      foreground: AppColors.warning,
      onTap: () => context.push('/queue'),
    ),
    _HomeService(
      icon: Icons.medical_services_rounded,
      title: 'Doctors',
      subtitle: 'Find care that fits your needs',
      semanticDestination: 'doctor search',
      tint: AppColors.success.withValues(alpha: 0.15),
      foreground: AppColors.success,
      onTap: () => context.go('/doctors'),
    ),
    _HomeService(
      icon: Icons.monitor_heart_rounded,
      title: 'Medical Records',
      subtitle: 'Review your health information',
      semanticDestination: 'your medical records',
      tint: AppColors.secondary.withValues(alpha: 0.15),
      foreground: AppColors.secondary,
      onTap: () => context.go('/medical-records'),
    ),
    _HomeService(
      icon: Icons.bed_rounded,
      title: 'My Admissions',
      subtitle: 'View your admission details',
      semanticDestination: 'your admission details',
      tint: AppColors.primaryDark.withValues(alpha: 0.15),
      foreground: AppColors.primary,
      onTap: () => context.go('/my-admission'),
    ),
    _HomeService(
      icon: Icons.health_and_safety_rounded,
      title: 'Smart Care',
      subtitle: 'Describe symptoms, get matched',
      semanticDestination: 'Smart Care doctor matching',
      tint: AppColors.error.withValues(alpha: 0.15),
      foreground: AppColors.error,
      onTap: () => context.push('/smart-care'),
    ),
  ];

  Widget _entrance({required Widget child, required bool reduceMotion}) {
    if (reduceMotion) return child;
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: const Duration(milliseconds: 250),
      builder: (context, value, child) => Opacity(
        opacity: value,
        child: Transform.translate(
          offset: Offset(0, 12 * (1 - value)),
          child: child,
        ),
      ),
      child: child,
    );
  }

  String _initials(UserModel user) {
    final initials = [user.firstName, user.lastName]
        .where((name) => name.trim().isNotEmpty)
        .map((name) => name.trim()[0])
        .join()
        .toUpperCase();
    return initials.isEmpty ? '?' : initials;
  }

  Future<void> _confirmLogout() async {
    final confirmed = await AppConfirmDialog.show(
      context,
      title: 'Log out?',
      message: 'Are you sure you want to log out?',
      confirmLabel: 'Log out',
    );
    if (!confirmed || !mounted) return;
    await context.read<AuthProvider>().logout();
    if (mounted) context.go('/login');
  }
}

class _GreetingHero extends StatelessWidget {
  const _GreetingHero({required this.user});
  final UserModel user;

  @override
  Widget build(BuildContext context) {
    final greetingName = user.firstName.trim();
    return Container(
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24.0),
        gradient: const LinearGradient(
          colors: [AppColors.primary, AppColors.primaryDark],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: .16),
            blurRadius: 22,
            offset: const Offset(0, 10),
          ),
        ],
      ),
      child: Stack(
        children: [
          Positioned(
            right: -30,
            top: -55,
            child: _DecorativeCircle(size: 170, opacity: .07),
          ),
          Positioned(
            right: 100,
            bottom: -52,
            child: _DecorativeCircle(size: 112, opacity: .06),
          ),
          Padding(
            padding: const EdgeInsets.all(22),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  greetingName.isEmpty ? 'Hello!' : 'Hello, $greetingName',
                  style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                    color: Colors.white,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  'Welcome to your patient portal.',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Colors.white70,
                  ),
                ),
                const SizedBox(height: 18),
                Consumer<AppointmentProvider>(
                  builder: (context, provider, _) {
                    if (provider.appointmentsLoading ||
                        provider.appointmentsError != null) {
                      return const SizedBox.shrink();
                    }
                    final next = _findNextAppointment(provider.appointments);
                    if (next == null) return _NoUpcomingAppointment();

                    final localStart = ApiDateTime.parseUtcToLocal(
                      next.scheduledStart.toUtc().toIso8601String(),
                    );
                    final matchingQueueEntries = next.status == 3
                        ? provider.queue
                              .where(
                                (entry) => entry.patientId == next.patientId,
                              )
                              .toList()
                        : const [];
                    final queueEntry = matchingQueueEntries.isEmpty
                        ? null
                        : matchingQueueEntries.first;
                    return _SummaryPill(
                      date: DateFormat('EEE, d MMM').format(localStart),
                      time: DateFormat('h:mm a').format(localStart),
                      doctor: next.doctorName?.trim().isNotEmpty == true
                          ? next.doctorName!.trim()
                          : 'Doctor details unavailable',
                      queueNumber: queueEntry?.queueNumber,
                    );
                  },
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  AppointmentModel? _findNextAppointment(List<AppointmentModel> appointments) {
    final now = DateTime.now();
    final todayCheckedIn = appointments.where((appointment) {
      final start = appointment.scheduledStart;
      return appointment.status == 3 &&
          start.year == now.year &&
          start.month == now.month &&
          start.day == now.day;
    });
    final upcoming = appointments.where(
      (appointment) =>
          (appointment.status == 1 || appointment.status == 2) &&
          !appointment.scheduledStart.isBefore(now),
    );
    final candidates = [...todayCheckedIn, ...upcoming].toList()
      ..sort((a, b) => a.scheduledStart.compareTo(b.scheduledStart));
    return candidates.isEmpty ? null : candidates.first;
  }
}

class _NoUpcomingAppointment extends StatelessWidget {
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.fromLTRB(14, 11, 10, 11),
    decoration: BoxDecoration(
      color: Colors.white.withValues(alpha: .12),
      borderRadius: BorderRadius.circular(14),
      border: Border.all(color: Colors.white.withValues(alpha: .15)),
    ),
    child: Row(
      children: [
        const Icon(
          Icons.event_busy_rounded,
          color: Colors.white70,
          size: 20,
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            'No upcoming appointments',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: Colors.white,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
        TextButton(
          onPressed: () => context.push('/appointments/search'),
          style: TextButton.styleFrom(
            foregroundColor: Colors.white,
            minimumSize: const Size(48, 44),
          ),
          child: const Text('Book now'),
        ),
      ],
    ),
  );
}

class _SummaryPill extends StatelessWidget {
  const _SummaryPill({
    required this.date,
    required this.time,
    required this.doctor,
    this.queueNumber,
  });

  final String date;
  final String time;
  final String doctor;
  final int? queueNumber;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
    decoration: BoxDecoration(
      color: Colors.white.withValues(alpha: .12),
      borderRadius: BorderRadius.circular(14),
      border: Border.all(color: Colors.white.withValues(alpha: .15)),
    ),
    child: Row(
      children: [
        const Icon(
          Icons.event_rounded,
          color: Colors.white70,
          size: 21,
        ),
        const SizedBox(width: 11),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '$date · $time',
                style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  color: Colors.white,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                doctor,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(
                  context,
                ).textTheme.bodySmall?.copyWith(color: Colors.white70),
              ),
            ],
          ),
        ),
        if (queueNumber != null) ...[
          const SizedBox(width: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 6),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: .16),
              borderRadius: BorderRadius.circular(999),
            ),
            child: Text(
              'Queue #$queueNumber',
              style: Theme.of(
                context,
              ).textTheme.labelSmall?.copyWith(color: Colors.white),
            ),
          ),
        ],
      ],
    ),
  );
}

class _DecorativeCircle extends StatelessWidget {
  const _DecorativeCircle({required this.size, required this.opacity});
  final double size;
  final double opacity;

  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    decoration: BoxDecoration(
      shape: BoxShape.circle,
      color: Colors.white.withValues(alpha: opacity),
    ),
  );
}

class _HomeService {
  const _HomeService({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.semanticDestination,
    required this.tint,
    required this.foreground,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final String semanticDestination;
  final Color tint;
  final Color foreground;
  final VoidCallback onTap;
}
