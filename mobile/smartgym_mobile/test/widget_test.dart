import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartgym_mobile/main.dart';

void main() {
  testWidgets('SmartGym app renders LoginScreen by default', (WidgetTester tester) async {
    await tester.pumpWidget(
      const ProviderScope(
        child: SmartGymMobileApp(),
      ),
    );

    expect(find.text('SmartGym Mobile'), findsOneWidget);
    expect(find.text('Sign In to SmartGym'), findsOneWidget);
  });
}
