import 'package:flutter/material.dart';
import '../models/inventory_models.dart';
import '../services/inventory_api_service.dart';
import 'stock_history_screen.dart';
import '../widgets/animated_gym_background.dart';

class InventoryScreen extends StatefulWidget {
  final InventoryApiService? apiService;

  const InventoryScreen({super.key, this.apiService});

  @override
  State<InventoryScreen> createState() => _InventoryScreenState();
}

class _InventoryScreenState extends State<InventoryScreen> {
  late final InventoryApiService _api;
  final TextEditingController _searchController = TextEditingController();

  List<InventoryItem> _items = [];
  bool _loading = false;
  String? _errorMessage;
  bool _isLowStockOnly = false;

  @override
  void initState() {
    super.initState();
    _api = widget.apiService ?? InventoryApiService();
    _loadInventory();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadInventory() async {
    setState(() {
      _loading = true;
      _errorMessage = null;
    });

    try {
      final items = await _api.fetchInventory(
        isLowStock: _isLowStockOnly ? true : null,
        searchTerm: _searchController.text.trim().isNotEmpty ? _searchController.text.trim() : null,
      );
      setState(() {
        _items = items;
        _loading = false;
      });
    } catch (e) {
      setState(() {
        _errorMessage = e.toString().replaceAll('Exception: ', '');
        _loading = false;
      });
    }
  }

  void _showAdjustStockSheet(InventoryItem item) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: const Color(0xFF111827),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => _StockAdjustSheet(
        item: item,
        onAdjust: (qtyChange, type, reason) async {
          Navigator.pop(ctx);
          try {
            await _api.adjustStock(
              inventoryItemId: item.id,
              quantityChange: qtyChange,
              movementType: type,
              reason: reason,
            );
            if (mounted) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  backgroundColor: const Color(0xFF10B981),
                  content: Text('Stock for ${item.productName} adjusted successfully.'),
                ),
              );
              _loadInventory();
            }
          } catch (e) {
            if (mounted) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  backgroundColor: const Color(0xFFF43F5E),
                  content: Text(e.toString().replaceAll('Exception: ', '')),
                ),
              );
            }
          }
        },
      ),
    );
  }

  void _showReorderSheet(InventoryItem item) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: const Color(0xFF111827),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => _ReorderSheet(
        item: item,
        onReorder: (qty, notes) async {
          Navigator.pop(ctx);
          try {
            final res = await _api.reorderProduct(
              inventoryItemId: item.id,
              quantity: qty,
              notes: notes,
            );
            if (mounted) {
              final poNumber = res['orderNumber'] ?? 'PO';
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  backgroundColor: const Color(0xFF10B981),
                  content: Text('Purchase Order #$poNumber placed with ${item.supplierName}!'),
                ),
              );
              _loadInventory();
            }
          } catch (e) {
            if (mounted) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  backgroundColor: const Color(0xFFF43F5E),
                  content: Text(e.toString().replaceAll('Exception: ', '')),
                ),
              );
            }
          }
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final lowStockCount = _items.where((i) => i.isLowStock).length;
    final totalUnits = _items.fold<int>(0, (sum, i) => sum + i.quantityInStock);

    return AnimatedGymBackground(
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827).withValues(alpha: 0.85),
        elevation: 0,
        title: const Text(
          'Supplement Inventory',
          style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadInventory,
          ),
        ],
      ),
      child: Column(
        children: [
          // KPI Metric Header
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            color: const Color(0xFF111827),
            child: Row(
              children: [
                Expanded(
                  child: _KpiChip(
                    title: 'TOTAL ITEMS',
                    value: _items.length.toString(),
                    color: const Color(0xFF6366F1),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _KpiChip(
                    title: 'LOW STOCK',
                    value: lowStockCount.toString(),
                    color: lowStockCount > 0 ? const Color(0xFFF43F5E) : const Color(0xFF10B981),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _KpiChip(
                    title: 'TOTAL UNITS',
                    value: totalUnits.toString(),
                    color: const Color(0xFF06B6D4),
                  ),
                ),
              ],
            ),
          ),

          // Search & Filter Bar
          Padding(
            padding: const EdgeInsets.all(12.0),
            child: Column(
              children: [
                TextField(
                  controller: _searchController,
                  style: const TextStyle(color: Colors.white, fontSize: 14),
                  decoration: InputDecoration(
                    hintText: 'Search SKU, Product, or Category...',
                    hintStyle: const TextStyle(color: Color(0xFF64748B), fontSize: 14),
                    prefixIcon: const Icon(Icons.search, color: Color(0xFF64748B), size: 20),
                    suffixIcon: _searchController.text.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear, size: 18, color: Color(0xFF64748B)),
                            onPressed: () {
                              _searchController.clear();
                              _loadInventory();
                            },
                          )
                        : null,
                    filled: true,
                    fillColor: const Color(0xFF111827),
                    contentPadding: const EdgeInsets.symmetric(vertical: 0, horizontal: 16),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                      borderSide: const BorderSide(color: Color(0xFF1E293B)),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                      borderSide: const BorderSide(color: Color(0xFF1E293B)),
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                      borderSide: const BorderSide(color: Color(0xFF6366F1)),
                    ),
                  ),
                  onSubmitted: (_) => _loadInventory(),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    FilterChip(
                      label: Text(
                        'Low Stock Alarms ($lowStockCount)',
                        style: TextStyle(
                          fontSize: 12,
                          color: _isLowStockOnly ? Colors.white : const Color(0xFF94A3B8),
                          fontWeight: _isLowStockOnly ? FontWeight.w700 : FontWeight.w500,
                        ),
                      ),
                      selected: _isLowStockOnly,
                      selectedColor: const Color(0xFFF43F5E).withValues(alpha: 0.25),
                      backgroundColor: const Color(0xFF111827),
                      checkmarkColor: const Color(0xFFF43F5E),
                      side: BorderSide(
                        color: _isLowStockOnly ? const Color(0xFFF43F5E) : const Color(0xFF1E293B),
                      ),
                      onSelected: (val) {
                        setState(() {
                          _isLowStockOnly = val;
                        });
                        _loadInventory();
                      },
                    ),
                  ],
                ),
              ],
            ),
          ),

          // Main Inventory List
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _errorMessage != null
                    ? Center(
                        child: Padding(
                          padding: const EdgeInsets.all(24.0),
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              const Icon(Icons.error_outline, color: Color(0xFFF43F5E), size: 40),
                              const SizedBox(height: 12),
                              Text(
                                _errorMessage!,
                                textAlign: TextAlign.center,
                                style: const TextStyle(color: Colors.white70),
                              ),
                              const SizedBox(height: 16),
                              ElevatedButton(
                                onPressed: _loadInventory,
                                style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF6366F1)),
                                child: const Text('Try Again'),
                              ),
                            ],
                          ),
                        ),
                      )
                    : _items.isEmpty
                        ? const Center(
                            child: Text(
                              'No inventory items found.',
                              style: TextStyle(color: Color(0xFF94A3B8)),
                            ),
                          )
                        : RefreshIndicator(
                            onRefresh: _loadInventory,
                            child: ListView.separated(
                              padding: const EdgeInsets.all(12),
                              itemCount: _items.length,
                              separatorBuilder: (context, index) => const SizedBox(height: 10),
                              itemBuilder: (context, index) {
                                final item = _items[index];
                                return _InventoryCard(
                                  item: item,
                                  onAdjust: () => _showAdjustStockSheet(item),
                                  onReorder: () => _showReorderSheet(item),
                                  onHistory: () {
                                    Navigator.of(context).push(
                                      MaterialPageRoute(
                                        builder: (_) => StockHistoryScreen(item: item, apiService: _api),
                                      ),
                                    );
                                  },
                                );
                              },
                            ),
                          ),
          ),
        ],
      ),
    );
  }
}

