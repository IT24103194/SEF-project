import 'package:flutter/material.dart';

class StatusBadge extends StatelessWidget {
  final String status;

  const StatusBadge({super.key, required this.status});

  Color _getColor() {
    switch (status.toLowerCase()) {
      case 'resolved':
      case 'completed':
      case 'active':
        return const Color(0xFF10B981);
      case 'requiresapproval':
      case 'pending':
      case 'inreview':
        return const Color(0xFFF59E0B);
      case 'rejected':
      case 'cancelled':
        return const Color(0xFFF43F5E);
      case 'inrepair':
      case 'ordered':
      case 'diagnosing':
        return const Color(0xFF06B6D4);
      default:
        return const Color(0xFF6366F1);
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _getColor();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Text(
        status.toUpperCase(),
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.5,
        ),
      ),
    );
  }
}
