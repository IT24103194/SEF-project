import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/facility_issue_model.dart';
import '../services/facility_api_service.dart';

class FacilityIssueDetailScreen extends ConsumerStatefulWidget {
  final String issueId;

  const FacilityIssueDetailScreen({super.key, required this.issueId});

  @override
  ConsumerState<FacilityIssueDetailScreen> createState() => _FacilityIssueDetailScreenState();
}

class _FacilityIssueDetailScreenState extends ConsumerState<FacilityIssueDetailScreen> {
  FacilityIssueModel? _issue;
  List<IssueHistoryModel> _history = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadDetails();
  }

  Future<void> _loadDetails() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    final api = ref.read(facilityApiServiceProvider);
    try {
      final issue = await api.fetchIssueDetails(widget.issueId);
      final history = await api.fetchIssueHistory(widget.issueId);
      if (mounted) {
        setState(() {
          _issue = issue;
          _history = history;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = 'Failed to load issue details.';
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
    return Scaffold(
      backgroundColor: const Color(0xFF0B0F19),
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827),
        elevation: 0,
        title: const Text('Issue Tracking Details'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadDetails,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: Color(0xFF6366F1)))
          : _error != null || _issue == null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(_error ?? 'Issue not found', style: const TextStyle(color: Colors.white70)),
                      const SizedBox(height: 12),
                      ElevatedButton(onPressed: _loadDetails, child: const Text('Retry')),
                    ],
                  ),
                )
              : SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // Status Banner
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: _getStatusColor(_issue!.statusName).withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: _getStatusColor(_issue!.statusName).withValues(alpha: 0.4)),
                        ),
                        child: Row(
                          children: [
                            Icon(Icons.track_changes, color: _getStatusColor(_issue!.statusName), size: 28),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    _issue!.statusName,
                                    style: TextStyle(
                                      color: _getStatusColor(_issue!.statusName),
                                      fontSize: 16,
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                  Text(
                                    'Reported on ${_issue!.reportedAt.day}/${_issue!.reportedAt.month}/${_issue!.reportedAt.year}',
                                    style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
                                  ),
                                ],
                              ),
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                              decoration: BoxDecoration(
                                color: const Color(0xFF1F2937),
                                borderRadius: BorderRadius.circular(6),
                              ),
                              child: Text(
                                '${_issue!.severityName} Severity',
                                style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 11, fontWeight: FontWeight.w600),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 20),

                      // Title & Description Card
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: const Color(0xFF111827),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: Colors.white.withValues(alpha: 0.05)),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              _issue!.title,
                              style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                const Icon(Icons.location_on, size: 14, color: Color(0xFF6366F1)),
                                const SizedBox(width: 4),
                                Text(
                                  '${_issue!.locationName} (${_issue!.locationFloor})',
                                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                                ),
                              ],
                            ),
                            if (_issue!.equipmentName != null) ...[
                              const SizedBox(height: 4),
                              Row(
                                children: [
                                  const Icon(Icons.fitness_center, size: 14, color: Color(0xFF10B981)),
                                  const SizedBox(width: 4),
                                  Text(
                                    _issue!.equipmentName!,
                                    style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                                  ),
                                ],
                              ),
                            ],
                            const Divider(color: Colors.white12, height: 24),
                            const Text(
                              'Problem Description',
                              style: TextStyle(color: Color(0xFF94A3B8), fontSize: 12, fontWeight: FontWeight.w600),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              _issue!.sanitizedDescription ?? _issue!.description,
                              style: const TextStyle(color: Colors.white, fontSize: 14, height: 1.4),
                            ),
                            if (_issue!.moderationStatus == 'Flagged') ...[
                              const SizedBox(height: 8),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                decoration: BoxDecoration(
                                  color: Colors.amber.withValues(alpha: 0.15),
                                  borderRadius: BorderRadius.circular(6),
                                ),
                                child: const Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Icon(Icons.shield, color: Colors.amber, size: 14),
                                    SizedBox(width: 6),
                                    Text('Content Moderated & Sanitized', style: TextStyle(color: Colors.amber, fontSize: 11)),
                                  ],
                                ),
                              ),
                            ],
                          ],
                        ),
                      ),
                      const SizedBox(height: 20),

                      // Resolution Data (if resolved)
                      if (_issue!.resolutionNotes != null && _issue!.resolutionNotes!.isNotEmpty) ...[
                        Container(
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: const Color(0xFF10B981).withValues(alpha: 0.12),
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: const Color(0xFF10B981).withValues(alpha: 0.3)),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Row(
                                children: [
                                  Icon(Icons.check_circle, color: Color(0xFF10B981), size: 20),
                                  SizedBox(width: 8),
                                  Text('Issue Resolved', style: TextStyle(color: Color(0xFF10B981), fontWeight: FontWeight.bold, fontSize: 15)),
                                ],
                              ),
                              const SizedBox(height: 8),
                              Text(
                                _issue!.resolutionNotes!,
                                style: const TextStyle(color: Colors.white, fontSize: 13, height: 1.4),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 20),
                      ],

                      // Attached Photos
                      if (_issue!.images.isNotEmpty) ...[
                        const Text(
                          'Attached Photos',
                          style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 12),
                        SizedBox(
                          height: 100,
                          child: ListView.builder(
                            scrollDirection: Axis.horizontal,
                            itemCount: _issue!.images.length,
                            itemBuilder: (context, index) {
                              final img = _issue!.images[index];
                              return Container(
                                width: 120,
                                margin: const EdgeInsets.only(right: 12),
                                decoration: BoxDecoration(
                                  borderRadius: BorderRadius.circular(10),
                                  color: const Color(0xFF111827),
                                  border: Border.all(color: Colors.white12),
                                ),
                                clipBehavior: Clip.antiAlias,
                                child: Image.network(
                                  img.imageUrl.startsWith('http')
                                      ? img.imageUrl
                                      : 'http://10.0.2.2:5000${img.imageUrl}',
                                  fit: BoxFit.cover,
                                  errorBuilder: (_, __, ___) => const Center(
                                    child: Icon(Icons.image, color: Colors.white38),
                                  ),
                                ),
                              );
                            },
                          ),
                        ),
                        const SizedBox(height: 20),
                      ],

                      // Status Tracking Timeline
                      const Text(
                        'Status History & Audit Trail',
                        style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 12),
                      if (_history.isEmpty)
                        const Text('No status transitions recorded.', style: TextStyle(color: Colors.white54, fontSize: 13))
                      else
                        ListView.builder(
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          itemCount: _history.length,
                          itemBuilder: (context, index) {
                            final h = _history[index];
                            return Container(
                              margin: const EdgeInsets.only(bottom: 10),
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: const Color(0xFF111827),
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(color: Colors.white.withValues(alpha: 0.05)),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: [
                                      Text(
                                        h.action,
                                        style: const TextStyle(color: Color(0xFF6366F1), fontWeight: FontWeight.bold, fontSize: 13),
                                      ),
                                      Text(
                                        '${h.timestamp.day}/${h.timestamp.month} ${h.timestamp.hour.toString().padLeft(2, '0')}:${h.timestamp.minute.toString().padLeft(2, '0')}',
                                        style: const TextStyle(color: Color(0xFF64748B), fontSize: 11),
                                      ),
                                    ],
                                  ),
                                  if (h.fromStatus != null && h.toStatus != null) ...[
                                    const SizedBox(height: 4),
                                    Text(
                                      '${h.fromStatus} → ${h.toStatus}',
                                      style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 12),
                                    ),
                                  ],
                                  if (h.notes != null) ...[
                                    const SizedBox(height: 4),
                                    Text(
                                      '"${h.notes}"',
                                      style: const TextStyle(color: Colors.white70, fontStyle: FontStyle.italic, fontSize: 12),
                                    ),
                                  ],
                                ],
                              ),
                            );
                          },
                        ),
                    ],
                  ),
                ),
    );
  }
}
