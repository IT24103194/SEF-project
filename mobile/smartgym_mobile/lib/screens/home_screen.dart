import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/auth_provider.dart';
import '../providers/notification_provider.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final user = authState.user;
    final unreadCount = ref.watch(notificationProvider.select((s) => s.unreadCount));
    final isTrainer = user?.isTrainer ?? false;

    return Scaffold(
      backgroundColor: const Color(0xFF0B0F19),
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827),
        elevation: 0,
        title: Row(
          children: [
            const Icon(Icons.fitness_center, color: Color(0xFF6366F1), size: 22),
            const SizedBox(width: 8),
            Text(
              isTrainer ? 'SmartGym Trainer' : 'SmartGym Mobile',
              style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
            ),
          ],
        ),
        actions: [
          Stack(
            alignment: Alignment.center,
            children: [
              IconButton(
                icon: const Icon(Icons.notifications_outlined),
                tooltip: 'Notifications',
                onPressed: () {
                  Navigator.of(context).pushNamed('/notifications');
                },
              ),
              if (unreadCount > 0)
                Positioned(
                  top: 10,
                  right: 10,
                  child: Container(
                    padding: const EdgeInsets.all(3),
                    decoration: const BoxDecoration(
                      color: Color(0xFFEF4444),
                      shape: BoxShape.circle,
                    ),
                    constraints: const BoxConstraints(
                      minWidth: 16,
                      minHeight: 16,
                    ),
                    child: Text(
                      '$unreadCount',
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 9,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                ),
            ],
          ),
          IconButton(
            icon: const Icon(Icons.account_circle_outlined),
            tooltip: 'My Profile',
            onPressed: () => Navigator.of(context).pushNamed('/profile'),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Sign Out',
            onPressed: () async {
              await ref.read(authProvider.notifier).logout();
              if (context.mounted) {
                Navigator.of(context).pushReplacementNamed('/login');
              }
            },
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Welcome Header
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: isTrainer
                      ? [const Color(0xFF047857), const Color(0xFF065F46)]
                      : [const Color(0xFF6366F1), const Color(0xFF06B6D4)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(16),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.25),
                    blurRadius: 10,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Welcome back, ${user?.firstName ?? 'Athlete'}!',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 20,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.black.withValues(alpha: 0.25),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Text(
                          isTrainer ? 'TRAINER' : 'MEMBER',
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    isTrainer
                        ? 'Manage your assigned classes, track member attendance & monitor gym floor'
                        : 'Track your fitness goals, book classes & manage your membership',
                    style: TextStyle(
                      color: Colors.white.withValues(alpha: 0.9),
                      fontSize: 13,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            const Text(
              'Quick Actions',
              style: TextStyle(
                color: Colors.white,
                fontSize: 16,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 12),

            // TRAINER SPECIFIC ACTIONS
            if (isTrainer) ...[
              _ActionTile(
                title: 'Trainer Station & Schedules',
                subtitle: 'Assigned classes, timings, and capacity roster',
                icon: Icons.fitness_center,
                iconColor: const Color(0xFF10B981),
                onTap: () => Navigator.of(context).pushNamed('/trainer-dashboard'),
              ),
              const SizedBox(height: 12),

              _ActionTile(
                title: 'Take Class Attendance',
                subtitle: 'Check-in registered members & mark no-shows',
                icon: Icons.how_to_reg,
                iconColor: const Color(0xFF06B6D4),
                onTap: () => Navigator.of(context).pushNamed('/trainer-dashboard'),
              ),
              const SizedBox(height: 12),

              _ActionTile(
                title: 'Facility & Equipment Tickets',
                subtitle: 'Inspect broken equipment reports & resolution progress',
                icon: Icons.build_circle_outlined,
                iconColor: const Color(0xFFF59E0B),
                onTap: () => Navigator.of(context).pushNamed('/facility-issues'),
              ),
              const SizedBox(height: 12),

              _ActionTile(
                title: 'Supplement & Equipment Inventory',
                subtitle: 'Check gym floor stock, supplements & adjustment logs',
                icon: Icons.inventory_2_outlined,
                iconColor: const Color(0xFF6366F1),
                onTap: () => Navigator.of(context).pushNamed('/inventory'),
              ),
              const SizedBox(height: 12),
            ],

            // MEMBER SPECIFIC ACTIONS
            if (!isTrainer) ...[
              _ActionTile(
                title: 'My Membership & Fitness Goals',
                subtitle: 'Check validity, renew plans & track workout milestones',
                icon: Icons.card_membership_outlined,
                iconColor: const Color(0xFF6366F1),
                onTap: () => Navigator.of(context).pushNamed('/membership'),
              ),
              const SizedBox(height: 12),

              _ActionTile(
                title: 'Browse & Book Classes',
                subtitle: 'Reserve fitness classes, check timetable & my bookings',
                icon: Icons.calendar_month_outlined,
                iconColor: const Color(0xFF10B981),
                onTap: () => Navigator.of(context).pushNamed('/classes'),
              ),
              const SizedBox(height: 12),

              _ActionTile(
                title: 'Equipment & Facility Issues',
                subtitle: 'Take photos with camera, report broken gym equipment',
                icon: Icons.camera_alt_outlined,
                iconColor: const Color(0xFF06B6D4),
                onTap: () => Navigator.of(context).pushNamed('/facility-issues'),
              ),
              const SizedBox(height: 12),

              _ActionTile(
                title: 'Member Feedback & Ratings',
                subtitle: 'Rate our gym facilities, classes, and instructors',
                icon: Icons.star_rate_outlined,
                iconColor: const Color(0xFFF59E0B),
                onTap: () => Navigator.of(context).pushNamed('/feedback'),
              ),
              const SizedBox(height: 12),

              _ActionTile(
                title: 'Nutrition & Supplements Catalog',
                subtitle: 'Browse protein, vitamins, and energy products',
                icon: Icons.inventory_2_outlined,
                iconColor: const Color(0xFF8B5CF6),
                onTap: () => Navigator.of(context).pushNamed('/inventory'),
              ),
              const SizedBox(height: 12),
            ],

            // Notifications
            _ActionTile(
              title: 'Notifications & Alerts',
              subtitle: 'Class bookings, tickets & membership notices',
              icon: Icons.notifications_active_outlined,
              iconColor: const Color(0xFFEC4899),
              onTap: () => Navigator.of(context).pushNamed('/notifications'),
            ),
          ],
        ),
      ),
    );
  }
}

class _ActionTile extends StatelessWidget {
  final String title;
  final String subtitle;
  final IconData icon;
  final Color iconColor;
  final VoidCallback onTap;

  const _ActionTile({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.iconColor,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: const Color(0xFF111827),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFF1E293B)),
        ),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: iconColor.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(icon, color: iconColor, size: 26),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    subtitle,
                    style: const TextStyle(
                      color: Color(0xFF94A3B8),
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
            ),
            const Icon(Icons.chevron_right, color: Color(0xFF64748B)),
          ],
        ),
      ),
    );
  }
}
