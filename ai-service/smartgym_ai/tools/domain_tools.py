from typing import Optional, Any
from uuid import UUID
from pydantic import BaseModel, Field
import psycopg2

from smartgym_ai.tools.base_tool import ToolBase
from smartgym_ai.configuration.settings import settings
from smartgym_ai.services.tool_persistence import tool_persistence_service
from smartgym_ai.services.resilience import TimeoutHandler, RetryManager
from smartgym_ai.services.logging import ai_logger, get_correlation_id


# --- Tool 1: getEquipmentDetails ---

class GetEquipmentDetailsInput(BaseModel):
    equipment_id: Optional[str] = Field(default=None, description="Unique equipment UUID")
    serial_number: Optional[str] = Field(default=None, description="Equipment serial number")
    name: Optional[str] = Field(default=None, description="Equipment name or model string")


class GetEquipmentDetailsTool(ToolBase):
    """
    Retrieves authoritative equipment specifications, operational status,
    location, and warranty data from the SmartGym database.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "getEquipmentDetails"

    @property
    def description(self) -> str:
        return "Retrieves equipment specifications, location, warranty, and operational status from the facility database."

    @property
    def args_schema(self):
        return GetEquipmentDetailsInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(
        self,
        equipment_id: Optional[str] = None,
        serial_number: Optional[str] = None,
        name: Optional[str] = None
    ) -> dict[str, Any]:
        if not equipment_id and not serial_number and not name:
            raise ValueError("At least one search parameter (equipment_id, serial_number, or name) must be provided.")

        if not self.db_url:
            return {"found": False, "error": "Database connection string is not configured."}

        query = """
        SELECT e."Id", e."Name", e."Model", e."Manufacturer", e."SerialNumber",
               e."Status", e."LocationId", l."Name" AS "LocationName",
               e."LastServicedDate", e."WarrantyExpiryDate", e."PurchaseDate"
        FROM "equipment" e
        LEFT JOIN "locations" l ON e."LocationId" = l."Id"
        WHERE (%s::uuid IS NOT NULL AND e."Id" = %s::uuid)
           OR (%s IS NOT NULL AND e."SerialNumber" ILIKE %s)
           OR (%s IS NOT NULL AND e."Name" ILIKE %s)
        LIMIT 1
        """

        clean_eq_id = None
        if equipment_id:
            try:
                clean_eq_id = str(UUID(str(equipment_id).strip()))
            except ValueError:
                pass

        sn = serial_number.strip() if serial_number else None
        eq_name = f"%{name.strip()}%" if name else None

        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(query, (clean_eq_id, clean_eq_id, sn, sn, eq_name, eq_name))
                row = cur.fetchone()
                if not row:
                    return {
                        "found": False,
                        "error": f"Equipment '{equipment_id or serial_number or name}' not found in facility database."
                    }

                return {
                    "found": True,
                    "equipment_id": str(row[0]),
                    "name": row[1],
                    "model": row[2],
                    "manufacturer": row[3],
                    "serial_number": row[4],
                    "status": row[5],
                    "location_id": str(row[6]) if row[6] else None,
                    "location_name": row[7] or "Unassigned Location",
                    "last_serviced_date": row[8].isoformat() if row[8] else None,
                    "warranty_expiry_date": row[9].isoformat() if row[9] else None,
                    "purchase_date": row[10].isoformat() if row[10] else None
                }


# --- Tool 2: getMaintenanceHistory ---

class GetMaintenanceHistoryInput(BaseModel):
    equipment_id: str = Field(description="Equipment UUID to retrieve repair history for")
    limit: int = Field(default=5, ge=1, le=50, description="Maximum number of historical records to return")


class GetMaintenanceHistoryTool(ToolBase):
    """
    Retrieves previous repair orders, technician service notes,
    and historical repair costs for a specific equipment item.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "getMaintenanceHistory"

    @property
    def description(self) -> str:
        return "Retrieves historical repair orders, technician notes, parts used, and costs for equipment."

    @property
    def args_schema(self):
        return GetMaintenanceHistoryInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(self, equipment_id: str, limit: int = 5) -> list[dict[str, Any]]:
        clean_id = str(UUID(str(equipment_id).strip()))

        if not self.db_url:
            return []

        query = """
        SELECT ro."Id", ro."OrderNumber", ro."TechnicianName", ro."Status",
               ro."EstimatedCost", ro."ActualCost", s."Name" AS "SupplierName",
               ro."CreatedAt"
        FROM "repair_orders" ro
        LEFT JOIN "suppliers" s ON ro."SupplierId" = s."Id"
        WHERE ro."EquipmentId" = %s::uuid
        ORDER BY ro."CreatedAt" DESC
        LIMIT %s
        """

        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(query, (clean_id, limit))
                rows = cur.fetchall()
                records = []
                for r in rows:
                    records.append({
                        "repair_order_id": str(r[0]),
                        "order_number": r[1],
                        "technician_name": r[2] or "In-house Technician",
                        "status": r[3],
                        "estimated_cost": float(r[4]) if r[4] is not None else 0.0,
                        "actual_cost": float(r[5]) if r[5] is not None else None,
                        "supplier_name": r[6] or "Certified Equipment Supplier",
                        "created_at": r[7].isoformat() if r[7] else None
                    })
                return records


