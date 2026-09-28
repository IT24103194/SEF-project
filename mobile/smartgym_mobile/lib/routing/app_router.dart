import 'package:flutter/material.dart';
import '../screens/login_screen.dart';
import '../screens/home_screen.dart';
import '../screens/facility_report_screen.dart';
import '../screens/facility_issue_list_screen.dart';
import '../screens/classes_screen.dart';
import '../screens/inventory_screen.dart';
import '../screens/membership_screen.dart';

class AppRouter {
  static const String initialRoute = '/login';

  static Map<String, WidgetBuilder> get routes => {
        '/login': (context) => const LoginScreen(),
        '/home': (context) => const HomeScreen(),
        '/facility-report': (context) => const FacilityReportScreen(),
        '/facility-issues': (context) => const FacilityIssueListScreen(),
        '/classes': (context) => const ClassesScreen(),
        '/inventory': (context) => const InventoryScreen(),
        '/membership': (context) => const MembershipScreen(),
      };
}
