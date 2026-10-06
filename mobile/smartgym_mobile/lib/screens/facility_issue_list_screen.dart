import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/facility_issue_model.dart';
import '../services/facility_api_service.dart';
import 'facility_issue_detail_screen.dart';
import 'facility_report_screen.dart';
import '../widgets/animated_gym_background.dart';

class FacilityIssueListScreen extends ConsumerStatefulWidget {
  const FacilityIssueListScreen({super.key});

  @override
  ConsumerState<FacilityIssueListScreen> createState() => _FacilityIssueListScreenState();
}

class _FacilityIssueListScreenState extends ConsumerState<FacilityIssueListScreen> {
  List<FacilityIssueModel> _issues = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadIssues();
  }

  Future<void> _loadIssues() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    final api = ref.read(facilityApiServiceProvider);
    try {
      final items = await api.fetchMemberIssues();
      if (mounted) {
        setState(() {
          _issues = items;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = 'Failed to load issues.';
          _isLoading = false;
        });
      }
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'SUBMITTED':
        return const Color(0xFF64748B);
      case 'AI_ANALYZING':
        return const Color(0xFF8B5CF6);
      case 'PENDING_APPROVAL':
        return const Color(0xFFF59E0B);
      case 'APPROVED':
        return const Color(0xFF3B82F6);
      case 'VENDOR_CONTACTED':
        return const Color(0xFF06B6D4);
      case 'REPAIR_SCHEDULED':
        return const Color(0xFFEC4899);
      case 'IN_PROGRESS':
        return const Color(0xFF38BDF8);
      case 'RESOLVED':
        return const Color(0xFF10B981);
      case 'REJECTED':
        return const Color(0xFFEF4444);
      case 'REVISION_REQUIRED':
        return const Color(0xFFF97316);
      default:
        return const Color(0xFF94A3B8);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedGymBackground(
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827).withValues(alpha: 0.85),
        elevation: 0,
        title: const Text('My Reported Issues'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadIssues,
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        backgroundColor: const Color(0xFF6366F1),
        icon: const Icon(Icons.add_a_photo, color: Colors.white),
        label: const Text('Report Issue', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        onPressed: () async {
          await Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const FacilityReportScreen()),
          );
          _loadIssues();
        },
      ),
      child: _isLoading
          ? const Center(child: CircularProgressIndicator(color: Color(0xFF6366F1)))
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(_error!, style: const TextStyle(color: Colors.white70)),
                      const SizedBox(height: 12),
                      ElevatedButton(onPressed: _loadIssues, child: const Text('Retry')),
                    ],
                  ),
                )
              : _issues.isEmpty
                  ? Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Icon(Icons.check_circle_outline, color: Color(0xFF10B981), size: 56),
                          const SizedBox(height: 16),
                          const Text('No Open Equipment Issues', style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 8),
                          const Text('Everything in your gym is operating smoothly!', style: TextStyle(color: Color(0xFF94A3B8), fontSize: 14)),
                          const SizedBox(height: 24),
                          ElevatedButton.icon(
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFF6366F1),
                              foregroundColor: Colors.white,
                              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                            ),
                            icon: const Icon(Icons.camera_alt),
                            label: const Text('Report an Issue'),
                            onPressed: () async {
                              await Navigator.of(context).push(
                                MaterialPageRoute(builder: (_) => const FacilityReportScreen()),
                              );
                              _loadIssues();
                            },
                          ),
                        ],
                      ),
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.fromLTRB(16, 16, 16, 80),
                      itemCount: _issues.length,
                      itemBuilder: (context, index) {
                        final issue = _issues[index];
                        final statusColor = _getStatusColor(issue.statusName);

                        return Container(
                          margin: const EdgeInsets.only(bottom: 12),
                          decoration: BoxDecoration(
                            color: const Color(0xFF111827),
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: Colors.white.withValues(alpha: 0.05)),
                          ),
                          child: InkWell(
                            borderRadius: BorderRadius.circular(12),
                            onTap: () {
                              Navigator.of(context).push(
                                MaterialPageRoute(
                                  builder: (_) => FacilityIssueDetailScreen(issueId: issue.id),
                                ),
                              );
                            },
                            child: Padding(
                              padding: const EdgeInsets.all(16),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: [
                                      Container(
                                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                        decoration: BoxDecoration(
                                          color: statusColor.withValues(alpha: 0.15),
                                          borderRadius: BorderRadius.circular(6),
                                          border: Border.all(color: statusColor.withValues(alpha: 0.4)),
                                        ),
                                        child: Text(
                                          issue.statusName,
                                          style: TextStyle(
                                            color: statusColor,
                                            fontSize: 11,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                      ),
                                      Text(
                                        '${issue.reportedAt.day}/${issue.reportedAt.month}/${issue.reportedAt.year}',
                                        style: const TextStyle(color: Color(0xFF64748B), fontSize: 12),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 10),
                                  Text(
                                    issue.title,
                                    style: const TextStyle(
                                      color: Colors.white,
                                      fontSize: 16,
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                  const SizedBox(height: 6),
                                  Text(
                                    issue.sanitizedDescription ?? issue.description,
                                    maxLines: 2,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                                  ),
                                  const SizedBox(height: 12),
                                  Row(
                                    children: [
                                      const Icon(Icons.location_on, size: 14, color: Color(0xFF6366F1)),
                                      const SizedBox(width: 4),
                                      Text(
                                        issue.locationName,
                                        style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 12),
                                      ),
                                      if (issue.equipmentName != null) ...[
                                        const SizedBox(width: 12),
                                        const Icon(Icons.fitness_center, size: 14, color: Color(0xFF10B981)),
                                        const SizedBox(width: 4),
                                        Expanded(
                                          child: Text(
                                            issue.equipmentName!,
                                            maxLines: 1,
                                            overflow: TextOverflow.ellipsis,
                                            style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 12),
                                          ),
                                        ),
                                      ],
                                      if (issue.images.isNotEmpty) ...[
                                        const SizedBox(width: 8),
                                        const Icon(Icons.photo, size: 14, color: Color(0xFF38BDF8)),
                                        const SizedBox(width: 2),
                                        Text(
                                          '${issue.images.length}',
                                          style: const TextStyle(color: Color(0xFF38BDF8), fontSize: 12, fontWeight: FontWeight.bold),
                                        ),
                                      ],
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ),
                        );
                      },
                    ),
    );
  }
}