# --- Tool 3: getSimilarFacilityIssues ---

class GetSimilarFacilityIssuesInput(BaseModel):
    equipment_id: Optional[str] = Field(default=None, description="Equipment UUID")
    equipment_name: Optional[str] = Field(default=None, description="Equipment name or category")
    query_text: Optional[str] = Field(default=None, description="Symptom or issue keyword (e.g., 'belt', 'noise')")
    limit: int = Field(default=5, ge=1, le=50)


class GetSimilarFacilityIssuesTool(ToolBase):
    """
    Searches historical facility issues to find recurring patterns,
    past resolutions, and known failure modes across identical equipment.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "getSimilarFacilityIssues"

    @property
    def description(self) -> str:
        return "Finds similar past facility issues, symptoms, and previous resolution notes for gym equipment."

    @property
    def args_schema(self):
        return GetSimilarFacilityIssuesInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(
        self,
        equipment_id: Optional[str] = None,
        equipment_name: Optional[str] = None,
        query_text: Optional[str] = None,
        limit: int = 5
    ) -> list[dict[str, Any]]:
        if not self.db_url:
            return []

        clean_eq_id = None
        if equipment_id:
            try:
                clean_eq_id = str(UUID(str(equipment_id).strip()))
            except ValueError:
                pass

        kw = f"%{query_text.strip()}%" if query_text else None
        eq_kw = f"%{equipment_name.strip()}%" if equipment_name else None

        query = """
        SELECT fi."Id", fi."Title", fi."Description", fi."SanitizedDescription",
               fi."Severity", fi."Status", fi."ResolutionNotes", fi."CreatedAt"
        FROM "facility_issues" fi
        WHERE (%s::uuid IS NOT NULL AND fi."EquipmentId" = %s::uuid)
           OR (%s IS NOT NULL AND fi."Title" ILIKE %s)
           OR (%s IS NOT NULL AND fi."Description" ILIKE %s)
        ORDER BY fi."CreatedAt" DESC
        LIMIT %s
        """

        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(query, (clean_eq_id, clean_eq_id, kw or eq_kw, kw or eq_kw, kw, kw, limit))
                rows = cur.fetchall()
                results = []
                for r in rows:
                    results.append({
                        "issue_id": str(r[0]),
                        "title": r[1],
                        "description": r[3] or r[2],  # Use sanitized description when available
                        "severity": r[4],
                        "status": r[5],
                        "resolution_notes": r[6] or "Resolved by maintenance team",
                        "created_at": r[7].isoformat() if r[7] else None
                    })
                return results


# --- Tool 4: checkInventory ---

class CheckInventoryInput(BaseModel):
    part_number: Optional[str] = Field(default=None, description="Part number or SKU")
    sku: Optional[str] = Field(default=None, description="Product SKU")
    keyword: Optional[str] = Field(default=None, description="Component keyword (e.g. 'belt', 'cable', 'bearing')")
    product_id: Optional[str] = Field(default=None, description="Product UUID")


class CheckInventoryTool(ToolBase):
    """
    Queries gym inventory items and current stock levels for required spare parts.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "checkInventory"

    @property
    def description(self) -> str:
        return "Checks current warehouse/facility stock availability and bin location for spare parts or products."

    @property
    def args_schema(self):
        return CheckInventoryInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(
        self,
        part_number: Optional[str] = None,
        sku: Optional[str] = None,
        keyword: Optional[str] = None,
        product_id: Optional[str] = None
    ) -> dict[str, Any]:
        term = sku or part_number or keyword
        if not term and not product_id:
            raise ValueError("Must provide SKU, part number, keyword, or product ID.")

        if not self.db_url:
            return {"found": False, "is_available": False, "quantity_in_stock": 0}

        clean_pid = None
        if product_id:
            try:
                clean_pid = str(UUID(str(product_id).strip()))
            except ValueError:
                pass

        search_pattern = f"%{term.strip()}%" if term else None

        query = """
        SELECT p."Id", p."SKU", p."Name", p."UnitPrice", p."CostPrice",
               i."QuantityInStock", i."ReorderThreshold", i."LocationBin", i."LastRestockedAt"
        FROM "products" p
        JOIN "inventory_items" i ON p."Id" = i."ProductId"
        WHERE (%s::uuid IS NOT NULL AND p."Id" = %s::uuid)
           OR (%s IS NOT NULL AND p."SKU" ILIKE %s)
           OR (%s IS NOT NULL AND p."Name" ILIKE %s)
        LIMIT 1
        """

        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(query, (clean_pid, clean_pid, search_pattern, search_pattern, search_pattern, search_pattern))
                row = cur.fetchone()
                if not row:
                    return {
                        "found": False,
                        "is_available": False,
                        "quantity_in_stock": 0,
                        "error": f"Part / inventory item '{term or product_id}' not found in stock."
                    }

                qty = row[5] or 0
                return {
                    "found": True,
                    "product_id": str(row[0]),
                    "sku": row[1],
                    "name": row[2],
                    "unit_price": float(row[3]) if row[3] is not None else 0.0,
                    "cost_price": float(row[4]) if row[4] is not None else 0.0,
                    "quantity_in_stock": qty,
                    "reorder_threshold": row[6] or 0,
                    "is_available": qty > 0,
                    "location_bin": row[7] or "General Bin",
                    "last_restocked_at": row[8].isoformat() if row[8] else None
                }


