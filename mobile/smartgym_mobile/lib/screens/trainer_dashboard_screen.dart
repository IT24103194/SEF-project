import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/class_schedule_model.dart';
import '../models/trainer_models.dart';
import '../providers/trainer_provider.dart';
import '../services/trainer_api_service.dart';

class TrainerDashboardScreen extends ConsumerStatefulWidget {
  const TrainerDashboardScreen({super.key});

  @override
  ConsumerState<TrainerDashboardScreen> createState() => _TrainerDashboardScreenState();
}

class _TrainerDashboardScreenState extends ConsumerState<TrainerDashboardScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  ClassScheduleModel? _selectedSchedule;

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

  void _openAttendanceSheet(ClassScheduleModel schedule) {
    setState(() {
      _selectedSchedule = schedule;
    });
    _tabController.animateTo(1);
  }

  Future<void> _markAttendance(ScheduleAttendeeModel attendee, String status) async {
    final api = ref.read(trainerApiServiceProvider);
    try {
      final success = await api.recordAttendance(
        bookingId: attendee.bookingId,
        status: status,
      );

      if (mounted) {
        if (success) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Marked ${attendee.memberName} as $status'),
              backgroundColor: status == 'Attended' ? const Color(0xFF10B981) : const Color(0xFFEF4444),
              duration: const Duration(seconds: 2),
            ),
          );
          if (_selectedSchedule != null) {
            ref.invalidate(scheduleAttendeesProvider(_selectedSchedule!.id));
          }
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Failed to update attendance.')),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final schedulesAsync = ref.watch(trainerSchedulesProvider);

    return Scaffold(
      backgroundColor: const Color(0xFF0B0F19),
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827),
        elevation: 0,
        title: const Text(
          'Trainer Station',
          style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () {
              ref.invalidate(trainerSchedulesProvider);
              if (_selectedSchedule != null) {
                ref.invalidate(scheduleAttendeesProvider(_selectedSchedule!.id));
              }
            },
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: const Color(0xFF10B981),
          labelColor: const Color(0xFF10B981),
          unselectedLabelColor: Colors.grey,
          tabs: const [
            Tab(icon: Icon(Icons.fitness_center), text: 'Assigned Classes'),
            Tab(icon: Icon(Icons.how_to_reg), text: 'Attendance Roster'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          // TAB 1: Assigned Classes & Schedules
          RefreshIndicator(
            color: const Color(0xFF10B981),
            onRefresh: () async => ref.invalidate(trainerSchedulesProvider),
            child: SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // KPI Overview
                  schedulesAsync.maybeWhen(
                    data: (schedules) {
                      final totalCapacity = schedules.fold<int>(0, (sum, s) => sum + s.capacity);
                      final totalBooked = schedules.fold<int>(0, (sum, s) => sum + (s.capacity - s.availableSpots));
                      return Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          gradient: const LinearGradient(
                            colors: [Color(0xFF065F46), Color(0xFF047857)],
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                          ),
                          borderRadius: BorderRadius.circular(14),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceAround,
                          children: [
                            _KpiItem(label: 'Classes', value: '${schedules.length}'),
                            Container(height: 35, width: 1, color: Colors.white24),
                            _KpiItem(label: 'Booked', value: '$totalBooked'),
                            Container(height: 35, width: 1, color: Colors.white24),
                            _KpiItem(label: 'Total Spots', value: '$totalCapacity'),
                          ],
                        ),
                      );
                    },
                    orElse: () => const SizedBox.shrink(),
                  ),
                  const SizedBox(height: 20),

                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Class Schedule',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      OutlinedButton.icon(
                        style: OutlinedButton.styleFrom(
                          foregroundColor: const Color(0xFF06B6D4),
                          side: const BorderSide(color: Color(0xFF06B6D4)),
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        ),
                        icon: const Icon(Icons.build_circle_outlined, size: 16),
                        label: const Text('Facility Tickets', style: TextStyle(fontSize: 12)),
                        onPressed: () => Navigator.of(context).pushNamed('/facility-issues'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),

                  schedulesAsync.when(
                    loading: () => const Center(
                      child: Padding(
                        padding: EdgeInsets.all(32.0),
                        child: CircularProgressIndicator(color: Color(0xFF10B981)),
                      ),
                    ),
                    error: (err, _) => Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: const Color(0xFFEF4444).withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Text('Error loading schedules: $err', style: const TextStyle(color: Color(0xFFEF4444))),
                    ),
                    data: (schedules) {
                      if (schedules.isEmpty) {
                        return Container(
                          padding: const EdgeInsets.all(32),
                          decoration: BoxDecoration(
                            color: const Color(0xFF111827),
                            borderRadius: BorderRadius.circular(14),
                          ),
                          child: const Center(
                            child: Text(
                              'No scheduled classes assigned yet.',
                              style: TextStyle(color: Color(0xFF94A3B8)),
                            ),
                          ),
                        );
                      }

                      return ListView.separated(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        itemCount: schedules.length,
                        separatorBuilder: (_, __) => const SizedBox(height: 12),
                        itemBuilder: (context, index) {
                          final schedule = schedules[index];
                          final bookedCount = schedule.capacity - schedule.availableSpots;
                          final isSelected = _selectedSchedule?.id == schedule.id;

                          return Container(
                            padding: const EdgeInsets.all(16),
                            decoration: BoxDecoration(
                              color: const Color(0xFF111827),
                              borderRadius: BorderRadius.circular(14),
                              border: Border.all(
                                color: isSelected ? const Color(0xFF10B981) : const Color(0xFF1E293B),
                                width: isSelected ? 1.5 : 1.0,
                              ),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                  children: [
                                    Expanded(
                                      child: Text(
                                        schedule.className,
                                        style: const TextStyle(
                                          color: Colors.white,
                                          fontWeight: FontWeight.w700,
                                          fontSize: 16,
                                        ),
                                      ),
                                    ),
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                      decoration: BoxDecoration(
                                        color: const Color(0xFF10B981).withValues(alpha: 0.15),
                                        borderRadius: BorderRadius.circular(6),
                                      ),
                                      child: Text(
                                        'Room: ${schedule.room}',
                                        style: const TextStyle(color: Color(0xFF10B981), fontSize: 11, fontWeight: FontWeight.bold),
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  'Trainer: ${schedule.trainerName}',
                                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                                ),
                                const SizedBox(height: 4),
                                Row(
                                  children: [
                                    const Icon(Icons.schedule, size: 14, color: Color(0xFF64748B)),
                                    const SizedBox(width: 4),
                                    Text(
                                      '${schedule.startTime.month}/${schedule.startTime.day} @ ${schedule.startTime.hour.toString().padLeft(2, '0')}:${schedule.startTime.minute.toString().padLeft(2, '0')} (${schedule.durationMinutes}m)',
                                      style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
                                    ),
                                    const Spacer(),
                                    Text(
                                      '$bookedCount / ${schedule.capacity} Booked',
                                      style: TextStyle(
                                        color: schedule.isFull ? const Color(0xFFEF4444) : const Color(0xFF10B981),
                                        fontWeight: FontWeight.w600,
                                        fontSize: 12,
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 12),
                                SizedBox(
                                  width: double.infinity,
                                  child: ElevatedButton.icon(
                                    style: ElevatedButton.styleFrom(
                                      backgroundColor: const Color(0xFF10B981),
                                      foregroundColor: Colors.white,
                                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                    ),
                                    icon: const Icon(Icons.how_to_reg, size: 16),
                                    label: const Text('Take Attendance Roster'),
                                    onPressed: () => _openAttendanceSheet(schedule),
                                  ),
                                ),
                              ],
                            ),
                          );
                        },
                      );
                    },
                  ),
                ],
              ),
            ),
          ),

          // TAB 2: Attendance Roster Sheet
          _selectedSchedule == null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.group_outlined, size: 48, color: Color(0xFF64748B)),
                      const SizedBox(height: 12),
                      const Text(
                        'Select a class schedule from the list\nto record attendee attendance.',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: Color(0xFF94A3B8), fontSize: 14),
                      ),
                      const SizedBox(height: 16),
                      ElevatedButton(
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF10B981),
                          foregroundColor: Colors.white,
                        ),
                        onPressed: () => _tabController.animateTo(0),
                        child: const Text('View Schedules'),
                      ),
                    ],
                  ),
                )
              : _ScheduleAttendanceRoster(
                  schedule: _selectedSchedule!,
                  onMarkAttendance: _markAttendance,
                ),
        ],
      ),
    );
  }
}

