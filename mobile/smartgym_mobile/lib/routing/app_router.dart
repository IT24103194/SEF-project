import 'package:flutter/material.dart';
import '../screens/login_screen.dart';
import '../screens/register_screen.dart';
import '../screens/home_screen.dart';
import '../screens/profile_screen.dart';
import '../screens/facility_report_screen.dart';
import '../screens/facility_issue_list_screen.dart';
import '../screens/classes_screen.dart';
import '../screens/inventory_screen.dart';
import '../screens/membership_screen.dart';
import '../screens/notifications_screen.dart';
import '../screens/feedback_screen.dart';
import '../screens/trainer_dashboard_screen.dart';

class AppRouter {
  static const String initialRoute = '/login';

  static Map<String, WidgetBuilder> get routes => {
        '/login': (context) => const LoginScreen(),
        '/register': (context) => const RegisterScreen(),
        '/home': (context) => const HomeScreen(),
        '/profile': (context) => const ProfileScreen(),
        '/facility-report': (context) => const FacilityReportScreen(),
        '/facility-issues': (context) => const FacilityIssueListScreen(),
        '/classes': (context) => const ClassesScreen(),
        '/inventory': (context) => const InventoryScreen(),
        '/membership': (context) => const MembershipScreen(),
        '/goals': (context) => const MembershipScreen(),
        '/notifications': (context) => const NotificationsScreen(),
        '/feedback': (context) => const FeedbackScreen(),
        '/trainer-dashboard': (context) => const TrainerDashboardScreen(),
      };
}
