import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/feedback_model.dart';
import 'package:smartgym_mobile/screens/feedback_screen.dart';
import 'package:smartgym_mobile/services/api_service.dart';
import 'package:smartgym_mobile/services/feedback_api_service.dart';

class MockFeedbackApiService extends FeedbackApiService {
  final List<FeedbackModel> mockList;
  String? lastSubmittedSubject;
  String? lastSubmittedContent;
  int? lastSubmittedRating;

  MockFeedbackApiService({required this.mockList}) : super(ApiService());

  @override
  Future<List<FeedbackModel>> fetchFeedbacks() async => mockList;

  @override
  Future<bool> submitFeedback({
    required String subject,
    required String content,
    required int rating,
  }) async {
    lastSubmittedSubject = subject;
    lastSubmittedContent = content;
    lastSubmittedRating = rating;
    return true;
  }
}

void main() {
  final sampleFeedbacks = <FeedbackModel>[
    FeedbackModel(
      id: 'fb-1',
      memberName: 'John Doe',
      subject: 'Equipment & Facility',
      content: 'Treadmills in Cardio Zone A are clean and working smoothly!',
      rating: 5,
      statusName: 'Reviewed',
      adminResponse: 'Thank you John! Our maintenance crew appreciates your feedback.',
      createdAt: DateTime.now().subtract(const Duration(days: 2)),
    ),
  ];

  testWidgets('FeedbackScreen renders form and previous feedback history', (tester) async {
    final mockService = MockFeedbackApiService(mockList: sampleFeedbacks);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          feedbackApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: FeedbackScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Member Feedback'), findsOneWidget);
    expect(find.text('Share Your Experience'), findsOneWidget);
    expect(find.text('Category'), findsOneWidget);
    expect(find.text('Comments'), findsOneWidget);
    expect(find.text('Submit Feedback'), findsOneWidget);
    expect(find.text('My Feedback History'), findsOneWidget);

    // Verify existing feedback item
    expect(find.text('Equipment & Facility'), findsOneWidget); // in past card
    expect(find.text('Treadmills in Cardio Zone A are clean and working smoothly!'), findsOneWidget);
    expect(find.text('REVIEWED'), findsOneWidget);
    expect(find.text('Staff Response: Thank you John! Our maintenance crew appreciates your feedback.'), findsOneWidget);
  });

  testWidgets('FeedbackScreen validates comments before submission', (tester) async {
    final mockService = MockFeedbackApiService(mockList: sampleFeedbacks);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          feedbackApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: FeedbackScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Tap submit without typing comments
    await tester.tap(find.text('Submit Feedback'));
    await tester.pumpAndSettle();

    expect(find.text('Please enter comments'), findsOneWidget);
  });

  testWidgets('FeedbackScreen submits feedback successfully', (tester) async {
    final mockService = MockFeedbackApiService(mockList: sampleFeedbacks);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          feedbackApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: FeedbackScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Enter comments
    await tester.enterText(find.byType(TextFormField), 'Great kettlebell section in Studio B!');
    await tester.pumpAndSettle();

    // Tap submit
    await tester.tap(find.text('Submit Feedback'));
    await tester.pumpAndSettle();

    expect(mockService.lastSubmittedContent, equals('Great kettlebell section in Studio B!'));
    expect(mockService.lastSubmittedRating, equals(5));
  });
}
