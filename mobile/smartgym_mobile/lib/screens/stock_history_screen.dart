import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/inventory_models.dart';
import '../services/inventory_api_service.dart';
import '../widgets/animated_gym_background.dart';

class StockHistoryScreen extends StatefulWidget {
  final InventoryItem item;
  final InventoryApiService? apiService;

  const StockHistoryScreen({
    super.key,
    required this.item,
    this.apiService,
  });

  @override
  State<StockHistoryScreen> createState() => _StockHistoryScreenState();
}

class _StockHistoryScreenState extends State<StockHistoryScreen> {
  late final InventoryApiService _api;
  late Future<List<StockMovement>> _historyFuture;

  @override
  void initState() {
    super.initState();
    _api = widget.apiService ?? InventoryApiService();
    _historyFuture = _api.fetchHistory(widget.item.id);
  }

  void _refresh() {
    setState(() {
      _historyFuture = _api.fetchHistory(widget.item.id);
    });
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedGymBackground(
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827).withValues(alpha: 0.85),
        elevation: 0,
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Stock Movement History',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
            ),
            Text(
              '${widget.item.productName} (${widget.item.productSKU})',
              style: const TextStyle(fontSize: 12, color: Color(0xFF94A3B8)),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _refresh,
          ),
        ],
      ),
      child: FutureBuilder<List<StockMovement>>(
        future: _historyFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24.0),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.error_outline, color: Color(0xFFF43F5E), size: 40),
                    const SizedBox(height: 12),
                    Text(
                      'Failed to load history: ${snapshot.error}',
                      textAlign: TextAlign.center,
                      style: const TextStyle(color: Colors.white70),
                    ),
                    const SizedBox(height: 16),
                    ElevatedButton(
                      onPressed: _refresh,
                      style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF6366F1)),
                      child: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            );
          }

          final movements = snapshot.data ?? [];
          if (movements.isEmpty) {
            return const Center(
              child: Text(
                'No recorded stock movements for this item.',
                style: TextStyle(color: Color(0xFF94A3B8)),
              ),
            );
          }

          return ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: movements.length,
            separatorBuilder: (context, index) => const SizedBox(height: 10),
            itemBuilder: (context, index) {
              final m = movements[index];
              final isPositive = m.quantityChange > 0;
              final qtyText = isPositive ? '+${m.quantityChange}' : '${m.quantityChange}';
              final qtyColor = isPositive ? const Color(0xFF10B981) : const Color(0xFFF43F5E);
              final dateStr = DateFormat('MMM d, yyyy · HH:mm').format(m.createdAt);

              return Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFF111827),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFF1E293B)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                          decoration: BoxDecoration(
                            color: qtyColor.withValues(alpha: 0.15),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            m.movementType.toUpperCase(),
                            style: TextStyle(
                              color: qtyColor,
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                        Text(
                          qtyText,
                          style: TextStyle(
                            color: qtyColor,
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Text(
                      m.reason,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 13,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          'By: ${m.performedByUserName ?? 'System / Batch'}',
                          style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 11),
                        ),
                        Text(
                          dateStr,
                          style: const TextStyle(color: Color(0xFF64748B), fontSize: 11),
                        ),
                      ],
                    ),
                  ],
                ),
              );
            },
          );
        },
      ),
    );
  }
}
