import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/class_schedule_model.dart';
import '../providers/class_provider.dart';

class ClassesScreen extends ConsumerStatefulWidget {
  const ClassesScreen({super.key});

  @override
  ConsumerState<ClassesScreen> createState() => _ClassesScreenState();
}

class _ClassesScreenState extends ConsumerState<ClassesScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  bool _isActionInProgress = false;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  void _showBookingDialog(ClassScheduleModel schedule) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E293B),
        title: Text(
          'Book ${schedule.className}',
          style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Instructor: ${schedule.trainerName}', style: const TextStyle(color: Color(0xFF94A3B8))),
            const SizedBox(height: 4),
            Text('Studio: ${schedule.room}', style: const TextStyle(color: Color(0xFF94A3B8))),
            const SizedBox(height: 4),
            Text(
              'Time: ${schedule.startTime.month}/${schedule.startTime.day} @ ${schedule.startTime.hour.toString().padLeft(2, '0')}:${schedule.startTime.minute.toString().padLeft(2, '0')} (${schedule.durationMinutes} mins)',
              style: const TextStyle(color: Color(0xFF94A3B8)),
            ),
            const SizedBox(height: 8),
            Text(
              'Available spots: ${schedule.availableSpots} / ${schedule.capacity}',
              style: TextStyle(
                color: schedule.isFull ? const Color(0xFFEF4444) : const Color(0xFF10B981),
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 12),
            const Text(
              'Note: Active gym membership is required to confirm reservation.',
              style: TextStyle(color: Color(0xFF64748B), fontSize: 12),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel', style: TextStyle(color: Colors.white70)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFF6366F1),
              foregroundColor: Colors.white,
            ),
            onPressed: schedule.isFull
                ? null
                : () async {
                    Navigator.pop(ctx);
                    await _handleBookClass(schedule);
                  },
            child: const Text('Confirm Reservation'),
          ),
        ],
      ),
    );
  }

  Future<void> _handleBookClass(ClassScheduleModel schedule) async {
    setState(() => _isActionInProgress = true);
    final scaffold = ScaffoldMessenger.of(context);

    try {
      final api = ref.read(classApiServiceProvider);
      await api.bookClass(schedule.id);

      scaffold.showSnackBar(
        SnackBar(
          content: Text('Successfully reserved spot in ${schedule.className}!'),
          backgroundColor: const Color(0xFF10B981),
        ),
      );

      // Refresh data
      ref.invalidate(classSchedulesProvider);
      ref.invalidate(myBookingsProvider);
    } catch (e) {
      scaffold.showSnackBar(
        SnackBar(
          content: Text(e.toString().replaceAll('Exception: ', '')),
          backgroundColor: const Color(0xFFEF4444),
        ),
      );
    } finally {
      if (mounted) setState(() => _isActionInProgress = false);
    }
  }

  Future<void> _handleCancelBooking(ClassBookingModel booking) async {
    final scaffold = ScaffoldMessenger.of(context);
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E293B),
        title: const Text('Cancel Reservation', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        content: Text(
          'Are you sure you want to cancel your reservation for ${booking.className}?',
          style: const TextStyle(color: Color(0xFF94A3B8)),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Keep Booking', style: TextStyle(color: Colors.white70)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFEF4444),
              foregroundColor: Colors.white,
            ),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Yes, Cancel'),
          ),
        ],
      ),
    );

    if (confirm != true || !mounted) return;

    setState(() => _isActionInProgress = true);

    try {
      final api = ref.read(classApiServiceProvider);
      await api.cancelBooking(booking.id);

      scaffold.showSnackBar(
        SnackBar(
          content: Text('Reservation for ${booking.className} cancelled.'),
          backgroundColor: const Color(0xFF10B981),
        ),
      );

      ref.invalidate(classSchedulesProvider);
      ref.invalidate(myBookingsProvider);
    } catch (e) {
      scaffold.showSnackBar(
        SnackBar(
          content: Text(e.toString().replaceAll('Exception: ', '')),
          backgroundColor: const Color(0xFFEF4444),
        ),
      );
    } finally {
      if (mounted) setState(() => _isActionInProgress = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0B0F19),
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827),
        elevation: 0,
        title: const Text('Fitness Classes', style: TextStyle(fontWeight: FontWeight.bold)),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () {
              ref.invalidate(classSchedulesProvider);
              ref.invalidate(myBookingsProvider);
            },
            tooltip: 'Refresh',
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: const Color(0xFF6366F1),
          tabs: const [
            Tab(text: 'Available Classes', icon: Icon(Icons.calendar_today, size: 18)),
            Tab(text: 'My Reservations', icon: Icon(Icons.bookmark, size: 18)),
          ],
        ),
      ),
      body: _isActionInProgress
          ? const Center(child: CircularProgressIndicator(color: Color(0xFF6366F1)))
          : TabBarView(
              controller: _tabController,
              children: [
                _buildAvailableClassesTab(),
                _buildMyBookingsTab(),
              ],
            ),
    );
  }

  Widget _buildAvailableClassesTab() {
    final schedulesAsync = ref.watch(classSchedulesProvider);

    return schedulesAsync.when(
      loading: () => const Center(child: CircularProgressIndicator(color: Color(0xFF6366F1))),
      error: (err, _) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, color: Color(0xFFEF4444), size: 48),
              const SizedBox(height: 12),
              const Text(
                'Failed to load class schedule',
                style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                err.toString(),
                textAlign: TextAlign.center,
                style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () => ref.invalidate(classSchedulesProvider),
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
      ),
      data: (schedules) {
        if (schedules.isEmpty) {
          return const Center(
            child: Text(
              'No upcoming sessions scheduled.',
              style: TextStyle(color: Color(0xFF94A3B8)),
            ),
          );
        }

        return ListView.builder(
          padding: const EdgeInsets.all(16),
          itemCount: schedules.length,
          itemBuilder: (context, index) {
            final schedule = schedules[index];
            final startTime = schedule.startTime;

            return Card(
              color: const Color(0xFF1E293B),
              margin: const EdgeInsets.only(bottom: 12),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Text(
                            schedule.className,
                            style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                          decoration: BoxDecoration(
                            color: schedule.isFull
                                ? const Color(0xFFEF4444).withValues(alpha: 0.2)
                                : const Color(0xFF10B981).withValues(alpha: 0.2),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            schedule.isFull ? 'FULL' : '${schedule.availableSpots} spots left',
                            style: TextStyle(
                              color: schedule.isFull ? const Color(0xFFEF4444) : const Color(0xFF10B981),
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      '${schedule.categoryName} • ${schedule.intensityLevel} Intensity',
                      style: const TextStyle(color: Color(0xFF6366F1), fontSize: 12, fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        const Icon(Icons.person, size: 15, color: Color(0xFF94A3B8)),
                        const SizedBox(width: 4),
                        Text(schedule.trainerName, style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13)),
                        const SizedBox(width: 16),
                        const Icon(Icons.location_on, size: 15, color: Color(0xFF94A3B8)),
                        const SizedBox(width: 4),
                        Text(schedule.room, style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13)),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        const Icon(Icons.access_time, size: 15, color: Color(0xFF94A3B8)),
                        const SizedBox(width: 4),
                        Text(
                          '${startTime.month}/${startTime.day} @ ${startTime.hour.toString().padLeft(2, '0')}:${startTime.minute.toString().padLeft(2, '0')} (${schedule.durationMinutes} min)',
                          style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton(
                        style: ElevatedButton.styleFrom(
                          backgroundColor: schedule.isFull ? const Color(0xFF334155) : const Color(0xFF6366F1),
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                        ),
                        onPressed: schedule.isFull ? null : () => _showBookingDialog(schedule),
                        child: Text(schedule.isFull ? 'Class Full' : 'Book Spot'),
                      ),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildMyBookingsTab() {
    final bookingsAsync = ref.watch(myBookingsProvider);

    return bookingsAsync.when(
      loading: () => const Center(child: CircularProgressIndicator(color: Color(0xFF6366F1))),
      error: (err, _) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Text(
            'Failed to load reservations: $err',
            style: const TextStyle(color: Color(0xFFEF4444)),
          ),
        ),
      ),
      data: (bookings) {
        if (bookings.isEmpty) {
          return const Center(
            child: Text(
              'No class reservations found.\nBook an upcoming class from the Available Classes tab!',
              textAlign: TextAlign.center,
              style: TextStyle(color: Color(0xFF94A3B8)),
            ),
          );
        }

        return ListView.builder(
          padding: const EdgeInsets.all(16),
          itemCount: bookings.length,
          itemBuilder: (context, index) {
            final booking = bookings[index];
            final startTime = booking.classStartTime;
            final isConfirmed = booking.isConfirmed;

            return Card(
              color: const Color(0xFF1E293B),
              margin: const EdgeInsets.only(bottom: 12),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Text(
                            booking.className,
                            style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                          decoration: BoxDecoration(
                            color: isConfirmed
                                ? const Color(0xFF10B981).withValues(alpha: 0.2)
                                : const Color(0xFFEF4444).withValues(alpha: 0.2),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            isConfirmed ? 'CONFIRMED' : 'CANCELLED',
                            style: TextStyle(
                              color: isConfirmed ? const Color(0xFF10B981) : const Color(0xFFEF4444),
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Instructor: ${booking.trainerName} • ${booking.room}',
                      style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Session: ${startTime.month}/${startTime.day} @ ${startTime.hour.toString().padLeft(2, '0')}:${startTime.minute.toString().padLeft(2, '0')}',
                      style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                    ),
                    if (booking.attendanceStatus != null) ...[
                      const SizedBox(height: 4),
                      Text(
                        'Attendance: ${booking.attendanceStatus}',
                        style: const TextStyle(color: Color(0xFF6366F1), fontSize: 12, fontWeight: FontWeight.w600),
                      ),
                    ],
                    if (isConfirmed && startTime.isAfter(DateTime.now())) ...[
                      const SizedBox(height: 12),
                      Align(
                        alignment: Alignment.centerRight,
                        child: OutlinedButton(
                          style: OutlinedButton.styleFrom(
                            foregroundColor: const Color(0xFFEF4444),
                            side: const BorderSide(color: Color(0xFFEF4444)),
                          ),
                          onPressed: () => _handleCancelBooking(booking),
                          child: const Text('Cancel Reservation'),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }
}