# --- Tool 5: getSupplierDetails ---

class GetSupplierDetailsInput(BaseModel):
    supplier_id: Optional[str] = Field(default=None, description="Supplier UUID")
    name: Optional[str] = Field(default=None, description="Supplier name or brand (e.g., 'LifeFitness', 'Matrix')")
    category_or_keyword: Optional[str] = Field(default=None, description="Product category or specialty keyword")


class GetSupplierDetailsTool(ToolBase):
    """
    Retrieves authorized equipment manufacturer, distributor, or service vendor contact details.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "getSupplierDetails"

    @property
    def description(self) -> str:
        return "Retrieves verified supplier, vendor, contact person, and dispatch details for equipment parts."

    @property
    def args_schema(self):
        return GetSupplierDetailsInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(
        self,
        supplier_id: Optional[str] = None,
        name: Optional[str] = None,
        category_or_keyword: Optional[str] = None
    ) -> dict[str, Any]:
        term = name or category_or_keyword
        if not supplier_id and not term:
            raise ValueError("Must provide supplier_id or name/keyword.")

        if not self.db_url:
            return {"found": False, "error": "Database not configured."}

        clean_sid = None
        if supplier_id:
            try:
                clean_sid = str(UUID(str(supplier_id).strip()))
            except ValueError:
                pass

        search_pattern = f"%{term.strip()}%" if term else None

        query = """
        SELECT s."Id", s."Name", s."ContactPerson", s."Email", s."Phone", s."Address", s."IsActive"
        FROM "suppliers" s
        WHERE (%s::uuid IS NOT NULL AND s."Id" = %s::uuid)
           OR (%s IS NOT NULL AND s."Name" ILIKE %s)
        LIMIT 1
        """

        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(query, (clean_sid, clean_sid, search_pattern, search_pattern))
                row = cur.fetchone()
                if not row:
                    return {
                        "found": False,
                        "error": f"Supplier '{supplier_id or term}' not found."
                    }

                return {
                    "found": True,
                    "supplier_id": str(row[0]),
                    "name": row[1],
                    "contact_person": row[2] or "Support Desk",
                    "email": row[3],
                    "phone": row[4],
                    "address": row[5],
                    "is_active": row[6]
                }


# --- Tool 6: getProductDetails ---

class GetProductDetailsInput(BaseModel):
    product_id: Optional[str] = Field(default=None, description="Product UUID")
    sku: Optional[str] = Field(default=None, description="Product SKU code")
    name: Optional[str] = Field(default=None, description="Product name or keyword")


class GetProductDetailsTool(ToolBase):
    """
    Retrieves catalog specifications, pricing, and supplier link for gym parts and products.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "getProductDetails"

    @property
    def description(self) -> str:
        return "Retrieves catalog product specifications, retail price, cost price, and assigned supplier details."

    @property
    def args_schema(self):
        return GetProductDetailsInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(
        self,
        product_id: Optional[str] = None,
        sku: Optional[str] = None,
        name: Optional[str] = None
    ) -> dict[str, Any]:
        term = sku or name
        if not product_id and not term:
            raise ValueError("Must provide product_id, sku, or name.")

        if not self.db_url:
            return {"found": False, "error": "Database not configured."}

        clean_pid = None
        if product_id:
            try:
                clean_pid = str(UUID(str(product_id).strip()))
            except ValueError:
                pass

        search_pattern = f"%{term.strip()}%" if term else None

        query = """
        SELECT p."Id", p."SKU", p."Name", p."Description", p."UnitPrice",
               p."CostPrice", p."SupplierId", s."Name" AS "SupplierName", p."IsActive"
        FROM "products" p
        LEFT JOIN "suppliers" s ON p."SupplierId" = s."Id"
        WHERE (%s::uuid IS NOT NULL AND p."Id" = %s::uuid)
           OR (%s IS NOT NULL AND p."SKU" ILIKE %s)
           OR (%s IS NOT NULL AND p."Name" ILIKE %s)
        LIMIT 1
        """

        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(query, (clean_pid, clean_pid, search_pattern, search_pattern, search_pattern, search_pattern))
                row = cur.fetchone()
                if not row:
                    return {
                        "found": False,
                        "error": f"Product '{product_id or term}' not found in catalog."
                    }

                return {
                    "found": True,
                    "product_id": str(row[0]),
                    "sku": row[1],
                    "name": row[2],
                    "description": row[3],
                    "unit_price": float(row[4]) if row[4] is not None else 0.0,
                    "cost_price": float(row[5]) if row[5] is not None else 0.0,
                    "supplier_id": str(row[6]) if row[6] else None,
                    "supplier_name": row[7] or "Unassigned Supplier",
                    "is_active": row[8]
                }