class _KpiItem extends StatelessWidget {
  final String label;
  final String value;

  const _KpiItem({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(
          value,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 20,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          label,
          style: const TextStyle(
            color: Colors.white70,
            fontSize: 12,
          ),
        ),
      ],
    );
  }
}

class _ScheduleAttendanceRoster extends ConsumerWidget {
  final ClassScheduleModel schedule;
  final Future<void> Function(ScheduleAttendeeModel attendee, String status) onMarkAttendance;

  const _ScheduleAttendanceRoster({
    required this.schedule,
    required this.onMarkAttendance,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final attendeesAsync = ref.watch(scheduleAttendeesProvider(schedule.id));

    return Padding(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Header
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: const Color(0xFF111827),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0xFF1E293B)),
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        schedule.className,
                        style: const TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'Room: ${schedule.room} | Capacity: ${schedule.capacity}',
                        style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.refresh, color: Color(0xFF10B981)),
                  onPressed: () => ref.invalidate(scheduleAttendeesProvider(schedule.id)),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),

          const Text(
            'Enrolled Members & Check-in',
            style: TextStyle(
              color: Colors.white,
              fontSize: 15,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 10),

          // Attendees List
          Expanded(
            child: attendeesAsync.when(
              loading: () => const Center(
                child: CircularProgressIndicator(color: Color(0xFF10B981)),
              ),
              error: (err, _) => Center(
                child: Text('Error loading attendees: $err', style: const TextStyle(color: Color(0xFFEF4444))),
              ),
              data: (attendees) {
                if (attendees.isEmpty) {
                  return Container(
                    padding: const EdgeInsets.all(32),
                    alignment: Alignment.center,
                    child: const Text(
                      'No members have booked this class yet.',
                      style: TextStyle(color: Color(0xFF94A3B8)),
                    ),
                  );
                }

                return ListView.separated(
                  itemCount: attendees.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 10),
                  itemBuilder: (context, index) {
                    final attendee = attendees[index];

                    return Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: const Color(0xFF111827),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: const Color(0xFF1E293B)),
                      ),
                      child: Row(
                        children: [
                          CircleAvatar(
                            radius: 18,
                            backgroundColor: const Color(0xFF10B981).withValues(alpha: 0.2),
                            child: Text(
                              attendee.memberName.isNotEmpty ? attendee.memberName[0] : 'M',
                              style: const TextStyle(color: Color(0xFF10B981), fontWeight: FontWeight.bold),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  attendee.memberName,
                                  style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 14),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  attendee.memberEmail,
                                  style: const TextStyle(color: Color(0xFF64748B), fontSize: 11),
                                ),
                              ],
                            ),
                          ),
                          // Actions: Present / No Show
                          Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              IconButton(
                                icon: const Icon(Icons.check_circle_outline),
                                color: attendee.isAttended ? const Color(0xFF10B981) : const Color(0xFF64748B),
                                tooltip: 'Mark Present',
                                onPressed: () => onMarkAttendance(attendee, 'Attended'),
                              ),
                              IconButton(
                                icon: const Icon(Icons.cancel_outlined),
                                color: attendee.isNoShow ? const Color(0xFFEF4444) : const Color(0xFF64748B),
                                tooltip: 'Mark No Show',
                                onPressed: () => onMarkAttendance(attendee, 'NoShow'),
                              ),
                            ],
                          ),
                        ],
                      ),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
