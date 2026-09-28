import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import InventoryPage from '../InventoryPage';
import inventoryApi from '../../services/inventoryApi';

vi.mock('../../services/inventoryApi', () => {
  return {
    default: {
      getInventory: vi.fn(),
      getProducts: vi.fn(),
      getSuppliers: vi.fn(),
      getCategories: vi.fn(),
      adjustStock: vi.fn(),
      reorderStock: vi.fn(),
      getStockHistory: vi.fn(),
      exportInventoryCsv: vi.fn(),
      importInventoryCsv: vi.fn(),
      deleteProduct: vi.fn(),
      deleteSupplier: vi.fn(),
    },
  };
});

describe('InventoryPage Component', () => {
  const mockInventoryItems = [
    {
      id: 'inv-1',
      productId: 'prod-1',
      productSKU: 'WHEY-100',
      productName: 'Gold Whey Protein 1kg',
      categoryName: 'Protein',
      supplierName: 'Optimum Nutrition',
      unitPrice: 59.99,
      costPrice: 30.00,
      quantityInStock: 5,
      reorderThreshold: 10,
      maxStockLevel: 100,
      locationBin: 'Bin-A1',
      isLowStock: true,
      updatedAt: '2026-09-28T00:00:00Z',
    },
    {
      id: 'inv-2',
      productId: 'prod-2',
      productSKU: 'CREAT-300',
      productName: 'Creatine Monohydrate 300g',
      categoryName: 'Performance',
      supplierName: 'Muscletech',
      unitPrice: 29.99,
      costPrice: 15.00,
      quantityInStock: 45,
      reorderThreshold: 15,
      maxStockLevel: 150,
      locationBin: 'Bin-B2',
      isLowStock: false,
      updatedAt: '2026-09-28T00:00:00Z',
    },
  ];

  const mockCategories = [
    { id: 'cat-1', name: 'Protein' },
    { id: 'cat-2', name: 'Performance' },
  ];

  const mockSuppliers = [
    { id: 'sup-1', name: 'Optimum Nutrition', email: 'on@supplements.com', phone: '+123456789', productCount: 4, isActive: true },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    inventoryApi.getCategories.mockResolvedValue(mockCategories);
    inventoryApi.getSuppliers.mockResolvedValue({ items: mockSuppliers, totalCount: 1, totalPages: 1 });
    inventoryApi.getInventory.mockResolvedValue({ items: mockInventoryItems, totalCount: 2, totalPages: 1 });
    inventoryApi.getProducts.mockResolvedValue({ items: [], totalCount: 0, totalPages: 1 });
  });

  it('renders header, navigation tabs, and KPI cards with correct counts', async () => {
    render(<InventoryPage />);

    // Header
    expect(screen.getByText(/Supplier & Supplement Inventory/i)).toBeDefined();

    // Tabs
    expect(screen.getByText(/Inventory Stock/i)).toBeDefined();
    expect(screen.getByText(/Low Stock Alerts/i)).toBeDefined();
    expect(screen.getByText(/Products Catalog/i)).toBeDefined();
    expect(screen.getByText(/Suppliers Directory/i)).toBeDefined();

    // Wait for items to be displayed in table
    await waitFor(() => {
      expect(screen.getByText('WHEY-100')).toBeDefined();
      expect(screen.getByText('Gold Whey Protein 1kg')).toBeDefined();
      expect(screen.getByText('CREAT-300')).toBeDefined();
    });

    // Check low stock badge is rendered
    await waitFor(() => {
      const lowStockBadges = screen.getAllByText(/Low Stock/i);
      expect(lowStockBadges.length).toBeGreaterThan(0);
    });
  });

  it('switches to Suppliers Directory tab and displays supplier data', async () => {
    render(<InventoryPage />);

    // Click Suppliers Directory tab
    const suppliersTabBtn = screen.getByText(/Suppliers Directory/i);
    fireEvent.click(suppliersTabBtn);

    await waitFor(() => {
      expect(inventoryApi.getSuppliers).toHaveBeenCalled();
      const elements = screen.getAllByText('Optimum Nutrition');
      expect(elements.length).toBeGreaterThan(0);
      expect(screen.getByText('on@supplements.com')).toBeDefined();
      expect(screen.getByText('4 SKUs')).toBeDefined();
    });
  });

  it('opens Stock Adjustment modal and enforces non-negative stock validation', async () => {
    render(<InventoryPage />);

    await waitFor(() => {
      expect(screen.getByText('WHEY-100')).toBeDefined();
    });

    // Click "Adjust" button on the first item (current stock: 5)
    const adjustButtons = screen.getAllByTitle('Adjust Stock');
    fireEvent.click(adjustButtons[0]);

    // Modal should be open
    expect(screen.getByText(/Adjust Stock Level/i)).toBeDefined();

    // Input deduction that would result in negative stock (e.g. -10 when stock is 5)
    const qtyInput = screen.getByPlaceholderText(/e.g. 10 or -5/i);
    fireEvent.change(qtyInput, { target: { value: '-10' } });

    // Warning message should be visible
    expect(screen.getByText(/Stock cannot drop below zero/i)).toBeDefined();

    // Submit button should be disabled
    const applyButton = screen.getByText(/Apply Stock Adjustment/i);
    expect(applyButton.disabled).toBe(true);
  });

  it('opens Reorder modal and displays calculated total order cost', async () => {
    render(<InventoryPage />);

    await waitFor(() => {
      expect(screen.getByText('WHEY-100')).toBeDefined();
    });

    // Click "Reorder" button on first item (costPrice = $30.00)
    const reorderButtons = screen.getAllByTitle('Reorder');
    fireEvent.click(reorderButtons[0]);

    // Reorder modal should open
    expect(screen.getByText(/Supplier Reorder Request/i)).toBeDefined();
    const supMatches = screen.getAllByText(/Optimum Nutrition/i);
    expect(supMatches.length).toBeGreaterThan(0);

    // Default quantity is 25, 25 * $30 = $750.00
    expect(screen.getByText('$750.00')).toBeDefined();
  });
});
