import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/facility_issue_model.dart';
import 'package:smartgym_mobile/screens/facility_issue_detail_screen.dart';
import 'package:smartgym_mobile/screens/facility_issue_list_screen.dart';
import 'package:smartgym_mobile/screens/facility_report_screen.dart';
import 'package:smartgym_mobile/services/api_service.dart';
import 'package:smartgym_mobile/services/facility_api_service.dart';

class MockFacilityApiService extends FacilityApiService {
  final List<LocationModel> mockLocations;
  final List<EquipmentModel> mockEquipment;
  final List<FacilityIssueModel> mockIssues;
  final List<IssueHistoryModel> mockHistory;

  MockFacilityApiService({
    required this.mockLocations,
    required this.mockEquipment,
    required this.mockIssues,
    required this.mockHistory,
  }) : super(ApiService());

  @override
  Future<List<LocationModel>> fetchLocations() async => mockLocations;

  @override
  Future<List<EquipmentModel>> fetchEquipment({String? locationId}) async => mockEquipment;

  @override
  Future<List<FacilityIssueModel>> fetchMemberIssues() async => mockIssues;

  @override
  Future<FacilityIssueModel?> fetchIssueDetails(String id) async {
    return mockIssues.firstWhere((i) => i.id == id, orElse: () => mockIssues.first);
  }

  @override
  Future<List<IssueHistoryModel>> fetchIssueHistory(String id) async => mockHistory;

  @override
  Future<bool> submitIssueWithImage({
    required String locationId,
    String? equipmentId,
    required String title,
    required String description,
    int severity = 2,
    String? imagePath,
    List<int>? imageBytes,
    String? fileName,
  }) async {
    return true;
  }
}

void main() {
  const sampleLocation = LocationModel(
    id: 'loc-1',
    name: 'Cardio Zone A',
    floor: 'Ground Floor',
    description: 'Treadmills and rowers',
  );

  const sampleEquipment = EquipmentModel(
    id: 'eq-1',
    locationId: 'loc-1',
    locationName: 'Cardio Zone A',
    serialNumber: 'LF-TRD-001',
    name: 'LifeFitness Treadmill T1',
    model: 'Elevation T95',
    manufacturer: 'LifeFitness USA',
    statusName: 'Operational',
  );

  final sampleIssue = FacilityIssueModel(
    id: 'iss-1',
    reportedByMemberId: 'mem-1',
    reporterName: 'Nuwan Perera',
    equipmentId: 'eq-1',
    equipmentName: 'LifeFitness Treadmill T1',
    equipmentSerialNumber: 'LF-TRD-001',
    locationId: 'loc-1',
    locationName: 'Cardio Zone A',
    locationFloor: 'Ground Floor',
    title: 'Treadmill Belt Slipping',
    description: 'Belt slips when running above 10 km/h.',
    sanitizedDescription: 'Belt slips when running above 10 km/h.',
    moderationStatus: 'Clean',
    severityName: 'High',
    statusName: 'SUBMITTED',
    reportedAt: DateTime(2026, 9, 28, 10, 0),
    images: [],
  );

  final sampleHistory = [
    IssueHistoryModel(
      id: 'hist-1',
      action: 'ISSUE_SUBMITTED',
      fromStatus: null,
      toStatus: 'SUBMITTED',
      performedByUserName: 'Nuwan Perera',
      notes: null,
      timestamp: DateTime(2026, 9, 28, 10, 0),
    ),
    IssueHistoryModel(
      id: 'hist-2',
      action: 'STATUS_TRANSITION',
      fromStatus: 'SUBMITTED',
      toStatus: 'APPROVED',
      performedByUserName: 'Admin User',
      notes: 'Approved for vendor inspection',
      timestamp: DateTime(2026, 9, 28, 10, 30),
    ),
  ];

  late MockFacilityApiService mockService;

  setUp(() {
    mockService = MockFacilityApiService(
      mockLocations: [sampleLocation],
      mockEquipment: [sampleEquipment],
      mockIssues: [sampleIssue],
      mockHistory: sampleHistory,
    );
  });

  testWidgets('FacilityReportScreen renders fields and photo capture buttons', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          facilityApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: FacilityReportScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Report Equipment Issue'), findsOneWidget);
    expect(find.text('Take Photo'), findsOneWidget);
    expect(find.text('Gallery'), findsOneWidget);
    expect(find.text('Submit Equipment Report'), findsOneWidget);
  });

  testWidgets('FacilityReportScreen validates required fields on submit', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          facilityApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: FacilityReportScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Ensure button is scrolled into view in SingleChildScrollView
    await tester.ensureVisible(find.text('Submit Equipment Report'));
    await tester.pumpAndSettle();

    // Tap submit without typing title or description
    await tester.tap(find.text('Submit Equipment Report'));
    await tester.pumpAndSettle();

    expect(find.text('Required'), findsWidgets);
  });

  testWidgets('FacilityIssueListScreen displays reported issues list', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          facilityApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: FacilityIssueListScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('My Reported Issues'), findsOneWidget);
    expect(find.text('Treadmill Belt Slipping'), findsOneWidget);
    expect(find.text('SUBMITTED'), findsOneWidget);
    expect(find.text('Cardio Zone A'), findsOneWidget);
  });

  testWidgets('FacilityIssueDetailScreen displays issue details and status history', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          facilityApiServiceProvider.overrideWithValue(mockService),
        ],
        child: const MaterialApp(
          home: FacilityIssueDetailScreen(issueId: 'iss-1'),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Issue Tracking Details'), findsOneWidget);
    expect(find.text('Treadmill Belt Slipping'), findsOneWidget);
    expect(find.text('Problem Description'), findsOneWidget);
    expect(find.text('Belt slips when running above 10 km/h.'), findsOneWidget);
    expect(find.text('Status History & Audit Trail'), findsOneWidget);
    expect(find.text('ISSUE_SUBMITTED'), findsOneWidget);
    expect(find.text('STATUS_TRANSITION'), findsOneWidget);
  });
}