class _KpiChip extends StatelessWidget {
  final String title;
  final String value;
  final Color color;

  const _KpiChip({
    required this.title,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 10),
      decoration: BoxDecoration(
        color: const Color(0xFF0B0F19),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: const Color(0xFF1E293B)),
      ),
      child: Column(
        children: [
          Text(
            title,
            style: const TextStyle(fontSize: 10, color: Color(0xFF64748B), fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 2),
          Text(
            value,
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: color),
          ),
        ],
      ),
    );
  }
}

class _InventoryCard extends StatelessWidget {
  final InventoryItem item;
  final VoidCallback onAdjust;
  final VoidCallback onReorder;
  final VoidCallback onHistory;

  const _InventoryCard({
    required this.item,
    required this.onAdjust,
    required this.onReorder,
    required this.onHistory,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFF111827),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: item.isLowStock ? const Color(0xFFF43F5E).withValues(alpha: 0.4) : const Color(0xFF1E293B),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header: SKU and Stock Count
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.productSKU,
                      style: const TextStyle(
                        color: Color(0xFF06B6D4),
                        fontSize: 12,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      item.productName,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 15,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    '${item.quantityInStock} IN STOCK',
                    style: TextStyle(
                      color: item.isLowStock ? const Color(0xFFF43F5E) : const Color(0xFF10B981),
                      fontSize: 14,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  if (item.isLowStock)
                    Container(
                      margin: const EdgeInsets.only(top: 4),
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: const Color(0xFFF43F5E).withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: const Text(
                        'LOW STOCK',
                        style: TextStyle(
                          color: Color(0xFFFB7185),
                          fontSize: 9,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                ],
              ),
            ],
          ),
          const SizedBox(height: 8),

          // Metadata row
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: const Color(0xFF6366F1).withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  item.categoryName,
                  style: const TextStyle(color: Color(0xFF818CF8), fontSize: 11, fontWeight: FontWeight.w600),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Supplier: ${item.supplierName}',
                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 11),
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              Text(
                'Bin: ${item.locationBin ?? "—"}',
                style: const TextStyle(color: Color(0xFF64748B), fontSize: 11),
              ),
            ],
          ),
          const Divider(color: Color(0xFF1E293B), height: 20),

          // Actions
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  icon: const Icon(Icons.sync_alt, size: 14),
                  label: const Text('Adjust', style: TextStyle(fontSize: 12)),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: Colors.white,
                    side: const BorderSide(color: Color(0xFF334155)),
                    padding: const EdgeInsets.symmetric(vertical: 8),
                  ),
                  onPressed: onAdjust,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: ElevatedButton.icon(
                  icon: const Icon(Icons.shopping_cart_outlined, size: 14),
                  label: const Text('Reorder', style: TextStyle(fontSize: 12)),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF6366F1),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 8),
                  ),
                  onPressed: onReorder,
                ),
              ),
              const SizedBox(width: 8),
              IconButton(
                icon: const Icon(Icons.history, color: Color(0xFF94A3B8), size: 20),
                tooltip: 'Movement History',
                onPressed: onHistory,
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _StockAdjustSheet extends StatefulWidget {
  final InventoryItem item;
  final void Function(int qtyChange, String movementType, String reason) onAdjust;

  const _StockAdjustSheet({required this.item, required this.onAdjust});

  @override
  State<_StockAdjustSheet> createState() => _StockAdjustSheetState();
}

class _StockAdjustSheetState extends State<_StockAdjustSheet> {
  final _qtyController = TextEditingController();
  final _reasonController = TextEditingController();
  String _movementType = 'Adjustment';
  String? _error;

  @override
  void dispose() {
    _qtyController.dispose();
    _reasonController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final curStock = widget.item.quantityInStock;
    final change = int.tryParse(_qtyController.text) ?? 0;
    final projected = curStock + change;
    final isNegative = projected < 0;

    return Padding(
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.of(context).viewInsets.bottom + 20,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Adjust Stock: ${widget.item.productName}',
            style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 4),
          Text(
            'Current balance: $curStock units',
            style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
          ),
          const SizedBox(height: 16),

          // Live stock calculator
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: const Color(0xFF0B0F19),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(
                color: isNegative ? const Color(0xFFF43F5E) : const Color(0xFF1E293B),
              ),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                Text('Current: $curStock', style: const TextStyle(color: Color(0xFF94A3B8))),
                const Icon(Icons.arrow_forward, size: 16, color: Color(0xFF64748B)),
                Text(
                  'Change: ${change >= 0 ? "+$change" : change}',
                  style: TextStyle(
                    color: change >= 0 ? const Color(0xFF10B981) : const Color(0xFFF43F5E),
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const Icon(Icons.arrow_forward, size: 16, color: Color(0xFF64748B)),
                Text(
                  'New: $projected',
                  style: TextStyle(
                    color: isNegative ? const Color(0xFFF43F5E) : Colors.white,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
          ),
          if (isNegative)
            const Padding(
              padding: EdgeInsets.only(top: 6),
              child: Text(
                'Business rule violation: Stock cannot become negative.',
                style: TextStyle(color: Color(0xFFF43F5E), fontSize: 11),
              ),
            ),
          const SizedBox(height: 12),

          // Movement type dropdown
          DropdownButtonFormField<String>(
            initialValue: _movementType,
            dropdownColor: const Color(0xFF111827),
            style: const TextStyle(color: Colors.white, fontSize: 13),
            decoration: const InputDecoration(
              labelText: 'Movement Type',
              labelStyle: TextStyle(color: Color(0xFF94A3B8)),
              enabledBorder: UnderlineInputBorder(borderSide: BorderSide(color: Color(0xFF334155))),
            ),
            items: const [
              DropdownMenuItem(value: 'Adjustment', child: Text('Cycle Count Adjustment')),
              DropdownMenuItem(value: 'Restock', child: Text('Restock (+) [Delivery]')),
              DropdownMenuItem(value: 'Sale', child: Text('Point of Sale / Front Desk (-)')),
              DropdownMenuItem(value: 'Waste', child: Text('Damaged / Expired (-) [Waste]')),
              DropdownMenuItem(value: 'Return', child: Text('Supplier Return (-)')),
            ],
            onChanged: (val) {
              if (val != null) setState(() => _movementType = val);
            },
          ),
          const SizedBox(height: 12),

          // Quantity change
          TextField(
            controller: _qtyController,
            keyboardType: const TextInputType.numberWithOptions(signed: true),
            style: const TextStyle(color: Colors.white),
            decoration: const InputDecoration(
              labelText: 'Quantity Change (+ for add, - for deduct)',
              labelStyle: TextStyle(color: Color(0xFF94A3B8)),
              hintText: 'e.g. 10 or -5',
              hintStyle: TextStyle(color: Color(0xFF64748B)),
              enabledBorder: UnderlineInputBorder(borderSide: BorderSide(color: Color(0xFF334155))),
            ),
            onChanged: (_) => setState(() {}),
          ),
          const SizedBox(height: 12),

          // Reason field
          TextField(
            controller: _reasonController,
            style: const TextStyle(color: Colors.white),
            decoration: const InputDecoration(
              labelText: 'Audit Reason (Mandatory)',
              labelStyle: TextStyle(color: Color(0xFF94A3B8)),
              hintText: 'e.g. Regular monthly recount / Damaged box',
              hintStyle: TextStyle(color: Color(0xFF64748B)),
              enabledBorder: UnderlineInputBorder(borderSide: BorderSide(color: Color(0xFF334155))),
            ),
            onChanged: (_) => setState(() {}),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.only(top: 8),
              child: Text(_error!, style: const TextStyle(color: Color(0xFFF43F5E), fontSize: 12)),
            ),
          const SizedBox(height: 20),

          // Submit button
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF6366F1),
                padding: const EdgeInsets.symmetric(vertical: 12),
              ),
              onPressed: isNegative || change == 0 || _reasonController.text.trim().isEmpty
                  ? null
                  : () {
                      widget.onAdjust(change, _movementType, _reasonController.text.trim());
                    },
              child: const Text('Apply Stock Adjustment'),
            ),
          ),
        ],
      ),
    );
  }
}

