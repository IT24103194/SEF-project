using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(s => s.Name);

        builder.Property(s => s.ContactPerson)
            .HasMaxLength(100);

        builder.Property(s => s.Email)
            .HasMaxLength(256);

        builder.Property(s => s.Phone)
            .HasMaxLength(50);

        builder.Property(s => s.Address)
            .HasMaxLength(300);

        builder.HasIndex(s => s.IsActive);

        builder.HasMany(s => s.Products)
            .WithOne(p => p.Supplier)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.PurchaseOrders)
            .WithOne(po => po.Supplier)
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.SKU)
            .IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.IsActive);

        builder.Property(p => p.UnitPrice)
            .HasPrecision(10, 2);

        builder.Property(p => p.CostPrice)
            .HasPrecision(10, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_products_unit_price", "\"UnitPrice\" >= 0");
            t.HasCheckConstraint("CK_products_cost_price", "\"CostPrice\" >= 0");
        });

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.InventoryItem)
            .WithOne(i => i.Product)
            .HasForeignKey<InventoryItem>(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");
        builder.HasKey(i => i.Id);

        builder.HasIndex(i => i.ProductId)
            .IsUnique();

        builder.Property(i => i.LocationBin)
            .HasMaxLength(100);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_inventory_items_quantity", "\"QuantityInStock\" >= 0");
            t.HasCheckConstraint("CK_inventory_items_reorder_threshold", "\"ReorderThreshold\" >= 0");
            t.HasCheckConstraint("CK_inventory_items_max_stock", "\"MaxStockLevel\" >= \"ReorderThreshold\"");
        });

        builder.HasOne(i => i.Product)
            .WithOne(p => p.InventoryItem)
            .HasForeignKey<InventoryItem>(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.StockMovements)
            .WithOne(sm => sm.InventoryItem)
            .HasForeignKey(sm => sm.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.Reason)
            .HasMaxLength(250);

        builder.HasIndex(sm => new { sm.InventoryItemId, sm.CreatedAt });
        builder.HasIndex(sm => sm.MovementType);

        builder.HasOne(sm => sm.InventoryItem)
            .WithMany(i => i.StockMovements)
            .HasForeignKey(sm => sm.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");
        builder.HasKey(po => po.Id);

        builder.Property(po => po.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(po => po.OrderNumber)
            .IsUnique();

        builder.Property(po => po.TotalAmount)
            .HasPrecision(12, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_purchase_orders_total_amount", "\"TotalAmount\" >= 0");
        });

        builder.HasIndex(po => po.Status);
        builder.HasIndex(po => po.OrderedAt);

        builder.HasOne(po => po.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(po => po.Items)
            .WithOne(poi => poi.PurchaseOrder)
            .HasForeignKey(poi => poi.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("purchase_order_items");
        builder.HasKey(poi => poi.Id);

        builder.Property(poi => poi.UnitCost)
            .HasPrecision(10, 2);

        builder.Property(poi => poi.TotalCost)
            .HasPrecision(12, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_purchase_order_items_quantity", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_purchase_order_items_unit_cost", "\"UnitCost\" >= 0");
            t.HasCheckConstraint("CK_purchase_order_items_total_cost", "\"TotalCost\" >= 0");
        });

        builder.HasOne(poi => poi.PurchaseOrder)
            .WithMany(po => po.Items)
            .HasForeignKey(poi => poi.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(poi => poi.Product)
            .WithMany(p => p.PurchaseOrderItems)
            .HasForeignKey(poi => poi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
