import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/class_schedule_model.dart';
import 'package:smartgym_mobile/models/trainer_models.dart';
import 'package:smartgym_mobile/screens/trainer_dashboard_screen.dart';
import 'package:smartgym_mobile/services/api_service.dart';
import 'package:smartgym_mobile/services/trainer_api_service.dart';

class MockTrainerApiService extends TrainerApiService {
  final List<ClassScheduleModel> schedules;
  final List<ScheduleAttendeeModel> attendees;
  String? lastMarkedBookingId;
  String? lastMarkedStatus;

  MockTrainerApiService({
    required this.schedules,
    required this.attendees,
  }) : super(ApiService());

  @override
  Future<List<ClassScheduleModel>> fetchAssignedSchedules() async => schedules;

  @override
  Future<List<ScheduleAttendeeModel>> fetchScheduleAttendees(String scheduleId) async => attendees;

  @override
  Future<bool> recordAttendance({
    required String bookingId,
    required String status,
    String? notes,
  }) async {
    lastMarkedBookingId = bookingId;
    lastMarkedStatus = status;
    return true;
  }
}

void main() {
  final sampleSchedules = <ClassScheduleModel>[
    ClassScheduleModel(
      id: 'sched-1',
      classId: 'cls-1',
      className: 'HIIT Conditioning',
      categoryName: 'High Intensity',
      intensityLevel: 'High',
      trainerId: 'trainer-1',
      trainerName: 'Marcus Vance',
      room: 'Studio A',
      startTime: DateTime.now().add(const Duration(hours: 2)),
      endTime: DateTime.now().add(const Duration(hours: 3)),
      durationMinutes: 60,
      capacity: 20,
      bookedCount: 15,
      status: 1,
    ),
  ];

  final sampleAttendees = <ScheduleAttendeeModel>[
    ScheduleAttendeeModel(
      bookingId: 'book-10',
      memberId: 'mem-1',
      memberName: 'John Athlete',
      memberEmail: 'john@example.com',
      status: 'Confirmed',
      bookedAt: DateTime.now().subtract(const Duration(days: 1)),
    ),
    ScheduleAttendeeModel(
      bookingId: 'book-11',
      memberId: 'mem-2',
      memberName: 'Sarah Connor',
      memberEmail: 'sarah@example.com',
      status: 'Confirmed',
      bookedAt: DateTime.now().subtract(const Duration(days: 2)),
      attendanceStatus: 'Attended',
    ),
  ];

  testWidgets('TrainerDashboardScreen renders assigned schedules and KPI metrics', (tester) async {
    final mockService = MockTrainerApiService(
      schedules: sampleSchedules,
      attendees: sampleAttendees,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          trainerApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: TrainerDashboardScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Trainer Station'), findsOneWidget);
    expect(find.text('Assigned Classes'), findsOneWidget);
    expect(find.text('Attendance Roster'), findsOneWidget);
    expect(find.text('HIIT Conditioning'), findsOneWidget);
    expect(find.text('Room: Studio A'), findsOneWidget);
    expect(find.text('15 / 20 Booked'), findsOneWidget);
    expect(find.text('Take Attendance Roster'), findsOneWidget);
  });

  testWidgets('TrainerDashboardScreen opens attendance roster and displays enrolled members', (tester) async {
    final mockService = MockTrainerApiService(
      schedules: sampleSchedules,
      attendees: sampleAttendees,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          trainerApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: TrainerDashboardScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Tap Take Attendance Roster button
    await tester.tap(find.text('Take Attendance Roster'));
    await tester.pumpAndSettle();

    expect(find.text('Enrolled Members & Check-in'), findsOneWidget);
    expect(find.text('John Athlete'), findsOneWidget);
    expect(find.text('Sarah Connor'), findsOneWidget);
  });

  testWidgets('TrainerDashboardScreen marks attendee present', (tester) async {
    final mockService = MockTrainerApiService(
      schedules: sampleSchedules,
      attendees: sampleAttendees,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          trainerApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: TrainerDashboardScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();
    await tester.tap(find.text('Take Attendance Roster'));
    await tester.pumpAndSettle();

    // Tap Present checkmark for first attendee
    final checkmarkFinders = find.byIcon(Icons.check_circle_outline);
    expect(checkmarkFinders, findsWidgets);

    await tester.tap(checkmarkFinders.first);
    await tester.pump();

    expect(mockService.lastMarkedBookingId, equals('book-10'));
    expect(mockService.lastMarkedStatus, equals('Attended'));
  });
}