class _ReorderSheet extends StatefulWidget {
  final InventoryItem item;
  final void Function(int qty, String? notes) onReorder;

  const _ReorderSheet({required this.item, required this.onReorder});

  @override
  State<_ReorderSheet> createState() => _ReorderSheetState();
}

class _ReorderSheetState extends State<_ReorderSheet> {
  final _qtyController = TextEditingController(text: '20');
  final _notesController = TextEditingController();

  @override
  void dispose() {
    _qtyController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final qty = int.tryParse(_qtyController.text) ?? 0;
    final totalCost = (qty * widget.item.costPrice).toStringAsFixed(2);

    return Padding(
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.of(context).viewInsets.bottom + 20,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Reorder: ${widget.item.productName}',
            style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 4),
          Text(
            'Supplier: ${widget.item.supplierName} · Cost/Unit: \$${widget.item.costPrice.toStringAsFixed(2)}',
            style: const TextStyle(color: Color(0xFF06B6D4), fontSize: 12),
          ),
          const SizedBox(height: 16),

          TextField(
            controller: _qtyController,
            keyboardType: TextInputType.number,
            style: const TextStyle(color: Colors.white),
            decoration: const InputDecoration(
              labelText: 'Reorder Quantity',
              labelStyle: TextStyle(color: Color(0xFF94A3B8)),
              enabledBorder: UnderlineInputBorder(borderSide: BorderSide(color: Color(0xFF334155))),
            ),
            onChanged: (_) => setState(() {}),
          ),
          const SizedBox(height: 12),

          TextField(
            controller: _notesController,
            style: const TextStyle(color: Colors.white),
            decoration: const InputDecoration(
              labelText: 'PO Notes (Optional)',
              labelStyle: TextStyle(color: Color(0xFF94A3B8)),
              hintText: 'e.g. Urgent restocking',
              hintStyle: TextStyle(color: Color(0xFF64748B)),
              enabledBorder: UnderlineInputBorder(borderSide: BorderSide(color: Color(0xFF334155))),
            ),
          ),
          const SizedBox(height: 16),

          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text('Estimated Order Total:', style: TextStyle(color: Color(0xFF94A3B8))),
              Text(
                '\$$totalCost',
                style: const TextStyle(
                  color: Color(0xFF10B981),
                  fontSize: 18,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),

          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              icon: const Icon(Icons.shopping_cart),
              label: const Text('Place Purchase Order'),
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF6366F1),
                padding: const EdgeInsets.symmetric(vertical: 12),
              ),
              onPressed: qty <= 0
                  ? null
                  : () {
                      widget.onReorder(qty, _notesController.text.trim().isNotEmpty ? _notesController.text.trim() : null);
                    },
            ),
          ),
        ],
      ),
    );
  }
}
