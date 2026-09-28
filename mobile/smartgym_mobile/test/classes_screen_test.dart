import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/class_schedule_model.dart';
import 'package:smartgym_mobile/providers/class_provider.dart';
import 'package:smartgym_mobile/screens/classes_screen.dart';

void main() {
  final sampleSchedules = [
    ClassScheduleModel(
      id: 'sch-1',
      classId: 'cls-1',
      className: 'Metabolic Blast HIIT',
      categoryName: 'High-Intensity Interval Training',
      intensityLevel: 'High',
      trainerId: 'tr-1',
      trainerName: 'Kavinda Fernando',
      room: 'Studio 1 — High Intensity',
      startTime: DateTime.now().add(const Duration(days: 1)),
      endTime: DateTime.now().add(const Duration(days: 1, hours: 1)),
      durationMinutes: 45,
      capacity: 18,
      bookedCount: 4,
      status: 1,
    ),
    ClassScheduleModel(
      id: 'sch-2',
      classId: 'cls-2',
      className: 'Sunrise Vinyasa Yoga',
      categoryName: 'Mind & Body',
      intensityLevel: 'Low',
      trainerId: 'tr-2',
      trainerName: 'Anoma Silva',
      room: 'Studio 2',
      startTime: DateTime.now().add(const Duration(days: 2)),
      endTime: DateTime.now().add(const Duration(days: 2, hours: 1)),
      durationMinutes: 60,
      capacity: 15,
      bookedCount: 15, // FULL
      status: 1,
    ),
  ];

  final sampleBookings = [
    ClassBookingModel(
      id: 'bk-1',
      scheduleId: 'sch-1',
      className: 'Metabolic Blast HIIT',
      trainerName: 'Kavinda Fernando',
      room: 'Studio 1',
      classStartTime: DateTime.now().add(const Duration(days: 1)),
      classEndTime: DateTime.now().add(const Duration(days: 1, hours: 1)),
      memberId: 'mem-1',
      memberName: 'Nuwan Perera',
      memberEmail: 'member@smartgym.com',
      bookingTime: DateTime.now().subtract(const Duration(hours: 2)),
      status: 1,
    ),
  ];

  testWidgets('ClassesScreen renders class schedule list and spots left', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          classSchedulesProvider.overrideWith((ref) async => sampleSchedules),
          myBookingsProvider.overrideWith((ref) async => sampleBookings),
        ],
        child: const MaterialApp(
          home: ClassesScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Fitness Classes'), findsOneWidget);
    expect(find.text('Available Classes'), findsOneWidget);
    expect(find.text('My Reservations'), findsOneWidget);

    expect(find.text('Metabolic Blast HIIT'), findsOneWidget);
    expect(find.text('14 spots left'), findsOneWidget);
    expect(find.text('FULL'), findsOneWidget);
  });

  testWidgets('ClassesScreen shows booking confirmation dialog when Book Spot is tapped', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          classSchedulesProvider.overrideWith((ref) async => sampleSchedules),
          myBookingsProvider.overrideWith((ref) async => sampleBookings),
        ],
        child: const MaterialApp(
          home: ClassesScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    final bookSpotButton = find.text('Book Spot');
    expect(bookSpotButton, findsOneWidget);

    await tester.tap(bookSpotButton);
    await tester.pumpAndSettle();

    expect(find.text('Book Metabolic Blast HIIT'), findsOneWidget);
    expect(find.text('Confirm Reservation'), findsOneWidget);
  });

  testWidgets('ClassesScreen switches to My Reservations tab and shows confirmed booking', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          classSchedulesProvider.overrideWith((ref) async => sampleSchedules),
          myBookingsProvider.overrideWith((ref) async => sampleBookings),
        ],
        child: const MaterialApp(
          home: ClassesScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    final reservationsTab = find.text('My Reservations');
    await tester.tap(reservationsTab);
    await tester.pumpAndSettle();

    expect(find.text('CONFIRMED'), findsOneWidget);
    expect(find.text('Cancel Reservation'), findsOneWidget);
  });
}
